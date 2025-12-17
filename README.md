# Nazeh.RateLimiting

[![NuGet](https://img.shields.io/nuget/v/Nazeh.RateLimiting.svg)](https://www.nuget.org/packages/Nazeh.RateLimiting/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A simple, configurable wrapper around ASP.NET Core Rate Limiting with support for multiple policies, IP-based limiting, and easy configuration through `appsettings.json`.

## Features

✅ **Easy Configuration** - Configure all rate limiting policies through `appsettings.json`  
✅ **Multiple Policies** - Global, API, Authentication, and IP-based policies  
✅ **Sliding Window Support** - Advanced sliding window algorithm for smoother rate limiting  
✅ **Proxy-Aware** - Automatically detects client IP behind proxies (X-Forwarded-For, X-Real-IP)  
✅ **Structured Logging** - Integrates with `ILogger` for proper logging  
✅ **Modern .NET** - Supports .NET 7.0, 8.0, 9.0, and 10.0  

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Nazeh.RateLimiting
```

Or via Package Manager Console:

```powershell
Install-Package Nazeh.RateLimiting
```

## Quick Start

### 1. Configure `appsettings.json`

Add the `RateLimiting` section to your `appsettings.json`:

```json
{
  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 0
    },
    "ApiLimit": {
      "PermitLimit": 20,
      "Window": "00:01:00",
      "QueueLimit": 0
    },
    "AuthenticationLimit": {
      "PermitLimit": 5,
      "Window": "00:01:00",
      "QueueLimit": 0
    },
    "IpLimit": {
      "PermitLimit": 200,
      "Window": "00:01:00",
      "QueueLimit": 0,
      "SegmentSize": 10
    }
  }
}
```

### 2. Register Services in `Program.cs`

Add rate limiting to your application with just two lines:

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Add rate limiting services
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

// 2. Use rate limiting middleware
app.UseRateLimiter();

app.MapGet("/", () => "Hello World!"); // Protected by global limit

app.Run();
```

### 3. Apply Specific Policies to Endpoints

Use the built-in policies on specific endpoints:

```csharp
// API endpoint with API rate limit
app.MapGet("/api/data", () => new { message = "API Data" })
   .RequireRateLimiting("api");

// Authentication endpoint with stricter limits
app.MapPost("/auth/login", ([FromBody] LoginRequest request) => 
{
    // Login logic
    return Results.Ok(new { token = "..." });
})
.RequireRateLimiting("authentication");

// IP-based sliding window limit
app.MapGet("/public/data", () => "Public data")
   .RequireRateLimiting("ip");
```

## Configuration Options

### RateLimitingOptions

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Enabled` | `bool` | Enable or disable rate limiting globally | `true` |
| `GlobalLimit` | `RateLimitPolicy` | Global rate limit for all requests | 100 req/min |
| `ApiLimit` | `RateLimitPolicy` | Rate limit for API endpoints | 20 req/min |
| `AuthenticationLimit` | `RateLimitPolicy` | Rate limit for auth endpoints | 5 req/min |
| `IpLimit` | `SlidingWindowRateLimitPolicy` | IP-based sliding window limit | 200 req/min |

### RateLimitPolicy

| Property | Type | Description |
|----------|------|-------------|
| `PermitLimit` | `int` | Maximum requests allowed in the time window |
| `Window` | `TimeSpan` | Time window duration (e.g., `"00:01:00"` for 1 minute) |
| `QueueLimit` | `int` | Number of requests that can be queued when limit is reached |

### SlidingWindowRateLimitPolicy

Extends `RateLimitPolicy` with:

| Property | Type | Description |
|----------|------|-------------|
| `SegmentSize` | `int` | Number of segments per window for sliding calculation |

## Advanced Usage

### Custom Configuration Section

Use a different configuration section name:

```csharp
builder.Services.AddEasyRateLimiting(builder.Configuration, "MyCustomRateLimiting");
```

### Environment-Specific Configuration

Override settings in `appsettings.Development.json`:

```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

### Behind a Reverse Proxy

The library automatically detects the client IP from:
- `X-Forwarded-For` header (takes the first IP)
- `X-Real-IP` header
- `RemoteIpAddress` (fallback)

Make sure your reverse proxy (nginx, IIS, etc.) is configured to pass these headers.

## Response Format

When rate limit is exceeded, the API returns a `429 Too Many Requests` response:

```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45.5
}
```

Headers included:
- `Retry-After`: Seconds until the rate limit resets

## Logging

The library logs rate limit events using `ILogger`:

```
[Warning] Rate limit exceeded for IP 192.168.1.100 on endpoint /api/data. Retry after: 30 seconds
```

Works with any logging provider (Serilog, NLog, Console, etc.).

## Examples

### Minimal API Example

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();
app.UseRateLimiter();

app.MapGet("/", () => "Hello!"); // Global limit only

app.MapGet("/api/users", () => new[] { "Alice", "Bob" })
   .RequireRateLimiting("api");

app.MapPost("/login", () => Results.Ok())
   .RequireRateLimiting("authentication");

app.Run();
```

### Controller Example

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("api")]
    public IActionResult GetProducts()
    {
        return Ok(new[] { "Product1", "Product2" });
    }

    [HttpPost]
    [EnableRateLimiting("api")]
    public IActionResult CreateProduct([FromBody] Product product)
    {
        return Created($"/api/products/{product.Id}", product);
    }
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or have questions:
- Open an issue on [GitHub](https://github.com/AhmedNazeh/Nazeh.RateLimiting/issues)
- Check the [documentation](https://github.com/AhmedNazeh/Nazeh.RateLimiting)

## Credits

Built with ❤️ by Ahmed Nazeh using ASP.NET Core Rate Limiting.

