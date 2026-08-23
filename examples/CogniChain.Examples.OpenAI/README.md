# CogniChain + OpenAI

Five runnable examples showing CogniChain's chain builder over the OpenAI SDK.

## Setup

```bash
export OPENAI_API_KEY="your-api-key-here"
export OPENAI_MODEL="gpt-5-mini"   # optional, this is the default
dotnet run
```

## What's included

| Example | Shows |
|---|---|
| Basic Prompt | A single `.Prompt(...)` step |
| Structured Output | `.Prompt<T>(...)` deserializing straight into a record |
| Multi-Turn Conversation | Reusing a `ChainContext` to carry history across calls |
| Tool Calling | `.WithToolsFrom(...)` + automatic function invocation |
| Streaming | `.RunStreamingAsync(...)` printing tokens as they arrive |

## How the client is built

```csharp
IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()   // lets WithTools()/WithToolsFrom() actually get called
    .UseCogniChainRetry()      // CogniChain's transient-failure retry middleware
    .Build();
```

## Learn more

- [CogniChain documentation](../../docs/)
- [OpenAI .NET SDK](https://github.com/openai/openai-dotnet)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
