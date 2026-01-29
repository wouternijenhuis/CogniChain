using CogniChain.Examples.Azure.Tools;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates tool registration and execution.
/// </summary>
public class ToolIntegrationExample : IExample
{
    public string Name => "Tool Integration";
    public string Description => "Shows how to register and use tools with CogniChain";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var toolRegistry = new ToolRegistry();
        toolRegistry.RegisterTool(new AzureResourceTool());
        toolRegistry.RegisterTool(new CostEstimatorTool());

        Console.WriteLine("Available tools:");
        Console.WriteLine(toolRegistry.GetToolDescriptions());

        var resourceResult = await toolRegistry.ExecuteToolAsync("azure_resources", "list storage accounts");
        Console.WriteLine($"Azure resource tool result: {resourceResult}");
    }
}
