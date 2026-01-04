# CogniChain

A powerful .NET 10 library for orchestrating LLM (Large Language Model) workflows with support for prompt templates, chains, tools, memory, retries, and streaming.

## Features

- **🎯 Prompt Templates**: Flexible template system with variable substitution
- **⛓️ Chain Orchestration**: Sequential and parallel execution of workflow steps
- **🛠️ Tool Framework**: Extensible tool/function calling system for LLMs
- **💾 Conversation Memory**: Smart memory management with configurable history limits
- **🔄 Retry Logic**: Built-in exponential backoff retry mechanism
- **📡 Streaming Support**: Real-time streaming of LLM responses
- **🎨 Fluent API**: Clean, intuitive builder pattern for complex workflows
- **✅ Type-Safe**: Fully typed with nullable reference types enabled
- **📚 Well Documented**: Comprehensive XML documentation and examples

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package CogniChain
```

Or via Package Manager Console:

```powershell
Install-Package CogniChain
```

## Quick Start

### Basic Prompt Template

```csharp
using CogniChain;

var template = new PromptTemplate("Hello {name}, welcome to {place}!");
var prompt = template.Format(new { name = "Alice", place = "CogniChain" });
Console.WriteLine(prompt);
// Output: Hello Alice, welcome to CogniChain!
```

### Conversation Memory

```csharp
var memory = new ConversationMemory(maxMessages: 10);
memory.AddSystemMessage("You are a helpful assistant.");
memory.AddUserMessage("What's the weather like?");
memory.AddAssistantMessage("I'd be happy to help with weather information!");

// Get formatted history
Console.WriteLine(memory.GetFormattedHistory());
```

### Tool Registry

```csharp
// Define a custom tool
public class CalculatorTool : ToolBase
{
    public override string Name => "Calculator";
    public override string Description => "Performs basic arithmetic";
    
    public override Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        // Your calculation logic here
        return Task.FromResult($"Result: {input}");
    }
}

// Register and use
var toolRegistry = new ToolRegistry();
toolRegistry.RegisterTool(new CalculatorTool());
var result = await toolRegistry.ExecuteToolAsync("Calculator", "2 + 2");
```

### Chain Execution

```csharp
// Define chain steps
public class UpperCaseStep : IChainStep
{
    public Task<ChainResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(new ChainResult
        {
            Output = input.ToUpper(),
            Success = true
        });
    }
}

// Build and execute chain
var chain = Chain.Create()
    .AddStep(new UpperCaseStep())
    .AddStep(new AddPrefixStep());

var result = await chain.RunAsync("hello world");
Console.WriteLine(result.Output); // "HELLO WORLD [processed]"
```

### LLM Orchestrator

```csharp
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    MaxConversationHistory = 10,
    RetryPolicy = new RetryPolicy { MaxRetries = 3 }
});

// Register tools
orchestrator.Tools.RegisterTool(new CalculatorTool());

// Use memory
orchestrator.Memory.AddSystemMessage("You are a helpful assistant.");

// Execute workflows
var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Analyze: {input}"))
    .WithVariables(new Dictionary<string, string> { ["input"] = "data" })
    .AddStep(new ProcessingStep());

var result = await workflow.ExecuteAsync();
```

## Advanced Usage

### Streaming Responses

```csharp
var streamingHandler = new StreamingHandler();
var stream = StreamingHandler.SimulateStreamAsync("Your response here", chunkSize: 10);

await streamingHandler.ProcessStreamAsync(stream, chunk => 
{
    Console.Write(chunk); // Process each chunk as it arrives
});
```

### Custom Retry Policies

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelayMs = 1000,
    BackoffMultiplier = 2.0,
    MaxDelayMs = 30000,
    UseJitter = true
};

var retryHandler = new RetryHandler(retryPolicy);
var result = await retryHandler.ExecuteAsync(async () => 
{
    // Your operation that might fail
    return await SomeRiskyOperation();
});
```

### Workflow Builder Pattern

```csharp
var orchestrator = new LLMOrchestrator();

var result = await orchestrator.CreateWorkflow()
    .WithPrompt(PromptTemplate.FromString("Process: {data}"))
    .WithVariables(new { data = "input" })
    .AddStep(new ValidationStep())
    .AddStep(new ProcessingStep())
    .AddStep(new FormattingStep())
    .ExecuteAsync();
```

## Documentation

For more detailed information, see:

- [API Reference](docs/api-reference.md)
- [Examples](examples/CogniChain.Examples/)
- [Architecture Guide](docs/architecture.md)
- [Best Practices](docs/best-practices.md)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Code of Conduct
- Development setup
- Pull request process
- Coding standards

## Security

For security concerns, please see [SECURITY.md](SECURITY.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a history of changes.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📖 [Documentation](docs/)
- 💬 [Discussions](https://github.com/wouternijenhuis/CogniChain/discussions)
- 🐛 [Issue Tracker](https://github.com/wouternijenhuis/CogniChain/issues)

## Acknowledgments

Built with ❤️ for the .NET community.