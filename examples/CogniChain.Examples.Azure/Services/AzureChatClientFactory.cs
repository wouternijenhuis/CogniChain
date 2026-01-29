using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CogniChain.Examples.Azure.Configuration;
using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Services;

/// <summary>
/// Factory for creating Azure OpenAI chat clients.
/// </summary>
public class AzureChatClientFactory : IChatClientFactory
{
    private readonly AzureOpenAISettings _settings;
    private readonly AzureOpenAIClient _client;

    public string AuthenticationMethod { get; }

    public AzureChatClientFactory(AzureOpenAISettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Use API version from settings (defaults to 2025-01-01-preview for o4-mini support)
        var serviceVersion = settings.ApiVersion switch
        {
            "2024-12-01-preview" => AzureOpenAIClientOptions.ServiceVersion.V2024_12_01_Preview,
            "2025-01-01-preview" => AzureOpenAIClientOptions.ServiceVersion.V2025_01_01_Preview,
            _ => AzureOpenAIClientOptions.ServiceVersion.V2025_01_01_Preview
        };
        var options = new AzureOpenAIClientOptions(serviceVersion);

        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            _client = new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey), options);
            AuthenticationMethod = "API Key";
        }
        else
        {
            _client = new AzureOpenAIClient(new Uri(settings.Endpoint), new DefaultAzureCredential(), options);
            AuthenticationMethod = "Azure Identity (DefaultAzureCredential)";
        }
    }

    public ChatClient CreateChatClient() => _client.GetChatClient(_settings.DeploymentName);
}
