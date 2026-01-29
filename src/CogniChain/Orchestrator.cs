namespace CogniChain;

/// <summary>
/// Configuration for the LLM orchestrator.
/// </summary>
public class OrchestratorConfig
{
    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;

    /// <summary>
    /// Gets or sets the maximum conversation history size.
    /// </summary>
    public int MaxConversationHistory { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to enable streaming by default.
    /// </summary>
    public bool EnableStreaming { get; set; } = false;
}

/// <summary>
/// High-level orchestrator for LLM workflows combining prompts, chains, tools, memory, and retry logic.
/// </summary>
public class LLMOrchestrator
{
    private readonly ConversationMemory _memory;
    private readonly ToolRegistry _toolRegistry;
    private readonly RetryHandler _retryHandler;
    private readonly OrchestratorConfig _config;

    /// <summary>
    /// Gets the conversation memory.
    /// </summary>
    public ConversationMemory Memory => _memory;

    /// <summary>
    /// Gets the tool registry.
    /// </summary>
    public ToolRegistry Tools => _toolRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMOrchestrator"/> class.
    /// </summary>
    /// <param name="config">The orchestrator configuration.</param>
    public LLMOrchestrator(OrchestratorConfig? config = null)
    {
        _config = config ?? new OrchestratorConfig();
        _memory = new ConversationMemory(_config.MaxConversationHistory);
        _toolRegistry = new ToolRegistry();
        _retryHandler = new RetryHandler(_config.RetryPolicy);
    }

    /// <summary>
    /// Executes a prompt template with the provided variables.
    /// </summary>
    /// <param name="template">The prompt template.</param>
    /// <param name="variables">The variables to substitute.</param>
    /// <returns>The formatted prompt.</returns>
    public string ExecutePrompt(PromptTemplate template, Dictionary<string, string> variables)
    {
        return template.Format(variables);
    }

    /// <summary>
    /// Executes a prompt template with the provided variables.
    /// </summary>
    /// <param name="template">The prompt template.</param>
    /// <param name="variables">The variables to substitute.</param>
    /// <returns>The formatted prompt.</returns>
    public string ExecutePrompt(PromptTemplate template, object variables)
    {
        return template.Format(variables);
    }

    /// <summary>
    /// Executes a chain with retry logic.
    /// </summary>
    /// <param name="chain">The chain to execute.</param>
    /// <param name="input">The initial input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the chain execution.</returns>
    public async Task<ChainResult> ExecuteChainAsync(
        Chain chain,
        string input,
        CancellationToken cancellationToken = default)
    {
        return await _retryHandler.ExecuteAsync(
            async () => await chain.RunAsync(input, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Executes a chain with streaming output.
    /// </summary>
    /// <param name="chain">The chain to execute.</param>
    /// <param name="input">The initial input.</param>
    /// <param name="onChunk">Callback for each output chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the chain execution.</returns>
    public async Task<ChainResult> ExecuteChainStreamingAsync(
        Chain chain,
        string input,
        Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        return await _retryHandler.ExecuteAsync(
            async () => await chain.RunStreamingAsync(input, onChunk, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Creates a fluent builder for constructing orchestrated workflows.
    /// </summary>
    /// <returns>A new <see cref="WorkflowBuilder"/> instance.</returns>
    public WorkflowBuilder CreateWorkflow()
    {
        return new WorkflowBuilder(this);
    }
}

/// <summary>
/// Fluent builder for constructing orchestrated workflows.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WorkflowBuilder"/> class.
/// </remarks>
/// <param name="orchestrator">The orchestrator instance.</param>
public class WorkflowBuilder(LLMOrchestrator orchestrator)
{
    private readonly LLMOrchestrator _orchestrator = orchestrator;
    private readonly Chain _chain = Chain.Create();
    private PromptTemplate? _template;
    private Dictionary<string, string>? _variables;

    /// <summary>
    /// Sets the prompt template for the workflow.
    /// </summary>
    /// <param name="template">The prompt template.</param>
    /// <returns>The workflow builder for fluent chaining.</returns>
    public WorkflowBuilder WithPrompt(PromptTemplate template)
    {
        _template = template;
        return this;
    }

    /// <summary>
    /// Sets the variables for the prompt template.
    /// </summary>
    /// <param name="variables">The variables dictionary.</param>
    /// <returns>The workflow builder for fluent chaining.</returns>
    public WorkflowBuilder WithVariables(Dictionary<string, string> variables)
    {
        _variables = variables;
        return this;
    }

    /// <summary>
    /// Adds a step to the workflow chain.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <returns>The workflow builder for fluent chaining.</returns>
    public WorkflowBuilder AddStep(IChainStep step)
    {
        _chain.AddStep(step);
        return this;
    }

    /// <summary>
    /// Executes the workflow.
    /// </summary>
    /// <param name="initialInput">The initial input (overrides prompt template if provided).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the workflow execution.</returns>
    public async Task<ChainResult> ExecuteAsync(
        string? initialInput = null,
        CancellationToken cancellationToken = default)
    {
        var input = initialInput;

        if (input == null && _template != null && _variables != null)
        {
            input = _orchestrator.ExecutePrompt(_template, _variables);
        }

        if (input == null)
        {
            throw new InvalidOperationException("No input provided. Set initialInput or configure a prompt template with variables.");
        }

        return await _orchestrator.ExecuteChainAsync(_chain, input, cancellationToken);
    }
}
