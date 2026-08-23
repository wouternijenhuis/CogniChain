using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>
/// A simple <see cref="IChatReducer"/> that caps conversation history to the most recent
/// <paramref name="maxMessages"/> non-system messages, always preserving system messages in their
/// original relative position. This is CogniChain's default reducer; for LLM-summarized trimming, see
/// the (experimental) <c>SummarizingChatReducer</c> in <c>Microsoft.Extensions.AI</c> and pass it via
/// <c>ChainBuilder&lt;TIn, TCurrent&gt;.WithHistoryReducer</c> instead.
/// </summary>
public sealed class MessageCountReducer(int maxMessages) : IChatReducer
{
    private readonly int _maxMessages = maxMessages > 0
        ? maxMessages
        : throw new ArgumentOutOfRangeException(nameof(maxMessages), maxMessages, "Must be greater than zero.");

    /// <inheritdoc />
    /// <remarks>
    /// The cut point never lands between a <see cref="ChatRole.Tool"/> result message and the
    /// preceding assistant message that requested it — a provider will typically reject a message list
    /// containing an orphaned tool result, so the window is extended backward as needed to keep such a
    /// pair together.
    /// </remarks>
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        var all = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        var nonSystemIndices = new List<int>();
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Role != ChatRole.System)
            {
                nonSystemIndices.Add(i);
            }
        }

        var dropCount = Math.Max(0, nonSystemIndices.Count - _maxMessages);
        var keepFrom = dropCount;

        while (keepFrom > 0 && keepFrom < nonSystemIndices.Count && all[nonSystemIndices[keepFrom]].Role == ChatRole.Tool)
        {
            keepFrom--;
        }

        var firstKeptIndex = keepFrom < nonSystemIndices.Count ? nonSystemIndices[keepFrom] : all.Count;

        var kept = new List<ChatMessage>(all.Count);
        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Role == ChatRole.System || i >= firstKeptIndex)
            {
                kept.Add(all[i]);
            }
        }

        return Task.FromResult<IEnumerable<ChatMessage>>(kept);
    }
}
