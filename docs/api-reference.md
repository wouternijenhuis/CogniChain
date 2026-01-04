# CogniChain API Reference

## Namespaces

- [CogniChain](#cognichain-namespace)

## CogniChain Namespace

### Core Classes

#### PromptTemplate

A template system for creating parameterized prompts with variable substitution.

**Constructor**
```csharp
public PromptTemplate(string template)
```

**Properties**
- `Template` (string): The raw template string
- `Variables` (IReadOnlyList<string>): List of variables in the template

**Methods**
- `Format(Dictionary<string, string> variables)`: Formats the template with a dictionary
- `Format(object variables)`: Formats the template with an anonymous object
- `FromString(string template)`: Static factory method

**Example**
```csharp
var template = new PromptTemplate("Hello {name}!");
var result = template.Format(new { name = "World" });
```

---

#### Chain

Orchestrates sequential execution of chain steps.

**Methods**
- `AddStep(IChainStep step)`: Adds a step to the chain
- `RunAsync(string input, CancellationToken ct)`: Executes the chain
- `RunStreamingAsync(string input, Action<string> onChunk, CancellationToken ct)`: Executes with streaming
- `Create()`: Static factory method

**Example**
```csharp
var chain = Chain.Create()
    .AddStep(new MyStep())
    .AddStep(new AnotherStep());
var result = await chain.RunAsync("input");
```

---

#### ConversationMemory

Manages conversation history with configurable size limits.

**Constructor**
```csharp
public ConversationMemory(int maxMessages = -1)
```

**Properties**
- `Messages` (IReadOnlyList<Message>): All messages in the conversation

**Methods**
- `AddMessage(string role, string content)`: Adds a message
- `AddUserMessage(string content)`: Adds a user message
- `AddAssistantMessage(string content)`: Adds an assistant message
- `AddSystemMessage(string content)`: Adds a system message
- `GetFormattedHistory()`: Returns formatted conversation history
- `Clear()`: Clears all messages
- `GetLastMessages(int count)`: Gets the last N messages
- `GetMessagesByRole(string role)`: Filters messages by role

---

#### ToolRegistry

Manages a collection of tools available to the LLM.

**Methods**
- `RegisterTool(ITool tool)`: Registers a tool
- `GetTool(string name)`: Retrieves a tool by name
- `GetAllTools()`: Returns all registered tools
- `ExecuteToolAsync(string name, string input, CancellationToken ct)`: Executes a tool
- `GetToolDescriptions()`: Returns formatted tool descriptions

---

#### RetryHandler

Provides retry logic with exponential backoff.

**Constructor**
```csharp
public RetryHandler(RetryPolicy? policy = null)
```

**Methods**
- `ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)`: Executes with retry logic
- `ExecuteAsync(Func<Task> operation, CancellationToken ct)`: Executes void operation with retry

---

#### StreamingHandler

Handles streaming operations for LLM responses.

**Methods**
- `ProcessStreamAsync(IAsyncEnumerable<string> source, Action<string>? onChunk, CancellationToken ct)`: Processes a stream
- `SimulateStreamAsync(string content, int chunkSize, int delayMs, CancellationToken ct)`: Simulates streaming (static)

---

#### LLMOrchestrator

High-level orchestrator combining all features.

**Constructor**
```csharp
public LLMOrchestrator(OrchestratorConfig? config = null)
```

**Properties**
- `Memory` (ConversationMemory): The conversation memory
- `Tools` (ToolRegistry): The tool registry

**Methods**
- `ExecutePrompt(PromptTemplate template, Dictionary<string, string> variables)`: Executes a prompt template
- `ExecutePrompt(PromptTemplate template, object variables)`: Executes a prompt template
- `ExecuteChainAsync(Chain chain, string input, CancellationToken ct)`: Executes a chain with retry
- `ExecuteChainStreamingAsync(Chain chain, string input, Action<string> onChunk, CancellationToken ct)`: Executes with streaming
- `CreateWorkflow()`: Creates a workflow builder

---

### Interfaces

#### IChainStep

Represents a step in a chain.

```csharp
public interface IChainStep
{
    Task<ChainResult> ExecuteAsync(string input, CancellationToken ct = default);
}
```

#### ITool

Represents a tool that can be called.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(string input, CancellationToken ct = default);
}
```

---

### Models

#### ChainResult

Result of a chain execution.

**Properties**
- `Output` (string): The output text
- `Metadata` (Dictionary<string, object>): Additional metadata
- `Success` (bool): Whether execution succeeded
- `ErrorMessage` (string?): Error message if failed

#### Message

Represents a conversation message.

**Properties**
- `Role` (string): The role (user, assistant, system)
- `Content` (string): The message content
- `Timestamp` (DateTime): When the message was created
- `Metadata` (Dictionary<string, object>): Additional metadata

#### RetryPolicy

Configuration for retry behavior.

**Properties**
- `MaxRetries` (int): Maximum retry attempts (default: 3)
- `InitialDelayMs` (int): Initial delay in milliseconds (default: 1000)
- `BackoffMultiplier` (double): Exponential backoff multiplier (default: 2.0)
- `MaxDelayMs` (int): Maximum delay in milliseconds (default: 30000)
- `UseJitter` (bool): Whether to add jitter (default: true)

#### OrchestratorConfig

Configuration for the orchestrator.

**Properties**
- `RetryPolicy` (RetryPolicy): The retry policy
- `MaxConversationHistory` (int): Maximum conversation history size (default: 10)
- `EnableStreaming` (bool): Enable streaming by default (default: false)

---

## Extension Points

### Creating Custom Chain Steps

Implement `IChainStep`:

```csharp
public class MyCustomStep : IChainStep
{
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        // Your logic here
        return new ChainResult
        {
            Output = processedInput,
            Success = true,
            Metadata = new Dictionary<string, object> { ["key"] = "value" }
        };
    }
}
```

### Creating Custom Tools

Extend `ToolBase`:

```csharp
public class MyTool : ToolBase
{
    public override string Name => "MyTool";
    public override string Description => "Does something useful";
    
    public override async Task<string> ExecuteAsync(string input, CancellationToken ct = default)
    {
        // Your tool logic here
        return result;
    }
}
```
