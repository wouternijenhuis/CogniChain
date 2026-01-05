# CogniChain API Reference

> Complete API documentation for building LLM applications with CogniChain

## Table of Contents

- [Quick Reference](#quick-reference)
- [Core Classes](#core-classes)
- [Interfaces](#interfaces)
- [Configuration](#configuration)
- [Extension Points](#extension-points)

## Quick Reference

| What do you want to do? | Use this |
|---|---|
| Create a prompt with variables | `PromptTemplate` |
| Build a multi-step workflow | `Chain.Create().AddStep()` |
| Manage conversation history | `ConversationMemory` |
| Let LLM call functions | `ToolRegistry` + `ToolBase` |
| Handle API failures | `RetryHandler` |
| Stream responses | `StreamingHandler` |
| Orchestrate everything | `LLMOrchestrator` |

---

## Core Classes

### PromptTemplate

Create reusable prompts with type-safe variable substitution.

**When to use:** Building prompt templates for different scenarios, managing prompt variations.

**Basic Usage**
```csharp
var template = new PromptTemplate("Translate {text} to {language}");
var prompt = template.Format(new { text = "Hello", language = "Spanish" });
```


**Constructor**
```csharp
public PromptTemplate(string template)
```

**Properties**
- `Template` (string): The raw template string
- `Variables` (IReadOnlyList<string>): List of all variables found in template

**Methods**

`Format(Dictionary<string, string> variables)` → `string`
- Formats template using a dictionary
- **Throws:** `ArgumentException` if any variable is missing

`Format(object variables)` → `string`  
- Formats template using object properties
- Properties must match variable names (case-sensitive)

`FromString(string template)` → `PromptTemplate` (static)
- Factory method for creating templates

**Examples**

```csharp
// Dictionary-based
var vars = new Dictionary<string, string>
{
    ["name"] = "Alice",
    ["role"] = "developer"
};
var prompt = template.Format(vars);

// Object-based (cleaner for IntelliSense)
var prompt = template.Format(new 
{ 
    name = "Alice", 
    role = "developer" 
});

// Get all variables in template
var template = new PromptTemplate("Hello {name}, you are {age} years old");
Console.WriteLine(template.Variables.Count); // 2
```

**Common Patterns**

```csharp
// Multi-line template
var template = new PromptTemplate(@"
You are a {role}.
Task: {task}
Context: {context}
");

// Conditional variables (handle missing with defaults)
var safeTemplate = new PromptTemplate("Hello {name}");
var vars = new Dictionary<string, string> { ["name"] = userName ?? "Guest" };
```

---

### Chain

Sequential workflow orchestration with automatic error handling.

**When to use:** Multi-step LLM operations, complex workflows, building pipelines.

**Basic Usage**
```csharp
var chain = Chain.Create()
    .AddStep(new PrepareStep())
    .AddStep(new LLMCallStep())
    .AddStep(new PostProcessStep());

var result = await chain.RunAsync("user input");
```

**Methods**

`Create()` → `Chain` (static)
- Factory method for creating new chains

`AddStep(IChainStep step)` → `Chain`
- Adds a step to the chain
- Returns chain for fluent chaining
- Steps execute in order added

`RunAsync(string input, CancellationToken ct)` → `Task<ChainResult>`
- Executes all steps sequentially
- Each step receives previous step's output
- Stops on first failure
- Merges metadata from all steps

`RunStreamingAsync(string input, Action<string> onChunk, CancellationToken ct)` → `Task<ChainResult>`
- Same as RunAsync but calls onChunk after each step
- Useful for progress updates

**Examples**

```csharp
// Basic chain
var chain = Chain.Create()
    .AddStep(new ValidateInputStep())
    .AddStep(new CallLLMStep())
    .AddStep(new ParseResponseStep());

var result = await chain.RunAsync(userInput);

if (result.Success)
{
    Console.WriteLine(result.Output);
    Console.WriteLine($"Tokens used: {result.Metadata["tokens"]}");
}

// With streaming progress
await chain.RunStreamingAsync(
    userInput, 
    output => Console.WriteLine($"Step completed: {output.Substring(0, 50)}...")
);

// Error handling
var result = await chain.RunAsync(input);
if (!result.Success)
{
    Console.WriteLine($"Chain failed: {result.ErrorMessage}");
}
```

---

### ConversationMemory

Manages conversation history with automatic pruning.

**When to use:** Chatbots, multi-turn conversations, maintaining context.

**Basic Usage**
```csharp
var memory = new ConversationMemory(maxMessages: 10);
memory.AddUserMessage("Hello!");
memory.AddAssistantMessage("Hi! How can I help?");
```

**Constructor**
```csharp
public ConversationMemory(int maxMessages = -1)
```
- `maxMessages`: Maximum messages to keep (-1 for unlimited)
- System messages are always preserved

**Properties**
- `Messages` (IReadOnlyList<Message>): All messages in conversation

**Methods**

`AddMessage(string role, string content)` → `void`
- Adds message with specified role
- Auto-trims if over maxMessages limit

`AddUserMessage(string content)` → `void`  
`AddAssistantMessage(string content)` → `void`  
`AddSystemMessage(string content)` → `void`
- Convenience methods for common roles

`GetFormattedHistory()` → `string`
- Returns conversation as formatted string
- Format: `role: content\nrole: content...`

`Clear()` → `void`
- Removes all messages

`GetLastMessages(int count)` → `IEnumerable<Message>`
- Returns most recent N messages

`GetMessagesByRole(string role)` → `IEnumerable<Message>`
- Filters messages by role

**Examples**

```csharp
// Chatbot conversation
var memory = new ConversationMemory(maxMessages: 20);
memory.AddSystemMessage("You are a helpful assistant.");

// Add conversation turns
memory.AddUserMessage("What's the weather?");
memory.AddAssistantMessage("I don't have weather data, but I can help with other questions.");

// Build prompt with history
var prompt = $@"
Conversation history:
{memory.GetFormattedHistory()}

Respond to the last user message:";

// Get only recent context
var recentMessages = memory.GetLastMessages(5);
foreach (var msg in recentMessages)
{
    Console.WriteLine($"{msg.Role}: {msg.Content}");
}

// Filter by role
var userQuestions = memory.GetMessagesByRole("user");
```

**Memory Management Tips**

```csharp
// For chatbots - keep last 10-20 exchanges
var chatMemory = new ConversationMemory(maxMessages: 20);

// For single-turn Q&A - minimal history
var qaMemory = new ConversationMemory(maxMessages: 1);

// For document analysis - larger context
var docMemory = new ConversationMemory(maxMessages: 50);

// System messages are preserved
memory.AddSystemMessage("Important context"); // Never trimmed
// ... add 100 user messages ...
// System message still there!
```

---

### ToolRegistry

Function calling framework for LLMs.

**When to use:** Giving LLMs access to APIs, databases, calculations, external tools.

**Basic Usage**
```csharp
var registry = new ToolRegistry();
registry.RegisterTool(new WeatherTool());
registry.RegisterTool(new CalculatorTool());

var result = await registry.ExecuteToolAsync("get_weather", "Seattle");
```

**Methods**

`RegisterTool(ITool tool)` → `void`
- Adds tool to registry
- Overwrites if tool with same name exists

`GetTool(string name)` → `ITool?`
- Returns tool by name or null

`GetAllTools()` → `IEnumerable<ITool>`
- Returns all registered tools

`ExecuteToolAsync(string toolName, string input, CancellationToken ct)` → `Task<string>`
- Executes tool by name
- **Throws:** `InvalidOperationException` if tool not found

`GetToolDescriptions()` → `string`
- Returns formatted list of all tools
- Format: `- toolName: description`

**Examples**

```csharp
// Define a tool
public class WeatherTool : ToolBase
{
    public override string Name => "get_weather";
    public override string Description => "Get weather for a city";
    
    public override async Task<string> ExecuteAsync(string city, CancellationToken ct)
    {
        var weather = await WeatherApi.GetAsync(city);
        return $"Temperature: {weather.Temp}°C";
    }
}

// Register and use
var registry = new ToolRegistry();
registry.RegisterTool(new WeatherTool());
registry.RegisterTool(new CalculatorTool());

// List available tools (for LLM prompt)
var toolsDescription = registry.GetToolDescriptions();
Console.WriteLine(toolsDescription);
// Output:
// - get_weather: Get weather for a city
// - calculator: Perform calculations

// Execute tool when LLM requests it
if (llmWantsToCallTool)
{
    var result = await registry.ExecuteToolAsync("get_weather", "Seattle");
    // Send result back to LLM
}
```

---

### RetryHandler

Resilient execution with exponential backoff.

**When to use:** API calls that might fail, rate-limited operations, network requests.

**Basic Usage**
```csharp
var handler = new RetryHandler(new RetryPolicy { MaxRetries = 3 });
var result = await handler.ExecuteAsync(async () => await CallLLMAsync());
```

**Constructor**
```csharp
public RetryHandler(RetryPolicy? policy = null)
```
- Uses `RetryPolicy.Default` if null

**Methods**

`ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)` → `Task<T>`
- Executes operation with retry logic
- Returns result on success
- **Throws:** `RetryException` after max retries

`ExecuteAsync(Func<Task> operation, CancellationToken ct)` → `Task`
- Void operation variant

**Examples**

```csharp
// Wrap LLM API call
var retryHandler = new RetryHandler(new RetryPolicy
{
    MaxRetries = 3,
    InitialDelayMs = 1000,
    BackoffMultiplier = 2.0
});

try
{
    var response = await retryHandler.ExecuteAsync(async () =>
    {
        return await openAIClient.GetCompletionAsync(prompt);
    });
}
catch (RetryException ex)
{
    Console.WriteLine($"Failed after retries: {ex.InnerException.Message}");
}

// Different policies for different scenarios
var aggressiveRetry = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelayMs = 2000,
    BackoffMultiplier = 2.0,
    MaxDelayMs = 60000,
    UseJitter = true  // Prevents thundering herd
};

var fastFailure = new RetryPolicy
{
    MaxRetries = 1,
    InitialDelayMs = 500
};
```

---

### StreamingHandler

Process streaming LLM responses.

**When to use:** Real-time UI updates, long-running generations, better UX.

**Basic Usage**
```csharp
var handler = new StreamingHandler();
await handler.ProcessStreamAsync(llmStream, chunk => Console.Write(chunk));
```

**Methods**

`ProcessStreamAsync(IAsyncEnumerable<string> source, Action<string>? onChunk, CancellationToken ct)` → `Task<string>`
- Processes stream and calls onChunk for each piece
- Returns complete concatenated result

`SimulateStreamAsync(string content, int chunkSize, int delayMs, CancellationToken ct)` → `IAsyncEnumerable<string>` (static)
- Testing utility to simulate streaming

**Examples**

```csharp
// Real-time console output
var handler = new StreamingHandler();
await handler.ProcessStreamAsync(
    llmClient.StreamAsync(prompt),
    chunk => Console.Write(chunk)
);

// Update UI progressively
await handler.ProcessStreamAsync(
    llmStream,
    chunk => 
    {
        textBox.Text += chunk;
        textBox.ScrollToEnd();
    }
);

// Testing with simulated stream
var stream = StreamingHandler.SimulateStreamAsync(
    "This is a test response",
    chunkSize: 5,
    delayMs: 100
);
await handler.ProcessStreamAsync(stream, chunk => Console.Write(chunk));
```

---

### LLMOrchestrator

High-level coordinator combining all components.

**When to use:** Building complete LLM applications, managing multiple features.

**Basic Usage**
```csharp
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    MaxConversationHistory = 10,
    RetryPolicy = new RetryPolicy { MaxRetries = 3 }
});
```

**Constructor**
```csharp
public LLMOrchestrator(OrchestratorConfig? config = null)
```

**Properties**
- `Memory` (ConversationMemory): Conversation memory instance
- `Tools` (ToolRegistry): Tool registry instance

**Methods**

`ExecutePrompt(PromptTemplate template, Dictionary<string, string> vars)` → `string`  
`ExecutePrompt(PromptTemplate template, object vars)` → `string`
- Format prompt template

`ExecuteChainAsync(Chain chain, string input, CancellationToken ct)` → `Task<ChainResult>`
- Execute chain with retry logic

`ExecuteChainStreamingAsync(Chain chain, string input, Action<string> onChunk, CancellationToken ct)` → `Task<ChainResult>`
- Execute chain with streaming

`CreateWorkflow()` → `WorkflowBuilder`
- Start building a workflow

**Examples**

```csharp
// Complete application setup
var orchestrator = new LLMOrchestrator(new OrchestratorConfig
{
    MaxConversationHistory = 20,
    RetryPolicy = new RetryPolicy { MaxRetries = 3 }
});

// Register tools
orchestrator.Tools.RegisterTool(new WeatherTool());
orchestrator.Tools.RegisterTool(new SearchTool());

// Set system context
orchestrator.Memory.AddSystemMessage("You are a helpful assistant.");

// Build workflow
var result = await orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate("Help with: {task}"))
    .WithVariables(new { task = userRequest })
    .AddStep(new LLMCallStep())
    .ExecuteAsync();
```

---

### WorkflowBuilder

Fluent API for building complex workflows.

**When to use:** Composing prompts, variables, and chain steps together.

**Methods**

`WithPrompt(PromptTemplate template)` → `WorkflowBuilder`
- Set the prompt template

`WithVariables(Dictionary<string, string> variables)` → `WorkflowBuilder`  
`WithVariables(object variables)` → `WorkflowBuilder` (inferred)
- Set template variables

`AddStep(IChainStep step)` → `WorkflowBuilder`
- Add processing step

`ExecuteAsync(string? initialInput, CancellationToken ct)` → `Task<ChainResult>`
- Execute the workflow
- Uses formatted prompt if no initialInput provided

**Examples**

```csharp
var orchestrator = new LLMOrchestrator();

// Complete workflow
var result = await orchestrator.CreateWorkflow()
    .WithPrompt(new PromptTemplate(@"
        Role: {role}
        Task: {task}
        Context: {context}
    "))
    .WithVariables(new
    {
        role = "code reviewer",
        task = "review this PR",
        context = prDetails
    })
    .AddStep(new LLMCallStep())
    .AddStep(new FormatMarkdownStep())
    .ExecuteAsync();

// Or provide input directly
var result = await orchestrator.CreateWorkflow()
    .AddStep(new ProcessingStep())
    .ExecuteAsync(directInput);
```

---

## Interfaces

### IChainStep

Implement this to create custom workflow steps.

```csharp
public interface IChainStep
{
    Task<ChainResult> ExecuteAsync(string input, CancellationToken ct = default);
}
```

**Example Implementation**

```csharp
public class OpenAIStep : IChainStep
{
    private readonly OpenAIClient _client;
    
    public OpenAIStep(OpenAIClient client)
    {
        _client = client;
    }
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        try
        {
            var response = await _client.GetCompletionAsync(input, ct);
            
            return new ChainResult
            {
                Output = response.Text,
                Success = true,
                Metadata = 
                {
                    ["tokens"] = response.Usage.TotalTokens,
                    ["model"] = response.Model
                }
            };
        }
        catch (Exception ex)
        {
            return new ChainResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

---

### ITool

Implement this to create LLM-callable tools.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(string input, CancellationToken ct = default);
}
```

**Prefer using `ToolBase`** abstract class for convenience:

```csharp
public class DatabaseQueryTool : ToolBase
{
    public override string Name => "query_database";
    public override string Description => "Query the database with SQL";
    
    public override async Task<string> ExecuteAsync(string sqlQuery, CancellationToken ct)
    {
        // Validate and execute
        var results = await _db.QueryAsync(sqlQuery, ct);
        return JsonSerializer.Serialize(results);
    }
}
```

---

## Configuration

### OrchestratorConfig

```csharp
public class OrchestratorConfig
{
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;
    public int MaxConversationHistory { get; set; } = 10;
    public bool EnableStreaming { get; set; } = false;
}
```

### RetryPolicy

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MaxDelayMs { get; set; } = 30000;
    public bool UseJitter { get; set; } = true;
}
```

**Preset Configurations**

```csharp
// Quick failure for user-facing
var userFacing = new RetryPolicy
{
    MaxRetries = 2,
    InitialDelayMs = 500,
    MaxDelayMs = 5000
};

// Resilient for background jobs
var background = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelayMs = 2000,
    MaxDelayMs = 60000,
    UseJitter = true
};

// No retries for testing
var noRetry = new RetryPolicy { MaxRetries = 0 };
```

---

## Models

### ChainResult

```csharp
public class ChainResult
{
    public string Output { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Message

```csharp
public class Message
{
    public string Role { get; set; }           // "user", "assistant", "system"
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

---

## Extension Points

### Creating Custom Chain Steps

Most common extension point - integrate with any LLM or service:

```csharp
public class MyCustomLLMStep : IChainStep
{
    private readonly MyLLMClient _client;
    
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        var response = await _client.CallAsync(input, ct);
        
        return new ChainResult
        {
            Output = response.Text,
            Success = true,
            Metadata = { ["custom_field"] = response.CustomData }
        };
    }
}
```

### Creating Tools

Give LLMs access to your application's capabilities:

```csharp
public class EmailTool : ToolBase
{
    public override string Name => "send_email";
    public override string Description => "Send an email. Input: to,subject,body";
    
    public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
    {
        var parts = input.Split(',');
        await EmailService.SendAsync(parts[0], parts[1], parts[2]);
        return "Email sent successfully";
    }
}
```

---

## See Also

- [Getting Started Guide](getting-started.md) - Step-by-step tutorial
- [Best Practices](best-practices.md) - Production tips
- [Examples](../examples/CogniChain.Examples/) - Working samples
