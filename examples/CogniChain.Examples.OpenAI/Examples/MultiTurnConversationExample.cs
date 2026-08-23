using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>Reusing a <see cref="ChainContext"/> across <c>RunAsync</c> calls carries conversation history forward.</summary>
public sealed class MultiTurnConversationExample(IChatClient chatClient) : IExample
{
    public string Name => "Multi-Turn Conversation";

    public string Description => "The same ChainContext is passed to every RunAsync call, so the model sees prior turns.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .WithSystemMessage("You are a concise C# mentor. Keep every answer to two sentences.")
            .Prompt("{message}")
            .Build();

        var context = chain.CreateContext();

        foreach (var message in new[] { "What is a record type?", "How is that different from a class?" })
        {
            var result = await chain.RunAsync(new { message }, context, cancellationToken);
            Console.WriteLine($"> {message}");
            Console.WriteLine(result.Value);
            Console.WriteLine();
        }
    }
}
