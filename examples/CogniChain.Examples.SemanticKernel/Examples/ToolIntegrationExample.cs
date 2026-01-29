using CogniChain.Examples.SemanticKernel.Tools;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates tool integration with CogniChain.
/// </summary>
public class ToolIntegrationExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Tool Integration with CogniChain";
    public string Description => "Shows how to register and use tools";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var toolRegistry = new ToolRegistry();
        toolRegistry.RegisterTool(new CodeAnalysisTool(_kernel));
        toolRegistry.RegisterTool(new DocumentSearchTool());

        Console.WriteLine("Available tools:");
        Console.WriteLine(toolRegistry.GetToolDescriptions());

        var codeAnalysisResult = await toolRegistry.ExecuteToolAsync(
            "analyze_code",
            "public void DoSomething() { }");
        Console.WriteLine($"Code analysis result: {codeAnalysisResult}");
    }
}
