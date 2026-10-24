using BusTicket.Api;
using BusTicket.Core;
using Microsoft.EntityFrameworkCore;

// API BusTicket : comptes, trajets, réservation + paiement (simulé), billets PDF, contrôle à l'embarquement.
// Lancement : dotnet run --project Server   →   http://0.0.0.0:5080 (joignable depuis un téléphone du même réseau)

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Database"] ?? "busticket.db");
builder.Services.AddDbContext<BusTicketDb>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IDataStore, SqliteDataStore>();
builder.Services.AddScoped(sp => new BookingEngine(sp.GetRequiredService<IDataStore>()));
builder.Services.AddSingleton(new SessionTokens(
    builder.Configuration["TokenSecret"] ?? throw new InvalidOperationException("TokenSecret manquant dans appsettings.json"),
    TimeSpan.FromDays(30)));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BusTicketDb>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<BookingEngine>().EnsureDemoAccount();
}

app.UseCors();
app.UseMiddleware<ErrorMiddleware>();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.Now }));

// ----- Comptes -----

api.MapPost("/auth/register", (RegisterRequest request, BookingEngine engine, SessionTokens tokens) =>
{
    var user = engine.Register(request);
    return Results.Ok(new AuthResponse(tokens.Issue(user.Id), user));
});

api.MapPost("/auth/login", (LoginRequest request, BookingEngine engine, SessionTokens tokens) =>
{
    var user = engine.Login(request);
    return Results.Ok(new AuthResponse(tokens.Issue(user.Id), user));
});

var secured = api.MapGroup("").AddEndpointFilter<AuthFilter>();

secured.MapGet("/me", (HttpContext http, BookingEngine engine) =>
    engine.GetUser(http.UserId()) is { } user ? Results.Ok(user) : Results.Unauthorized());

// ----- Trajets -----

api.MapGet("/cities", (BookingEngine engine) => Results.Ok(engine.Cities));

api.MapGet("/trips", (string from, string to, DateTime date, BookingEngine engine) =>
    Results.Ok(engine.Search(from, to, date)));

api.MapGet("/trips/{id}", (string id, BookingEngine engine) => Results.Ok(engine.GetTrip(id)));

// ----- Billets -----

secured.MapPost("/bookings", (CreateBookingRequest request, HttpContext http, BookingEngine engine) =>
{
    var booking = engine.Book(http.UserId(), request);
    return Results.Created($"/api/bookings/{booking.Reference}", booking);
});

secured.MapGet("/bookings", (HttpContext http, BookingEngine engine) => Results.Ok(engine.BookingsOf(http.UserId())));

secured.MapGet("/bookings/{reference}", (string reference, HttpContext http, BookingEngine engine) =>
    Results.Ok(engine.GetBooking(http.UserId(), reference)));

secured.MapGet("/bookings/{reference}/pdf", (string reference, HttpContext http, BookingEngine engine) =>
{
    var booking = engine.GetBooking(http.UserId(), reference);
    return Results.File(TicketPdf.Create(booking), "application/pdf", TicketPdf.FileName(booking));
});

// ----- Contrôle à l'embarquement -----

secured.MapPost("/tickets/verify", (VerifyRequest request, BookingEngine engine) =>
    Results.Ok(engine.Verify(request.Payload, request.Board)));

app.Run();
