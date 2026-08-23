namespace CogniChain.Examples.Shared;

/// <summary>A single runnable example, shown by <see cref="ExampleRunner"/>.</summary>
public interface IExample
{
    /// <summary>Gets the example's display name.</summary>
    string Name { get; }

    /// <summary>Gets a one-line description shown before the example runs.</summary>
    string Description { get; }

    /// <summary>Runs the example.</summary>
    Task RunAsync(CancellationToken cancellationToken = default);
}
