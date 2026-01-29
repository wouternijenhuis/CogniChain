using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates prompt execution settings.
/// </summary>
public class PromptExecutionSettingsExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Prompt Execution Settings";
    public string Description => "Shows how to control temperature, max tokens, and other settings";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 150,
            TopP = 0.95
        };

        var result = await _kernel.InvokePromptAsync(
            "Write a haiku about cloud computing.",
            new KernelArguments(executionSettings),
            cancellationToken: cancellationToken);

        Console.WriteLine($"With custom settings: {result}");
    }
}
