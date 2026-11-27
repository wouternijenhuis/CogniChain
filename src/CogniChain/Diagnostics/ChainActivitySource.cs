using System.Diagnostics;
using System.Reflection;

namespace CogniChain.Diagnostics;

/// <summary>
/// The <see cref="ActivitySource"/> CogniChain uses for tracing. Each chain run gets a
/// <c>cognichain.chain</c> span with one <c>cognichain.step</c> child span per step; failures set
/// the span status, an <c>error.type</c> tag, and an exception event. Model-level details (token
/// usage, model name, other <c>gen_ai.*</c> attributes) are left to <c>UseOpenTelemetry()</c> on the
/// underlying <see cref="Microsoft.Extensions.AI.IChatClient"/>, whose spans nest beneath these, so
/// nothing is counted twice.
/// </summary>
public static class ChainActivitySource
{
    /// <summary>The name under which the activity source is registered; pass this to your OTel exporter's listener/source names.</summary>
    public const string Name = "CogniChain";

    /// <summary>The shared activity source instance.</summary>
    public static readonly ActivitySource Instance = new(Name, GetVersion());

    private static string? GetVersion() =>
        typeof(ChainActivitySource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ChainActivitySource).Assembly.GetName().Version?.ToString();

    /// <summary>Starts an activity for one whole chain run, or <see langword="null"/> if nothing is listening.</summary>
    public static Activity? StartChain(string chainName)
    {
        var activity = Instance.StartActivity($"cognichain.chain {chainName}", ActivityKind.Internal);
        activity?.SetTag("cognichain.chain.name", chainName);
        return activity;
    }

    /// <summary>Starts an activity for one chain step, or <see langword="null"/> if nothing is listening.</summary>
    public static Activity? StartStep(string chainName, string stepName, int stepIndex) =>
        StartStep(chainName, stepName, stepIndex, parent: null);

    /// <summary>
    /// Starts a step activity under an explicit <paramref name="parent"/>. Streaming runs need this:
    /// an async iterator resumes on its consumer's execution context, so <see cref="Activity.Current"/>
    /// doesn't reliably point at the chain span between yields.
    /// </summary>
    internal static Activity? StartStep(string chainName, string stepName, int stepIndex, Activity? parent)
    {
        var activity = parent is null
            ? Instance.StartActivity($"cognichain.step {stepName}", ActivityKind.Internal)
            : Instance.StartActivity($"cognichain.step {stepName}", ActivityKind.Internal, parent.Context);
        if (activity is not null)
        {
            activity.SetTag("cognichain.chain.name", chainName);
            activity.SetTag("cognichain.step.name", stepName);
            activity.SetTag("cognichain.step.index", stepIndex);
        }

        return activity;
    }

    /// <summary>Marks <paramref name="activity"/> as failed by <paramref name="exception"/>, following OTel error conventions.</summary>
    internal static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.AddException(exception);
    }
}
