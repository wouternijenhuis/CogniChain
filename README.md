# CogniChain

> A modern .NET 10 library for building LLM-powered applications with built-in support for prompt management, conversation memory, tool calling, and resilient workflows.

## Why CogniChain?

Building reliable LLM applications requires more than just API calls. CogniChain provides the building blocks you need:

- **🎯 Prompt Management**: Reusable templates with type-safe variable substitution
- **⛓️ Workflow Orchestration**: Chain multiple operations with automatic error handling
- **🛠️ Tool Integration**: Let your LLM call functions and APIs in your application
- **💾 Context Management**: Automatic conversation history with smart memory limits
- **🔄 Resilience**: Built-in retry logic with exponential backoff for API failures
- **📡 Streaming**: Real-time response streaming for better user experience
- **🎨 Developer Experience**: Fluent API with full IntelliSense support

Perfect for building chatbots, AI assistants, content generation pipelines, and intelligent automation workflows.

## Installation

Install CogniChain via NuGet:

```bash
dotnet add package CogniChain
```

**Requirements:**
- .NET 10.0 or later
- Any LLM API client (OpenAI, Azure OpenAI, Anthropic, etc.)

> **Note**: CogniChain doesn't include LLM API clients. Bring your own client and wrap it in chain steps or tools.

## Quick Start

### Your First LLM Workflow in 5 Minutes

Here's a complete example of building a simple AI assistant:

```csharp
using CogniChain;

// 1. Set up the orchestrator
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    MaxConversationHistory = 10,
    RetryPolicy = new RetryPolicy { MaxRetries = 3 }
});

// 2. Add system context
orchestrator.Memory.AddSystemMessage("You are a helpful coding assistant.");

// 3. Create a workflow with prompt template
var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Help me with: {task}"))
    .WithVariables(new Dictionary<string, string> 
    { 
        ["task"] = "writing a C# async method" 
    })
    .AddStep(new YourLLMCallStep()); // Your LLM API integration here

// 4. Execute
var result = await workflow.ExecuteAsync();
Console.WriteLine(result.Output);
```

### Common Use Cases

#### Building a Chatbot

```csharp
var orchestrator = new LLMOrchestrator();
orchestrator.Memory.AddSystemMessage("You are a friendly customer support agent.");

// Handle user messages
while (true)
{
    var userMessage = Console.ReadLine();
    orchestrator.Memory.AddUserMessage(userMessage);
    
    // Create prompt with history
    var prompt = $"{orchestrator.Memory.GetFormattedHistory()}\n\nRespond to the last user message:";
    
    // Call your LLM API here with the prompt
    var response = await CallYourLLMAsync(prompt);
    
    orchestrator.Memory.AddAssistantMessage(response);
    Console.WriteLine(response);
}
```

#### Content Generation Pipeline

```csharp
// Chain multiple AI operations
var chain = Chain.Create()
    .AddStep(new GenerateOutlineStep())      // Generate article outline
    .AddStep(new ExpandSectionsStep())       // Expand each section
    .AddStep(new ProofreadStep())            // Proofread content
    .AddStep(new FormatMarkdownStep());      // Format as markdown

var article = await chain.RunAsync("Write about .NET performance tips");
Console.WriteLine(article.Output);
```

#### Function Calling / Tool Use

```csharp
// Define tools your LLM can use
public class WeatherTool : ToolBase
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather for a city";
    
    public override async Task<string> ExecuteAsync(string city, CancellationToken ct)
    {
        var weather = await WeatherApi.GetWeatherAsync(city);
        return $"Temperature: {weather.Temp}°C, Conditions: {weather.Conditions}";
    }
}

// Register and use
orchestrator.Tools.RegisterTool(new WeatherTool());

// When LLM wants to call the tool:
var result = await orchestrator.Tools.ExecuteToolAsync("get_weather", "Seattle");
```

## Core Components Guide

### 1. Prompt Templates

Create reusable prompts with variable substitution:

```csharp
// Simple template
var template = new PromptTemplate("Translate '{text}' to {language}");
var prompt = template.Format(new { text = "Hello", language = "Spanish" });

// Multi-variable template
var reviewTemplate = new PromptTemplate(@"
Review this {type} code:
{code}

Focus on: {aspects}
");

var prompt = reviewTemplate.Format(new Dictionary<string, string>
{
    ["type"] = "C#",
    ["code"] = sourceCode,
    ["aspects"] = "performance and readability"
});
```

### 2. Conversation Memory

Manage multi-turn conversations automatically:

```csharp
var memory = new ConversationMemory(maxMessages: 20);

// System message sets behavior
memory.AddSystemMessage("You are an expert C# developer.");

// Add conversation turns
memory.AddUserMessage("How do I use async/await?");
memory.AddAssistantMessage("Async/await is used for asynchronous programming...");

// Get history for context
var history = memory.GetFormattedHistory(); // Pass to LLM
var lastTwo = memory.GetLastMessages(2);    // Get recent messages
```

**Memory Management Tips:**
- Use 10-20 messages for chatbots
- Use 1-5 messages for focused tasks
- System messages are preserved even when trimming

### 3. Chain Workflows

Build multi-step AI pipelines:

```csharp
public class LLMCallStep : IChainStep
{
    private readonly OpenAIClient _client;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var response = await _client.GetCompletionAsync(input);
        
        return new ChainResult
        {
            Output = response,
            Success = true,
            Metadata = { ["tokens"] = response.TokenCount }
        };
    }
}

// Build pipeline
var chain = Chain.Create()
    .AddStep(new PreparePromptStep())
    .AddStep(new LLMCallStep())
    .AddStep(new ParseResponseStep());

var result = await chain.RunAsync(userInput);
```

### 4. Retry & Resilience

Handle API failures gracefully:

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,              // Try up to 3 times
    InitialDelayMs = 1000,       // Start with 1 second delay
    BackoffMultiplier = 2.0,     // Double the delay each time
    UseJitter = true             // Add randomness to prevent thundering herd
};

var retryHandler = new RetryHandler(retryPolicy);

// Wrap your LLM call
var response = await retryHandler.ExecuteAsync(async () =>
{
    return await openAIClient.GetCompletionAsync(prompt);
});
```

### 5. Streaming Responses

Provide real-time feedback to users:

```csharp
var streamingHandler = new StreamingHandler();

// Your streaming LLM call
async IAsyncEnumerable<string> StreamFromLLM()
{
    await foreach (var chunk in llmClient.StreamCompletionAsync(prompt))
    {
        yield return chunk.Text;
    }
}

// Process stream with UI updates
await streamingHandler.ProcessStreamAsync(
    StreamFromLLM(), 
    chunk => Console.Write(chunk)  // Update UI in real-time
);
```

## Integration Examples

### OpenAI Integration

```csharp
using OpenAI.Chat;

public class OpenAIStep : IChainStep
{
    private readonly ChatClient _client;
    
    public OpenAIStep(ChatClient client)
    {
        _client = client;
    }
    
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

// Use in workflow
var client = new ChatClient("gpt-4", apiKey);
var orchestrator = new LLMOrchestrator();

var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Explain {concept} in simple terms"))
    .WithVariables(new { concept = "dependency injection" })
    .AddStep(new OpenAIStep(client));

var result = await workflow.ExecuteAsync();
```

### Azure OpenAI Integration

```csharp
using Azure.AI.OpenAI;

public class AzureOpenAIStep : IChainStep
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages = { new ChatRequestUserMessage(input) }
        };
        
        var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions, ct);
        
        return new ChainResult
        {
            Output = response.Value.Choices[0].Message.Content,
            Success = true
        };
    }
}
```

### Semantic Kernel Integration

```csharp
using Microsoft.SemanticKernel;

public class SemanticKernelStep : IChainStep
{
    private readonly Kernel _kernel;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var result = await _kernel.InvokePromptAsync(input, cancellationToken: ct);
        
        return new ChainResult
        {
            Output = result.ToString(),
            Success = true
        };
    }
}
```

## Troubleshooting

### Common Issues

**Q: My conversation history keeps growing, slowing down API calls**
```csharp
// A: Set a reasonable limit based on your use case
var memory = new ConversationMemory(maxMessages: 10);  // Good for chatbots
```

**Q: API calls are failing intermittently**
```csharp
// A: Use retry logic with exponential backoff
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    RetryPolicy = new RetryPolicy 
    { 
        MaxRetries = 3,
        InitialDelayMs = 1000,
        UseJitter = true 
    }
});
```

**Q: How do I pass context between chain steps?**
```csharp
// A: Use the Metadata dictionary
public class Step1 : IChainStep
{
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        return new ChainResult
        {
            Output = "processed",
            Metadata = { ["userId"] = "123", ["timestamp"] = DateTime.UtcNow }
        };
    }
}
```

**Q: Can I use this with local LLMs (Ollama, LM Studio)?**
```csharp
// A: Yes! Create a custom IChainStep that calls your local LLM API
public class LocalLLMStep : IChainStep
{
    private readonly HttpClient _httpClient;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:11434/api/generate",
            new { model = "llama2", prompt = input },
            ct
        );
        
        var result = await response.Content.ReadFromJsonAsync<LocalLLMResponse>(ct);
        return new ChainResult { Output = result.Response, Success = true };
    }
}
```

## FAQ

**Do I need an LLM API key?**
Yes. CogniChain is a framework for building LLM applications, not an LLM provider. Bring your own API client.

**Which LLMs are supported?**
Any LLM you can call from .NET! OpenAI, Azure OpenAI, Anthropic, Google, Ollama, etc. Just wrap calls in `IChainStep`.

**Can I use this in production?**
Yes! CogniChain includes retry logic, error handling, and is fully async for production workloads.

**Is this compatible with Semantic Kernel or LangChain?**
Yes! You can use CogniChain alongside or integrate it into those frameworks via custom steps.

**How do I handle rate limits?**
Use the built-in `RetryPolicy` with appropriate delays, or implement custom rate limiting in your chain steps.

## Documentation

### For Users
- 📘 [Getting Started Guide](docs/getting-started.md) - Step-by-step tutorial
- 📗 [API Reference](docs/api-reference.md) - Complete API documentation
- 📙 [Best Practices](docs/best-practices.md) - Production tips and patterns
- 💡 [Examples](examples/CogniChain.Examples/) - Working code samples

### For Contributors
- 🏗️ [Architecture Guide](docs/architecture.md) - Internal design and patterns
- 🤝 [Contributing](CONTRIBUTING.md) - How to contribute
- 🔒 [Security](SECURITY.md) - Security policy

## Community & Support

- 💬 [GitHub Discussions](https://github.com/wouternijenhuis/CogniChain/discussions) - Ask questions and share ideas
- 🐛 [Issue Tracker](https://github.com/wouternijenhuis/CogniChain/issues) - Report bugs or request features
- 📖 [Changelog](CHANGELOG.md) - See what's new

## License

MIT License - see [LICENSE](LICENSE) file for details.

---

**Built with ❤️ for the .NET community** | [Star on GitHub](https://github.com/wouternijenhuis/CogniChain) ⭐