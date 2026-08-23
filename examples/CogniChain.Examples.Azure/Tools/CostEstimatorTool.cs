using System.ComponentModel;

namespace CogniChain.Examples.Azure.Tools;

/// <summary>A simulated Azure cost estimator.</summary>
public sealed class CostEstimatorTool
{
    [Description("Estimates the simulated monthly cost, in USD, of an Azure SKU.")]
    public decimal EstimateMonthlyCost([Description("The SKU name, e.g. 'Standard_B2s'.")] string sku) =>
        sku.Contains("B2s", StringComparison.OrdinalIgnoreCase) ? 30.5m : 120.0m;
}
