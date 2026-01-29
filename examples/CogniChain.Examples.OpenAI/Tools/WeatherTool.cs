using CogniChain;

namespace CogniChain.Examples.OpenAI.Tools;

/// <summary>
/// Tool for getting weather information.
/// </summary>
public class WeatherTool : ToolBase
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather for a city";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        // Simulated weather API response - replace with actual weather API
        var result = $"Weather in {input}: 72°F, Partly Cloudy";
        return Task.FromResult(result);
    }
}
