using CogniChain.Examples.OpenAI.Tools;

namespace CogniChain.Examples.OpenAI.Examples;

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
        toolRegistry.RegisterTool(new WeatherTool());
        toolRegistry.RegisterTool(new CalculatorTool());

        Console.WriteLine("Available tools:");
        Console.WriteLine(toolRegistry.GetToolDescriptions());

        var weatherResult = await toolRegistry.ExecuteToolAsync("get_weather", "Seattle");
        Console.WriteLine($"Weather tool result: {weatherResult}");
    }
}
