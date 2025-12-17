using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nazeh.RateLimiting;

/// <summary>
/// Extension methods for configuring rate limiting in ASP.NET Core applications.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds configurable rate limiting services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add rate limiting to.</param>
    /// <param name="configuration">The configuration containing rate limiting settings.</param>
    /// <param name="sectionName">The configuration section name (defaults to "RateLimiting").</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEasyRateLimiting(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string sectionName = RateLimitingOptions.SectionName)
    {
        // 1. Configure the options from appsettings
        services.Configure<RateLimitingOptions>(configuration.GetSection(sectionName));

        // 2. Register the Rate Limiter
        services.AddRateLimiter(options =>
        {
            // Resolve services to get config and logger
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
            var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("Nazeh.RateLimiting");

            if (!config.Enabled)
            {
                logger?.LogWarning("Rate limiting is disabled in configuration");
                return;
            }

            logger?.LogInformation("Configuring rate limiting policies");

            // --- Global Limiter ---
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = GetClientIpAddress(context);
                
                logger?.LogDebug("Global rate limiter applied for IP: {ClientIp}", clientIp);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.GlobalLimit.PermitLimit,
                        Window = config.GlobalLimit.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = config.GlobalLimit.QueueLimit
                    });
            });

            // --- Policy: API ---
            options.AddPolicy("api", context =>
            {
                var clientIp = GetClientIpAddress(context);
                logger?.LogDebug("API rate limiter applied for IP: {ClientIp}", clientIp);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = config.ApiLimit.PermitLimit,
                    Window = config.ApiLimit.Window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = config.ApiLimit.QueueLimit
                });
            });

            // --- Policy: Authentication ---
            options.AddPolicy("authentication", context =>
            {
                var clientIp = GetClientIpAddress(context);
                logger?.LogDebug("Authentication rate limiter applied for IP: {ClientIp}", clientIp);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = config.AuthenticationLimit.PermitLimit,
                    Window = config.AuthenticationLimit.Window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = config.AuthenticationLimit.QueueLimit
                });
            });

            // --- Policy: IP Sliding Window ---
            options.AddPolicy("ip", context =>
            {
                var clientIp = GetClientIpAddress(context);
                logger?.LogDebug("IP rate limiter (sliding window) applied for IP: {ClientIp}", clientIp);

                return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = config.IpLimit.PermitLimit,
                    Window = config.IpLimit.Window,
                    SegmentsPerWindow = config.IpLimit.SegmentSize,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = config.IpLimit.QueueLimit
                });
            });

            // --- Rejection Handling ---
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? retry.TotalSeconds
                    : (double?)null;

                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString("F0");
                }

                var clientIp = GetClientIpAddress(context.HttpContext);
                var endpoint = context.HttpContext.Request.Path;
                
                logger?.LogWarning(
                    "Rate limit exceeded for IP {ClientIp} on endpoint {Endpoint}. Retry after: {RetryAfter} seconds",
                    clientIp,
                    endpoint,
                    retryAfter);

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfter
                }, cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Gets the client IP address from the HTTP context, checking proxy headers.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header (common in reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check for X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

