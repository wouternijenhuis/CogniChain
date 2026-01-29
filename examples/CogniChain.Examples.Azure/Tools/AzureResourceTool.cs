using CogniChain;

namespace CogniChain.Examples.Azure.Tools;

/// <summary>
/// Tool for querying Azure resources.
/// </summary>
public class AzureResourceTool : ToolBase
{
    public override string Name => "azure_resources";
    public override string Description => "Query Azure resources in the subscription";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        // Simulated Azure resource query - replace with actual Azure SDK calls
        return Task.FromResult($"Found 3 storage accounts matching '{input}' (simulated)");
    }
}
