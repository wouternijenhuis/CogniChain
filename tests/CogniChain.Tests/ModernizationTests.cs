using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CogniChain.DependencyInjection;
using CogniChain.Diagnostics;
using CogniChain.Tests.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace CogniChain.Tests;

public sealed record WindReport(string City, int WindSpeed);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(WindReport))]
internal sealed partial class SnakeCaseJsonContext : JsonSerializerContext;

public class ModernizationTests
{
    [Fact]
    public async Task WithJsonSerializerOptions_PromptOfT_UsesSourceGeneratedOptionsForSchemaAndDeserialization()
    {
        // Arrange: snake_case only resolves through the supplied context, never through the default options.
        var client = new FakeChatClient();
        client.EnqueueResponse("""{"city":"Rotterdam","wind_speed":42}""");
        var serializerOptions = SnakeCaseJsonContext.Default.Options;
        var chain = Chain.Create(client)
            .WithJsonSerializerOptions(serializerOptions)
            .Prompt<WindReport>("Wind in {city}")
            .Build();

        // Act
        var result = await chain.RunAsync(new { city = "Rotterdam" });

        // Assert
        Assert.Equal(new WindReport("Rotterdam", 42), result.Value);
        var format = Assert.IsType<ChatResponseFormatJson>(client.RequestOptions[0]!.ResponseFormat);
        Assert.Contains("wind_speed", format.Schema!.Value.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_EmitsChainSpanWithStepSpansAsChildren()
    {
        // Arrange
        var chainName = $"telemetry-{Guid.NewGuid():N}";
        using var recorder = new ActivityRecorder(chainName);
        var chain = Chain.Create<string>(new FakeChatClient())
            .Then<string>(s => s + "1", name: "one")
            .Then<string>(s => s + "2", name: "two")
            .Build(chainName);

        // Act
        await chain.RunAsync("x");

        // Assert
        var chainSpan = Assert.Single(recorder.Activities, a => a.OperationName == $"cognichain.chain {chainName}");
        var steps = recorder.Activities.Where(a => a.OperationName.StartsWith("cognichain.step", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, steps.Count);
        Assert.All(steps, s => Assert.Equal(chainSpan.SpanId, s.ParentSpanId));
    }

    [Fact]
    public async Task RunStreamingAsync_EmitsStepSpansUnderTheChainSpan()
    {
        // Arrange
        var chainName = $"telemetry-{Guid.NewGuid():N}";
        using var recorder = new ActivityRecorder(chainName);
        var client = new FakeChatClient();
        client.EnqueueStreaming("a", "b");
        var chain = Chain.Create<string>(client)
            .Then<string>(s => s.ToUpperInvariant(), name: "upper")
            .Prompt("Echo", name: "echo")
            .Build(chainName);

        // Act
        await foreach (var _ in chain.RunStreamingAsync("x"))
        {
        }

        // Assert
        var chainSpan = Assert.Single(recorder.Activities, a => a.OperationName == $"cognichain.chain {chainName}");
        var steps = recorder.Activities.Where(a => a.OperationName.StartsWith("cognichain.step", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, steps.Count);
        Assert.All(steps, s => Assert.Equal(chainSpan.SpanId, s.ParentSpanId));
    }

    [Fact]
    public async Task RunAsync_StepFailure_RecordsErrorTypeAndExceptionEventOnSpans()
    {
        // Arrange
        var chainName = $"telemetry-{Guid.NewGuid():N}";
        using var recorder = new ActivityRecorder(chainName);
        var chain = Chain.Create<string>(new FakeChatClient())
            .Then<string>(_ => throw new InvalidOperationException("boom"), name: "fails")
            .Build(chainName);

        // Act
        await Assert.ThrowsAsync<ChainStepException>(() => chain.RunAsync("x"));

        // Assert
        var step = Assert.Single(recorder.Activities, a => a.OperationName == "cognichain.step fails");
        Assert.Equal(ActivityStatusCode.Error, step.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, step.GetTagItem("error.type"));
        Assert.Contains(step.Events, e => e.Name == "exception");
        var chainSpan = Assert.Single(recorder.Activities, a => a.OperationName == $"cognichain.chain {chainName}");
        Assert.Equal(ActivityStatusCode.Error, chainSpan.Status);
    }

    [Fact]
    public async Task AddChain_RegistersKeyedChainNamedAfterItsKey()
    {
        // Arrange
        var client = new FakeChatClient();
        var services = new ServiceCollection();
        services.AddSingleton<IChatClient>(client);
        services.AddChain<string, string>("shout", builder => builder.Then<string>(s => s.ToUpperInvariant()));
        using var provider = services.BuildServiceProvider();

        // Act
        var chain = provider.GetRequiredKeyedService<Chain<string, string>>("shout");
        var result = await chain.RunAsync("hi");

        // Assert
        Assert.Equal("shout", chain.Name);
        Assert.Equal("HI", result.Value);
        Assert.Same(chain, provider.GetRequiredKeyedService<Chain<string, string>>("shout"));
    }

    [Fact]
    public async Task AsAIFunction_InvokingTheFunctionRunsTheChain()
    {
        // Arrange
        var chain = Chain.Create<string>(new FakeChatClient()).Then<string>(s => s + "!").Build("exclaim");
        var function = chain.AsAIFunction(description: "Adds an exclamation mark.");

        // Act
        var result = await function.InvokeAsync(new AIFunctionArguments { ["input"] = "hi" });

        // Assert
        Assert.Equal("exclaim", function.Name);
        Assert.Equal("Adds an exclamation mark.", function.Description);
        Assert.Equal("hi!", Assert.IsType<JsonElement>(result).GetString());
    }

    [Fact]
    public async Task Branch_NestedChainWithoutReducer_InheritsTheParentReducer()
    {
        // Arrange: the branch has no reducer of its own, so the parent's two-message cap must apply to its model call.
        var client = new FakeChatClient();
        client.EnqueueResponse("reply");
        var branch = Chain.Create<string>(client).Prompt("Question").Build();
        var chain = Chain.Create<string>(client)
            .WithHistoryReducer(new MessageCountReducer(2))
            .Branch(_ => true, branch, branch)
            .Build();
        var context = chain.CreateContext();
        for (var i = 0; i < 6; i++)
        {
            context.Messages.Add(new ChatMessage(i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"m{i}"));
        }

        // Act
        await chain.RunAsync("x", context);

        // Assert
        Assert.Equal(2, client.Requests[0].Count);
        Assert.Equal("Question", client.Requests[0][^1].Text);
    }

    /// <summary>Collects stopped CogniChain activities belonging to one chain, so parallel tests don't interfere.</summary>
    private sealed class ActivityRecorder : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly ConcurrentQueue<Activity> _activities = new();

        public ActivityRecorder(string chainName)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ChainActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (Equals(activity.GetTagItem("cognichain.chain.name"), chainName))
                    {
                        _activities.Enqueue(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public IReadOnlyList<Activity> Activities => [.. _activities];

        public void Dispose() => _listener.Dispose();
    }
}
