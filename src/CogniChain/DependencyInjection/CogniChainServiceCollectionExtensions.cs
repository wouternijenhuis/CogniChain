using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CogniChain.DependencyInjection;

/// <summary>
/// Registers named <see cref="Chain{TIn, TOut}"/> instances built from the app's registered
/// <see cref="IChatClient"/>. Register the chat client first with the standard
/// <c>IServiceCollection.AddChatClient(...)</c> from <c>Microsoft.Extensions.AI</c> — CogniChain does
/// not duplicate that registration.
/// </summary>
public static class CogniChainServiceCollectionExtensions
{
    /// <summary>
    /// Builds and registers a chain as a keyed singleton under <paramref name="name"/>. Resolve it with
    /// <c>serviceProvider.GetRequiredKeyedService&lt;Chain&lt;TIn, TOut&gt;&gt;(name)</c> or the
    /// <c>[FromKeyedServices(name)]</c> attribute.
    /// </summary>
    /// <typeparam name="TIn">The chain's input type.</typeparam>
    /// <typeparam name="TOut">The chain's output type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key the chain is registered under, and its <see cref="Chain{TIn, TOut}.Name"/>.</param>
    /// <param name="configure">Builds the chain's steps from a fresh <see cref="ChainBuilder{TIn, TIn}"/>.</param>
    public static IServiceCollection AddChain<TIn, TOut>(
        this IServiceCollection services,
        string name,
        Func<ChainBuilder<TIn, TIn>, ChainBuilder<TIn, TOut>> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddKeyedSingleton(name, (serviceProvider, _) =>
        {
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var builder = Chain.Create<TIn>(chatClient, loggerFactory);
            return configure(builder).Build(name);
        });

        return services;
    }
}
