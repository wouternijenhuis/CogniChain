using Microsoft.Agents.AI;

namespace CogniChain.Agents;

/// <summary>Adapts an <see cref="AIAgent"/> into a chain step: each call is a fresh, session-less turn.</summary>
internal sealed class AgentChainStep(AIAgent agent) : IChainStep<string, string>
{
    private readonly AIAgent _agent = agent;

    public async ValueTask<string> ExecuteAsync(string input, ChainContext context, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync(input, session: null, options: null, cancellationToken).ConfigureAwait(false);
        return response.Text;
    }
}
