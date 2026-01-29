# CogniChain Azure OpenAI Integration Example

This example demonstrates how to integrate CogniChain with Azure OpenAI Service.

## Prerequisites

- .NET 10.0 or later
- Azure OpenAI resource with a deployed model
- Azure OpenAI API key or Azure Identity credentials

## Setup

### Option 1: API Key Authentication

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key-here"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"  # Optional, defaults to gpt-4o-mini
```

### Option 2: Azure Identity Authentication (Recommended for Production)

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
# Ensure you're logged in via Azure CLI: az login
```

Then build and run:

```bash
dotnet build
dotnet run
```

## What's Included

This example demonstrates:

1. **Basic Chain Step** - Using `AzureOpenAIStep` with CogniChain workflows
2. **Authentication Options** - Both API key and Azure Identity (DefaultAzureCredential)
3. **Multi-turn Conversations** - Managing conversation history with `ConversationMemory`
4. **Content Pipelines** - Chaining multiple LLM calls for content generation
5. **Streaming Responses** - Real-time response streaming
6. **Retry Handler** - Built-in resilience with exponential backoff
7. **Tool Integration** - Azure-specific tools

## Key Classes

### `AzureOpenAIStep`

A custom `IChainStep` implementation for Azure OpenAI:

```csharp
public class AzureOpenAIStep : IChainStep
{
    private readonly ChatClient _client;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var response = await _client.CompleteChatAsync(input);
        
        return new ChainResult
        {
            Output = response.Value.Content[0].Text,
            Success = true,
            Metadata = { ["provider"] = "Azure OpenAI" }
        };
    }
}
```

### Azure Identity Authentication

```csharp
// Recommended for production - uses managed identity, Azure CLI, etc.
var client = new AzureOpenAIClient(
    new Uri(endpoint), 
    new DefaultAzureCredential());
```

## Learn More

- [CogniChain Documentation](../../../docs/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure.AI.OpenAI NuGet Package](https://www.nuget.org/packages/Azure.AI.OpenAI)
