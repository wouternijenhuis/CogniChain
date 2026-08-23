# CogniChain

[![CI](https://github.com/wouternijenhuis/CogniChain/actions/workflows/ci.yml/badge.svg)](https://github.com/wouternijenhuis/CogniChain/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/CogniChain.svg)](https://www.nuget.org/packages/CogniChain)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![DevDad.net](https://img.shields.io/badge/DevDad.net-toolbox-6b5b95)](https://devdad.net)

> Part of the **[DevDad.net](https://devdad.net)** toolbox — *professional engineering, shaped by real life.*

A typed, composable chain layer for [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)'s `IChatClient`. CogniChain sits in the gap between a raw one-shot chat call and a full [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/) workflow: type-safe steps, structured output, tool calling, resilient middleware, and real token streaming — all delegated to the platform, none of it reinvented.

## Why CogniChain?

- **🎯 Typed steps** — `.Prompt<T>()` composes with `.Then<T>()`, `.Map()`, and `.Branch()`; the compiler checks that adjacent steps line up.
- **📦 Structured output** — `.Prompt<T>()` deserializes the model's response straight into your type via JSON schema.
- **🛠️ Real tool calling** — `.WithTools()` / `.WithToolsFrom()` hand `AIFunction`s to the model; it decides whether to call them.
- **💾 Working conversation history** — reuse a `ChainContext` across calls and the system message and prior turns actually reach the model.
- **🔄 Resilient by default** — `RetryingChatClient` classifies transient failures correctly, honors `Retry-After`, and never retries cancellation.
- **📡 Real streaming** — `RunStreamingAsync` yields tokens as they arrive, not once per step.
- **🤖 Agent Framework bridge** — the optional `CogniChain.Agents` package turns a chain into an `AIAgent`, or drops an `AIAgent` into a chain.

## Installation

```bash
dotnet add package CogniChain
dotnet add package CogniChain.Agents   # optional: Microsoft Agent Framework bridge
```

**Requirements:** .NET 10.0+, and any `IChatClient` — OpenAI, Azure OpenAI, or anything else `Microsoft.Extensions.AI` supports.

## Quick start

```csharp
using CogniChain;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-5-mini")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var chain = Chain.Create(chatClient)
    .WithSystemMessage("You are a helpful coding assistant.")
    .Prompt("Explain {concept} in one sentence for a C# developer.")
    .Build();

var result = await chain.RunAsync(new { concept = "dependency injection" });
Console.WriteLine(result.Value);
```

### Structured output

```csharp
public sealed record MovieSuggestion(string Title, int Year, string Reason);

var chain = Chain.Create(chatClient)
    .Prompt<MovieSuggestion>("Suggest one movie about {theme}.")
    .Build();

var result = await chain.RunAsync(new { theme = "time travel" });
Console.WriteLine($"{result.Value.Title} ({result.Value.Year})");
```

### Multi-step pipelines and tools

```csharp
var chain = Chain.Create(chatClient)
    .WithTools(AIFunctionFactory.Create(GetWeather))
    .Prompt<Outline>("Outline an article about {topic}.")
    .Then<Article>(async (outline, context, ct) =>
    {
        var response = await context.ChatClient.GetResponseAsync(
            $"Write it: {string.Join(", ", outline.Sections)}", context.Options, ct);
        return new Article(outline.Sections[0], response.Text);
    })
    .Build();
```

See [`docs/getting-started.md`](docs/getting-started.md) for a full walkthrough, and
[`examples/`](examples/) for runnable OpenAI, Azure OpenAI, and Agent Framework projects.

## Core concepts

| Concept | What it does |
|---|---|
| `Chain<TIn, TOut>` / `ChainBuilder` | Composes typed steps; `.Build()` produces an immutable, reusable pipeline. |
| `PromptTemplate` | `{placeholder}` rendering with `{{ }}` escaping, so JSON-bearing prompts work. |
| `ChainContext` | Per-run state: chat client, message history, options, usage, logger. Reuse it across calls for multi-turn conversations. |
| `MessageCountReducer` | Bounds conversation history, always preserving system messages. |
| `RetryingChatClient` | `IChatClient` middleware: capped exponential backoff with full jitter, correct transient-failure classification. |
| `ChainActivitySource` | Per-step OpenTelemetry spans, alongside `UseOpenTelemetry()` on the chat client itself. |

## Relationship to the rest of the .NET AI stack

CogniChain doesn't compete with `Microsoft.Extensions.AI` or the Microsoft Agent Framework — it's a thin layer on top of the first and interoperable with the second:

- **`Microsoft.Extensions.AI`** is the provider abstraction (`IChatClient`, tools, middleware). CogniChain composes chains *out of* `IChatClient`; it never implements a provider itself.
- **Microsoft Agent Framework** (`Microsoft.Agents.AI`) is the successor to Semantic Kernel's and AutoGen's agent abstractions, for multi-agent orchestration. `CogniChain.Agents` bridges the two: `chain.AsAIAgent()` and `agent.AsChainStep()`.
- **Semantic Kernel** is in maintenance mode. If you're on it today, CogniChain slots in the same place Agent Framework does — as a typed layer above `IChatClient` — and doesn't require migrating off SK first.

Upgrading from CogniChain 0.x? See [`docs/migration.md`](docs/migration.md).

## Documentation

- 📘 [Getting Started](docs/getting-started.md)
- 📗 [API Reference](docs/api-reference.md)
- 📙 [Best Practices](docs/best-practices.md)
- 🏗️ [Architecture](docs/architecture.md)
- 🔀 [Migration from 0.x](docs/migration.md)
- 💡 [Examples](examples/) — [OpenAI](examples/CogniChain.Examples.OpenAI), [Azure OpenAI](examples/CogniChain.Examples.Azure), [Agent Framework](examples/CogniChain.Examples.AgentFramework)

## Community & support

- 💬 [GitHub Discussions](https://github.com/wouternijenhuis/CogniChain/discussions)
- 🐛 [Issue Tracker](https://github.com/wouternijenhuis/CogniChain/issues)
- 🤝 [Contributing](CONTRIBUTING.md) · 🔒 [Security](SECURITY.md) · 📖 [Changelog](CHANGELOG.md)

## License

MIT License — see [LICENSE](LICENSE).

---

**Built with ❤️ for the .NET community**, by [DevDad.net](https://devdad.net) | [Star on GitHub](https://github.com/wouternijenhuis/CogniChain) ⭐
