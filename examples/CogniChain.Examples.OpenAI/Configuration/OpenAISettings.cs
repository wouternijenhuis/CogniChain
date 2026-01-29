namespace CogniChain.Examples.OpenAI.Configuration;

/// <summary>
/// Configuration settings for OpenAI service.
/// </summary>
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";

    public static OpenAISettings FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("Please set the OPENAI_API_KEY environment variable.");

        return new OpenAISettings
        {
            ApiKey = apiKey,
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini"
        };
    }
}
