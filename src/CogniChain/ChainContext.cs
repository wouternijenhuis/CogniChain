using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CogniChain;

/// <summary>
/// The execution context shared across every step of a single <c>RunAsync</c>/<c>RunStreamingAsync</c>
/// call. Reuse the same instance across multiple calls (via the <c>RunAsync(input, context, ct)</c>
/// overload) to carry conversation history forward — this is CogniChain's replacement for the old,
/// non-functional <c>ConversationMemory</c>.
/// </summary>
public sealed class ChainContext
{
    /// <summary>Gets the chat client steps use to talk to the model.</summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    /// Gets the running conversation history. Prompt steps append the rendered user message before
    /// calling the model and the model's reply afterward, so system messages and prior turns are
    /// always part of the request.
    /// </summary>
    public IList<ChatMessage> Messages { get; }

    /// <summary>Gets the chat options (tools, temperature, etc.) used for model calls in this run.</summary>
    public ChatOptions Options { get; }

    /// <summary>Gets the service provider supplied when the context was created, or an empty one.</summary>
    public IServiceProvider Services { get; }

    /// <summary>Gets a bag for passing arbitrary state between steps, surfaced on <see cref="ChainResult{T}.Items"/>.</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>Gets the token usage accumulated across every model call made so far in this run.</summary>
    public UsageDetails Usage { get; } = new();

    /// <summary>Gets the logger for this chain's execution.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the reducer applied to <see cref="Messages"/> before each model call, or <see langword="null"/> for no trimming.</summary>
    public IChatReducer? Reducer { get; }

    internal ChainContext(IChatClient chatClient, ChatOptions options, IServiceProvider? services, ILogger? logger, IChatReducer? reducer)
    {
        ChatClient = chatClient;
        Options = options;
        Services = services ?? EmptyServiceProvider.Instance;
        Logger = logger ?? NullLogger.Instance;
        Reducer = reducer;
        Messages = new List<ChatMessage>();
    }

    /// <summary>Applies <see cref="Reducer"/> to <see cref="Messages"/>, if one is configured.</summary>
    public async ValueTask ReduceHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (Reducer is null)
        {
            return;
        }

        var reduced = await Reducer.ReduceAsync(Messages, cancellationToken).ConfigureAwait(false);

        Messages.Clear();
        foreach (var message in reduced)
        {
            Messages.Add(message);
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }
}
