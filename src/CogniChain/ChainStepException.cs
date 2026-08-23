namespace CogniChain;

/// <summary>
/// Thrown when a chain step fails. Wraps the original exception with the failing step's name and
/// position, so a multi-step chain's failures are diagnosable without instrumenting every step.
/// </summary>
public sealed class ChainStepException : Exception
{
    /// <summary>Gets the name of the step that failed.</summary>
    public string StepName { get; }

    /// <summary>Gets the zero-based index of the step that failed within its chain.</summary>
    public int StepIndex { get; }

    /// <summary>Initializes a new instance of the <see cref="ChainStepException"/> class.</summary>
    public ChainStepException(string stepName, int stepIndex, Exception innerException)
        : base($"Chain step '{stepName}' (index {stepIndex}) failed: {innerException.Message}", innerException)
    {
        StepName = stepName;
        StepIndex = stepIndex;
    }
}
