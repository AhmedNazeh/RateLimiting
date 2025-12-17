# Nazeh.RateLimiting - Package Contents

## NuGet Package Information

**Package ID:** `Nazeh.RateLimiting`  
**Version:** `1.0.0`  
**License:** MIT  
**Author:** Ahmed Nazeh  

## Package Structure

The NuGet package (`Nazeh.RateLimiting.1.0.0.nupkg`) contains:

### 1. Compiled Libraries

```
lib/
├── net8.0/
│   └── Nazeh.RateLimiting.dll
├── net9.0/
│   └── Nazeh.RateLimiting.dll
└── net10.0/
    └── Nazeh.RateLimiting.dll
```

### 2. Documentation

```
README.md (included in package)
```

### 3. Dependencies

**Framework References:**
- `Microsoft.AspNetCore.App` (for all target frameworks)

**NuGet Dependencies:**
- None (all dependencies are provided by the framework reference)

## Public API Surface

### Namespace: `Nazeh.RateLimiting`

#### Classes

**1. RateLimitingOptions**
```csharp
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public bool Enabled { get; set; }
    public RateLimitPolicy GlobalLimit { get; set; }
    public RateLimitPolicy ApiLimit { get; set; }
    public RateLimitPolicy AuthenticationLimit { get; set; }
    public SlidingWindowRateLimitPolicy IpLimit { get; set; }
}
```

**2. RateLimitPolicy**
```csharp
public class RateLimitPolicy
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int QueueLimit { get; set; }
}
```

**3. SlidingWindowRateLimitPolicy**
```csharp
public class SlidingWindowRateLimitPolicy : RateLimitPolicy
{
    public int SegmentSize { get; set; }
}
```

**4. RateLimitingExtensions (static)**
```csharp
public static class RateLimitingExtensions
{
    public static IServiceCollection AddEasyRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RateLimitingOptions.SectionName);
}
```

## Usage Example

### Installation

```bash
dotnet add package Nazeh.RateLimiting
```

### Basic Setup

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Register rate limiting services
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

// Enable rate limiting middleware
app.UseRateLimiter();

// Define endpoints
app.MapGet("/api/data", () => "Data")
   .RequireRateLimiting("api");

app.Run();
```

### Configuration (appsettings.json)

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
    },
    "IpLimit": {
      "PermitLimit": 200,
      "Window": "00:01:00",
      "QueueLimit": 10,
      "SegmentSize": 10
    }
  }
}
```

## Built-in Policies

The package automatically configures four rate limiting policies:

| Policy Name | Key | Algorithm | Default Limit |
|-------------|-----|-----------|---------------|
| Global Limiter | (automatic) | Fixed Window | 100 requests/minute |
| API Policy | `"api"` | Fixed Window | 20 requests/minute |
| Authentication Policy | `"authentication"` | Fixed Window | 5 requests/minute |
| IP Policy | `"ip"` | Sliding Window | 200 requests/minute |

### Using Policies

**Minimal API:**
```csharp
app.MapGet("/endpoint", handler)
   .RequireRateLimiting("api");
```

**Controllers:**
```csharp
[EnableRateLimiting("api")]
public class MyController : ControllerBase { }
```

## Response Format

When a rate limit is exceeded, the package returns:

**HTTP Status:** `429 Too Many Requests`

**Headers:**
```
Retry-After: 45
```

**Body (JSON):**
```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45.5
}
```

## Features

### ✅ Proxy-Aware IP Detection

Automatically detects client IP from:
1. `X-Forwarded-For` header (takes first IP)
2. `X-Real-IP` header
3. `RemoteIpAddress` (fallback)

### ✅ Structured Logging

Integrates with `ILogger`:
```
[Warning] Rate limit exceeded for IP 192.168.1.100 on endpoint /api/data. Retry after: 30 seconds
```

### ✅ Configuration Hot-Reload

Uses `IOptions<RateLimitingOptions>` for configuration, supporting hot-reload scenarios.

### ✅ Environment-Specific Settings

Override settings per environment:

**appsettings.Development.json:**
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

**appsettings.Production.json:**
```json
{
  "RateLimiting": {
    "GlobalLimit": {
      "PermitLimit": 50
    }
  }
}
```

## Target Frameworks

- ✅ .NET 8.0 (`net8.0`)
- ✅ .NET 9.0 (`net9.0`)
- ✅ .NET 10.0 (`net10.0`)

## Compatibility

### Minimum Requirements

- ASP.NET Core 8.0 or higher
- C# 12.0 or higher (for implicit usings and nullable reference types)

### Compatible With

- ✅ Minimal APIs
- ✅ Controller-based APIs
- ✅ Blazor Server
- ✅ gRPC services
- ✅ SignalR hubs
- ✅ Any ASP.NET Core application

## Package Metadata

```xml
<PackageId>Nazeh.RateLimiting</PackageId>
<Version>1.0.0</Version>
<Authors>Ahmed Nazeh</Authors>
<Company>Nazeh</Company>
<Title>Easy Rate Limiting for ASP.NET Core</Title>
<Description>
  A simple, configurable wrapper around ASP.NET Core Rate Limiting 
  with support for multiple policies, IP-based limiting, and easy 
  configuration through appsettings.json
</Description>
<PackageTags>ratelimiting;aspnetcore;middleware;throttling;api;ratelimit</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/AhmedNazeh/Nazeh.RateLimiting</PackageProjectUrl>
<RepositoryUrl>https://github.com/AhmedNazeh/Nazeh.RateLimiting</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

## File Sizes

| Framework | DLL Size |
|-----------|----------|
| net8.0 | ~8 KB |
| net9.0 | ~8 KB |
| net10.0 | ~8 KB |

**Total Package Size:** ~25 KB (including README)

## Verification

To verify the package contents:

```bash
# Extract package contents
unzip Nazeh.RateLimiting.1.0.0.nupkg -d extracted

# View structure
tree extracted
```

Or use NuGet Package Explorer:
- Download from: https://github.com/NuGetPackageExplorer/NuGetPackageExplorer
- Open `Nazeh.RateLimiting.1.0.0.nupkg`

## Installation Verification

After installing the package, verify it's working:

```csharp
using Nazeh.RateLimiting; // Should resolve

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEasyRateLimiting(builder.Configuration); // Should compile
```

## Support & Documentation

- 📖 **README:** Included in package
- 💡 **Examples:** https://github.com/AhmedNazeh/Nazeh.RateLimiting/blob/main/EXAMPLES.md
- 🚀 **Quick Start:** https://github.com/AhmedNazeh/Nazeh.RateLimiting/blob/main/QUICKSTART.md
- 🔨 **Build Guide:** https://github.com/AhmedNazeh/Nazeh.RateLimiting/blob/main/BUILD.md

## License

MIT License - See LICENSE file in the repository.

---

**Package Location:**
```
C:\Nuget\RateLimiting\Nazeh.RateLimiting\bin\Debug\Nazeh.RateLimiting.1.0.0.nupkg
```

**Ready for publishing to NuGet.org or private feeds!** ✅

