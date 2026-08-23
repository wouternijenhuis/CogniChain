using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>The smallest possible chain: one plain-text prompt step.</summary>
public sealed class BasicPromptExample(IChatClient chatClient) : IExample
{
    public string Name => "Basic Prompt";

    public string Description => "A single-step chain: render a template and get the model's text response.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .Prompt("Explain {concept} in one sentence for a C# developer.")
            .Build();

        var result = await chain.RunAsync(new { concept = "the Azure Well-Architected Framework" }, cancellationToken);

        Console.WriteLine(result.Value);
    }
}
