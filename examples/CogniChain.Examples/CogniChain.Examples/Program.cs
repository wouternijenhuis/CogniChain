using CogniChain;

Console.WriteLine("=== CogniChain Examples ===\n");

// Example 1: Prompt Templates
Console.WriteLine("1. Prompt Template Example:");
var template = new PromptTemplate("Hello {name}, welcome to {place}!");
var prompt = template.Format(new { name = "Alice", place = "CogniChain" });
Console.WriteLine(prompt);
Console.WriteLine();

// Example 2: Conversation Memory
Console.WriteLine("2. Conversation Memory Example:");
var memory = new ConversationMemory(maxMessages: 5);
memory.AddSystemMessage("You are a helpful assistant.");
memory.AddUserMessage("What's the weather like?");
memory.AddAssistantMessage("I don't have real-time weather data, but I can help with other questions!");
memory.AddUserMessage("Thanks!");
Console.WriteLine(memory.GetFormattedHistory());
Console.WriteLine();

// Example 3: Tools
Console.WriteLine("3. Tool Registry Example:");
var toolRegistry = new ToolRegistry();
toolRegistry.RegisterTool(new CalculatorTool());
toolRegistry.RegisterTool(new TextTransformTool());
Console.WriteLine("Available tools:");
Console.WriteLine(toolRegistry.GetToolDescriptions());
var calcResult = await toolRegistry.ExecuteToolAsync("Calculator", "2 + 2");
Console.WriteLine($"Calculator result: {calcResult}");
Console.WriteLine();

// Example 4: Chain Execution
Console.WriteLine("4. Chain Execution Example:");
var chain = Chain.Create()
    .AddStep(new UpperCaseStep())
    .AddStep(new AddExclamationStep());
var chainResult = await chain.RunAsync("hello world");
Console.WriteLine($"Chain result: {chainResult.Output}");
Console.WriteLine();

// Example 5: Streaming
Console.WriteLine("5. Streaming Example:");
var streamingHandler = new StreamingHandler();
var streamSource = StreamingHandler.SimulateStreamAsync("This is a streaming response from an LLM", chunkSize: 5, delayMs: 100);
Console.Write("Streaming: ");
await streamingHandler.ProcessStreamAsync(streamSource, chunk => Console.Write(chunk));
Console.WriteLine("\n");

// Example 6: Orchestrator
Console.WriteLine("6. Orchestrator Example:");
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    MaxConversationHistory = 10
});
orchestrator.Tools.RegisterTool(new CalculatorTool());
orchestrator.Memory.AddSystemMessage("You are a helpful assistant.");
var promptTemplate = new PromptTemplate("Analyze this: {input}");
var analysis = orchestrator.ExecutePrompt(promptTemplate, new { input = "Machine Learning" });
Console.WriteLine($"Formatted prompt: {analysis}");
Console.WriteLine();

// Example 7: Workflow Builder
Console.WriteLine("7. Workflow Builder Example:");
var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Process this input: {text}"))
    .WithVariables(new Dictionary<string, string> { ["text"] = "sample data" })
    .AddStep(new UpperCaseStep())
    .AddStep(new AddExclamationStep());
var workflowResult = await workflow.ExecuteAsync();
Console.WriteLine($"Workflow result: {workflowResult.Output}");

Console.WriteLine("\n=== Examples Complete ===");

// Example tool implementations
class CalculatorTool : ToolBase
{
    public override string Name => "Calculator";
    public override string Description => "Performs basic arithmetic operations";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        // Simple calculator implementation
        var result = $"Result of '{input}' = 4 (simulated)";
        return Task.FromResult(result);
    }
}

class TextTransformTool : ToolBase
{
    public override string Name => "TextTransform";
    public override string Description => "Transforms text in various ways";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(input.ToUpper());
    }
}

class UpperCaseStep : IChainStep
{
    public Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChainResult
        {
            Output = input.ToUpper(),
            Success = true
        });
    }
}

class AddExclamationStep : IChainStep
{
    public Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChainResult
        {
            Output = $"{input}!",
            Success = true
        });
    }
}
