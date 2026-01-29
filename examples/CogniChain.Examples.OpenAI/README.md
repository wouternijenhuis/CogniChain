# CogniChain OpenAI Integration Example

This example demonstrates how to integrate CogniChain with the OpenAI API.

## Prerequisites

- .NET 10.0 or later
- OpenAI API key

## Setup

1. Set your OpenAI API key as an environment variable:

```bash
export OPENAI_API_KEY="your-api-key-here"
```

2. Build and run the example:

```bash
dotnet build
dotnet run
```

## What's Included

This example demonstrates:

1. **Basic Chain Step** - Using `OpenAIStep` with CogniChain workflows
2. **Multi-turn Conversations** - Managing conversation history with `ConversationMemory`
3. **Multi-Step Chains** - Chaining multiple LLM calls for content pipelines
4. **Streaming Responses** - Real-time response streaming
5. **Tool Integration** - Registering and executing tools

## Key Classes

### `OpenAIStep`

A custom `IChainStep` implementation that wraps OpenAI API calls:

```csharp
public class OpenAIStep : IChainStep
{
    private readonly ChatClient _client;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var response = await _client.CompleteChatAsync(input);
        
        return new ChainResult
        {
            Output = response.Value.Content[0].Text,
            Success = true
        };
    }
}
```

## Learn More

- [CogniChain Documentation](../../../docs/)
- [OpenAI .NET SDK](https://github.com/openai/openai-dotnet)
