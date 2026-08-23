using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>
/// A simple <see cref="IChatReducer"/> that caps conversation history to the most recent
/// <paramref name="maxMessages"/> non-system messages, always preserving system messages. This is
/// CogniChain's default reducer; for LLM-summarized trimming, see the (experimental)
/// <c>SummarizingChatReducer</c> in <c>Microsoft.Extensions.AI</c> and pass it via
/// <c>ChainBuilder&lt;TIn, TCurrent&gt;.WithHistoryReducer</c> instead.
/// </summary>
public sealed class MessageCountReducer(int maxMessages) : IChatReducer
{
    private readonly int _maxMessages = maxMessages > 0
        ? maxMessages
        : throw new ArgumentOutOfRangeException(nameof(maxMessages), maxMessages, "Must be greater than zero.");

    /// <inheritdoc />
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        var all = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        var system = all.Where(m => m.Role == ChatRole.System);
        var rest = all.Where(m => m.Role != ChatRole.System).ToList();

        var kept = rest.Count > _maxMessages ? rest.Skip(rest.Count - _maxMessages) : rest;

        return Task.FromResult<IEnumerable<ChatMessage>>(system.Concat(kept).ToList());
    }
}
