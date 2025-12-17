# Nazeh.RateLimiting - Quick Start Guide

Get started with rate limiting in your ASP.NET Core application in just 3 steps!

## Installation

```bash
dotnet add package Nazeh.RateLimiting
```

## Step 1: Configure appsettings.json

Add the `RateLimiting` section to your `appsettings.json`:

```json
{
  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 10
    },
    "ApiLimit": {
      "PermitLimit": 20,
      "Window": "00:01:00",
      "QueueLimit": 5
    },
    "AuthenticationLimit": {
      "PermitLimit": 5,
      "Window": "00:01:00",
      "QueueLimit": 0
    }
  }
}
```

## Step 2: Register in Program.cs

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add rate limiting
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

// Enable rate limiting middleware
app.UseRateLimiter();

app.Run();
```

## Step 3: Apply to Endpoints

```csharp
// Protected by global limit only
app.MapGet("/", () => "Hello World!");

// Protected by API rate limit
app.MapGet("/api/products", () => new[] { "Product1", "Product2" })
   .RequireRateLimiting("api");

// Protected by authentication rate limit
app.MapPost("/auth/login", (LoginRequest req) => Results.Ok())
   .RequireRateLimiting("authentication");
```

## That's It! 🎉

Your API is now protected with rate limiting. When limits are exceeded, clients receive:

```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45.5
}
```

## Available Policies

| Policy | Default Limit | Use Case |
|--------|---------------|----------|
| `global` | 100 req/min | Automatic, applies to all requests |
| `api` | 20 req/min | Standard API endpoints |
| `authentication` | 5 req/min | Login, register, password reset |
| `ip` | 200 req/min | Public endpoints (sliding window) |

## Configuration Options

### Time Windows

```json
"Window": "00:01:00"  // 1 minute
"Window": "00:05:00"  // 5 minutes
"Window": "01:00:00"  // 1 hour
```

### Permit Limits

- `PermitLimit`: Maximum requests allowed in the time window
- `QueueLimit`: Number of requests to queue when limit is reached (0 = reject immediately)

### Sliding Window

For smoother rate limiting, use the IP policy with sliding windows:

```json
"IpLimit": {
  "PermitLimit": 200,
  "Window": "00:01:00",
  "QueueLimit": 10,
  "SegmentSize": 10  // Divides window into 10 segments
}
```

## Testing

Test your rate limits with curl:

```bash
# Test until rate limit is hit
for i in {1..25}; do
  curl http://localhost:5000/api/products
done
```

Or with PowerShell:

```powershell
1..25 | ForEach-Object {
    Invoke-RestMethod -Uri "http://localhost:5000/api/products"
}
```

## Environment-Specific Configuration

Disable rate limiting in development:

**appsettings.Development.json**
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

Stricter limits in production:

**appsettings.Production.json**
```json
{
  "RateLimiting": {
    "GlobalLimit": {
      "PermitLimit": 50,
      "Window": "00:01:00"
    },
    "AuthenticationLimit": {
      "PermitLimit": 3,
      "Window": "00:05:00"
    }
  }
}
```

## Next Steps

- 📖 Read the [full documentation](README.md)
- 💡 See [more examples](EXAMPLES.md)
- 🔨 Learn about [building and publishing](BUILD.md)

## Common Issues

### Rate limiting not working?

1. ✅ Check `app.UseRateLimiter()` is called
2. ✅ Verify `"Enabled": true` in appsettings.json
3. ✅ Ensure middleware order (UseRateLimiter before UseAuthorization)

### Behind a reverse proxy?

The library automatically detects client IP from:
- `X-Forwarded-For` header
- `X-Real-IP` header
- `RemoteIpAddress` (fallback)

Make sure your proxy passes these headers!

## Support

- 🐛 [Report issues](https://github.com/AhmedNazeh/Nazeh.RateLimiting/issues)
- 💬 [Ask questions](https://github.com/AhmedNazeh/Nazeh.RateLimiting/discussions)
- ⭐ [Star on GitHub](https://github.com/AhmedNazeh/Nazeh.RateLimiting)

---

Made with ❤️ by Ahmed Nazeh

