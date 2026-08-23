using System.Diagnostics;
using System.Reflection;

namespace CogniChain.Diagnostics;

/// <summary>
/// The <see cref="ActivitySource"/> CogniChain uses for per-step tracing. Each chain step's activity
/// follows the OpenTelemetry Semantic Conventions for Generative AI where applicable (<c>gen_ai.*</c>
/// tags), complementing the spans that <c>UseOpenTelemetry()</c> adds around the underlying
/// <see cref="Microsoft.Extensions.AI.IChatClient"/> calls.
/// </summary>
public static class ChainActivitySource
{
    /// <summary>The name under which the activity source is registered; pass this to your OTel exporter's listener/source names.</summary>
    public const string Name = "CogniChain";

    /// <summary>The shared activity source instance.</summary>
    public static readonly ActivitySource Instance = new(Name, GetVersion());

    private static string GetVersion() =>
        typeof(ChainActivitySource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ChainActivitySource).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    /// <summary>Starts an activity for one chain step, or <see langword="null"/> if nothing is listening.</summary>
    public static Activity? StartStep(string chainName, string stepName, int stepIndex)
    {
        var activity = Instance.StartActivity($"cognichain.step {stepName}", ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("cognichain.chain.name", chainName);
            activity.SetTag("cognichain.step.name", stepName);
            activity.SetTag("cognichain.step.index", stepIndex);
        }

        return activity;
    }
}
