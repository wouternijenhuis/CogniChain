using System.Diagnostics;
using System.Runtime.CompilerServices;
using CogniChain.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CogniChain;

/// <summary>
/// Entry point for building a chain. See <see cref="ChainBuilder{TIn, TCurrent}"/> for the fluent
/// step-composition API.
/// </summary>
public static class Chain
{
    /// <summary>
    /// Starts building a chain over <paramref name="chatClient"/>. The chain's initial input type is
    /// inferred from the first step added (typically <c>.Prompt&lt;T&gt;(...)</c>, whose input is
    /// whatever object supplies the template's placeholder values).
    /// </summary>
    public static ChainBuilder<TIn, TIn> Create<TIn>(IChatClient chatClient, ILoggerFactory? loggerFactory = null) =>
        new(chatClient, [], [], null, [], null, loggerFactory ?? NullLoggerFactory.Instance);

    /// <summary>
    /// Starts building a chain whose initial input is untyped — the common case, where the first step
    /// is a <c>.Prompt&lt;T&gt;(...)</c> rendered against an anonymous object.
    /// </summary>
    public static ChainBuilder<object, object> Create(IChatClient chatClient, ILoggerFactory? loggerFactory = null) =>
        Create<object>(chatClient, loggerFactory);
}

/// <summary>
/// A built, immutable, reusable pipeline of typed steps over an <see cref="IChatClient"/>. Also an
/// <see cref="IChainStep{TIn, TOut}"/>, so a chain can be nested inside another chain (see
/// <c>ChainBuilder.Branch</c>) or bridged to an agent framework.
/// </summary>
/// <typeparam name="TIn">The chain's input type.</typeparam>
/// <typeparam name="TOut">The chain's output type.</typeparam>
public sealed class Chain<TIn, TOut> : IChainStep<TIn, TOut>
{
    private readonly IChatClient _chatClient;
    private readonly IReadOnlyList<StepNode> _nodes;
    private readonly IReadOnlyList<AITool> _tools;
    private readonly Action<ChatOptions>? _configureOptions;
    private readonly IReadOnlyList<string> _systemMessages;
    private readonly IChatReducer? _reducer;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Gets the chain's name, used in telemetry and in <see cref="ChainStepException"/>.</summary>
    public string Name { get; }

    internal Chain(
        string name,
        IChatClient chatClient,
        IReadOnlyList<StepNode> nodes,
        IReadOnlyList<AITool> tools,
        Action<ChatOptions>? configureOptions,
        IReadOnlyList<string> systemMessages,
        IChatReducer? reducer,
        ILoggerFactory loggerFactory)
    {
        Name = name;
        _chatClient = chatClient;
        _nodes = nodes;
        _tools = tools;
        _configureOptions = configureOptions;
        _systemMessages = systemMessages;
        _reducer = reducer;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a fresh execution context: a <see cref="ChatOptions"/> populated from this chain's
    /// tools and configuration, and <see cref="ChainContext.Messages"/> seeded with this chain's
    /// system messages. Pass the same context to successive <c>RunAsync</c> calls to carry
    /// conversation history across turns.
    /// </summary>
    public ChainContext CreateContext(IServiceProvider? services = null)
    {
        var options = new ChatOptions();
        if (_tools.Count > 0)
        {
            options.Tools = [.. _tools];
        }

        _configureOptions?.Invoke(options);

        var context = new ChainContext(_chatClient, options, services, _loggerFactory.CreateLogger($"CogniChain.Chain.{Name}"), _reducer);
        foreach (var systemMessage in _systemMessages)
        {
            context.Messages.Add(new ChatMessage(ChatRole.System, systemMessage));
        }

        return context;
    }

    /// <summary>Runs the chain against a fresh, single-turn context.</summary>
    public Task<ChainResult<TOut>> RunAsync(TIn input, CancellationToken cancellationToken = default) =>
        RunAsync(input, CreateContext(), cancellationToken);

    /// <summary>
    /// Runs the chain against <paramref name="context"/>. Reuse the same <see cref="ChainContext"/>
    /// across calls for multi-turn conversations — its <see cref="ChainContext.Messages"/> carries
    /// forward, reduced by <see cref="ChainContext.Reducer"/> before each model call.
    /// </summary>
    public async Task<ChainResult<TOut>> RunAsync(TIn input, ChainContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = await ExecuteNodesAsync(input, context, cancellationToken).ConfigureAwait(false);

        return new ChainResult<TOut>((TOut)value!)
        {
            Usage = context.Usage,
            Items = new Dictionary<string, object?>(context.Items),
        };
    }

    async ValueTask<TOut> IChainStep<TIn, TOut>.ExecuteAsync(TIn input, ChainContext context, CancellationToken cancellationToken)
    {
        var value = await ExecuteNodesAsync(input, context, cancellationToken).ConfigureAwait(false);
        return (TOut)value!;
    }

    /// <summary>Runs the chain against a fresh, single-turn context, streaming updates as they arrive.</summary>
    public IAsyncEnumerable<ChainUpdate> RunStreamingAsync(TIn input, CancellationToken cancellationToken = default) =>
        RunStreamingAsync(input, CreateContext(), cancellationToken);

    /// <summary>
    /// Runs the chain against <paramref name="context"/>, streaming updates as they arrive. Only the
    /// chain's terminal step streams token-by-token, and only when it is a plain-text prompt step
    /// (<c>.Prompt(string)</c>, not <c>.Prompt&lt;T&gt;(string)</c>); every other step yields a single
    /// <see cref="ChainUpdate"/> with <see cref="ChainUpdate.IsStepComplete"/> set once it finishes.
    /// </summary>
    public async IAsyncEnumerable<ChainUpdate> RunStreamingAsync(TIn input, ChainContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        object? current = input;
        for (var i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            var isTerminal = i == _nodes.Count - 1;

            if (isTerminal && node.Stream is not null)
            {
                using var activity = ChainActivitySource.StartStep(Name, node.Name, i);
                var enumerator = node.Stream(current, context, cancellationToken).GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (true)
                    {
                        bool hasNext;
                        try
                        {
                            hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                            throw new ChainStepException(node.Name, i, ex);
                        }

                        if (!hasNext)
                        {
                            break;
                        }

                        yield return new ChainUpdate { StepName = node.Name, StepIndex = i, ChatUpdate = enumerator.Current };
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                yield return new ChainUpdate { StepName = node.Name, StepIndex = i, IsStepComplete = true };
            }
            else
            {
                current = await InvokeNodeAsync(node, current, context, i, cancellationToken).ConfigureAwait(false);
                yield return new ChainUpdate { StepName = node.Name, StepIndex = i, IsStepComplete = true };
            }
        }
    }

    private async ValueTask<object?> ExecuteNodesAsync(object? input, ChainContext context, CancellationToken cancellationToken)
    {
        object? current = input;
        for (var i = 0; i < _nodes.Count; i++)
        {
            current = await InvokeNodeAsync(_nodes[i], current, context, i, cancellationToken).ConfigureAwait(false);
        }

        return current;
    }

    private async ValueTask<object?> InvokeNodeAsync(StepNode node, object? current, ChainContext context, int index, CancellationToken cancellationToken)
    {
        using var activity = ChainActivitySource.StartStep(Name, node.Name, index);
        try
        {
            return await node.Invoke(current, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw new ChainStepException(node.Name, index, ex);
        }
    }
}
