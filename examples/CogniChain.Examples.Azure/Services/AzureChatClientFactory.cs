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

        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            _client = new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));
            AuthenticationMethod = "API Key";
        }
        else
        {
            _client = new AzureOpenAIClient(new Uri(settings.Endpoint), new DefaultAzureCredential());
            AuthenticationMethod = "Azure Identity (DefaultAzureCredential)";
        }
    }

    public ChatClient CreateChatClient() => _client.GetChatClient(_settings.DeploymentName);
}
