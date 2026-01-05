# CogniChain Architecture

## Overview

CogniChain is designed with modularity, extensibility, and ease of use in mind. The library follows SOLID principles and uses common design patterns like Builder, Strategy, and Template Method.

## Component Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   LLMOrchestrator                       │
│  High-level coordinator for all components              │
└───────────┬─────────────────────────────────────────────┘
            │
            ├──────────────────────────────────────────────┐
            │                                              │
┌───────────▼───────────┐  ┌──────────────┐  ┌───────────▼──────────┐
│  ConversationMemory   │  │  ToolRegistry│  │   RetryHandler       │
│  - Message storage    │  │  - Tool mgmt │  │   - Retry logic      │
│  - History mgmt       │  │  - Execution │  │   - Backoff          │
└───────────────────────┘  └──────────────┘  └──────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                     Chain System                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐             │
│  │  Step 1  │─▶│  Step 2  │─▶│  Step 3  │             │
│  └──────────┘  └──────────┘  └──────────┘             │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               Prompt Templates                          │
│  Variable substitution and formatting                   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│             Streaming Support                           │
│  Async enumerable streaming of responses               │
└─────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Prompt Templates

**Purpose**: Provide a flexible way to create parameterized prompts.

**Design**:
- Simple variable substitution using `{variableName}` syntax
- Automatic variable extraction from template strings
- Support for both dictionary and object-based variable passing

**Key Classes**:
- `PromptTemplate`: Main template class

### 2. Chain System

**Purpose**: Enable sequential execution of operations with data flow between steps.

**Design**:
- Interface-based extensibility (`IChainStep`)
- Sequential execution with output piping
- Metadata collection across steps
- Support for both regular and streaming execution

**Key Classes**:
- `Chain`: Orchestrator for chain execution
- `IChainStep`: Interface for implementing custom steps
- `ChainResult`: Result wrapper with success status and metadata

### 3. Memory Management

**Purpose**: Maintain conversation history with intelligent trimming.

**Design**:
- Configurable history size
- Role-based message categorization
- System message preservation
- Filtering and querying capabilities

**Key Classes**:
- `ConversationMemory`: Main memory manager
- `Message`: Individual message representation

### 4. Tool Framework

**Purpose**: Allow LLMs to call external functions and tools.

**Design**:
- Registry pattern for tool management
- Abstract base class for easy implementation
- Async execution support
- Automatic tool description generation

**Key Classes**:
- `ToolRegistry`: Central tool management
- `ITool` / `ToolBase`: Tool interface and base implementation

### 5. Retry Logic

**Purpose**: Handle transient failures with exponential backoff.

**Design**:
- Configurable retry policies
- Exponential backoff with jitter
- Max delay caps
- Generic execution wrapper

**Key Classes**:
- `RetryHandler`: Execution wrapper with retry logic
- `RetryPolicy`: Configuration for retry behavior
- `RetryException`: Exception wrapper for failed retries

### 6. Streaming

**Purpose**: Support real-time streaming of LLM responses.

**Design**:
- IAsyncEnumerable-based streaming
- Event-driven chunk notification
- Simulation support for testing

**Key Classes**:
- `StreamingHandler`: Stream processing
- `StreamingResponse`: Response aggregation with events

### 7. Orchestrator

**Purpose**: High-level API combining all components.

**Design**:
- Fluent builder pattern for workflows
- Integrated retry logic
- Unified configuration
- Composition of all subsystems

**Key Classes**:
- `LLMOrchestrator`: Main orchestration class
- `WorkflowBuilder`: Fluent workflow builder
- `OrchestratorConfig`: Central configuration

## Design Patterns

### Builder Pattern
Used in `Chain` and `WorkflowBuilder` for fluent API construction:

```csharp
var workflow = orchestrator.CreateWorkflow()
    .WithPrompt(template)
    .WithVariables(vars)
    .AddStep(step1)
    .AddStep(step2)
    .ExecuteAsync();
```

### Strategy Pattern
`IChainStep` and `ITool` allow pluggable behaviors:

```csharp
public interface IChainStep
{
    Task<ChainResult> ExecuteAsync(string input, CancellationToken ct);
}
```

### Template Method
`ToolBase` provides a template for tool implementation:

```csharp
public abstract class ToolBase : ITool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Task<string> ExecuteAsync(string input, CancellationToken ct);
}
```

### Decorator Pattern
`RetryHandler` decorates operations with retry logic:

```csharp
await retryHandler.ExecuteAsync(async () => await operation());
```

## Extension Points

### Custom Chain Steps
Implement `IChainStep` for custom processing logic:

```csharp
public class MyStep : IChainStep
{
    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken ct)
    {
        // Custom logic
        return new ChainResult { Output = result, Success = true };
    }
}
```

### Custom Tools
Extend `ToolBase` for new tool implementations:

```csharp
public class MyTool : ToolBase
{
    public override string Name => "MyTool";
    public override string Description => "My tool description";
    public override async Task<string> ExecuteAsync(string input, CancellationToken ct)
    {
        // Tool logic
        return result;
    }
}
```

## Threading and Async

- All I/O operations are async
- CancellationToken support throughout
- Thread-safe where applicable (ToolRegistry, ConversationMemory)
- No blocking calls in the hot path

## Error Handling

- Exceptions bubble up with context
- `ChainResult` provides success/failure indication
- `RetryException` wraps failures after max retries
- Cancellation is properly propagated

## Performance Considerations

- Lazy evaluation where possible
- Minimal allocations in hot paths
- Efficient string operations
- Streaming to reduce memory footprint
- Configurable buffer sizes and delays

## Future Extensibility

The architecture supports future additions:

- Parallel chain execution
- Conditional branching in chains
- Plugin system for tools
- Persistent memory backends
- Telemetry and observability hooks
- Rate limiting and throttling
- Caching layers
