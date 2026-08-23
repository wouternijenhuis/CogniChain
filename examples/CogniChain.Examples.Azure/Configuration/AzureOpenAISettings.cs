namespace CogniChain.Examples.Azure.Configuration;

/// <summary>Reads Azure OpenAI connection settings from environment variables.</summary>
public sealed record AzureOpenAISettings(Uri Endpoint, string Deployment, string? ApiKey)
{
    /// <summary>Gets a value indicating whether to authenticate with <c>DefaultAzureCredential</c> instead of an API key.</summary>
    public bool UseAzureIdentity => string.IsNullOrEmpty(ApiKey);

    public static AzureOpenAISettings FromEnvironment()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Set the AZURE_OPENAI_ENDPOINT environment variable before running this example.");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
            ?? throw new InvalidOperationException("Set the AZURE_OPENAI_DEPLOYMENT environment variable before running this example.");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        return new AzureOpenAISettings(new Uri(endpoint), deployment, apiKey);
    }
}
