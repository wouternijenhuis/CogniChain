using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>Exposes a <see cref="Chain{TIn, TOut}"/> as an <see cref="AIFunction"/> tool another model can call.</summary>
public static class ChainAIFunctionExtensions
{
    /// <summary>
    /// Wraps <paramref name="chain"/> as an <see cref="AIFunction"/>: calling the function runs the
    /// chain end-to-end and returns its output. Useful for composing chains — a sub-chain becomes a
    /// tool another chain's model can call — or for exposing a chain to a hand-rolled agent loop.
    /// </summary>
    /// <param name="chain">The chain to wrap.</param>
    /// <param name="name">The tool name, or <see cref="Chain{TIn, TOut}.Name"/> if omitted.</param>
    /// <param name="description">The tool description shown to the model.</param>
    [RequiresUnreferencedCode("AIFunctionFactory.Create generates a JSON schema for TIn and TOut by reflection.")]
    [RequiresDynamicCode("AIFunctionFactory.Create generates a JSON schema for TIn and TOut by reflection.")]
    public static AIFunction AsAIFunction<TIn, TOut>(this Chain<TIn, TOut> chain, string? name = null, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(chain);

        async Task<TOut> Invoke(TIn input, CancellationToken cancellationToken)
        {
            var result = await chain.RunAsync(input, cancellationToken).ConfigureAwait(false);
            return result.Value;
        }

        return AIFunctionFactory.Create((Func<TIn, CancellationToken, Task<TOut>>)Invoke, new AIFunctionFactoryOptions
        {
            Name = name ?? chain.Name,
            Description = description,
        });
    }
}
