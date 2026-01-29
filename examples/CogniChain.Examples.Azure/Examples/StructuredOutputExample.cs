using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates structured JSON output from Azure OpenAI.
/// </summary>
public class StructuredOutputExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Structured Output";
    public string Description => "Shows how to get structured JSON responses";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var structuredPrompt = @"Extract the following information from this text and return as JSON:
Text: 'Azure Functions is a serverless compute service that runs event-driven code. It supports multiple languages including C#, Python, and JavaScript.'

Return JSON with: name, type, languages (as array)";

        var response = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(structuredPrompt)],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

        Console.WriteLine($"Structured response:\n{response.Value.Content[0].Text}");
    }
}
