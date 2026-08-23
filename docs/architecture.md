# Architecture

## Where CogniChain sits

```
┌─────────────────────────────────────────────────────────────┐
│  Your application                                             │
├─────────────────────────────────────────────────────────────┤
│  CogniChain              — typed chain composition            │
│  CogniChain.Agents        — bridge to Microsoft Agent Framework│
├─────────────────────────────────────────────────────────────┤
│  Microsoft.Agents.AI      — multi-agent orchestration          │
│  Microsoft.Extensions.AI  — IChatClient, AITool, middleware    │
├─────────────────────────────────────────────────────────────┤
│  Provider SDK              — OpenAI / Azure.AI.OpenAI / other  │
└─────────────────────────────────────────────────────────────┘
```

CogniChain owns exactly one job: **composing typed steps over an `IChatClient`**. Everything below that
line — the provider connection, structured output, tool-call schema generation, retry/cache/telemetry
middleware, function invocation — is `Microsoft.Extensions.AI`'s. CogniChain doesn't reimplement any of
it; `Chain<TIn, TOut>` and `ChainBuilder<TIn, TCurrent>` are the only things it adds. `CogniChain.Agents`
is a separate, optional package for the layer above, when a chain needs to participate in Agent
Framework orchestration.

## Request flow

A `.Prompt<T>()` or `.Prompt()` step, at run time:

1. Renders its `PromptTemplate` against the current value (an anonymous object, dictionary, or a
   previous step's typed output) and appends the result to `ChainContext.Messages` as a user message.
2. Calls `ChainContext.Reducer?.ReduceAsync(Messages)`, if a reducer was configured, to bound history.
3. Calls `ChainContext.ChatClient.GetResponseAsync(...)` (or the structured-output overload for
   `Prompt<T>`), passing `ChainContext.Options` — which carries any tools from `WithTools`/`WithToolsFrom`.
4. Appends the response to `Messages` and accumulates its `UsageDetails` into `ChainContext.Usage`.
5. Returns the response's text (or deserialized value) as this step's output, feeding the next step.

`.Then`, `.Map`, and `.Branch` steps skip 1–4 entirely — they're plain code that may or may not touch
`ChainContext.ChatClient` itself.

## Step composition

Internally, `ChainBuilder<TIn, TCurrent>` accumulates an ordered list of `StepNode`s — each an
`object?`-typed invoke delegate, plus (only for a streamable plain-text prompt step) a second
`IAsyncEnumerable<ChatResponseUpdate>`-returning delegate. The generic `TIn`/`TCurrent` type parameters
exist purely at the builder's public surface to give you compile-time step-adjacency checking;
execution itself works in boxed `object?` so a chain of arbitrarily different step types composes
without per-step reflection. `Chain<TIn, TOut>.RunAsync` walks that list, catching and wrapping each
step's failure in a `ChainStepException` (except `OperationCanceledException`, which always propagates
unwrapped) so a multi-step chain's errors are diagnosable without instrumenting every step yourself.

`RunStreamingAsync` walks the same list non-streaming for every step except the last; if the last step
is a plain-text `.Prompt()`, its token stream is surfaced directly as `ChainUpdate`s instead of being
awaited as a whole.

## Design choices

- **Immutable builder.** Every `ChainBuilder` method returns a new instance. A partially-configured
  builder (e.g. after `WithSystemMessage`) can be safely branched into two different chains.
- **`ChainContext` is the extension point**, not a fatter `Chain` API. Steps that need the chat client,
  tools, or accumulated usage read it from context rather than through constructor injection — the same
  context threads through nested chains (`Branch`) and reused conversations.
- **No dependency beyond `Microsoft.Extensions.AI`.** Provider SDKs, `Microsoft.Agents.AI`, and MCP are
  all example- or `CogniChain.Agents`-only dependencies; the core package stays provider-agnostic.
- **Reflection is opt-in and explicit.** `PromptTemplate.Render(object)` and `WithToolsFrom` are the
  only reflection-based APIs, and both are marked `[RequiresUnreferencedCode]` for trimmed/AOT
  consumers; every other API is fully static.

## Error handling

Model calls throw whatever the underlying `IChatClient` throws (typically an SDK-specific exception
carrying an HTTP status). `RetryingChatClient` classifies and retries transient ones; anything that
reaches a chain step unhandled is wrapped in `ChainStepException` with the failing step's name and
index, except cancellation, which is never wrapped.

## Extending CogniChain

- **A new kind of step** — implement `IChainStep<TIn, TOut>` and add it with `.Then(step)`.
- **A new kind of middleware** — subclass `DelegatingChatClient` (as `RetryingChatClient` does) and add
  a `ChatClientBuilder` extension method, the same way `Microsoft.Extensions.AI`'s own middleware works.
- **A different history strategy** — implement `IChatReducer` (or use the framework's
  `SummarizingChatReducer`) and pass it to `WithHistoryReducer`.
