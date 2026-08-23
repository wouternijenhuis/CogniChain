using System.ComponentModel;

namespace CogniChain.Examples.Azure.Tools;

/// <summary>A simulated Azure resource lookup.</summary>
public sealed class AzureResourceTool
{
    [Description("Looks up the simulated status of an Azure resource.")]
    public string GetResourceStatus([Description("The resource name, e.g. 'my-app-service'.")] string resourceName) =>
        $"Resource '{resourceName}' is Running in West Europe, SKU Standard_B2s. (simulated)";
}
