using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetInsights.Middleware
{
    /// <summary>
    /// Middleware for implementing rate limiting functionality.
    /// Limits the number of requests from a client within a specified time window.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, ClientRateLimitCounter> _clientCounters;

        public RateLimitMiddleware(RequestDelegate next, RateLimitOptions options)
        {
            _next = next;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clientCounters = new ConcurrentDictionary<string, ClientRateLimitCounter>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var now = DateTime.UtcNow;

            // Get or create a counter for this client
            var counter = _clientCounters.AddOrUpdate(
                clientId,
                new ClientRateLimitCounter { FirstRequestTime = now, RequestCount = 1 },
                (key, existingCounter) =>
                {
                    var timeElapsed = now - existingCounter.FirstRequestTime;

                    // Reset counter if time window has passed
                    if (timeElapsed.TotalSeconds > _options.TimeWindowSeconds)
                    {
                        return new ClientRateLimitCounter { FirstRequestTime = now, RequestCount = 1 };
                    }

                    existingCounter.RequestCount++;
                    return existingCounter;
                });

            var timeWindow = now - counter.FirstRequestTime;

            // Check if rate limit exceeded
            if (counter.RequestCount > _options.MaxRequestsPerWindow && 
                timeWindow.TotalSeconds <= _options.TimeWindowSeconds)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Add("Retry-After", _options.RetryAfterSeconds.ToString());
                context.Response.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerWindow.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", "0");
                context.Response.Headers.Add("X-RateLimit-Reset", 
                    Math.Ceiling((_options.TimeWindowSeconds - timeWindow.TotalSeconds)).ToString());

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"Maximum {_options.MaxRequestsPerWindow} requests allowed per {_options.TimeWindowSeconds} seconds",
                    retryAfter = _options.RetryAfterSeconds
                });
                return;
            }

            // Add rate limit headers to response
            var remainingRequests = Math.Max(0, _options.MaxRequestsPerWindow - counter.RequestCount);
            var resetTime = Math.Ceiling((_options.TimeWindowSeconds - timeWindow.TotalSeconds));

            context.Response.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerWindow.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", remainingRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", resetTime.ToString());

            // Cleanup old entries periodically
            if (_clientCounters.Count > 10000)
            {
                var oldEntries = _clientCounters
                    .Where(x => (now - x.Value.FirstRequestTime).TotalSeconds > _options.TimeWindowSeconds)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var entry in oldEntries)
                {
                    _clientCounters.TryRemove(entry, out _);
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Gets a unique identifier for the client.
        /// Attempts to use X-Forwarded-For header first, then falls back to RemoteIpAddress.
        /// </summary>
        private string GetClientIdentifier(HttpContext context)
        {
            // Check for X-Forwarded-For header (proxy/load balancer)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ips = forwardedFor.ToString().Split(',');
                if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
                {
                    return ips[0].Trim();
                }
            }

            // Fall back to RemoteIpAddress
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    /// <summary>
    /// Configuration options for rate limiting.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Maximum number of requests allowed within the time window.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 100;

        /// <summary>
        /// Time window in seconds.
        /// </summary>
        public double TimeWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Number of seconds to suggest client wait before retrying (in Retry-After header).
        /// </summary>
        public int RetryAfterSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Counter for tracking client requests.
    /// </summary>
    internal class ClientRateLimitCounter
    {
        public DateTime FirstRequestTime { get; set; }
        public int RequestCount { get; set; }
    }

    /// <summary>
    /// Extension methods for rate limiting middleware.
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        /// <summary>
        /// Adds rate limiting middleware to the application pipeline.
        /// </summary>
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder, RateLimitOptions options = null)
        {
            options ??= new RateLimitOptions();
            return builder.UseMiddleware<RateLimitMiddleware>(options);
        }
    }
}
