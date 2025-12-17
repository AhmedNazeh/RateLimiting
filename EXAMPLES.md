# Nazeh.RateLimiting - Usage Examples

This document provides comprehensive examples for using the Nazeh.RateLimiting package.

## Table of Contents
- [Basic Setup](#basic-setup)
- [Minimal API Examples](#minimal-api-examples)
- [Controller-Based Examples](#controller-based-examples)
- [Configuration Examples](#configuration-examples)
- [Advanced Scenarios](#advanced-scenarios)

## Basic Setup

### Step 1: Install the Package

```bash
dotnet add package Nazeh.RateLimiting
```

### Step 2: Configure appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Nazeh.RateLimiting": "Warning"
    }
  },
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

### Step 3: Register in Program.cs

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

## Minimal API Examples

### Example 1: Simple API with Different Policies

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();
app.UseRateLimiter();

// Public endpoint - Global limit only
app.MapGet("/", () => "Welcome to the API!");

// API endpoints - API policy
app.MapGet("/api/products", () => new[]
{
    new { Id = 1, Name = "Product 1" },
    new { Id = 2, Name = "Product 2" }
}).RequireRateLimiting("api");

app.MapGet("/api/products/{id}", (int id) => 
    new { Id = id, Name = $"Product {id}" }
).RequireRateLimiting("api");

// Authentication endpoints - Stricter limits
app.MapPost("/auth/login", (LoginRequest request) =>
{
    // Authentication logic here
    return Results.Ok(new { token = "sample-jwt-token" });
}).RequireRateLimiting("authentication");

app.MapPost("/auth/register", (RegisterRequest request) =>
{
    // Registration logic here
    return Results.Created("/api/users/123", new { id = 123 });
}).RequireRateLimiting("authentication");

// IP-based sliding window
app.MapGet("/public/feed", () => new[]
{
    new { Id = 1, Content = "Post 1" },
    new { Id = 2, Content = "Post 2" }
}).RequireRateLimiting("ip");

app.Run();

record LoginRequest(string Username, string Password);
record RegisterRequest(string Email, string Username, string Password);
```

### Example 2: E-Commerce API

```csharp
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();
app.UseRateLimiter();

// Product catalog - API limits
var products = app.MapGroup("/api/products")
    .RequireRateLimiting("api");

products.MapGet("/", () => GetAllProducts());
products.MapGet("/{id}", (int id) => GetProductById(id));
products.MapGet("/search", (string q) => SearchProducts(q));

// Shopping cart - API limits
var cart = app.MapGroup("/api/cart")
    .RequireRateLimiting("api");

cart.MapGet("/", () => GetCart());
cart.MapPost("/items", (CartItem item) => AddToCart(item));
cart.MapDelete("/items/{id}", (int id) => RemoveFromCart(id));

// Checkout - Authentication limits (more restrictive)
var checkout = app.MapGroup("/api/checkout")
    .RequireRateLimiting("authentication");

checkout.MapPost("/", (CheckoutRequest request) => ProcessCheckout(request));
checkout.MapPost("/payment", (PaymentRequest request) => ProcessPayment(request));

// Public endpoints - IP-based sliding window
app.MapGet("/api/public/deals", () => GetDeals())
   .RequireRateLimiting("ip");

app.Run();

// Helper methods (implement as needed)
static object GetAllProducts() => new[] { new { Id = 1, Name = "Product" } };
static object GetProductById(int id) => new { Id = id, Name = "Product" };
static object SearchProducts(string q) => new[] { new { Id = 1, Name = q } };
static object GetCart() => new { Items = Array.Empty<object>() };
static object AddToCart(CartItem item) => Results.Ok();
static object RemoveFromCart(int id) => Results.Ok();
static object ProcessCheckout(CheckoutRequest request) => Results.Ok();
static object ProcessPayment(PaymentRequest request) => Results.Ok();
static object GetDeals() => new[] { new { Id = 1, Deal = "50% off" } };

record CartItem(int ProductId, int Quantity);
record CheckoutRequest(string Address, string PaymentMethod);
record PaymentRequest(decimal Amount, string CardToken);
```

## Controller-Based Examples

### Example 3: Web API with Controllers

```csharp
// Program.cs
using Nazeh.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseRateLimiter();
app.MapControllers();

app.Run();
```

```csharp
// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new[] { "Product1", "Product2" });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(new { Id = id, Name = $"Product {id}" });
    }

    [HttpPost]
    public IActionResult Create([FromBody] Product product)
    {
        return Created($"/api/products/{product.Id}", product);
    }
}

public record Product(int Id, string Name, decimal Price);
```

```csharp
// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("authentication")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        // Authentication logic
        return Ok(new { token = "jwt-token" });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto dto)
    {
        // Registration logic
        return Created("/api/users/123", new { id = 123 });
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        // Password reset logic
        return Ok(new { message = "Reset email sent" });
    }
}

public record LoginDto(string Username, string Password);
public record RegisterDto(string Email, string Username, string Password);
public record ForgotPasswordDto(string Email);
```

## Configuration Examples

### Example 4: Development vs Production Settings

**appsettings.json** (default):
```json
{
  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 10
    }
  }
}
```

**appsettings.Development.json** (disable for development):
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

**appsettings.Production.json** (stricter limits):
```json
{
  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 50,
      "Window": "00:01:00",
      "QueueLimit": 5
    },
    "ApiLimit": {
      "PermitLimit": 10,
      "Window": "00:01:00",
      "QueueLimit": 2
    },
    "AuthenticationLimit": {
      "PermitLimit": 3,
      "Window": "00:05:00",
      "QueueLimit": 0
    }
  }
}
```

### Example 5: Custom Configuration Section

```csharp
// appsettings.json
{
  "MyCustomRateLimits": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 200,
      "Window": "00:02:00",
      "QueueLimit": 20
    }
  }
}

// Program.cs
builder.Services.AddEasyRateLimiting(
    builder.Configuration, 
    "MyCustomRateLimits"
);
```

## Advanced Scenarios

### Example 6: Different Limits for Different Time Windows

```json
{
  "RateLimiting": {
    "Enabled": true,
    "GlobalLimit": {
      "PermitLimit": 1000,
      "Window": "01:00:00",
      "QueueLimit": 50
    },
    "ApiLimit": {
      "PermitLimit": 100,
      "Window": "00:10:00",
      "QueueLimit": 10
    },
    "AuthenticationLimit": {
      "PermitLimit": 3,
      "Window": "00:15:00",
      "QueueLimit": 0
    },
    "IpLimit": {
      "PermitLimit": 500,
      "Window": "00:05:00",
      "QueueLimit": 25,
      "SegmentSize": 5
    }
  }
}
```

### Example 7: Behind Nginx Reverse Proxy

**nginx.conf**:
```nginx
location / {
    proxy_pass http://localhost:5000;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header Host $host;
}
```

The library will automatically detect the real client IP from these headers.

### Example 8: Combining with Authentication

```csharp
using Nazeh.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

builder.Services.AddAuthorization();

// Add rate limiting
builder.Services.AddEasyRateLimiting(builder.Configuration);

var app = builder.Build();

// Order matters: Rate limiting before authentication
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Public endpoint - only rate limited
app.MapGet("/public", () => "Public data")
   .RequireRateLimiting("ip");

// Protected endpoint - rate limited AND authenticated
app.MapGet("/private", () => "Private data")
   .RequireRateLimiting("api")
   .RequireAuthorization();

app.Run();
```

### Example 9: Custom Error Responses

The library returns a standard JSON response when rate limit is exceeded:

```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 45.5
}
```

Client-side handling example (JavaScript):

```javascript
async function callApi() {
  try {
    const response = await fetch('/api/data');
    
    if (response.status === 429) {
      const data = await response.json();
      const retryAfter = response.headers.get('Retry-After');
      console.log(`Rate limited. Retry after ${retryAfter} seconds`);
      
      // Wait and retry
      await new Promise(resolve => setTimeout(resolve, retryAfter * 1000));
      return callApi();
    }
    
    return await response.json();
  } catch (error) {
    console.error('API call failed:', error);
  }
}
```

## Testing Rate Limits

### Example 10: Testing with curl

```bash
# Test global limit
for i in {1..105}; do
  curl http://localhost:5000/
done

# Test API limit
for i in {1..25}; do
  curl http://localhost:5000/api/products
done

# Test authentication limit
for i in {1..10}; do
  curl -X POST http://localhost:5000/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"test"}'
done
```

### Example 11: Testing with PowerShell

```powershell
# Test rate limit
1..105 | ForEach-Object {
    Invoke-RestMethod -Uri "http://localhost:5000/api/products"
}
```

## Summary

The Nazeh.RateLimiting package provides:
- ✅ Simple configuration through `appsettings.json`
- ✅ Multiple built-in policies (global, api, authentication, ip)
- ✅ Easy integration with Minimal APIs and Controllers
- ✅ Automatic client IP detection behind proxies
- ✅ Structured logging and error responses

For more information, see the [README.md](README.md).

