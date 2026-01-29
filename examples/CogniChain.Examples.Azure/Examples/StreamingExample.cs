using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates streaming responses from Azure OpenAI.
/// </summary>
public class StreamingExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Streaming Response";
    public string Description => "Shows how to stream responses token by token";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.Write("Streaming: ");
        var streamingResult = _chatClient.CompleteChatStreamingAsync("List 3 Azure security best practices, one per line.");

        await foreach (var update in streamingResult)
        {
            foreach (var part in update.ContentUpdate)
            {
                Console.Write(part.Text);
            }
        }
        Console.WriteLine();
    }
}
