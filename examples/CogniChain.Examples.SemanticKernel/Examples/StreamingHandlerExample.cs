using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates CogniChain's StreamingHandler and StreamingResponse for processing streamed LLM responses.
/// </summary>
public class StreamingHandlerExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "CogniChain Streaming Handler";
    public string Description => "Shows how to use StreamingHandler and StreamingResponse for processing streams";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Example 1: Using StreamingResponse directly
        Console.WriteLine("=== StreamingResponse Example ===");
        var streamingResponse = new StreamingResponse();

        // Subscribe to events
        streamingResponse.ChunkReceived += (_, chunk) => Console.Write(chunk);
        streamingResponse.StreamComplete += (_, _) => Console.WriteLine("\n[Stream Complete]");

        // Get a response from Semantic Kernel and stream it through CogniChain
        var llmResponse = await _kernel.InvokePromptAsync(
            "List 3 benefits of using Semantic Kernel briefly.",
            cancellationToken: cancellationToken);

        foreach (var word in llmResponse.ToString().Split(' '))
        {
            streamingResponse.AddChunk(word + " ");
            await Task.Delay(50, cancellationToken); // Simulate streaming delay
        }
        streamingResponse.Complete();

        Console.WriteLine($"Total chunks: {streamingResponse.Chunks.Count}");
        Console.WriteLine();

        // Example 2: Using StreamingHandler with ProcessStreamAsync
        Console.WriteLine("=== StreamingHandler.ProcessStreamAsync Example ===");
        var handler = new StreamingHandler();

        var completeResponse = await handler.ProcessStreamAsync(
            SimulateSKStreamAsync("CogniChain works great with Semantic Kernel.", cancellationToken),
            chunk => Console.Write($"[{chunk}]"),
            cancellationToken);

        Console.WriteLine($"\nComplete response: {completeResponse}");
        Console.WriteLine();

        // Example 3: Using StreamingHandler.SimulateStreamAsync
        Console.WriteLine("=== StreamingHandler.SimulateStreamAsync Example ===");
        Console.Write("Simulated stream: ");
        await foreach (var chunk in StreamingHandler.SimulateStreamAsync(
            "CogniChain makes LLM streaming easy!",
            chunkSize: 5,
            delayMs: 100,
            cancellationToken))
        {
            Console.Write(chunk);
        }
        Console.WriteLine();
    }

    private static async IAsyncEnumerable<string> SimulateSKStreamAsync(
        string content,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in content.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Delay(75, cancellationToken);
        }
    }
}
