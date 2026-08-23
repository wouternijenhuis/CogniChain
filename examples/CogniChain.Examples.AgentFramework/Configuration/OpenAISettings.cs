namespace CogniChain.Examples.AgentFramework.Configuration;

/// <summary>Reads OpenAI connection settings from environment variables.</summary>
public sealed record OpenAISettings(string ApiKey, string Model)
{
    public static OpenAISettings FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("Set the OPENAI_API_KEY environment variable before running this example.");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-5-mini";

        return new OpenAISettings(apiKey, model);
    }
}
