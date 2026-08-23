# Getting Started

## Install

```bash
dotnet add package CogniChain
dotnet add package Microsoft.Extensions.AI.OpenAI   # or Azure.AI.OpenAI, or any IChatClient provider
```

## 1. Get an `IChatClient`

CogniChain doesn't provide one — it builds on whatever `Microsoft.Extensions.AI` gives you:

```csharp
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chatClient = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .GetChatClient("gpt-5-mini")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()   // required for WithTools()/WithToolsFrom() to actually be called
    .Build();
```

For Azure OpenAI, see [`examples/CogniChain.Examples.Azure`](../examples/CogniChain.Examples.Azure).

## 2. Build a chain

```csharp
using CogniChain;

var chain = Chain.Create(chatClient)
    .WithSystemMessage("You are a helpful coding assistant.")
    .Prompt("Explain {concept} in one sentence for a C# developer.")
    .Build();

var result = await chain.RunAsync(new { concept = "dependency injection" });
Console.WriteLine(result.Value);   // a string
```

`Chain.Create(chatClient)` starts an untyped builder; the first step you add fixes the chain's input
type. `.Prompt(template)` renders the template against whatever object you pass to `RunAsync` — an
anonymous object works, so does a dictionary, so does a record from a previous step.

## 3. Ask for structured output instead of text

```csharp
public sealed record Sentiment(string Label, double Confidence);

var chain = Chain.Create(chatClient)
    .Prompt<Sentiment>("Classify the sentiment of: {review}")
    .Build();

var result = await chain.RunAsync(new { review = "Best purchase ever!" });
Console.WriteLine(result.Value.Label);   // typed access, no JSON parsing
```

`Prompt<T>` asks the model for a JSON-schema-constrained response and deserializes it for you.

## 4. Chain multiple steps

```csharp
var chain = Chain.Create(chatClient)
    .Prompt<Outline>("Outline a short article about {topic}.")
    .Then<Article>(async (outline, context, ct) =>
    {
        var response = await context.ChatClient.GetResponseAsync(
            $"Write it, covering: {string.Join(", ", outline.Sections)}", context.Options, ct);
        return new Article(outline.Sections[0], response.Text);
    })
    .Build();
```

`.Then<T>` takes a delegate, or an `IChainStep<TIn, TOut>` for reusable, testable steps. Each call
changes the builder's generic type, so the compiler enforces that step N's output matches step N+1's
input.

## 5. Give the model tools

```csharp
var chain = Chain.Create(chatClient)
    .WithTools(AIFunctionFactory.Create(GetWeather))
    .Prompt("{question}")
    .Build();

string GetWeather(string city) => $"22°C and sunny in {city}.";
```

Tools only get invoked if the underlying `IChatClient` pipeline has `.UseFunctionInvocation()` — see
step 1.

## 6. Multi-turn conversations

Reuse a `ChainContext` across calls instead of letting `RunAsync` create a fresh one each time:

```csharp
var context = chain.CreateContext();

var first = await chain.RunAsync(new { message = "What is a record type?" }, context);
var second = await chain.RunAsync(new { message = "How is that different from a class?" }, context);
// `second` sees the full prior turn, because context.Messages carried it forward.
```

## 7. Stream the response

```csharp
await foreach (var update in chain.RunStreamingAsync(new { concept = "async/await" }))
{
    if (!update.IsStepComplete)
    {
        Console.Write(update.Text);
    }
}
```

Only a plain-text terminal `.Prompt(template)` step streams token-by-token; a structured-output,
`.Then`, `.Map`, or `.Branch` terminal step yields a single completion update instead.

## Next steps

- [API Reference](api-reference.md) — the full type-by-type surface.
- [Best Practices](best-practices.md) — history reduction, retries, telemetry, testing.
- [Architecture](architecture.md) — how CogniChain layers on `Microsoft.Extensions.AI`.
- Runnable projects in [`examples/`](../examples/) for OpenAI, Azure OpenAI, and the Microsoft Agent Framework bridge.
