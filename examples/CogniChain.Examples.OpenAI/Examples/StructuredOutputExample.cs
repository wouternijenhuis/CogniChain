using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// Demonstrates structured JSON output from OpenAI.
/// </summary>
public class StructuredOutputExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Structured JSON Output";
    public string Description => "Shows how to get structured JSON responses";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var jsonOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var response = await _chatClient.CompleteChatAsync(
            [new UserChatMessage("List 3 programming languages with their main use case. Return as JSON array with 'name' and 'useCase' fields.")],
            jsonOptions);

        Console.WriteLine($"JSON response:\n{response.Value.Content[0].Text}");
    }
}
