namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Orchestrates running all examples in sequence.
/// </summary>
public class ExampleRunner(IEnumerable<IExample> examples)
{
    private readonly IEnumerable<IExample> _examples = examples ?? throw new ArgumentNullException(nameof(examples));

    public async Task RunAllAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("=== CogniChain Semantic Kernel Integration Examples ===\n");

        var exampleNumber = 1;
        foreach (var example in _examples)
        {
            Console.WriteLine($"{exampleNumber}. {example.Name}:");

            try
            {
                await example.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running example: {ex.Message}");
            }

            Console.WriteLine();
            exampleNumber++;
        }

        Console.WriteLine("=== Semantic Kernel Examples Complete ===");
    }
}
