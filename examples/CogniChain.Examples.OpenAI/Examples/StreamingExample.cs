using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary><c>RunStreamingAsync</c> streams the model's response token-by-token.</summary>
public sealed class StreamingExample(IChatClient chatClient) : IExample
{
    public string Name => "Streaming";

    public string Description => "Tokens print as they arrive, instead of waiting for the full response.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient).Prompt("Write a two-line haiku about .NET 10.").Build();

        await foreach (var update in chain.RunStreamingAsync(new { }, cancellationToken))
        {
            if (!update.IsStepComplete)
            {
                Console.Write(update.Text);
            }
        }

        Console.WriteLine();
    }
}
