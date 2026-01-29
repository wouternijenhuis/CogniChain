using CogniChain.Examples.OpenAI.Steps;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// Demonstrates basic chain step execution with OpenAI.
/// </summary>
public class BasicChainStepExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Basic OpenAI Chain Step";
    public string Description => "Shows how to use CogniChain workflow with OpenAI";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var orchestrator = new LLMOrchestrator(new OrchestratorConfig
        {
            MaxConversationHistory = 10,
            RetryPolicy = new RetryPolicy { MaxRetries = 3, InitialDelayMs = 1000, UseJitter = true }
        });

        orchestrator.Memory.AddSystemMessage("You are a helpful coding assistant specializing in C# and .NET.");

        var workflow = orchestrator.CreateWorkflow()
            .WithPrompt(new PromptTemplate("Explain {concept} in 2-3 sentences for a C# developer."))
            .WithVariables(new Dictionary<string, string> { ["concept"] = "dependency injection" })
            .AddStep(new OpenAIStep(_chatClient));

        var result = await workflow.ExecuteAsync();
        Console.WriteLine($"Response: {result.Output}");
    }
}
