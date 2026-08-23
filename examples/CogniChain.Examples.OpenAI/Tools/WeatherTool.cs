using System.ComponentModel;

namespace CogniChain.Examples.OpenAI.Tools;

/// <summary>A simulated weather lookup, wired up via <c>WithToolsFrom(new WeatherTool())</c>.</summary>
public sealed class WeatherTool
{
    [Description("Gets the current simulated weather for a city.")]
    public string GetWeather([Description("The city to look up, e.g. 'Seattle'.")] string city) =>
        $"The weather in {city} is 18°C and partly cloudy. (simulated)";
}
