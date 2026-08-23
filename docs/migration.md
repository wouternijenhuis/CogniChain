# Migrating from CogniChain 0.2.x to 0.3.0

CogniChain 0.3.0 is a rebuild on `Microsoft.Extensions.AI`'s `IChatClient`, replacing the 0.2.x
string-in/string-out API entirely. There is no compatibility shim — this project is still pre-1.0, and
the old API's headline features (conversation memory, tool calling, streaming) didn't actually work
end-to-end, so a clean break was cheaper than papering over them. See the
[README](../README.md#why-cognichain) for why.

## Type mapping

| 0.2.x | 0.3.0 | Notes |
|---|---|---|
| `IChainStep` (`string → ChainResult`) | `IChainStep<TIn, TOut>` | Typed, and receives a `ChainContext`. |
| `Chain` / `Chain.Create()` / `.AddStep()` | `Chain<TIn, TOut>` / `Chain.Create(chatClient)` / `.Then(...)` | Requires an `IChatClient`; steps are typed. |
| `ChainResult` (`Output`, `Success`, `ErrorMessage`, `Metadata`) | `ChainResult<T>` (`Value`, `Usage`, `Items`) | Failures throw `ChainStepException` instead of a `Success = false` flag. |
| `LLMOrchestrator`, `OrchestratorConfig` | `Chain<TIn, TOut>` + `ChainContext` | The orchestrator's `Memory`/`Tools` were never read during execution in 0.x — `ChainContext` actually is. |
| `ConversationMemory` | `ChainContext.Messages` + `IChatReducer` | Reuse a `ChainContext` across `RunAsync` calls for multi-turn history. |
| `ITool` / `ToolBase` / `ToolRegistry` | `AIFunction` via `AIFunctionFactory.Create` / `.WithToolsFrom(...)` | The model now actually selects tools, via `.UseFunctionInvocation()`. |
| `RetryHandler` / `RetryPolicy` | `CogniChain.Middleware.RetryingChatClient` / `RetryPolicy` | `IChatClient` middleware, not a wrapper you call manually. |
| `StreamingHandler` / `StreamingResponse` | `Chain<TIn, TOut>.RunStreamingAsync` | Real per-token streaming, not a callback fired once per step. |
| `PromptTemplate.Format(...)` | `PromptTemplate.Render(...)` | Same idea; fixed escaping (`{{`/`}}`) and re-entrant substitution. |

## Before / after

**0.2.x:**

```csharp
var orchestrator = new LLMOrchestrator(new OrchestratorConfig { RetryPolicy = new RetryPolicy { MaxRetries = 3 } });
orchestrator.Memory.AddSystemMessage("You are a helpful coding assistant.");

var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Help me with: {task}"))
    .WithVariables(new Dictionary<string, string> { ["task"] = "writing a C# async method" })
    .AddStep(new YourLLMCallStep());

var result = await workflow.ExecuteAsync();
Console.WriteLine(result.Output);
```

**0.3.0:**

```csharp
IChatClient chatClient = /* your provider's IChatClient, see docs/getting-started.md */;

var chain = Chain.Create(chatClient)
    .WithSystemMessage("You are a helpful coding assistant.")
    .Prompt("Help me with: {task}")
    .Build();

var result = await chain.RunAsync(new { task = "writing a C# async method" });
Console.WriteLine(result.Value);
```

The 0.3.0 version calls the model itself — there's no `YourLLMCallStep` to write, and the system
message actually reaches it.

## Tool calling

**0.2.x** (the model never actually chose to call this — you called it by name yourself):

```csharp
orchestrator.Tools.RegisterTool(new WeatherTool());
var result = await orchestrator.Tools.ExecuteToolAsync("get_weather", "Seattle");
```

**0.3.0** (the model decides, based on the conversation, whether to call it):

```csharp
var chain = Chain.Create(chatClient)
    .WithTools(AIFunctionFactory.Create(GetWeather))
    .Prompt("{question}")
    .Build();

string GetWeather(string city) => $"22°C and sunny in {city}.";
```

Requires `.UseFunctionInvocation()` in the chat client pipeline — see
[`docs/getting-started.md`](getting-started.md#1-get-an-ichatclient).

## What has no direct replacement

- **`ChainResult.Metadata`** → use `ChainContext.Items` (read/write during the run) or
  `ChainResult.Items` (read-only snapshot after).
- **`RetryException`** → removed. `RetryingChatClient` lets the final attempt's exception propagate
  unwrapped, preserving its original type.
- **Semantic Kernel example** → replaced by
  [`examples/CogniChain.Examples.AgentFramework`](../examples/CogniChain.Examples.AgentFramework), using
  the optional `CogniChain.Agents` package. SK itself still works underneath any `IChatClient` you build
  from it — CogniChain has no opinion on how you got your `IChatClient`.

## Getting help

Open a [discussion](https://github.com/wouternijenhuis/CogniChain/discussions) if your 0.x usage doesn't
map cleanly onto the above — most gaps are intentional (removing dead functionality), but tell us if we
missed something real.
