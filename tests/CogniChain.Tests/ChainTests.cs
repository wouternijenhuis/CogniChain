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
    public async Task Branch_NestedChain_UsesItsOwnChatClientToolsAndSystemMessage()
    {
        // Arrange: regression test — a Branch sub-chain must run against its own configured chat
        // client, tools, and system message, not silently against the parent's.
        var outerClient = new FakeChatClient();
        var innerClient = new FakeChatClient();
        innerClient.EnqueueResponse("branch response");
        var tool = AIFunctionFactory.Create(() => "42", name: "answer");

        var whenTrue = Chain.Create<int>(innerClient)
            .WithSystemMessage("Branch-specific instructions.")
            .WithTools(tool)
            .Then<string>(async (n, context, ct) =>
            {
                context.Messages.Add(new ChatMessage(ChatRole.User, n.ToString()));
                var response = await context.ChatClient.GetResponseAsync(context.Messages, context.Options, ct);
                return response.Text;
            })
            .Build();
        var whenFalse = Chain.Create<int>(innerClient).Then<string>(_ => "unused").Build();
        var chain = Chain.Create<int>(outerClient).Branch(n => n % 2 == 0, whenTrue, whenFalse).Build();

        // Act
        var result = await chain.RunAsync(4);

        // Assert
        Assert.Equal("branch response", result.Value);
        Assert.Equal(0, outerClient.CallCount);
        Assert.Equal(1, innerClient.CallCount);
        Assert.Contains(innerClient.Requests[0], m => m.Role == ChatRole.System && m.Text == "Branch-specific instructions.");
        Assert.Contains(innerClient.RequestOptions[0]!.Tools!, t => t.Name == "answer");
    }

    [Fact]
    public async Task Branch_NestedChainStepFailure_IsNotDoubleWrappedInChainStepException()
    {
        // Arrange: regression test — a failure inside a Branch sub-chain's own step must surface with
        // the ORIGINAL exception as InnerException, not a ChainStepException wrapping a ChainStepException.
        var client = new FakeChatClient();
        var whenTrue = Chain.Create<int>(client)
            .Then<string>(_ => throw new InvalidOperationException("boom"), name: "inner-step")
            .Build();
        var whenFalse = Chain.Create<int>(client).Then<string>(_ => "unused").Build();
        var chain = Chain.Create<int>(client).Branch(_ => true, whenTrue, whenFalse, name: "the-branch").Build();

        // Act
        var exception = await Assert.ThrowsAsync<ChainStepException>(() => chain.RunAsync(1));

        // Assert: identifies the actual failing step inside the sub-chain, with the original exception
        // directly as InnerException.
        Assert.Equal("inner-step", exception.StepName);
        Assert.Equal(0, exception.StepIndex);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("boom", exception.InnerException!.Message);
    }

    [Fact]
    public void WithTools_DuplicateToolName_ThrowsInvalidOperationException()
    {
        // Arrange: regression test — the deleted ToolRegistry rejected duplicate tool names; WithTools
        // must too, rather than silently sending the model a ChatOptions.Tools list with duplicates.
        var client = new FakeChatClient();
        var toolA = AIFunctionFactory.Create(() => "1", name: "shared");
        var toolB = AIFunctionFactory.Create(() => "2", name: "shared");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Chain.Create(client).WithTools(toolA).WithTools(toolB));
    }

    [Fact]
    public async Task WithToolsFrom_RecordType_ExcludesSynthesizedMembers()
    {
        // Arrange: regression test — record-synthesized ToString/Equals/GetHashCode/Deconstruct must
        // not become model-callable tools alongside a record's real business methods.
        var client = new FakeChatClient();
        client.EnqueueResponse("ok");
        var chain = Chain.Create(client).WithToolsFrom(new CalculatorService()).Prompt("hi").Build();

        // Act
        await chain.RunAsync(new { });

        // Assert
        var toolNames = Assert.Single(client.RequestOptions)!.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("Add", toolNames);
        Assert.DoesNotContain("ToString", toolNames);
        Assert.DoesNotContain("Equals", toolNames);
        Assert.DoesNotContain("GetHashCode", toolNames);
        Assert.DoesNotContain("Deconstruct", toolNames);
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
    public async Task RunStreamingAsync_CancelledMidStream_PersistsPartialResponseToContextMessages()
    {
        // Arrange: regression test — cancelling mid-stream (or otherwise not draining it fully) must
        // not leave the just-added user message orphaned with no matching assistant reply.
        var client = new FakeChatClient();
        client.EnqueueStreaming("a", "b", "c");
        var chain = Chain.Create(client).Prompt("hi").Build();
        var context = chain.CreateContext();
        using var cts = new CancellationTokenSource();

        // Act
        var received = new List<string>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var update in chain.RunStreamingAsync(new { }, context, cts.Token))
            {
                if (update.IsStepComplete)
                {
                    continue;
                }

                received.Add(update.Text);
                if (received.Count == 2)
                {
                    cts.Cancel();
                }
            }
        });

        // Assert
        Assert.Equal(ChatRole.User, context.Messages[0].Role);
        Assert.Equal(ChatRole.Assistant, context.Messages[1].Role);
        Assert.Equal("ab", context.Messages[1].Text);
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

    private sealed record CalculatorService
    {
        [System.ComponentModel.Description("Adds two numbers.")]
        public int Add(int a, int b) => a + b;
    }
}
