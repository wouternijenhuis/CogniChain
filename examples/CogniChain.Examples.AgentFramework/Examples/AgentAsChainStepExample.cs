using CogniChain.Agents;
using CogniChain.Examples.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.AgentFramework.Examples;

/// <summary>An existing <see cref="AIAgent"/> — built any way, including by another framework or team — dropped into a chain via <c>AsChainStep()</c>.</summary>
public sealed class AgentAsChainStepExample(IChatClient chatClient) : IExample
{
    public string Name => "Agent as a Chain Step";

    public string Description => "A ChatClientAgent used as an ordinary Then step inside a chain.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AIAgent reviewer = new ChatClientAgent(
            chatClient,
            instructions: "You review a one-line commit message. Reply 'approve' or a one-sentence revision request.",
            name: "commit-reviewer");

        var chain = Chain.Create<string>(chatClient)
            .Then<string>(reviewer.AsChainStep())
            .Build();

        var result = await chain.RunAsync("fix bug", cancellationToken);

        Console.WriteLine(result.Value);
    }
}
