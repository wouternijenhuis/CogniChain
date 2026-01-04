namespace CogniChain;

/// <summary>
/// Represents the result of a chain execution.
/// </summary>
public class ChainResult
{
    /// <summary>
    /// Gets or sets the output text from the chain execution.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata from the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a step in a chain of operations.
/// </summary>
public interface IChainStep
{
    /// <summary>
    /// Executes the chain step.
    /// </summary>
    /// <param name="input">The input to the step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the step execution.</returns>
    Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestrates a sequence of chain steps.
/// </summary>
public class Chain
{
    private readonly List<IChainStep> _steps = new();

    /// <summary>
    /// Adds a step to the chain.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <returns>The current chain instance for fluent chaining.</returns>
    public Chain AddStep(IChainStep step)
    {
        _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
        return this;
    }

    /// <summary>
    /// Executes the chain sequentially.
    /// </summary>
    /// <param name="initialInput">The initial input to the chain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final result of the chain execution.</returns>
    public async Task<ChainResult> RunAsync(string initialInput, CancellationToken cancellationToken = default)
    {
        var currentInput = initialInput;
        var metadata = new Dictionary<string, object>();

        foreach (var step in _steps)
        {
            var result = await step.ExecuteAsync(currentInput, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            currentInput = result.Output;

            // Merge metadata
            foreach (var kvp in result.Metadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        return new ChainResult
        {
            Output = currentInput,
            Metadata = metadata,
            Success = true
        };
    }

    /// <summary>
    /// Executes the chain with streaming output.
    /// </summary>
    /// <param name="initialInput">The initial input to the chain.</param>
    /// <param name="onChunk">Callback for each output chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final result of the chain execution.</returns>
    public async Task<ChainResult> RunStreamingAsync(
        string initialInput,
        Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        var currentInput = initialInput;
        var metadata = new Dictionary<string, object>();

        foreach (var step in _steps)
        {
            var result = await step.ExecuteAsync(currentInput, cancellationToken);

            if (!result.Success)
            {
                return result;
            }

            currentInput = result.Output;
            onChunk?.Invoke(result.Output);

            // Merge metadata
            foreach (var kvp in result.Metadata)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        return new ChainResult
        {
            Output = currentInput,
            Metadata = metadata,
            Success = true
        };
    }

    /// <summary>
    /// Creates a new chain builder.
    /// </summary>
    /// <returns>A new <see cref="Chain"/> instance.</returns>
    public static Chain Create() => new();
}
