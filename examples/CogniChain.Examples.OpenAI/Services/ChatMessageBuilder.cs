using CogniChain;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Services;

/// <summary>
/// Builds chat messages from CogniChain conversation memory.
/// </summary>
public static class ChatMessageBuilder
{
    public static List<ChatMessage> BuildFromMemory(ConversationMemory memory)
    {
        var messages = new List<ChatMessage>();

        foreach (var msg in memory.Messages)
        {
            ChatMessage chatMessage = msg.Role switch
            {
                "system" => new SystemChatMessage(msg.Content),
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => throw new ArgumentException($"Unknown role: {msg.Role}")
            };
            messages.Add(chatMessage);
        }

        return messages;
    }
}
