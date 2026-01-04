namespace CogniChain.Tests;

public class ToolsTests
{
    private class TestTool : ToolBase
    {
        public override string Name => "TestTool";
        public override string Description => "A test tool";

        public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Processed: {input}");
        }
    }

    [Fact]
    public void RegisterTool_AddsTool()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool = new TestTool();

        // Act
        registry.RegisterTool(tool);

        // Assert
        var retrieved = registry.GetTool("TestTool");
        Assert.NotNull(retrieved);
        Assert.Equal("TestTool", retrieved.Name);
    }

    [Fact]
    public void GetTool_NonExistent_ReturnsNull()
    {
        // Arrange
        var registry = new ToolRegistry();

        // Act
        var tool = registry.GetTool("NonExistent");

        // Assert
        Assert.Null(tool);
    }

    [Fact]
    public async Task ExecuteToolAsync_ExecutesTool()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool());

        // Act
        var result = await registry.ExecuteToolAsync("TestTool", "test input");

        // Assert
        Assert.Equal("Processed: test input", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_NonExistent_ThrowsException()
    {
        // Arrange
        var registry = new ToolRegistry();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await registry.ExecuteToolAsync("NonExistent", "input"));
    }

    [Fact]
    public void GetAllTools_ReturnsAllRegistered()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool());

        // Act
        var tools = registry.GetAllTools().ToList();

        // Assert
        Assert.Single(tools);
    }

    [Fact]
    public void GetToolDescriptions_ReturnsFormattedList()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool());

        // Act
        var descriptions = registry.GetToolDescriptions();

        // Assert
        Assert.Contains("TestTool", descriptions);
        Assert.Contains("A test tool", descriptions);
    }
}
