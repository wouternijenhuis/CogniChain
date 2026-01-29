using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Services;

/// <summary>
/// Factory interface for creating chat clients.
/// </summary>
public interface IChatClientFactory
{
    ChatClient CreateChatClient();
    string AuthenticationMethod { get; }
}
