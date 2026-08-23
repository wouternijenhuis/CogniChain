namespace CogniChain.Examples.Shared;

/// <summary>Runs a set of <see cref="IExample"/>s in order, printing headers and surfacing failures without stopping the batch.</summary>
public static class ExampleRunner
{
    public static async Task RunAllAsync(IEnumerable<IExample> examples, CancellationToken cancellationToken = default)
    {
        foreach (var example in examples)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"  {example.Name}");
            Console.WriteLine($"  {example.Description}");
            Console.WriteLine(new string('=', 70));

            try
            {
                await example.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  FAILED: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
    }
}
