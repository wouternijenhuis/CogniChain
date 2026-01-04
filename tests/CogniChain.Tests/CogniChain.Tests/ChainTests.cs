namespace CogniChain.Tests;

public class ChainTests
{
    private class SimpleChainStep : IChainStep
    {
        private readonly Func<string, string> _transform;

        public SimpleChainStep(Func<string, string> transform)
        {
            _transform = transform;
        }

        public Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChainResult
            {
                Output = _transform(input),
                Success = true
            });
        }
    }

    [Fact]
    public async Task Chain_ExecutesStepsSequentially()
    {
        // Arrange
        var chain = Chain.Create()
            .AddStep(new SimpleChainStep(input => input.ToUpper()))
            .AddStep(new SimpleChainStep(input => $"[{input}]"));

        // Act
        var result = await chain.RunAsync("hello");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("[HELLO]", result.Output);
    }

    [Fact]
    public async Task Chain_MultipleSteps_PipesOutput()
    {
        // Arrange
        var chain = Chain.Create()
            .AddStep(new SimpleChainStep(input => input + " world"))
            .AddStep(new SimpleChainStep(input => input + "!"))
            .AddStep(new SimpleChainStep(input => input.ToUpper()));

        // Act
        var result = await chain.RunAsync("hello");

        // Assert
        Assert.Equal("HELLO WORLD!", result.Output);
    }

    [Fact]
    public async Task Chain_WithMetadata_MergesMetadata()
    {
        // Arrange
        var step = new TestStepWithMetadata();
        var chain = Chain.Create().AddStep(step);

        // Act
        var result = await chain.RunAsync("test");

        // Assert
        Assert.Contains("testKey", result.Metadata.Keys);
    }

    [Fact]
    public async Task Chain_Streaming_InvokesCallback()
    {
        // Arrange
        var chain = Chain.Create()
            .AddStep(new SimpleChainStep(input => input + "1"))
            .AddStep(new SimpleChainStep(input => input + "2"));
        var chunks = new List<string>();

        // Act
        var result = await chain.RunStreamingAsync("test", chunk => chunks.Add(chunk));

        // Assert
        Assert.Equal(2, chunks.Count);
        Assert.Equal("test1", chunks[0]);
        Assert.Equal("test12", chunks[1]);
    }

    private class TestStepWithMetadata : IChainStep
    {
        public Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChainResult
            {
                Output = input,
                Success = true,
                Metadata = new Dictionary<string, object> { ["testKey"] = "testValue" }
            });
        }
    }
}
