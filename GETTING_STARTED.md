# Getting Started with Nazeh.RateLimiting

Welcome! This guide will help you get started with the Nazeh.RateLimiting package.

## What is Nazeh.RateLimiting?

Nazeh.RateLimiting is a simple, easy-to-use NuGet package that adds rate limiting to your ASP.NET Core applications. It protects your APIs from abuse, ensures fair resource usage, and prevents DDoS attacks.

## Why Use This Package?

✅ **Easy Configuration** - All settings in `appsettings.json`  
✅ **One-Line Setup** - Just call `AddEasyRateLimiting()`  
✅ **Multiple Policies** - Global, API, Authentication, and IP-based  
✅ **Production-Ready** - Tested, documented, and battle-tested  
✅ **Zero Dependencies** - Only requires ASP.NET Core  

## Installation

```bash
dotnet add package Nazeh.RateLimiting
```

## 3-Step Setup

### Step 1: Add Configuration

Add this to your `appsettings.json`:

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

### Step 2: Register Services

Add this to your `Program.cs`:

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

### Step 3: Apply to Endpoints

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

Your API is now protected with rate limiting!

## What Happens When Limit is Exceeded?

When a client exceeds the rate limit, they receive:

**HTTP Status:** `429 Too Many Requests`

**Response Body:**
```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45.5
}
```

**Headers:**
```
Retry-After: 45
```

## Available Policies

| Policy | Default Limit | Use Case |
|--------|---------------|----------|
| `global` | 100 req/min | Automatic, applies to all requests |
| `api` | 20 req/min | Standard API endpoints |
| `authentication` | 5 req/min | Login, register, password reset |
| `ip` | 200 req/min | Public endpoints (sliding window) |

## Testing Your Setup

### Option 1: Run the Sample App

```bash
cd Nazeh.RateLimiting/SampleApp
dotnet run
```

Then visit `http://localhost:5000` in your browser.

### Option 2: Test with curl

```bash
# Test until rate limit is hit
for i in {1..25}; do
  curl http://localhost:5000/api/products
done
```

### Option 3: Test with PowerShell

```powershell
1..25 | ForEach-Object {
    Invoke-RestMethod -Uri "http://localhost:5000/api/products"
}
```

## Common Scenarios

### Scenario 1: Disable in Development

**appsettings.Development.json:**
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

### Scenario 2: Stricter Limits in Production

**appsettings.Production.json:**
```json
{
  "RateLimiting": {
    "GlobalLimit": {
      "PermitLimit": 50
    },
    "AuthenticationLimit": {
      "PermitLimit": 3,
      "Window": "00:05:00"
    }
  }
}
```

### Scenario 3: Different Time Windows

```json
{
  "RateLimiting": {
    "GlobalLimit": {
      "Window": "01:00:00"  // 1 hour
    },
    "ApiLimit": {
      "Window": "00:05:00"  // 5 minutes
    },
    "AuthenticationLimit": {
      "Window": "00:15:00"  // 15 minutes
    }
  }
}
```

## Troubleshooting

### Rate limiting not working?

1. ✅ Check `app.UseRateLimiter()` is called in `Program.cs`
2. ✅ Verify `"Enabled": true` in `appsettings.json`
3. ✅ Ensure middleware order (UseRateLimiter before UseAuthorization)

### Behind a reverse proxy?

The library automatically detects client IP from:
- `X-Forwarded-For` header
- `X-Real-IP` header
- `RemoteIpAddress` (fallback)

Make sure your proxy (nginx, IIS, etc.) passes these headers!

### Need more examples?

Check out **[EXAMPLES.md](EXAMPLES.md)** for 11 comprehensive examples covering:
- Minimal APIs
- Controllers
- E-commerce scenarios
- Authentication
- And more!

## Next Steps

### Learn More

- 📖 [Full Documentation](README.md) - Complete reference
- 💡 [Examples](EXAMPLES.md) - 11 comprehensive examples
- 🏗️ [Architecture](ARCHITECTURE.md) - How it works internally
- 📦 [Package Contents](PACKAGE_CONTENTS.md) - What's included

### Customize

- Adjust limits in `appsettings.json`
- Use different policies for different endpoints
- Configure environment-specific settings

### Advanced

- Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the design
- Check [BUILD.md](BUILD.md) to build from source
- Review [PACKAGE_CONTENTS.md](PACKAGE_CONTENTS.md) for API reference

## Documentation Index

| Document | Purpose |
|----------|---------|
| **[INDEX.md](INDEX.md)** | Complete documentation index |
| **[QUICKSTART.md](QUICKSTART.md)** | 3-step quick start (this file) |
| **[README.md](README.md)** | Full documentation |
| **[EXAMPLES.md](EXAMPLES.md)** | Code examples |
| **[BUILD.md](BUILD.md)** | Build and publish guide |
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | Architecture and design |
| **[PACKAGE_CONTENTS.md](PACKAGE_CONTENTS.md)** | Package reference |
| **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** | Project overview |

## Support

- 🐛 [Report issues](https://github.com/AhmedNazeh/Nazeh.RateLimiting/issues)
- 💬 [Ask questions](https://github.com/AhmedNazeh/Nazeh.RateLimiting/discussions)
- ⭐ [Star on GitHub](https://github.com/AhmedNazeh/Nazeh.RateLimiting)

## License

MIT License - Free to use in commercial and personal projects.

---

**Ready to protect your API?** Start with the 3-step setup above! 🚀

---

Made with ❤️ by Ahmed Nazeh

