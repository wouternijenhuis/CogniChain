using CogniChain.Examples.Azure.Services;
using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

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
        conversationMemory.AddSystemMessage("You are an Azure solutions architect who provides concise, practical advice.");

        string[] questions =
        [
            "What's the difference between Azure Functions and Azure Container Apps?",
            "When should I choose one over the other?",
            "What about cost considerations?"
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
