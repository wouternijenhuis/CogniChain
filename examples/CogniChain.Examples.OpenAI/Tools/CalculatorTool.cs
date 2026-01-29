using CogniChain;

namespace CogniChain.Examples.OpenAI.Tools;

/// <summary>
/// Tool for performing calculations.
/// </summary>
public class CalculatorTool : ToolBase
{
    public override string Name => "calculator";
    public override string Description => "Perform basic arithmetic calculations";

    public override Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        // Simple calculator simulation - replace with actual expression parser
        return Task.FromResult($"Result: {input} = 42 (simulated)");
    }
}
