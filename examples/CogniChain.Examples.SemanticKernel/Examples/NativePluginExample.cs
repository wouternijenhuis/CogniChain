using CogniChain.Examples.SemanticKernel.Plugins;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates native function plugins.
/// </summary>
public class NativePluginExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Native Function Plugin";
    public string Description => "Shows how to use native Semantic Kernel plugins";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _kernel.ImportPluginFromType<TextUtilityPlugin>();

        var result = await _kernel.InvokeAsync(
            "TextUtilityPlugin",
            "ReverseText",
            new KernelArguments { ["input"] = "CogniChain" },
            cancellationToken);

        Console.WriteLine($"Reversed text: {result}");

        var wordCount = await _kernel.InvokeAsync(
            "TextUtilityPlugin",
            "CountWords",
            new KernelArguments { ["input"] = "Semantic Kernel is powerful" },
            cancellationToken);

        Console.WriteLine($"Word count: {wordCount}");
    }
}
