using CogniChain.Examples.OpenAI.Services;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// Demonstrates multi-turn conversation with memory.
/// </summary>
public class MultiTurnConversationExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Multi-turn Conversation";
    public string Description => "Shows how to maintain conversation context across multiple turns";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var conversationMemory = new ConversationMemory(maxMessages: 10);
        conversationMemory.AddSystemMessage("You are a friendly AI tutor who explains things simply.");

        string[] questions =
        [
            "What is async/await in C#?",
            "Can you give me a simple example?",
            "What are common mistakes when using it?"
        ];

        foreach (var question in questions)
        {
            Console.WriteLine($"User: {question}");
            conversationMemory.AddUserMessage(question);

            var messages = ChatMessageBuilder.BuildFromMemory(conversationMemory);
            var response = await _chatClient.CompleteChatAsync(messages);
            var assistantResponse = response.Value.Content[0].Text;

            conversationMemory.AddAssistantMessage(assistantResponse);
            Console.WriteLine($"Assistant: {assistantResponse}\n");
        }
    }
}
