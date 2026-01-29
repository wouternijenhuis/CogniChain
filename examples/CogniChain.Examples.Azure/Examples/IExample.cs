namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Interface for runnable examples.
/// </summary>
public interface IExample
{
    string Name { get; }
    string Description { get; }
    Task RunAsync(CancellationToken cancellationToken = default);
}
