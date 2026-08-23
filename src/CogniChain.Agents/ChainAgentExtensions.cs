using Microsoft.Agents.AI;

namespace CogniChain.Agents;

/// <summary>Bridges CogniChain chains and the Microsoft Agent Framework.</summary>
public static class ChainAgentExtensions
{
    /// <summary>
    /// Wraps a text-in/text-out <see cref="Chain{TIn, TOut}"/> as an <see cref="AIAgent"/>, so it can
    /// take part in Agent Framework workflows (sequential/concurrent/handoff orchestration, MCP
    /// exposure, etc.) alongside other agents.
    /// </summary>
    /// <param name="chain">The chain to wrap. Its own configuration — system messages, tools, retry,
    /// telemetry — applies on every agent turn, since each turn runs the chain via a fresh <see cref="ChainContext"/>.</param>
    /// <param name="name">The agent's name.</param>
    /// <param name="description">The agent's description, shown to orchestrators and other agents.</param>
    public static AIAgent AsAIAgent(this Chain<string, string> chain, string name, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ChatClientAgent(new ChainChatClient(chain), instructions: null, name: name, description: description, tools: null, loggerFactory: null, services: null);
    }

    /// <summary>
    /// Wraps an <see cref="AIAgent"/> as a chain step, so an existing agent — including one backed by
    /// another framework or a hosted service — can appear anywhere in a chain via
    /// <c>ChainBuilder&lt;TIn, TCurrent&gt;.Then</c>. Each call is a fresh, session-less turn; for a
    /// multi-turn conversation with the agent, create an <see cref="AgentSession"/> yourself and call
    /// the agent directly instead.
    /// </summary>
    public static IChainStep<string, string> AsChainStep(this AIAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return new AgentChainStep(agent);
    }
}
