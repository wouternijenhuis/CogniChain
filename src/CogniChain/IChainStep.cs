namespace CogniChain;

/// <summary>
/// A single typed step in a <see cref="Chain{TIn, TOut}"/>. Implement this for reusable, testable
/// steps; for a one-off transform, pass a delegate to <c>ChainBuilder&lt;TIn, TCurrent&gt;.Then</c> instead.
/// </summary>
/// <typeparam name="TIn">The step's input type.</typeparam>
/// <typeparam name="TOut">The step's output type.</typeparam>
public interface IChainStep<in TIn, TOut>
{
    /// <summary>Executes the step.</summary>
    /// <param name="input">The input produced by the previous step (or the chain's initial input).</param>
    /// <param name="context">
    /// The shared execution context: chat client, conversation history, chat options, tools,
    /// accumulated usage, and a logger. Steps that call the model read <see cref="ChainContext.ChatClient"/>
    /// and typically append to <see cref="ChainContext.Messages"/>.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask<TOut> ExecuteAsync(TIn input, ChainContext context, CancellationToken cancellationToken = default);
}
