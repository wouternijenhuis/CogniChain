using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>The result of a successful <c>Chain{TIn, TOut}.RunAsync</c> call.</summary>
/// <typeparam name="T">The chain's output type.</typeparam>
/// <param name="Value">The chain's output value.</param>
public sealed record ChainResult<T>(T Value)
{
    /// <summary>Gets the token usage accumulated across every model call made during the run.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>Gets the contents of <see cref="ChainContext.Items"/> at the end of the run.</summary>
    public IReadOnlyDictionary<string, object?> Items { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// One update emitted by <c>Chain{TIn, TOut}.RunStreamingAsync</c>: either a token-level
/// <see cref="ChatResponseUpdate"/> from the chain's terminal step (when it is a plain-text prompt
/// step), or a lifecycle notification for a step that completed as a whole (structured-output,
/// delegate, map, and branch steps do not support token streaming).
/// </summary>
public sealed class ChainUpdate
{
    /// <summary>Gets the name of the step this update belongs to.</summary>
    public required string StepName { get; init; }

    /// <summary>Gets the zero-based index of the step this update belongs to.</summary>
    public required int StepIndex { get; init; }

    /// <summary>Gets a value indicating whether this update marks the step's completion.</summary>
    public bool IsStepComplete { get; init; }

    /// <summary>Gets the underlying token-level update, or <see langword="null"/> for a lifecycle-only update.</summary>
    public ChatResponseUpdate? ChatUpdate { get; init; }

    /// <summary>Gets the text delta of <see cref="ChatUpdate"/>, or an empty string if there is none.</summary>
    public string Text => ChatUpdate?.Text ?? string.Empty;
}
