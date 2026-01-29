using CogniChain.Examples.OpenAI.Configuration;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Services;

/// <summary>
/// Factory for creating OpenAI chat clients.
/// </summary>
public class OpenAIChatClientFactory(OpenAISettings settings)
{
    private readonly OpenAISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public ChatClient CreateChatClient() => new(_settings.Model, _settings.ApiKey);
}
