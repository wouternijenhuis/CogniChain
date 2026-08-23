using System.Net;
using System.Reflection;

namespace CogniChain.Middleware;

/// <summary>Configures <see cref="RetryingChatClient"/>.</summary>
public sealed class RetryPolicy
{
    private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
    [
        HttpStatusCode.RequestTimeout, // 408
        HttpStatusCode.TooManyRequests, // 429
        HttpStatusCode.InternalServerError, // 500
        HttpStatusCode.BadGateway, // 502
        HttpStatusCode.ServiceUnavailable, // 503
        HttpStatusCode.GatewayTimeout, // 504
    ];

    /// <summary>Gets the default policy: 3 attempts, 1s initial delay, 2x backoff, capped at 30s, full jitter.</summary>
    public static RetryPolicy Default { get; } = new();

    /// <summary>Gets or sets the maximum number of attempts, including the first. Must be at least 1.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Gets or sets the delay before the first retry.</summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the multiplier applied to the delay after each retry.</summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>Gets or sets the maximum delay between attempts, before jitter.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether "full jitter" is applied: the actual delay is chosen uniformly at random
    /// between zero and the computed (capped) backoff delay, which never exceeds <see cref="MaxDelay"/>.
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Gets or sets the predicate used to decide whether an exception is worth retrying.
    /// <see cref="OperationCanceledException"/> is never retried regardless of this predicate.
    /// The default classifies HTTP-shaped exceptions (an <see cref="HttpRequestException"/>, or any
    /// exception exposing a public <c>int Status</c> property — the shape used by the OpenAI and Azure
    /// SDKs' exception types) as transient when their status is 408, 429, or 5xx, and also retries
    /// <see cref="TimeoutException"/> and <see cref="IOException"/>.
    /// </summary>
    public Func<Exception, bool> IsTransient { get; init; } = DefaultIsTransient;

    /// <summary>
    /// Gets or sets a best-effort selector for a server-specified retry delay (e.g. from a
    /// <c>Retry-After</c> header). The default looks for a public <c>TimeSpan</c>-typed
    /// <c>RetryAfter</c> property on the exception via reflection; returns <see langword="null"/> when
    /// none is found, in which case the computed backoff delay is used instead.
    /// </summary>
    public Func<Exception, TimeSpan?> RetryAfterSelector { get; init; } = DefaultRetryAfterSelector;

    private static bool DefaultIsTransient(Exception exception)
    {
        switch (exception)
        {
            case HttpRequestException http:
                return http.StatusCode is null || TransientStatusCodes.Contains(http.StatusCode.Value);
            case TimeoutException or IOException:
                return true;
        }

        var statusProperty = exception.GetType().GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
        if (statusProperty?.PropertyType == typeof(int) && statusProperty.GetValue(exception) is int status)
        {
            return TransientStatusCodes.Contains((HttpStatusCode)status);
        }

        return false;
    }

    private static TimeSpan? DefaultRetryAfterSelector(Exception exception)
    {
        var property = exception.GetType().GetProperty("RetryAfter", BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(exception);
        return value switch
        {
            TimeSpan span => span,
            _ => null,
        };
    }
}
