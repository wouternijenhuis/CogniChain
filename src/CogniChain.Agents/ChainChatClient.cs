using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CogniChain.Agents;

/// <summary>
/// Adapts a <see cref="Chain{TIn, TOut}"/> of <c>string</c> to <c>string</c> as an <see cref="IChatClient"/>,
/// so it can be wrapped in a <c>ChatClientAgent</c>. The chain runs once per call, against the latest
/// user message's text; the chain's own <see cref="ChainContext"/> (created fresh per call) is where
/// prompts, tools, and history reduction configured on the chain apply — this adapter does not
/// re-interpret the caller's full message history itself.
/// </summary>
internal sealed class ChainChatClient(Chain<string, string> chain) : IChatClient
{
    private readonly Chain<string, string> _chain = chain;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var input = LastUserText(messages);
        var result = await _chain.RunAsync(input, cancellationToken).ConfigureAwait(false);

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, result.Value))
        {
            Usage = result.Usage,
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var input = LastUserText(messages);

        await foreach (var update in _chain.RunStreamingAsync(input, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (update.Text.Length > 0)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, update.Text);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static string LastUserText(IEnumerable<ChatMessage> messages)
    {
        ChatMessage? last = null;
        foreach (var message in messages)
        {
            if (message.Role == ChatRole.User)
            {
                last = message;
            }
        }

        return last?.Text ?? string.Empty;
    }
}
