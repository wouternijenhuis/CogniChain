using CogniChain;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Tools;

/// <summary>
/// Tool for analyzing code using Semantic Kernel.
/// </summary>
public class CodeAnalysisTool(Kernel kernel) : ToolBase
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public override string Name => "analyze_code";
    public override string Description => "Analyze code for quality, performance, and best practices";

    public override async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var result = await _kernel.InvokePromptAsync(
            $"Briefly analyze this code for issues: {input}",
            cancellationToken: cancellationToken);
        return result.ToString();
    }
}
