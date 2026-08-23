using CogniChain.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace CogniChain.Tests;

public class ChainTests
{
    [Fact]
    public async Task RunAsync_SequentialThenSteps_PipesOutputThroughEachStep()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<string>(client)
            .Then<string>(input => input.ToUpperInvariant())
            .Then<string>(input => $"[{input}]")
            .Build();

        // Act
        var result = await chain.RunAsync("hello");

        // Assert
        Assert.Equal("[HELLO]", result.Value);
    }

    [Fact]
    public async Task RunAsync_AsyncThenStep_ReceivesChainContext()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<string>(client)
            .Then<string>(async (input, context, ct) =>
            {
                context.Items["seen"] = input;
                await Task.Yield();
                return input + "!";
            })
            .Build();

        // Act
        var result = await chain.RunAsync("hi");

        // Assert
        Assert.Equal("hi!", result.Value);
        Assert.Equal("hi", result.Items["seen"]);
    }

    [Fact]
    public async Task RunAsync_StepThrows_WrapsInChainStepExceptionWithNameAndIndex()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<string>(client)
            .Then<string>(input => input, name: "first")
            .Then<string>(_ => throw new InvalidOperationException("boom"), name: "second")
            .Build();

        // Act
        var exception = await Assert.ThrowsAsync<ChainStepException>(() => chain.RunAsync("x"));

        // Assert
        Assert.Equal("second", exception.StepName);
        Assert.Equal(1, exception.StepIndex);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task RunAsync_StepThrowsOperationCanceled_PropagatesUnwrapped()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<string>(client)
            .Then<string>((string _, ChainContext _, CancellationToken _) => throw new OperationCanceledException())
            .Build();

        // Act & Assert: cancellation is never wrapped in a ChainStepException.
        await Assert.ThrowsAsync<OperationCanceledException>(() => chain.RunAsync("x"));
    }

    [Fact]
    public async Task Map_RunsOverElementsConcurrentlyAndPreservesOrder()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<IEnumerable<int>>(client)
            .Map<int, int>(async (n, _, ct) =>
            {
                await Task.Delay(5, ct);
                return n * 2;
            }, maxConcurrency: 3)
            .Build();

        // Act
        var result = await chain.RunAsync([1, 2, 3, 4]);

        // Assert
        Assert.Equal([2, 4, 6, 8], result.Value);
    }

    [Fact]
    public async Task Map_CurrentValueNotEnumerable_WrapsInvalidOperationExceptionInChainStepException()
    {
        // Arrange: TCurrent is object at the type level, but Map's compatibility with the runtime value
        // is only checked when the chain actually runs — like any step failure, it surfaces as a
        // ChainStepException wrapping the original InvalidOperationException.
        var client = new FakeChatClient();
        var chain = Chain.Create<object>(client)
            .Map<int, int>((n, _, _) => ValueTask.FromResult(n))
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ChainStepException>(() => chain.RunAsync(new object()));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task Branch_PredicateTrue_RunsWhenTrueChain()
    {
        // Arrange
        var client = new FakeChatClient();
        var whenTrue = Chain.Create<int>(client).Then<string>(n => $"even:{n}").Build();
        var whenFalse = Chain.Create<int>(client).Then<string>(n => $"odd:{n}").Build();
        var chain = Chain.Create<int>(client).Branch(n => n % 2 == 0, whenTrue, whenFalse).Build();

        // Act
        var result = await chain.RunAsync(4);

        // Assert
        Assert.Equal("even:4", result.Value);
    }

    [Fact]
    public async Task Branch_PredicateFalse_RunsWhenFalseChain()
    {
        // Arrange
        var client = new FakeChatClient();
        var whenTrue = Chain.Create<int>(client).Then<string>(n => $"even:{n}").Build();
        var whenFalse = Chain.Create<int>(client).Then<string>(n => $"odd:{n}").Build();
        var chain = Chain.Create<int>(client).Branch(n => n % 2 == 0, whenTrue, whenFalse).Build();

        // Act
        var result = await chain.RunAsync(3);

        // Assert
        Assert.Equal("odd:3", result.Value);
    }

    [Fact]
    public async Task Prompt_SendsRenderedTemplateAndReturnsResponseText()
    {
        // Arrange
        var client = new FakeChatClient();
        client.EnqueueResponse("Bonjour");
        var chain = Chain.Create(client).Prompt("Translate '{text}' to French").Build();

        // Act
        var result = await chain.RunAsync(new { text = "Hello" });

        // Assert
        Assert.Equal("Bonjour", result.Value);
        var sentMessage = Assert.Single(client.Requests[0]);
        Assert.Equal("Translate 'Hello' to French", sentMessage.Text);
    }

    [Fact]
    public async Task Prompt_WithSystemMessage_SendsItAheadOfTheUserMessage()
    {
        // Arrange: regression test for the old LLMOrchestrator, whose configured system message never
        // reached the model.
        var client = new FakeChatClient();
        client.EnqueueResponse("ok");
        var chain = Chain.Create(client)
            .WithSystemMessage("You are a terse assistant.")
            .Prompt("Say hi")
            .Build();

        // Act
        await chain.RunAsync(new { });

        // Assert
        var sent = client.Requests[0];
        Assert.Equal(2, sent.Count);
        Assert.Equal(ChatRole.System, sent[0].Role);
        Assert.Equal("You are a terse assistant.", sent[0].Text);
        Assert.Equal(ChatRole.User, sent[1].Role);
    }

    [Fact]
    public async Task PromptOfT_DeserializesJsonResponseAsStructuredOutput()
    {
        // Arrange
        var client = new FakeChatClient();
        client.EnqueueResponse("""{"City":"Paris","Population":2100000}""");
        var chain = Chain.Create(client).Prompt<CityFact>("Facts about {city}").Build();

        // Act
        var result = await chain.RunAsync(new { city = "Paris" });

        // Assert
        Assert.Equal("Paris", result.Value.City);
        Assert.Equal(2100000, result.Value.Population);
    }

    [Fact]
    public async Task WithTools_PopulatesChatOptionsToolsForPromptSteps()
    {
        // Arrange
        var client = new FakeChatClient();
        client.EnqueueResponse("ok");
        var tool = AIFunctionFactory.Create(() => "42", name: "answer");
        var chain = Chain.Create(client).WithTools(tool).Prompt("hi").Build();

        // Act
        await chain.RunAsync(new { });

        // Assert
        var tools = Assert.Single(client.RequestOptions)!.Tools;
        Assert.Contains(tools!, t => t.Name == "answer");
    }

    [Fact]
    public async Task RunAsync_WithSharedContext_CarriesConversationHistoryAcrossCalls()
    {
        // Arrange: regression test — reusing a ChainContext must carry Messages forward, unlike the old
        // LLMOrchestrator.Memory, which was never read during execution.
        var client = new FakeChatClient();
        client.EnqueueResponse("First reply");
        client.EnqueueResponse("Second reply");
        var chain = Chain.Create(client).Prompt("{message}").Build();
        var context = chain.CreateContext();

        // Act
        await chain.RunAsync(new { message = "Hello" }, context);
        await chain.RunAsync(new { message = "How are you?" }, context);

        // Assert: the second call's request includes the full prior turn plus the new user message.
        var secondRequest = client.Requests[1];
        Assert.Equal(3, secondRequest.Count);
        Assert.Equal("Hello", secondRequest[0].Text);
        Assert.Equal("First reply", secondRequest[1].Text);
        Assert.Equal("How are you?", secondRequest[2].Text);
    }

    [Fact]
    public async Task RunStreamingAsync_PlainTextTerminalStep_StreamsIndividualTokenUpdates()
    {
        // Arrange: regression test — the old Chain.RunStreamingAsync fired its callback once per whole
        // step, not once per token.
        var client = new FakeChatClient();
        client.EnqueueStreaming("Hel", "lo", " world");
        var chain = Chain.Create(client).Prompt("hi").Build();

        // Act
        var updates = new List<string>();
        await foreach (var update in chain.RunStreamingAsync(new { }))
        {
            if (!update.IsStepComplete)
            {
                updates.Add(update.Text);
            }
        }

        // Assert
        Assert.Equal(["Hel", "lo", " world"], updates);
    }

    [Fact]
    public async Task RunStreamingAsync_NonStreamableTerminalStep_YieldsSingleCompletionUpdate()
    {
        // Arrange
        var client = new FakeChatClient();
        var chain = Chain.Create<string>(client).Then<string>(input => input.ToUpperInvariant()).Build();

        // Act
        var updates = new List<ChainUpdate>();
        await foreach (var streamed in chain.RunStreamingAsync("hi"))
        {
            updates.Add(streamed);
        }

        // Assert
        var update = Assert.Single(updates);
        Assert.True(update.IsStepComplete);
    }

    private sealed record CityFact(string City, int Population);
}
