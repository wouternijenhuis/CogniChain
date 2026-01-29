using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates resilient API calls with retry handling.
/// </summary>
public class RetryHandlerExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Resilient Semantic Kernel Calls";
    public string Description => "Shows how to use RetryHandler for resilient API calls";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var retryHandler = new RetryHandler(new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelayMs = 500,
            BackoffMultiplier = 2.0,
            UseJitter = true
        });

        var result = await retryHandler.ExecuteAsync(async () =>
        {
            var response = await _kernel.InvokePromptAsync(
                "What is Semantic Kernel in one sentence?",
                cancellationToken: cancellationToken);
            return response.ToString();
        });

        Console.WriteLine($"Resilient response: {result}");
    }
}
