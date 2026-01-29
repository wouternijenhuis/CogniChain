using CogniChain;

namespace CogniChain.Examples.SemanticKernel.Tools;

/// <summary>
/// Tool for searching documentation.
/// </summary>
public class DocumentSearchTool : ToolBase
{
    public override string Name => "search_docs";
    public override string Description => "Search documentation for relevant information";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        // Simulated document search - replace with actual search implementation
        return Task.FromResult($"Found 5 relevant documents matching '{input}' (simulated)");
    }
}
