namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Interface for runnable examples.
/// </summary>
public interface IExample
{
    string Name { get; }
    string Description { get; }
    Task RunAsync(CancellationToken cancellationToken = default);
}
