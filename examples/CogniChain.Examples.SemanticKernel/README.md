# CogniChain Semantic Kernel Integration Example

This example demonstrates how to integrate CogniChain with Microsoft Semantic Kernel.

## Prerequisites

- .NET 10.0 or later
- OpenAI API key OR Azure OpenAI credentials

## Setup

### Using OpenAI

```bash
export OPENAI_API_KEY="your-api-key-here"
```

### Using Azure OpenAI

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key-here"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
```

Then build and run:

```bash
dotnet build
dotnet run
```

## What's Included

This example demonstrates:

1. **Basic Chain Step** - Using `SemanticKernelStep` with CogniChain workflows
2. **Prompt Templates** - Combining CogniChain and Semantic Kernel prompt templates
3. **Multi-turn Conversations** - Synchronizing `ConversationMemory` with SK `ChatHistory`
4. **Chain Pipelines** - Multi-step content generation
5. **Streaming Responses** - Real-time streaming with Semantic Kernel
6. **Tool Integration** - AI-powered tools using Semantic Kernel
7. **Retry Handler** - Resilient API calls with exponential backoff

## Key Classes

### `SemanticKernelStep`

A custom `IChainStep` implementation that wraps Semantic Kernel:

```csharp
public class SemanticKernelStep : IChainStep
{
    private readonly Kernel _kernel;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var result = await _kernel.InvokePromptAsync(input, cancellationToken: ct);
        
        return new ChainResult
        {
            Output = result.ToString(),
            Success = true,
            Metadata = { ["provider"] = "Semantic Kernel" }
        };
    }
}
```

### AI-Powered Tools

```csharp
class CodeAnalysisTool : ToolBase
{
    private readonly Kernel _kernel;
    
    public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
    {
        var result = await _kernel.InvokePromptAsync(
            $"Analyze this code: {input}", cancellationToken: ct);
        return result.ToString();
    }
}
```

## Combining CogniChain and Semantic Kernel

CogniChain and Semantic Kernel complement each other:

| Feature | CogniChain | Semantic Kernel |
|---------|------------|-----------------|
| Conversation Memory | `ConversationMemory` | `ChatHistory` |
| Prompt Templates | `PromptTemplate` | Prompt functions |
| Chains/Pipelines | `Chain` | Kernel plugins |
| Tool Calling | `ToolRegistry` | Native functions |
| Retry Logic | `RetryHandler` | Built-in retry |

Use CogniChain for:
- Flexible chain orchestration
- Cross-provider memory management
- Simple tool registration

Use Semantic Kernel for:
- AI orchestration and plugins
- Native function calling
- Vector stores and embeddings

## Learn More

- [CogniChain Documentation](../../../docs/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)
