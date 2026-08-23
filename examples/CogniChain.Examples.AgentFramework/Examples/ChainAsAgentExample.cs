using CogniChain.Agents;
using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.AgentFramework.Examples;

/// <summary>
/// <c>Chain&lt;string, string&gt;.AsAIAgent</c> wraps a chain as an <see cref="Microsoft.Agents.AI.AIAgent"/>,
/// so it can take part in Agent Framework orchestration (sequential/concurrent/handoff workflows,
/// other agents calling it, etc.) alongside agents built any other way.
/// </summary>
public sealed class ChainAsAgentExample(IChatClient chatClient) : IExample
{
    public string Name => "Chain as an Agent";

    public string Description => "A CogniChain chain, wrapped as an AIAgent via AsAIAgent().";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // .Prompt() renders a template against the current value's properties, which doesn't fit a
        // plain string passthrough — so this step talks to context.Messages directly instead, the way
        // any custom Then step can. That also means the system message from WithSystemMessage below
        // still reaches the model, since it was seeded into context.Messages when the chain ran.
        var chain = Chain.Create<string>(chatClient)
            .WithSystemMessage("You are a terse release-notes writer. One sentence per answer.")
            .Then<string>(async (input, context, ct) =>
            {
                context.Messages.Add(new ChatMessage(ChatRole.User, input));
                var response = await context.ChatClient.GetResponseAsync(context.Messages, context.Options, ct);
                context.Messages.AddMessages(response);
                return response.Text;
            })
            .Build();

        var agent = chain.AsAIAgent("release-notes-writer", "Summarizes a change in one terse sentence.");

        var response = await agent.RunAsync("We added retry-with-jitter to the HTTP client.", cancellationToken: cancellationToken);

        Console.WriteLine(response.Text);
    }
}
