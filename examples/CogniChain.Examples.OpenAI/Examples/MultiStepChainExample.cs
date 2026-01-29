using CogniChain.Examples.OpenAI.Steps;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// Demonstrates multi-step chain execution.
/// </summary>
public class MultiStepChainExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Multi-Step Chain";
    public string Description => "Shows how to chain multiple AI steps together";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create()
            .AddStep(new OpenAIStep(_chatClient, "Summarize this text in one sentence:"))
            .AddStep(new OpenAIStep(_chatClient, "Translate the following to French:"));

        var result = await chain.RunAsync(
            "CogniChain is a .NET library that helps developers build LLM-powered applications " +
            "with features like prompt templates, conversation memory, and tool integration.");

        Console.WriteLine($"Final output: {result.Output}");
    }
}
