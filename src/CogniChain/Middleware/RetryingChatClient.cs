using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CogniChain.Middleware;

/// <summary>
/// An <see cref="IChatClient"/> middleware that retries transient failures with capped exponential
/// backoff and full jitter. Unlike a naive <c>catch (Exception)</c> retry loop, this:
/// <list type="bullet">
/// <item>never retries <see cref="OperationCanceledException"/> — cancellation always propagates immediately;</item>
/// <item>only retries exceptions <see cref="RetryPolicy.IsTransient"/> classifies as transient (by
/// default: 408/429/5xx-shaped exceptions, <see cref="TimeoutException"/>, <see cref="IOException"/>) —
/// a 401 or a bad-request error fails on the first attempt;</item>
/// <item>honors a server-specified retry delay when <see cref="RetryPolicy.RetryAfterSelector"/> finds one;</item>
/// <item>always keeps the computed delay at or under <see cref="RetryPolicy.MaxDelay"/>, jitter included;</item>
/// <item>logs every retry attempt; and</item>
/// <item>lets the final attempt's exception propagate unwrapped, preserving its original type and stack trace.</item>
/// </list>
/// For streaming, only obtaining the <em>first</em> update is retried — once a stream has started
/// yielding tokens, a failure propagates directly, since replaying a partially consumed stream risks
/// duplicated output.
/// </summary>
public sealed class RetryingChatClient : DelegatingChatClient
{
    private readonly RetryPolicy _policy;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RetryingChatClient"/> class.</summary>
    public RetryingChatClient(IChatClient innerClient, RetryPolicy? policy = null, ILogger<RetryingChatClient>? logger = null)
        : base(innerClient)
    {
        _policy = policy ?? RetryPolicy.Default;
        _logger = logger ?? (ILogger)NullLogger<RetryingChatClient>.Instance;

        if (_policy.MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), _policy.MaxAttempts, "RetryPolicy.MaxAttempts must be at least 1.");
        }
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _policy.MaxAttempts && _policy.IsTransient(ex))
            {
                var delay = ComputeDelay(ex, attempt);
                _logger.LogWarning(ex, "CogniChain retry {Attempt}/{MaxAttempts} in {DelayMs}ms: {Message}", attempt, _policy.MaxAttempts, delay.TotalMilliseconds, ex.Message);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        var hasFirst = false;
        var connected = false;

        for (var attempt = 1; !connected; attempt++)
        {
            enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
            try
            {
                hasFirst = await enumerator.MoveNextAsync().ConfigureAwait(false);
                connected = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _policy.MaxAttempts && _policy.IsTransient(ex))
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
                var delay = ComputeDelay(ex, attempt);
                _logger.LogWarning(ex, "CogniChain streaming retry {Attempt}/{MaxAttempts} in {DelayMs}ms: {Message}", attempt, _policy.MaxAttempts, delay.TotalMilliseconds, ex.Message);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        try
        {
            if (hasFirst)
            {
                yield return enumerator!.Current;
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    yield return enumerator.Current;
                }
            }
        }
        finally
        {
            await enumerator!.DisposeAsync().ConfigureAwait(false);
        }
    }

    private TimeSpan ComputeDelay(Exception exception, int attempt)
    {
        var retryAfter = _policy.RetryAfterSelector(exception);
        if (retryAfter is { } explicitDelay && explicitDelay > TimeSpan.Zero)
        {
            return explicitDelay > _policy.MaxDelay ? _policy.MaxDelay : explicitDelay;
        }

        var raw = _policy.InitialDelay.TotalMilliseconds * Math.Pow(_policy.BackoffMultiplier, attempt - 1);
        var capped = Math.Min(raw, _policy.MaxDelay.TotalMilliseconds);
        var final = _policy.UseJitter ? Random.Shared.NextDouble() * capped : capped;
        return TimeSpan.FromMilliseconds(final);
    }
}
