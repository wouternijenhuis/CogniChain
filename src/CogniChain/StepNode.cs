using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>
/// One node in a built <see cref="Chain{TIn, TOut}"/>'s pipeline. Internal — the public, type-safe
/// surface is <see cref="Chain{TIn, TOut}"/> and <see cref="ChainBuilder{TIn, TCurrent}"/>; nodes work
/// in boxed <see cref="object"/> values so an arbitrary sequence of steps with different types can be
/// composed without reflection at execution time.
/// </summary>
internal sealed class StepNode
{
    public required string Name { get; init; }

    public required Func<object?, ChainContext, CancellationToken, ValueTask<object?>> Invoke { get; init; }

    /// <summary>
    /// Set only for a plain-text prompt step (<c>ChainBuilder.Prompt(string)</c>). When the chain's
    /// terminal step has this set, <c>RunStreamingAsync</c> streams real token updates from it instead
    /// of awaiting it as a whole.
    /// </summary>
    public Func<object?, ChainContext, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? Stream { get; init; }
}
