using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates streaming responses with Semantic Kernel.
/// </summary>
public class StreamingExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Streaming Response";
    public string Description => "Shows how to stream responses token by token";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

        Console.Write("Streaming: ");
        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
            [new ChatMessageContent(AuthorRole.User, "List 3 benefits of using Semantic Kernel, one per line.")],
            cancellationToken: cancellationToken))
        {
            Console.Write(chunk.Content);
        }
        Console.WriteLine();
    }
}
