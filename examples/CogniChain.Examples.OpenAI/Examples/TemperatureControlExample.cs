using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// Demonstrates controlling generation parameters like temperature.
/// </summary>
public class TemperatureControlExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Temperature Control";
    public string Description => "Shows how to control temperature for creative vs precise outputs";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Creative response with high temperature
        var creativeOptions = new ChatCompletionOptions
        {
            Temperature = 0.9f,
            MaxOutputTokenCount = 80
        };

        var creativeResult = await _chatClient.CompleteChatAsync(
            [new UserChatMessage("Write a creative metaphor about programming.")],
            creativeOptions);
        Console.WriteLine($"Creative (temp=0.9): {creativeResult.Value.Content[0].Text}");

        // Precise response with low temperature
        var preciseOptions = new ChatCompletionOptions
        {
            Temperature = 0.1f,
            MaxOutputTokenCount = 30
        };

        var preciseResult = await _chatClient.CompleteChatAsync(
            [new UserChatMessage("What is 2+2? Answer with just the number.")],
            preciseOptions);
        Console.WriteLine($"Precise (temp=0.1): {preciseResult.Value.Content[0].Text}");
    }
}
