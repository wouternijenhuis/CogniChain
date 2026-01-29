using CogniChain.Examples.SemanticKernel.Steps;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates multi-step chain pipeline.
/// </summary>
public class ChainPipelineExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Multi-Step Chain Pipeline";
    public string Description => "Shows how to chain multiple AI steps together";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create()
            .AddStep(new SemanticKernelStep(_kernel, "Create a brief outline (3 bullet points) for:"))
            .AddStep(new SemanticKernelStep(_kernel, "Expand this outline into a short paragraph:"));

        var result = await chain.RunAsync(
            "Building AI-powered .NET applications with Semantic Kernel and CogniChain");

        Console.WriteLine($"Generated content:\n{result.Output}");
    }
}
