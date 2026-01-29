namespace CogniChain.Examples.Azure.Configuration;

/// <summary>
/// Configuration settings for Azure OpenAI service.
/// </summary>
public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o-mini";
    public string? ApiKey { get; set; }
    public string ApiVersion { get; set; } = "2025-01-01-preview";
    public bool UseAzureIdentity => string.IsNullOrEmpty(ApiKey);

    public static AzureOpenAISettings FromEnvironment()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Please set the AZURE_OPENAI_ENDPOINT environment variable.");

        return new AzureOpenAISettings
        {
            Endpoint = endpoint,
            DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o-mini",
            ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"),
            ApiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "2025-01-01-preview"
        };
    }
}
