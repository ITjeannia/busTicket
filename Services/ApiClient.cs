using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using BusTicket.Core;

namespace BusTicket.Services;

/// <summary>Serveur injoignable (pas de réseau, serveur arrêté, délai dépassé) : l'application passe en mode hors ligne.</summary>
public sealed class ServerUnreachableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Adresse du serveur BusTicket, modifiable depuis le profil.</summary>
public sealed class ServerSettings
{
    private const string Key = "api_base_url";

    /// <summary>
    /// Adresse inscrite à la compilation (-p:ApiBaseUrl=…), sinon l'ordinateur de développement :
    /// l'émulateur Android le voit à l'adresse 10.0.2.2. Modifiable dans Profil › Adresse du serveur.
    /// </summary>
    public static string DefaultUrl =>
        typeof(ServerSettings).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ApiBaseUrl")?.Value is { Length: > 0 } built
            ? built
            : DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    public string BaseUrl
    {
        get => Preferences.Default.Get(Key, DefaultUrl);
        set => Preferences.Default.Set(Key, Normalize(value));
    }

    public static string Normalize(string url)
    {
        var u = url.Trim().TrimEnd('/');
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            u = "http://" + u;
        return u;
    }
}

/// <summary>Appels HTTP vers l'API (JSON). Les erreurs métier du serveur deviennent des <see cref="BusTicketException"/>.</summary>
public sealed class ApiClient(ServerSettings settings)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    private readonly HttpClient _http = new() { Timeout = Timeout };

    public string? Token { get; set; }

    public Task<T> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path, null);

    public Task<T> PostAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Post, path, body);

    /// <summary>Teste une adresse de serveur (écran Profil).</summary>
    public async Task<bool> PingAsync(string? baseUrl = null)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var response = await _http.GetAsync($"{baseUrl ?? settings.BaseUrl}/api/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body)
    {
        using var request = new HttpRequestMessage(method, $"{settings.BaseUrl}/api/{path}");
        if (Token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType());

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException or UriFormatException)
        {
            throw new ServerUnreachableException("Serveur injoignable", e);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>() ?? throw new ServerUnreachableException("Réponse vide du serveur");

            if ((int)response.StatusCode >= 500)
                throw new ServerUnreachableException($"Erreur serveur {(int)response.StatusCode}");

            var message = "Une erreur est survenue.";
            try { message = (await response.Content.ReadFromJsonAsync<ApiError>())?.Message ?? message; }
            catch (Exception) { /* corps non JSON : message générique */ }

            throw new BusTicketException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ErrorKind.Unauthorized,
                HttpStatusCode.NotFound => ErrorKind.NotFound,
                HttpStatusCode.Conflict => ErrorKind.Conflict,
                _ => ErrorKind.Validation,
            }, message);
        }
    }
}
