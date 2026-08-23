# CogniChain + Azure OpenAI

Five runnable examples showing CogniChain's chain builder over Azure OpenAI.

## Setup

**API key:**

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key-here"
export AZURE_OPENAI_DEPLOYMENT="your-deployment-name"
```

**Or Azure Identity (recommended for production):**

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_DEPLOYMENT="your-deployment-name"
az login   # DefaultAzureCredential picks this up automatically
```

Then:

```bash
dotnet run
```

## What's included

| Example | Shows |
|---|---|
| Basic Prompt | A single `.Prompt(...)` step |
| Content Pipeline | `.Prompt<T>(...)` piped into a `.Then(...)` step for a second model call |
| Multi-Turn Conversation | Reusing a `ChainContext` to carry history across calls |
| Tool Calling | `.WithToolsFrom(...)` + automatic function invocation |
| Streaming | `.RunStreamingAsync(...)` printing tokens as they arrive |

## How the client is built

```csharp
AzureOpenAIClient azureClient = useAzureIdentity
    ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
    : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

IChatClient chatClient = azureClient
    .GetChatClient(deployment)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseCogniChainRetry()
    .Build();
```

## Learn more

- [CogniChain documentation](../../docs/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure.AI.OpenAI NuGet package](https://www.nuget.org/packages/Azure.AI.OpenAI)
