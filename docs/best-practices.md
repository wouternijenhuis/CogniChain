# Best Practices for CogniChain

## General Guidelines

### 1. Use Dependency Injection

Register CogniChain components in your DI container:

```csharp
services.AddSingleton<LLMOrchestrator>(sp => 
    new LLMOrchestrator(new OrchestratorConfig
    {
        MaxConversationHistory = 20,
        RetryPolicy = new RetryPolicy { MaxRetries = 3 }
    }));
```

### 2. Configure Retry Policies Appropriately

Match retry configuration to your use case:

```csharp
// For user-facing applications - fast failure
var userFacingPolicy = new RetryPolicy
{
    MaxRetries = 2,
    InitialDelayMs = 500,
    MaxDelayMs = 5000
};

// For background processing - more resilient
var backgroundPolicy = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelayMs = 2000,
    MaxDelayMs = 60000
};
```

### 3. Manage Memory Wisely

Set appropriate conversation history limits:

```csharp
// For chatbots with context
var chatMemory = new ConversationMemory(maxMessages: 20);

// For one-shot operations
var oneShot = new ConversationMemory(maxMessages: 1);

// For long-running conversations with summarization
var smartMemory = new ConversationMemory(maxMessages: 50);
```

### 4. Use Cancellation Tokens

Always pass cancellation tokens for responsive applications:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await chain.RunAsync(input, cts.Token);
```

## Prompt Templates

### DO: Use Clear Variable Names

```csharp
// Good
var template = new PromptTemplate(
    "Analyze the {document_type} for {company_name} from {year}");

// Bad
var template = new PromptTemplate(
    "Analyze the {x} for {y} from {z}");
```

### DO: Validate Variables Before Formatting

```csharp
var variables = GetVariablesFromUser();
if (template.Variables.All(v => variables.ContainsKey(v)))
{
    var prompt = template.Format(variables);
}
```

### DON'T: Include Sensitive Data in Templates

```csharp
// Bad
var template = new PromptTemplate("API Key: {apiKey}");

// Good - handle sensitive data separately
var securePrompt = BuildSecurePrompt(apiKey);
```

## Chain Design

### DO: Keep Steps Focused

Each step should have a single responsibility:

```csharp
// Good
var chain = Chain.Create()
    .AddStep(new ExtractDataStep())
    .AddStep(new ValidateDataStep())
    .AddStep(new TransformDataStep())
    .AddStep(new FormatOutputStep());

// Bad
var chain = Chain.Create()
    .AddStep(new DoEverythingStep());
```

### DO: Handle Errors Gracefully

```csharp
public class ResilientStep : IChainStep
{
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        try
        {
            var output = await ProcessAsync(input, ct);
            return new ChainResult { Output = output, Success = true };
        }
        catch (Exception ex)
        {
            return new ChainResult 
            { 
                Success = false, 
                ErrorMessage = $"Processing failed: {ex.Message}" 
            };
        }
    }
}
```

### DO: Add Metadata for Observability

```csharp
return new ChainResult
{
    Output = result,
    Success = true,
    Metadata = new Dictionary<string, object>
    {
        ["duration_ms"] = stopwatch.ElapsedMilliseconds,
        ["tokens_used"] = tokenCount,
        ["step_name"] = "ProcessingStep"
    }
};
```

## Tool Development

### DO: Provide Clear Descriptions

```csharp
public class WeatherTool : ToolBase
{
    public override string Name => "get_weather";
    
    // Good - clear and specific
    public override string Description => 
        "Gets current weather for a city. Input format: 'city,country_code' (e.g., 'London,UK')";
    
    // Bad - vague
    // public override string Description => "Gets weather";
}
```

### DO: Validate Tool Input

```csharp
public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(input))
        return "Error: City name required";
    
    if (!IsValidCityFormat(input))
        return "Error: Use format 'City,CountryCode'";
    
    // Process valid input
    return await GetWeatherAsync(input, ct);
}
```

### DO: Handle Tool Failures

```csharp
public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
{
    try
    {
        return await ExecuteInternalAsync(input, ct);
    }
    catch (HttpRequestException ex)
    {
        return $"Weather service unavailable: {ex.Message}";
    }
    catch (Exception ex)
    {
        return $"Error retrieving weather: {ex.Message}";
    }
}
```

## Memory Management

### DO: Clear Memory When Appropriate

```csharp
// At the end of a conversation
memory.Clear();

// When switching contexts
if (newTopic != currentTopic)
{
    memory.Clear();
    memory.AddSystemMessage($"Now discussing: {newTopic}");
}
```

### DO: Use System Messages Effectively

```csharp
// Set behavior and constraints
memory.AddSystemMessage("You are a helpful coding assistant.");
memory.AddSystemMessage("Always include code examples.");
memory.AddSystemMessage("Format code using markdown.");
```

### DON'T: Store Excessive History

```csharp
// Bad - unbounded growth
var memory = new ConversationMemory(); // maxMessages = -1

// Good - bounded with reasonable limit
var memory = new ConversationMemory(maxMessages: 20);
```

## Streaming

### DO: Provide User Feedback

```csharp
await streamingHandler.ProcessStreamAsync(stream, chunk =>
{
    Console.Write(chunk);
    statusIndicator.Update(); // Keep user informed
});
```

### DO: Handle Streaming Errors

```csharp
try
{
    await streamingHandler.ProcessStreamAsync(stream, chunk => ProcessChunk(chunk));
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nStreaming cancelled by user");
}
catch (Exception ex)
{
    Console.WriteLine($"\nStreaming error: {ex.Message}");
}
```

## Orchestrator Usage

### DO: Configure Once, Use Many Times

```csharp
// Setup phase
var orchestrator = new LLMOrchestrator(config);
orchestrator.Tools.RegisterTool(new CalculatorTool());
orchestrator.Tools.RegisterTool(new SearchTool());
orchestrator.Memory.AddSystemMessage("You are a helpful assistant.");

// Use phase (multiple times)
for (var userInput in userInputs)
{
    var result = await orchestrator.ExecuteChainAsync(chain, userInput);
}
```

### DO: Use Workflow Builder for Complex Scenarios

```csharp
var result = await orchestrator.CreateWorkflow()
    .WithPrompt(template)
    .WithVariables(variables)
    .AddStep(new ValidationStep())
    .AddStep(new ProcessingStep())
    .AddStep(new FormattingStep())
    .ExecuteAsync();
```

## Testing

### DO: Test Chain Steps Independently

```csharp
[Fact]
public async Task MyStep_ProcessesInputCorrectly()
{
    var step = new MyStep();
    var result = await step.ExecuteAsync("test input");
    
    Assert.True(result.Success);
    Assert.Equal("expected output", result.Output);
}
```

### DO: Mock External Dependencies

```csharp
public class MyTool : ToolBase
{
    private readonly IExternalService _service;
    
    public MyTool(IExternalService service)
    {
        _service = service;
    }
    
    // Now you can mock IExternalService in tests
}
```

### DO: Test Error Scenarios

```csharp
[Fact]
public async Task Chain_HandlesStepFailure()
{
    var chain = Chain.Create()
        .AddStep(new FailingStep());
    
    var result = await chain.RunAsync("input");
    
    Assert.False(result.Success);
    Assert.NotNull(result.ErrorMessage);
}
```

## Performance

### DO: Reuse Instances

```csharp
// Good - reuse
private static readonly LLMOrchestrator _orchestrator = new();

// Bad - create each time
public async Task Process() 
{
    var orchestrator = new LLMOrchestrator();
    // ...
}
```

### DO: Use Streaming for Large Outputs

```csharp
// Good - streaming for responsive UI
await chain.RunStreamingAsync(input, chunk => ui.Update(chunk));

// Bad - wait for complete response
var result = await chain.RunAsync(input);
ui.Update(result.Output); // User waits
```

### DON'T: Block Async Code

```csharp
// Bad
var result = chain.RunAsync(input).Result;

// Good
var result = await chain.RunAsync(input);
```

## Security

### DO: Sanitize User Input

```csharp
var sanitized = SanitizeInput(userInput);
var result = await chain.RunAsync(sanitized);
```

### DO: Validate Tool Outputs

```csharp
public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
{
    var output = await GetDataAsync(input, ct);
    return ValidateAndSanitize(output);
}
```

### DON'T: Log Sensitive Information

```csharp
// Bad
_logger.LogInformation("Processing: {Input}", apiKey);

// Good
_logger.LogInformation("Processing request");
```
