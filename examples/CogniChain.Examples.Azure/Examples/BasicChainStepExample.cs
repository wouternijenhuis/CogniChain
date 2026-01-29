using CogniChain.Examples.Azure.Steps;
using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates basic chain step execution with Azure OpenAI.
/// </summary>
public class BasicChainStepExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Basic Azure OpenAI Chain Step";
    public string Description => "Shows how to use CogniChain workflow with Azure OpenAI";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var orchestrator = new LLMOrchestrator(new OrchestratorConfig
        {
            MaxConversationHistory = 10,
            RetryPolicy = new RetryPolicy { MaxRetries = 3, InitialDelayMs = 1000, UseJitter = true }
        });

        orchestrator.Memory.AddSystemMessage("You are a helpful Azure cloud architect assistant.");

        var workflow = orchestrator.CreateWorkflow()
            .WithPrompt(new PromptTemplate("Explain {concept} for an Azure developer in 2-3 sentences."))
            .WithVariables(new Dictionary<string, string> { ["concept"] = "Azure Functions" })
            .AddStep(new AzureOpenAIStep(_chatClient));

        var result = await workflow.ExecuteAsync();
        Console.WriteLine($"Response: {result.Output}");
    }
}
