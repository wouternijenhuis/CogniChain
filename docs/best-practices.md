# Best Practices

## Prompt design

- Keep placeholder names matching your input object's property names exactly (`{topic}` ↔ `Topic`);
  `Render(object)` reflects public readable properties only.
- Use `{{` / `}}` for literal braces — needed for any prompt that embeds JSON or example output.
- Prefer `.Prompt<T>()` over `.Prompt()` whenever you need to act on the response programmatically.
  Parsing model output yourself is exactly the failure mode structured output exists to remove.

```csharp
// Good — structured, no manual parsing
var chain = Chain.Create(chatClient).Prompt<Sentiment>("Classify: {text}").Build();

// Avoid — fragile string parsing of a free-text response
var chain = Chain.Create(chatClient).Prompt("Classify as positive/negative: {text}").Build();
var isPositive = result.Value.Contains("positive", StringComparison.OrdinalIgnoreCase);
```

## Conversation history

Reuse a `ChainContext` across `RunAsync` calls for multi-turn conversations — a fresh context per call
means the model never sees prior turns:

```csharp
var context = chain.CreateContext();
await chain.RunAsync(new { message = "..." }, context);
await chain.RunAsync(new { message = "..." }, context);   // sees the first turn
```

Unbounded history grows every request's token cost and eventually exceeds the model's context window.
For any chain reused across many turns, set a reducer:

```csharp
var chain = Chain.Create(chatClient)
    .WithHistoryReducer(new MessageCountReducer(maxMessages: 20))
    .Prompt("{message}")
    .Build();
```

`MessageCountReducer` always preserves system messages. For summarization instead of truncation, pass
the (experimental) `SummarizingChatReducer` from `Microsoft.Extensions.AI` instead.

## Tools

- Give every tool method a `[Description]` on itself and its parameters (`System.ComponentModel`) — the
  model chooses whether and how to call a tool based on that schema, not the C# signature.
- `.UseFunctionInvocation()` must be in the `IChatClient` pipeline (`AsBuilder().UseFunctionInvocation().Build()`)
  or `WithTools`/`WithToolsFrom` tools are advertised but never actually invoked.
- Treat tool arguments as untrusted input — the model chose them from user-influenced context. Validate
  before using them in a file path, a SQL query, or a shell command.
- Prefer `WithTools(AIFunctionFactory.Create(...))` for a single method; `WithToolsFrom(instance)` for a
  cohesive group of related tools on one class.

## Resilience

`RetryingChatClient` (via `.UseCogniChainRetry()`) handles transient failures — 408/429/5xx-shaped
exceptions, timeouts, and I/O errors — with capped exponential backoff and full jitter:

```csharp
var chatClient = baseClient.AsBuilder()
    .UseCogniChainRetry(new RetryPolicy { MaxAttempts = 5, MaxDelay = TimeSpan.FromSeconds(15) })
    .Build();
```

It never retries `OperationCanceledException` or a non-transient failure (a 401 or a bad request fails
immediately, not after three slow attempts). For HTTP-level policies beyond the chat client boundary —
connection pooling, circuit breakers — layer `Microsoft.Extensions.Http.Resilience` underneath the
provider's `HttpClient` instead; the two compose without conflict.

## Telemetry

```csharp
var chatClient = baseClient.AsBuilder()
    .UseOpenTelemetry(loggerFactory, sourceName: "MyApp")   // spans around each IChatClient call
    .Build();
```

Chain-level spans (one per step, tagged with chain and step name) come from `ChainActivitySource.Instance`
automatically — no opt-in needed, only an exporter listening on `ChainActivitySource.Name` ("CogniChain").
Read `ChainResult.Usage` after any run for accumulated token counts, or inspect `ChainContext.Usage`
mid-chain in a `.Then` step.

## Testing

Test against a fake `IChatClient` rather than a real endpoint. A minimal one only needs to implement
`GetResponseAsync` and `GetStreamingResponseAsync`:

```csharp
internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<Func<ChatResponse>> _responses = new();
    public void Enqueue(string text) => _responses.Enqueue(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) =>
        Task.FromResult(_responses.Dequeue()());

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
```

CogniChain's own test suite (`tests/CogniChain.Tests/Fakes/FakeChatClient.cs`) is a fuller version —
scripted responses and streaming updates, plus request recording for assertions on what was sent
(system messages, tool lists, conversation history). Copy it rather than re-deriving it.

For structured output, enqueue a response whose text is valid JSON matching your target type —
`Prompt<T>` deserializes it the same way it would a real model's response.

## Production checklist

- [ ] `.UseCogniChainRetry()` (or an equivalent policy) in the chat client pipeline.
- [ ] `.UseFunctionInvocation()` present if any chain uses tools.
- [ ] A history reducer on any chain whose `ChainContext` is reused across many turns.
- [ ] `.UseOpenTelemetry()` plus an exporter listening on `ChainActivitySource.Name`.
- [ ] Structured logging via `ChainContext.Logger` (or your own `ILoggerFactory`) wired to your sink.
- [ ] Tool arguments validated before use in anything security-sensitive.
- [ ] Secrets (API keys) from configuration/secret storage, never hardcoded — see the example projects'
      `*Settings.FromEnvironment()` pattern for the minimum bar.
