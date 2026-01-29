using CogniChain;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Steps;

/// <summary>
/// Chain step that executes prompts using Semantic Kernel.
/// </summary>
public class SemanticKernelStep(Kernel kernel, string? promptPrefix = null) : IChainStep
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    private readonly string? _promptPrefix = promptPrefix;

    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var prompt = _promptPrefix != null ? $"{_promptPrefix}\n\n{input}" : input;
        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);

        return new ChainResult
        {
            Output = result.ToString(),
            Success = true,
            Metadata = { ["provider"] = "Semantic Kernel" }
        };
    }
}
