using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CogniChain.Middleware;

/// <summary>Extension methods for adding <see cref="RetryingChatClient"/> to a <see cref="ChatClientBuilder"/> pipeline.</summary>
public static class RetryingChatClientBuilderExtensions
{
    /// <summary>Adds a <see cref="RetryingChatClient"/> as the next stage in the pipeline.</summary>
    /// <param name="builder">The chat client builder.</param>
    /// <param name="policy">The retry policy, or <see cref="RetryPolicy.Default"/> if omitted.</param>
    /// <param name="loggerFactory">The logger factory used to create the retry client's logger, or none for no logging.</param>
    public static ChatClientBuilder UseCogniChainRetry(this ChatClientBuilder builder, RetryPolicy? policy = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Use((inner, services) =>
        {
            var factory = loggerFactory ?? services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            var logger = factory?.CreateLogger<RetryingChatClient>();
            return new RetryingChatClient(inner, policy, logger);
        });
    }
}
