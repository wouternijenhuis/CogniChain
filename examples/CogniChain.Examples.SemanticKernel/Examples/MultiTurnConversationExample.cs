using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates multi-turn conversation with chat history.
/// </summary>
public class MultiTurnConversationExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Multi-turn Conversation with Chat History";
    public string Description => "Shows how to maintain conversation context using both CogniChain and SK";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        var cogniMemory = new ConversationMemory(maxMessages: 10);
        cogniMemory.AddSystemMessage("You are a senior .NET architect providing guidance on best practices.");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a senior .NET architect providing guidance on best practices.");

        string[] questions =
        [
            "What's the best way to structure a .NET solution?",
            "How should I handle cross-cutting concerns?",
            "What about testing strategies?"
        ];

        foreach (var question in questions)
        {
            Console.WriteLine($"User: {question}");

            cogniMemory.AddUserMessage(question);
            chatHistory.AddUserMessage(question);

            var response = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var assistantResponse = response.Content ?? "";

            cogniMemory.AddAssistantMessage(assistantResponse);
            chatHistory.AddAssistantMessage(assistantResponse);

            Console.WriteLine($"Assistant: {assistantResponse}\n");
        }
    }
}
