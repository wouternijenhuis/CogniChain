using CogniChain.Examples.SemanticKernel.Steps;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates basic chain step execution with Semantic Kernel.
/// </summary>
public class BasicChainStepExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Basic Semantic Kernel Chain Step";
    public string Description => "Shows how to use CogniChain workflow with Semantic Kernel";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var orchestrator = new LLMOrchestrator(new OrchestratorConfig
        {
            MaxConversationHistory = 10,
            RetryPolicy = new RetryPolicy { MaxRetries = 3, InitialDelayMs = 1000, UseJitter = true }
        });

        orchestrator.Memory.AddSystemMessage("You are a helpful AI assistant powered by Semantic Kernel.");

        var workflow = orchestrator.CreateWorkflow()
            .WithPrompt(new PromptTemplate("Explain {concept} in simple terms for a developer."))
            .WithVariables(new Dictionary<string, string> { ["concept"] = "RAG (Retrieval Augmented Generation)" })
            .AddStep(new SemanticKernelStep(_kernel));

        var result = await workflow.ExecuteAsync();
        Console.WriteLine($"Response: {result.Output}");
    }
}
