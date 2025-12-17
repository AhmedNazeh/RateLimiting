# Nazeh.RateLimiting - Architecture & Design

## Overview

This document describes the architecture and design decisions behind the Nazeh.RateLimiting package.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Application                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP Request
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Rate Limiting Middleware                      │
│                      (UseRateLimiter)                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ Global Limiter  │ ← Applied to ALL requests
                    │ (100 req/min)   │
                    └─────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ Endpoint Policy │ ← Applied based on endpoint
                    │ (if specified)  │
                    └─────────────────┘
                              │
                ┌─────────────┼─────────────┐
                │             │             │
                ▼             ▼             ▼
        ┌───────────┐  ┌──────────┐  ┌─────────┐
        │    API    │  │   Auth   │  │   IP    │
        │ (20/min)  │  │ (5/min)  │  │(200/min)│
        │  Fixed    │  │  Fixed   │  │ Sliding │
        └───────────┘  └──────────┘  └─────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ Limit Exceeded? │
                    └─────────────────┘
                         │        │
                    Yes  │        │ No
                         ▼        ▼
                    ┌────────┐  ┌──────────────┐
                    │ 429    │  │ Continue to  │
                    │Response│  │ Endpoint     │
                    └────────┘  └──────────────┘
```

## Component Design

### 1. Configuration Layer

```
┌─────────────────────────────────────────────────────────┐
│                  appsettings.json                        │
│  {                                                       │
│    "RateLimiting": {                                     │
│      "Enabled": true,                                    │
│      "GlobalLimit": { ... },                             │
│      "ApiLimit": { ... }                                 │
│    }                                                     │
│  }                                                       │
└─────────────────────────────────────────────────────────┘
                          │
                          │ IConfiguration
                          ▼
┌─────────────────────────────────────────────────────────┐
│              RateLimitingOptions                         │
│  - Enabled: bool                                         │
│  - GlobalLimit: RateLimitPolicy                          │
│  - ApiLimit: RateLimitPolicy                             │
│  - AuthenticationLimit: RateLimitPolicy                  │
│  - IpLimit: SlidingWindowRateLimitPolicy                 │
└─────────────────────────────────────────────────────────┘
                          │
                          │ IOptions<T>
                          ▼
┌─────────────────────────────────────────────────────────┐
│         RateLimitingExtensions.AddEasyRateLimiting()     │
│  - Configures rate limiter policies                      │
│  - Sets up rejection handler                             │
│  - Registers with DI container                           │
└─────────────────────────────────────────────────────────┘
```

### 2. Request Flow

```
HTTP Request
    │
    ▼
┌─────────────────────────────────────────┐
│  Extract Client IP                       │
│  1. Check X-Forwarded-For               │
│  2. Check X-Real-IP                     │
│  3. Use RemoteIpAddress                 │
└─────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────┐
│  Apply Global Limiter                    │
│  - Partition by IP                       │
│  - Check permit availability             │
└─────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────┐
│  Apply Endpoint Policy (if any)          │
│  - "api" → API rate limit               │
│  - "authentication" → Auth limit        │
│  - "ip" → IP sliding window             │
└─────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────┐
│  Decision                                │
│  - Permit granted? → Continue           │
│  - Permit denied? → Return 429          │
└─────────────────────────────────────────┘
```

### 3. Rate Limiting Algorithms

#### Fixed Window Algorithm

```
Time:     0s    10s   20s   30s   40s   50s   60s   70s
          │     │     │     │     │     │     │     │
Window 1: [─────────────────────────────────────────]
          │ Requests: 20                             │
          │ Limit: 20 ✓                              │
          └──────────────────────────────────────────┘
Window 2:                                         [─────
                                                  │ Req:
                                                  │ Lim:

Pros: Simple, predictable
Cons: Burst at window boundaries
```

#### Sliding Window Algorithm

```
Time:     0s    10s   20s   30s   40s   50s   60s
          │     │     │     │     │     │     │
Segments: [──┬──┬──┬──┬──┬──┬──┬──┬──┬──]
          │5 │5 │5 │5 │5 │5 │5 │5 │5 │5 │
          └──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘
                    ▲
                    │ Current time
                    │
          [─────────────────────────────]
          │ Active window (60s)          │
          │ Total: 50 requests           │
          └──────────────────────────────┘

Pros: Smoother rate limiting, no boundary bursts
Cons: Slightly more complex
```

## Design Patterns

### 1. Extension Method Pattern

```csharp
public static class RateLimitingExtensions
{
    public static IServiceCollection AddEasyRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Encapsulates complex setup logic
        // Provides clean, fluent API
    }
}
```

**Benefits:**
- Clean API surface
- Follows .NET conventions
- Easy to discover (IntelliSense)
- Chainable with other service registrations

### 2. Options Pattern

```csharp
services.Configure<RateLimitingOptions>(
    configuration.GetSection(RateLimitingOptions.SectionName)
);
```

**Benefits:**
- Type-safe configuration
- Supports hot-reload
- Testable
- Follows .NET Core conventions

### 3. Dependency Injection

```csharp
var config = serviceProvider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("Nazeh.RateLimiting");
```

**Benefits:**
- Loosely coupled
- Testable
- Works with any logging provider
- Framework-agnostic

## Key Design Decisions

### 1. Framework Reference vs Package References

**Decision:** Use `FrameworkReference` to `Microsoft.AspNetCore.App`

**Rationale:**
- Reduces package size
- Avoids version conflicts
- Leverages shared framework
- Standard practice for ASP.NET Core libraries

### 2. Multi-Framework Targeting

**Decision:** Target .NET 8.0, 9.0, and 10.0

**Rationale:**
- Broad compatibility
- Supports LTS versions (8.0)
- Future-proof (9.0, 10.0)
- No breaking changes between versions

### 3. IP Detection Strategy

**Decision:** Check proxy headers before RemoteIpAddress

**Rationale:**
- Most deployments use reverse proxies
- X-Forwarded-For is standard
- Fallback ensures it always works
- Transparent to users

### 4. Logging Strategy

**Decision:** Use `ILogger` from DI, not static logger

**Rationale:**
- Works with any logging provider (Serilog, NLog, etc.)
- Testable
- Follows .NET conventions
- No external dependencies

### 5. Configuration Structure

**Decision:** Nested configuration with strongly-typed models

**Rationale:**
- IntelliSense support
- Compile-time type checking
- Easy to validate
- Clear structure

## Performance Considerations

### 1. Partitioning Strategy

```csharp
RateLimitPartition.GetFixedWindowLimiter(
    partitionKey: clientIp,
    factory: _ => new FixedWindowRateLimiterOptions { ... }
);
```

**Impact:**
- Each IP gets its own rate limit counter
- Memory usage: O(n) where n = unique IPs
- Lookup time: O(1) (dictionary-based)

### 2. Queue Processing

```csharp
QueueProcessingOrder = QueueProcessingOrder.OldestFirst
```

**Impact:**
- Fair processing order
- Prevents request starvation
- Minimal overhead

### 3. Middleware Order

```csharp
app.UseRateLimiter();  // Early in pipeline
app.UseAuthentication();
app.UseAuthorization();
```

**Impact:**
- Rate limiting before expensive operations
- Protects authentication endpoints
- Reduces attack surface

## Extensibility Points

### 1. Custom Configuration Section

```csharp
builder.Services.AddEasyRateLimiting(
    builder.Configuration,
    "MyCustomRateLimiting"
);
```

### 2. Environment-Specific Settings

```json
// appsettings.Development.json
{ "RateLimiting": { "Enabled": false } }

// appsettings.Production.json
{ "RateLimiting": { "GlobalLimit": { "PermitLimit": 50 } } }
```

### 3. Custom Policies (Future Enhancement)

```csharp
// Potential future API
builder.Services.AddEasyRateLimiting(builder.Configuration)
    .AddCustomPolicy("premium", options => { ... });
```

## Security Considerations

### 1. IP Spoofing Protection

**Mitigation:**
- Trust only first IP in X-Forwarded-For
- Validate proxy configuration
- Document proxy setup requirements

### 2. Queue Limits

**Mitigation:**
- `QueueLimit` prevents memory exhaustion
- Set to 0 for strict enforcement
- Configurable per policy

### 3. Denial of Service

**Mitigation:**
- Rate limiting itself is the mitigation
- Global limiter protects entire application
- Endpoint-specific limits for sensitive operations

## Testing Strategy

### Unit Tests (Recommended)

```csharp
[Fact]
public void AddEasyRateLimiting_RegistersServices()
{
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();
    
    services.AddEasyRateLimiting(configuration);
    
    var provider = services.BuildServiceProvider();
    var options = provider.GetService<IOptions<RateLimitingOptions>>();
    
    Assert.NotNull(options);
}
```

### Integration Tests (Recommended)

```csharp
[Fact]
public async Task RateLimit_Exceeded_Returns429()
{
    var client = _factory.CreateClient();
    
    // Make 21 requests (limit is 20)
    for (int i = 0; i < 21; i++)
    {
        var response = await client.GetAsync("/api/products");
        
        if (i < 20)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        else
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
```

## Future Enhancements

### Potential Features

1. **Distributed Rate Limiting**
   - Redis-based shared counters
   - Multi-instance support

2. **Dynamic Configuration**
   - Runtime policy updates
   - Per-user rate limits

3. **Metrics & Monitoring**
   - Prometheus metrics
   - Application Insights integration

4. **Advanced Policies**
   - Token bucket algorithm
   - Leaky bucket algorithm
   - Adaptive rate limiting

5. **Admin API**
   - View current limits
   - Temporarily adjust limits
   - Whitelist/blacklist IPs

## Conclusion

The Nazeh.RateLimiting package is designed to be:

✅ **Simple** - Easy to configure and use  
✅ **Flexible** - Multiple policies, environment-specific settings  
✅ **Performant** - Efficient algorithms, minimal overhead  
✅ **Extensible** - Easy to customize and extend  
✅ **Production-Ready** - Tested, documented, and battle-tested patterns  

---

For implementation details, see the source code in:
- `RateLimitingConfiguration.cs`
- `RateLimitingExtensions.cs`

