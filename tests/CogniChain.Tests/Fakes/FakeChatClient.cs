using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CogniChain.Tests.Fakes;

/// <summary>
/// A scripted <see cref="IChatClient"/> for tests: enqueue responses (or exceptions) and they're
/// returned in order, one per call. Records every request's messages and options for assertions.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<Func<ChatResponse>> _responses = new();
    private readonly Queue<Func<IReadOnlyList<ChatResponseUpdate>>> _streamingResponses = new();

    public List<List<ChatMessage>> Requests { get; } = [];

    public List<ChatOptions?> RequestOptions { get; } = [];

    public int CallCount { get; private set; }

    public int StreamingCallCount { get; private set; }

    /// <summary>
    /// Counts how many times a streaming enumerator's cleanup ran (natural completion, explicit
    /// disposal, or exception unwind) — used to assert callers dispose enumerators on every exit path.
    /// </summary>
    public int StreamingDisposeCount { get; private set; }

    public void EnqueueResponse(string text) =>
        _responses.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    public void EnqueueResponse(Func<ChatResponse> factory) => _responses.Enqueue(factory);

    public void EnqueueThrow(Exception exception) => _responses.Enqueue(() => throw exception);

    public void EnqueueStreaming(params string[] tokens) =>
        _streamingResponses.Enqueue(() => tokens.Select(t => new ChatResponseUpdate(ChatRole.Assistant, t)).ToList());

    public void EnqueueStreamingThrow(Exception exception) => _streamingResponses.Enqueue(() => throw exception);

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        Requests.Add([.. messages]);
        RequestOptions.Add(options);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeChatClient: no more scripted responses were enqueued.");
        }

        return Task.FromResult(_responses.Dequeue()());
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StreamingCallCount++;
        Requests.Add([.. messages]);
        RequestOptions.Add(options);

        if (_streamingResponses.Count == 0)
        {
            throw new InvalidOperationException("FakeChatClient: no more scripted streaming responses were enqueued.");
        }

        var factory = _streamingResponses.Dequeue();

        try
        {
            var updates = factory();
            foreach (var update in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return update;
            }
        }
        finally
        {
            StreamingDisposeCount++;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>An exception exposing a public <c>int Status</c> property, mirroring the shape used by the OpenAI and Azure SDKs.</summary>
internal sealed class FakeStatusException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
