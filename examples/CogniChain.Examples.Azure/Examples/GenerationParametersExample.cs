using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates controlling generation parameters like temperature.
/// </summary>
public class GenerationParametersExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Generation Parameters";
    public string Description => "Shows how to control temperature and token limits";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Creative response with high temperature
        var creativeOptions = new ChatCompletionOptions
        {
            Temperature = 0.9f,
            MaxOutputTokenCount = 100
        };

        var creativeResponse = await _chatClient.CompleteChatAsync(
            [new UserChatMessage("Write a creative one-liner about cloud computing.")],
            creativeOptions);
        Console.WriteLine($"Creative response: {creativeResponse.Value.Content[0].Text}");
        Console.WriteLine();

        // Precise response with low temperature
        var preciseOptions = new ChatCompletionOptions
        {
            Temperature = 0.1f,
            MaxOutputTokenCount = 50
        };

        var preciseResponse = await _chatClient.CompleteChatAsync(
            [new UserChatMessage("What is the capital of France? Answer in one word.")],
            preciseOptions);
        Console.WriteLine($"Precise response: {preciseResponse.Value.Content[0].Text}");
    }
}
