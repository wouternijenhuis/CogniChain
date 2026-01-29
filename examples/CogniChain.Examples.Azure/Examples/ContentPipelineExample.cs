using CogniChain.Examples.Azure.Steps;
using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates content generation using chained steps.
/// </summary>
public class ContentPipelineExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Content Pipeline";
    public string Description => "Shows how to chain multiple AI steps for content generation";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create()
            .AddStep(new AzureOpenAIStep(_chatClient, "Generate a brief outline for a blog post about:"))
            .AddStep(new AzureOpenAIStep(_chatClient, "Expand this outline into a short introduction paragraph:"));

        var result = await chain.RunAsync("Best practices for securing Azure resources");
        Console.WriteLine($"Generated content:\n{result.Output}");
    }
}
