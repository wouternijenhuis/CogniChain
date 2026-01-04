namespace CogniChain;

/// <summary>
/// Configuration for retry logic.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retries in milliseconds.
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the maximum delay between retries in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether to add jitter to retry delays.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets the default retry policy.
    /// </summary>
    public static RetryPolicy Default => new();
}

/// <summary>
/// Provides retry logic with exponential backoff.
/// </summary>
public class RetryHandler
{
    private readonly RetryPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryHandler"/> class.
    /// </summary>
    /// <param name="policy">The retry policy to use.</param>
    public RetryHandler(RetryPolicy? policy = null)
    {
        _policy = policy ?? RetryPolicy.Default;
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = _policy.InitialDelayMs;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                attempt++;

                if (attempt >= _policy.MaxRetries)
                {
                    throw new RetryException($"Operation failed after {attempt} attempts", ex);
                }

                // Calculate delay with exponential backoff
                var currentDelay = Math.Min(delay, _policy.MaxDelayMs);

                // Add jitter if enabled (using Random.Shared for thread-safety)
                if (_policy.UseJitter)
                {
                    currentDelay = Random.Shared.Next((int)(currentDelay * 0.5), (int)(currentDelay * 1.5));
                }

                await Task.Delay(currentDelay, cancellationToken);

                delay = (int)(delay * _policy.BackoffMultiplier);
            }
        }
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }
}

/// <summary>
/// Exception thrown when a retry operation fails.
/// </summary>
public class RetryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RetryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
