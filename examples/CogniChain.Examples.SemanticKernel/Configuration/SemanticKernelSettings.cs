namespace CogniChain.Examples.SemanticKernel.Configuration;

/// <summary>
/// Configuration settings for Semantic Kernel with OpenAI or Azure OpenAI.
/// </summary>
public class SemanticKernelSettings
{
    public string? OpenAIApiKey { get; set; }
    public string? AzureEndpoint { get; set; }
    public string? AzureApiKey { get; set; }
    public string AzureDeployment { get; set; } = "gpt-4o-mini";
    public string OpenAIModel { get; set; } = "gpt-4o-mini";

    public bool UseAzure => !string.IsNullOrEmpty(AzureEndpoint) && !string.IsNullOrEmpty(AzureApiKey);
    public bool UseOpenAI => !string.IsNullOrEmpty(OpenAIApiKey);

    public static SemanticKernelSettings FromEnvironment()
    {
        var settings = new SemanticKernelSettings
        {
            OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            AzureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
            AzureApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"),
            AzureDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o-mini"
        };

        if (!settings.UseAzure && !settings.UseOpenAI)
        {
            throw new InvalidOperationException(
                "Please set either OPENAI_API_KEY or both AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY environment variables.");
        }

        return settings;
    }
}
