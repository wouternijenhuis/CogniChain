using System.Text;
using CogniChain.Tests.Fakes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CogniChain.Agents.Tests;

public class ChainAgentExtensionsTests
{
    [Fact]
    public async Task AsAIAgent_RunAsync_ReturnsChainOutputAndUsage()
    {
        // Arrange
        var client = new FakeChatClient();
        client.EnqueueResponse(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, "model answer"))
        {
            Usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 4, TotalTokenCount = 7 },
        });
        var agent = Chain.Create<string>(client).Prompt("Answer briefly").Build().AsAIAgent("answerer");

        // Act
        var response = await agent.RunAsync("question");

        // Assert
        Assert.Equal("answerer", agent.Name);
        Assert.Equal("model answer", response.Text);
        Assert.Equal(7, response.Usage?.TotalTokenCount);
    }

    [Fact]
    public async Task AsAIAgent_RunStreamingAsync_PromptTerminalStep_StreamsModelTokens()
    {
        // Arrange
        var client = new FakeChatClient();
        client.EnqueueStreaming("Hel", "lo");
        var agent = Chain.Create<string>(client).Prompt("Greet").Build().AsAIAgent("greeter");

        // Act
        var text = new StringBuilder();
        var updateCount = 0;
        await foreach (var update in agent.RunStreamingAsync("hi"))
        {
            text.Append(update.Text);
            updateCount++;
        }

        // Assert
        Assert.Equal("Hello", text.ToString());
        Assert.True(updateCount >= 2);
        Assert.Equal(1, client.StreamingCallCount);
    }

    [Fact]
    public async Task AsAIAgent_RunStreamingAsync_NonStreamableTerminalStep_EmitsTheChainOutput()
    {
        // Arrange: regression test — a chain ending in Then produced no streamed text at all.
        var agent = Chain.Create<string>(new FakeChatClient())
            .Then<string>(s => s.ToUpperInvariant())
            .Build()
            .AsAIAgent("shouter");

        // Act
        var text = new StringBuilder();
        await foreach (var update in agent.RunStreamingAsync("quiet"))
        {
            text.Append(update.Text);
        }

        // Assert
        Assert.Equal("QUIET", text.ToString());
    }

    [Fact]
    public async Task AsChainStep_RunsTheAgentAndAddsItsUsageToTheChain()
    {
        // Arrange: regression test — the agent's token usage was dropped from the chain's totals.
        var agentClient = new FakeChatClient();
        agentClient.EnqueueResponse(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, "agent says hi"))
        {
            Usage = new UsageDetails { TotalTokenCount = 11 },
        });
        AIAgent agent = new ChatClientAgent(agentClient, instructions: "Be friendly.", name: "friend");
        var chain = Chain.Create<string>(new FakeChatClient())
            .Then(agent.AsChainStep())
            .Build();

        // Act
        var result = await chain.RunAsync("hello");

        // Assert
        Assert.Equal("agent says hi", result.Value);
        Assert.Equal(11, result.Usage?.TotalTokenCount);
        Assert.Contains(agentClient.Requests[0], m => m.Role == ChatRole.User && m.Text == "hello");
    }
}
