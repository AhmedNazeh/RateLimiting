namespace Nazeh.RateLimiting;

/// <summary>
/// Configuration options for rate limiting policies.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// The default configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Enables or disables rate limiting globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Global rate limit applied to all requests.
    /// </summary>
    public RateLimitPolicy GlobalLimit { get; set; } = new() 
    { 
        PermitLimit = 100, 
        Window = TimeSpan.FromMinutes(1), 
        QueueLimit = 0 
    };

    /// <summary>
    /// Rate limit for API endpoints.
    /// </summary>
    public RateLimitPolicy ApiLimit { get; set; } = new() 
    { 
        PermitLimit = 20, 
        Window = TimeSpan.FromMinutes(1), 
        QueueLimit = 0 
    };

    /// <summary>
    /// Rate limit for authentication endpoints (login, register, etc.).
    /// </summary>
    public RateLimitPolicy AuthenticationLimit { get; set; } = new() 
    { 
        PermitLimit = 5, 
        Window = TimeSpan.FromMinutes(1), 
        QueueLimit = 0 
    };

    /// <summary>
    /// IP-based rate limit using a sliding window algorithm.
    /// </summary>
    public SlidingWindowRateLimitPolicy IpLimit { get; set; } = new() 
    { 
        PermitLimit = 200, 
        Window = TimeSpan.FromMinutes(1), 
        QueueLimit = 0,
        SegmentSize = 10 
    };
}

/// <summary>
/// Configuration for a fixed-window rate limit policy.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// The time window for the rate limit.
    /// </summary>
    public TimeSpan Window { get; set; }

    /// <summary>
    /// Number of requests that can be queued when the limit is reached.
    /// </summary>
    public int QueueLimit { get; set; }
}

/// <summary>
/// Configuration for a sliding-window rate limit policy.
/// </summary>
public class SlidingWindowRateLimitPolicy : RateLimitPolicy
{
    /// <summary>
    /// Number of segments per window for sliding window calculation.
    /// </summary>
    public int SegmentSize { get; set; }
}

