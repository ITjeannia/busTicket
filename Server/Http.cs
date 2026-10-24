using BusTicket.Core;

namespace BusTicket.Api;

/// <summary>Vérifie le jeton « Authorization: Bearer … » et mémorise l'utilisateur pour la requête.</summary>
public sealed class AuthFilter(SessionTokens tokens) : IEndpointFilter
{
    public const string UserIdKey = "userId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
        var userId = tokens.Validate(token);
        if (userId is null)
            return Results.Json(new ApiError("Session expirée. Reconnectez-vous."), statusCode: StatusCodes.Status401Unauthorized);

        context.HttpContext.Items[UserIdKey] = userId;
        return await next(context);
    }
}

public static class HttpContextExtensions
{
    public static string UserId(this HttpContext http) => (string)http.Items[AuthFilter.UserIdKey]!;
}

/// <summary>Transforme les erreurs métier en réponses JSON { message } avec le bon code HTTP.</summary>
public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext http)
    {
        try
        {
            await next(http);
        }
        catch (BusTicketException e)
        {
            await Write(http, e.Kind switch
            {
                ErrorKind.Validation => StatusCodes.Status400BadRequest,
                ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict,
            }, e.Message);
        }
        catch (BadHttpRequestException e)
        {
            await Write(http, StatusCodes.Status400BadRequest, "Requête invalide : " + e.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erreur inattendue sur {Path}", http.Request.Path);
            await Write(http, StatusCodes.Status500InternalServerError, "Une erreur est survenue sur le serveur. Réessayez.");
        }
    }

    private static Task Write(HttpContext http, int status, string message)
    {
        if (http.Response.HasStarted) return Task.CompletedTask;
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(new ApiError(message));
    }
}
