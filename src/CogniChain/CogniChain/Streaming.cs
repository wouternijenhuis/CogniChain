namespace CogniChain;

/// <summary>
/// Provides streaming capabilities for LLM responses.
/// </summary>
public class StreamingResponse
{
    private readonly List<string> _chunks = new();

    /// <summary>
    /// Gets all chunks received so far.
    /// </summary>
    public IReadOnlyList<string> Chunks => _chunks.AsReadOnly();

    /// <summary>
    /// Gets the complete response by joining all chunks.
    /// </summary>
    public string CompleteResponse => string.Join("", _chunks);

    /// <summary>
    /// Event raised when a new chunk is received.
    /// </summary>
    public event EventHandler<string>? ChunkReceived;

    /// <summary>
    /// Event raised when the stream is complete.
    /// </summary>
    public event EventHandler? StreamComplete;

    /// <summary>
    /// Adds a chunk to the streaming response.
    /// </summary>
    /// <param name="chunk">The chunk to add.</param>
    public void AddChunk(string chunk)
    {
        _chunks.Add(chunk);
        ChunkReceived?.Invoke(this, chunk);
    }

    /// <summary>
    /// Marks the stream as complete.
    /// </summary>
    public void Complete()
    {
        StreamComplete?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Handles streaming operations for LLM responses.
/// </summary>
public class StreamingHandler
{
    /// <summary>
    /// Processes a stream of chunks asynchronously.
    /// </summary>
    /// <param name="streamSource">The async enumerable source of chunks.</param>
    /// <param name="onChunk">Callback invoked for each chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete streamed response.</returns>
    public async Task<string> ProcessStreamAsync(
        IAsyncEnumerable<string> streamSource,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var response = new StreamingResponse();

        if (onChunk != null)
        {
            response.ChunkReceived += (_, chunk) => onChunk(chunk);
        }

        await foreach (var chunk in streamSource.WithCancellation(cancellationToken))
        {
            response.AddChunk(chunk);
        }

        response.Complete();
        return response.CompleteResponse;
    }

    /// <summary>
    /// Simulates a streaming response from a complete string.
    /// </summary>
    /// <param name="content">The complete content to stream.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="delayMs">Delay between chunks in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of string chunks.</returns>
    public static async IAsyncEnumerable<string> SimulateStreamAsync(
        string content,
        int chunkSize = 10,
        int delayMs = 50,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < content.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
            yield return chunk;

            if (delayMs > 0 && i + chunkSize < content.Length)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }
}
