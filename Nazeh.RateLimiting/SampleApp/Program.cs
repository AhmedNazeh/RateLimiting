using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add rate limiting services
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

// Enable rate limiting middleware
app.UseRateLimiter();

// Public endpoint - protected by global limit only
app.MapGet("/", () => new
{
    message = "Welcome to the Rate Limiting Demo API!",
    endpoints = new[]
    {
        "/api/products - Protected by API rate limit (20 req/min)",
        "/api/users - Protected by API rate limit (20 req/min)",
        "/auth/login - Protected by Authentication rate limit (5 req/min)",
        "/public/feed - Protected by IP-based sliding window (200 req/min)"
    }
});

// API endpoints - API rate limit policy
app.MapGet("/api/products", () => new[]
{
    new { Id = 1, Name = "Laptop", Price = 999.99 },
    new { Id = 2, Name = "Mouse", Price = 29.99 },
    new { Id = 3, Name = "Keyboard", Price = 79.99 }
}).RequireRateLimiting("api");

app.MapGet("/api/users", () => new[]
{
    new { Id = 1, Username = "john_doe" },
    new { Id = 2, Username = "jane_smith" }
}).RequireRateLimiting("api");

// Authentication endpoint - stricter rate limit
app.MapPost("/auth/login", (LoginRequest request) =>
{
    // Simulate authentication
    if (request.Username == "admin" && request.Password == "password")
    {
        return Results.Ok(new { token = "sample-jwt-token-12345", username = request.Username });
    }
    return Results.Unauthorized();
}).RequireRateLimiting("authentication");

// Public endpoint - IP-based sliding window
app.MapGet("/public/feed", () => new[]
{
    new { Id = 1, Title = "Post 1", Content = "This is the first post" },
    new { Id = 2, Title = "Post 2", Content = "This is the second post" },
    new { Id = 3, Title = "Post 3", Content = "This is the third post" }
}).RequireRateLimiting("ip");

app.Run();

record LoginRequest(string Username, string Password);
