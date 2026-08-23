# API Reference

Namespace `CogniChain` unless noted. This is a map of the surface, not exhaustive XML-doc — every
public member has its own `///` doc comment in source; IntelliSense has the details.

## Quick reference — "what do I want to do?"

| I want to... | Use |
|---|---|
| Start a chain | `Chain.Create(chatClient)` |
| Render a template and get text back | `.Prompt(template)` |
| Render a template and get a typed object back | `.Prompt<T>(template)` |
| Add a custom step | `.Then<T>(delegate)` or `.Then<T>(IChainStep<,>)` |
| Run a step per element of a collection | `.Map<TElement, TOut>(...)` |
| Branch on a condition | `.Branch(predicate, whenTrue, whenFalse)` |
| Give the model tools | `.WithTools(...)` / `.WithToolsFrom(instance)` |
| Set a system prompt | `.WithSystemMessage(...)` |
| Configure temperature, etc. | `.WithOptions(o => ...)` |
| Bound conversation history | `.WithHistoryReducer(...)` |
| Finish building | `.Build(name?)` |
| Run once | `chain.RunAsync(input)` |
| Run a multi-turn conversation | `chain.RunAsync(input, context)` with a reused `ChainContext` |
| Stream tokens | `chain.RunStreamingAsync(input)` |
| Retry transient failures | `.UseCogniChainRetry()` on a `ChatClientBuilder` (namespace `CogniChain.Middleware`) |
| Expose a chain as a tool | `chain.AsAIFunction()` |
| Bridge to Microsoft Agent Framework | `chain.AsAIAgent(...)` / `agent.AsChainStep()` (package `CogniChain.Agents`) |

## `Chain` (static)

```csharp
public static ChainBuilder<TIn, TIn> Create<TIn>(IChatClient chatClient, ILoggerFactory? loggerFactory = null);
public static ChainBuilder<object, object> Create(IChatClient chatClient, ILoggerFactory? loggerFactory = null);
```

Entry point. The untyped overload is the common case — the first step you add fixes the chain's real
input type.

## `ChainBuilder<TIn, TCurrent>`

Immutable fluent builder; every method returns a new builder. `TCurrent` is the output type of the
last step added, and becomes the input type of the next.

| Method | Produces | Notes |
|---|---|---|
| `Prompt<TOut>(string template, Action<ChatOptions>? configure = null, string? name = null)` | `ChainBuilder<TIn, TOut>` | Structured output via JSON schema. Not streamable. |
| `Prompt(string template, ...)` | `ChainBuilder<TIn, string>` | Plain text. Streamable when it's the terminal step. |
| `Then<TOut>(Func<TCurrent, ChainContext, CancellationToken, ValueTask<TOut>> step, string? name = null)` | `ChainBuilder<TIn, TOut>` | Arbitrary async transform. |
| `Then<TOut>(Func<TCurrent, TOut> step, string? name = null)` | `ChainBuilder<TIn, TOut>` | Synchronous convenience overload. |
| `Then<TOut>(IChainStep<TCurrent, TOut> step)` | `ChainBuilder<TIn, TOut>` | Reusable, testable step type. |
| `Map<TElement, TOut>(Func<TElement, ChainContext, CancellationToken, ValueTask<TOut>> step, int maxConcurrency = 4, string? name = null)` | `ChainBuilder<TIn, IReadOnlyList<TOut>>` | Fan-out; the current value must be `IEnumerable<TElement>` at run time. |
| `Branch<TOut>(Func<TCurrent, bool> predicate, Chain<TCurrent, TOut> whenTrue, Chain<TCurrent, TOut> whenFalse, string? name = null)` | `ChainBuilder<TIn, TOut>` | Both branches are pre-built chains sharing the parent's `ChainContext`. |
| `WithTools(params AITool[] tools)` | same builder | Adds to `ChatOptions.Tools` for every prompt step from here on. |
| `WithToolsFrom(object instance)` | same builder | Reflects `instance`'s public methods into `AIFunction`s. |
| `WithOptions(Action<ChatOptions> configure)` | same builder | Composes with any prior `WithOptions` call. |
| `WithSystemMessage(string content)` | same builder | Seeded into every fresh `ChainContext` this chain creates. |
| `WithHistoryReducer(IChatReducer reducer)` | same builder | Default: no reduction (unbounded history). |
| `Build(string? name = null)` | `Chain<TIn, TCurrent>` | |

## `Chain<TIn, TOut>`

Implements `IChainStep<TIn, TOut>`, so a built chain can be nested inside another (`Branch`) or bridged
to an agent framework.

| Member | Signature |
|---|---|
| `Name` | `string` |
| `CreateContext(IServiceProvider? services = null)` | `ChainContext` |
| `RunAsync(TIn input, CancellationToken ct = default)` | `Task<ChainResult<TOut>>` — fresh context |
| `RunAsync(TIn input, ChainContext context, CancellationToken ct = default)` | `Task<ChainResult<TOut>>` — reused context, carries history |
| `RunStreamingAsync(TIn input, CancellationToken ct = default)` | `IAsyncEnumerable<ChainUpdate>` |
| `RunStreamingAsync(TIn input, ChainContext context, CancellationToken ct = default)` | `IAsyncEnumerable<ChainUpdate>` |

## `ChainContext`

Per-run state, created by `Chain.CreateContext()` and threaded through every step.

| Member | Type | Notes |
|---|---|---|
| `ChatClient` | `IChatClient` | |
| `Messages` | `IList<ChatMessage>` | Reused across `RunAsync` calls to build multi-turn conversations. |
| `Options` | `ChatOptions` | Includes any tools/config from `WithTools`/`WithOptions`. |
| `Services` | `IServiceProvider` | Whatever was passed to `CreateContext`, or an empty provider. |
| `Items` | `IDictionary<string, object?>` | Free-form bag for passing state between steps; surfaced on `ChainResult.Items`. |
| `Usage` | `UsageDetails` | Accumulated across every model call in the run. |
| `Logger` | `ILogger` | |
| `Reducer` | `IChatReducer?` | From `WithHistoryReducer`, or `null`. |
| `ReduceHistoryAsync(CancellationToken ct = default)` | `ValueTask` | Applies `Reducer` to `Messages`; called automatically before every model call inside a prompt step. |

## `ChainResult<T>` / `ChainUpdate`

```csharp
public sealed record ChainResult<T>(T Value)
{
    public UsageDetails? Usage { get; init; }
    public IReadOnlyDictionary<string, object?> Items { get; init; }
}

public sealed class ChainUpdate
{
    public required string StepName { get; init; }
    public required int StepIndex { get; init; }
    public bool IsStepComplete { get; init; }
    public ChatResponseUpdate? ChatUpdate { get; init; }
    public string Text { get; }   // ChatUpdate?.Text ?? ""
}
```

## `IChainStep<TIn, TOut>`

```csharp
public interface IChainStep<in TIn, TOut>
{
    ValueTask<TOut> ExecuteAsync(TIn input, ChainContext context, CancellationToken cancellationToken = default);
}
```

Implement this for a reusable, unit-testable step; use a `.Then` delegate for a one-off transform.

## `PromptTemplate`

```csharp
public PromptTemplate(string template);
public static PromptTemplate FromString(string template);

public IReadOnlyList<string> Variables { get; }

public string Render(IReadOnlyDictionary<string, string?> values);
public string Render(object values);   // reflects public readable properties, cached per type
public ChatMessage RenderMessage(ChatRole role, object values);
public ChatMessage RenderMessage(ChatRole role, IReadOnlyDictionary<string, string?> values);
```

`{{` and `}}` render as literal `{` and `}`, so a template can contain JSON. A substituted value is
never re-scanned for further placeholders. Parsing happens once, at construction — a malformed
template (an unmatched `{`) throws immediately rather than on first render.

## `ChainStepException`

Thrown when any step fails (except `OperationCanceledException`, which always propagates unwrapped).

```csharp
public sealed class ChainStepException : Exception
{
    public string StepName { get; }
    public int StepIndex { get; }
    // InnerException is the original exception the step threw.
}
```

## `MessageCountReducer`

```csharp
public sealed class MessageCountReducer(int maxMessages) : IChatReducer
```

Keeps the most recent `maxMessages` non-system messages; system messages are always preserved. The
default reducer when you opt into history reduction. For LLM-summarized trimming instead, use the
(experimental) `SummarizingChatReducer` from `Microsoft.Extensions.AI` via `WithHistoryReducer`.

## `CogniChain.Middleware`

| Type | Purpose |
|---|---|
| `RetryPolicy` | `MaxAttempts`, `InitialDelay`, `BackoffMultiplier`, `MaxDelay`, `UseJitter`, `IsTransient`, `RetryAfterSelector`. `RetryPolicy.Default`: 3 attempts, 1s → 30s capped backoff, full jitter. |
| `RetryingChatClient : DelegatingChatClient` | Retries `RetryPolicy.IsTransient`-classified failures; never retries `OperationCanceledException`; for streaming, only the first update is retried. |
| `RetryingChatClientBuilderExtensions.UseCogniChainRetry(...)` | `ChatClientBuilder` extension to add the above to a pipeline. |

## `CogniChain.DependencyInjection`

```csharp
services.AddChain<TIn, TOut>("summarize", b => b.Prompt<TOut>("..."));
// resolve with: serviceProvider.GetRequiredKeyedService<Chain<TIn, TOut>>("summarize")
```

Requires an `IChatClient` already registered (via `Microsoft.Extensions.AI`'s own `AddChatClient(...)`).

## `CogniChain.Diagnostics`

```csharp
public static class ChainActivitySource
{
    public const string Name = "CogniChain";
    public static readonly ActivitySource Instance;
}
```

One `Activity` per chain step, with `cognichain.chain.name`, `cognichain.step.name`, and
`cognichain.step.index` tags. Point your OTel exporter at `ChainActivitySource.Name` alongside
`.UseOpenTelemetry()` on the chat client for full request + step tracing.

## `CogniChain.Agents` (optional package)

```csharp
public static AIAgent AsAIAgent(this Chain<string, string> chain, string name, string? description = null);
public static IChainStep<string, string> AsChainStep(this AIAgent agent);
```

Bridges to the Microsoft Agent Framework. See
[`examples/CogniChain.Examples.AgentFramework`](../examples/CogniChain.Examples.AgentFramework).

## `ChainAIFunctionExtensions` (core package)

```csharp
public static AIFunction AsAIFunction<TIn, TOut>(this Chain<TIn, TOut> chain, string? name = null, string? description = null);
```

Wraps a chain as a tool another model can call — no `CogniChain.Agents` dependency needed.
