using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CogniChain;

/// <summary>
/// Fluent, immutable builder for a <see cref="Chain{TIn, TOut}"/>. Every method returns a new builder;
/// each call to <c>.Prompt&lt;T&gt;</c>, <c>.Then&lt;T&gt;</c>, <c>.Map&lt;TElement, T&gt;</c>, or
/// <c>.Branch&lt;T&gt;</c> changes the generic <c>TCurrent</c> to that step's output type, so the
/// compiler enforces that adjacent steps line up.
/// </summary>
/// <typeparam name="TIn">The chain's fixed input type, established by the first step.</typeparam>
/// <typeparam name="TCurrent">The output type of the last step added so far.</typeparam>
public sealed class ChainBuilder<TIn, TCurrent>
{
    private readonly IChatClient _chatClient;
    private readonly IReadOnlyList<StepNode> _nodes;
    private readonly IReadOnlyList<AITool> _tools;
    private readonly Action<ChatOptions>? _configureOptions;
    private readonly IReadOnlyList<string> _systemMessages;
    private readonly IChatReducer? _reducer;
    private readonly ILoggerFactory _loggerFactory;

    internal ChainBuilder(
        IChatClient chatClient,
        IReadOnlyList<StepNode> nodes,
        IReadOnlyList<AITool> tools,
        Action<ChatOptions>? configureOptions,
        IReadOnlyList<string> systemMessages,
        IChatReducer? reducer,
        ILoggerFactory loggerFactory)
    {
        _chatClient = chatClient;
        _nodes = nodes;
        _tools = tools;
        _configureOptions = configureOptions;
        _systemMessages = systemMessages;
        _reducer = reducer;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Adds a structured-output prompt step: renders <paramref name="template"/> against the current
    /// value's public properties, sends it to the model, and deserializes the response as
    /// <typeparamref name="TOut"/> via JSON schema. Does not support token streaming — see
    /// <see cref="Prompt(string, Action{ChatOptions}?, string?)"/> for a streamable plain-text step.
    /// </summary>
    public ChainBuilder<TIn, TOut> Prompt<TOut>(string template, Action<ChatOptions>? configure = null, string? name = null)
    {
        var parsed = new PromptTemplate(template);
        var stepName = name ?? $"Prompt<{typeof(TOut).Name}>";

        async ValueTask<object?> Invoke(object? current, ChainContext context, CancellationToken cancellationToken)
        {
            var rendered = parsed.Render(current!);
            context.Messages.Add(new ChatMessage(ChatRole.User, rendered));
            await context.ReduceHistoryAsync(cancellationToken).ConfigureAwait(false);

            var options = configure is null ? context.Options : CloneOptions(context.Options, configure);
            var response = await context.ChatClient.GetResponseAsync<TOut>(context.Messages, options, useJsonSchemaResponseFormat: null, cancellationToken).ConfigureAwait(false);

            context.AddUsage(response.Usage);
            context.Messages.AddMessages(response);

            return response.Result;
        }

        return Append<TOut>(new StepNode { Name = stepName, Invoke = Invoke });
    }

    /// <summary>
    /// Adds a plain-text prompt step: renders <paramref name="template"/> against the current value's
    /// public properties and sends it to the model. When this is the chain's terminal step,
    /// <c>RunStreamingAsync</c> streams the model's response token-by-token.
    /// </summary>
    public ChainBuilder<TIn, string> Prompt(string template, Action<ChatOptions>? configure = null, string? name = null)
    {
        var parsed = new PromptTemplate(template);
        var stepName = name ?? "Prompt";

        async ValueTask<object?> Invoke(object? current, ChainContext context, CancellationToken cancellationToken)
        {
            var rendered = parsed.Render(current!);
            context.Messages.Add(new ChatMessage(ChatRole.User, rendered));
            await context.ReduceHistoryAsync(cancellationToken).ConfigureAwait(false);

            var options = configure is null ? context.Options : CloneOptions(context.Options, configure);
            var response = await context.ChatClient.GetResponseAsync(context.Messages, options, cancellationToken).ConfigureAwait(false);

            context.AddUsage(response.Usage);
            context.Messages.AddMessages(response);

            return response.Text;
        }

        async IAsyncEnumerable<ChatResponseUpdate> Stream(object? current, ChainContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var rendered = parsed.Render(current!);
            context.Messages.Add(new ChatMessage(ChatRole.User, rendered));
            await context.ReduceHistoryAsync(cancellationToken).ConfigureAwait(false);

            var options = configure is null ? context.Options : CloneOptions(context.Options, configure);
            var updates = new List<ChatResponseUpdate>();

            // try/finally (not try/catch) so a partial response is still recorded into context.Messages
            // if the caller cancels mid-stream or stops enumerating early — otherwise the user message
            // added above is left dangling with no matching assistant reply.
            try
            {
                await foreach (var update in context.ChatClient.GetStreamingResponseAsync(context.Messages, options, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    updates.Add(update);
                    yield return update;
                }
            }
            finally
            {
                if (updates.Count > 0)
                {
                    var response = updates.ToChatResponse();
                    context.AddUsage(response.Usage);
                    context.Messages.AddMessages(response);
                }
            }
        }

        return Append<string>(new StepNode { Name = stepName, Invoke = Invoke, Stream = Stream });
    }

    /// <summary>Adds an arbitrary asynchronous transform step.</summary>
    public ChainBuilder<TIn, TOut> Then<TOut>(Func<TCurrent, ChainContext, CancellationToken, ValueTask<TOut>> step, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(step);

        async ValueTask<object?> Invoke(object? current, ChainContext context, CancellationToken cancellationToken) =>
            await step((TCurrent)current!, context, cancellationToken).ConfigureAwait(false);

        return Append<TOut>(new StepNode { Name = name ?? $"Then<{typeof(TOut).Name}>", Invoke = Invoke });
    }

    /// <summary>Adds a synchronous transform step. Convenience overload for pure functions.</summary>
    public ChainBuilder<TIn, TOut> Then<TOut>(Func<TCurrent, TOut> step, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Then<TOut>((input, _, _) => ValueTask.FromResult(step(input)), name ?? $"Then<{typeof(TOut).Name}>");
    }

    /// <summary>Adds a reusable, testable step implementing <see cref="IChainStep{TIn, TOut}"/>.</summary>
    public ChainBuilder<TIn, TOut> Then<TOut>(IChainStep<TCurrent, TOut> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Then<TOut>(step.ExecuteAsync, step.GetType().Name);
    }

    /// <summary>
    /// Adds a fan-out step: runs <paramref name="step"/> once per element of the current value (which
    /// must implement <see cref="IEnumerable{TElement}"/>), with at most <paramref name="maxConcurrency"/>
    /// invocations in flight at a time.
    /// </summary>
    /// <remarks>
    /// Invocations run concurrently against the shared <see cref="ChainContext"/>. Keep
    /// <paramref name="step"/> free of side effects on <see cref="ChainContext.Messages"/> or
    /// <see cref="ChainContext.Items"/> — those members are not safe for concurrent mutation. Reading
    /// <see cref="ChainContext.ChatClient"/> to make independent, per-element model calls is safe:
    /// <c>IChatClient</c> implementations are documented as thread-safe for concurrent use.
    /// </remarks>
    public ChainBuilder<TIn, IReadOnlyList<TOut>> Map<TElement, TOut>(
        Func<TElement, ChainContext, CancellationToken, ValueTask<TOut>> step,
        int maxConcurrency = 4,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), maxConcurrency, "Must be at least 1.");
        }

        async ValueTask<object?> Invoke(object? current, ChainContext context, CancellationToken cancellationToken)
        {
            if (current is not IEnumerable<TElement> sequence)
            {
                throw new InvalidOperationException(
                    $"Map<{typeof(TElement).Name}, {typeof(TOut).Name}> requires the current chain value to implement " +
                    $"IEnumerable<{typeof(TElement).Name}>, but the value was {(current is null ? "null" : current.GetType().Name)}.");
            }

            var elements = sequence.ToList();
            var results = new TOut[elements.Count];

            using var throttle = new SemaphoreSlim(maxConcurrency);
            var tasks = elements.Select(async (element, index) =>
            {
                await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    results[index] = await step(element, context, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return (IReadOnlyList<TOut>)results;
        }

        return Append<IReadOnlyList<TOut>>(new StepNode { Name = name ?? $"Map<{typeof(TElement).Name},{typeof(TOut).Name}>", Invoke = Invoke });
    }

    /// <summary>
    /// Adds a conditional step: evaluates <paramref name="predicate"/> against the current value and
    /// runs <paramref name="whenTrue"/> or <paramref name="whenFalse"/> — both pre-built chains — to
    /// produce the next value. The chosen chain runs against its <em>own</em> configured chat client,
    /// tools, option configuration, and reducer; only <see cref="ChainContext.Messages"/>,
    /// <see cref="ChainContext.Items"/>, and <see cref="ChainContext.Usage"/> are shared with this
    /// chain's context, so conversation history and accumulated usage flow across the branch.
    /// </summary>
    public ChainBuilder<TIn, TOut> Branch<TOut>(
        Func<TCurrent, bool> predicate,
        Chain<TCurrent, TOut> whenTrue,
        Chain<TCurrent, TOut> whenFalse,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(whenTrue);
        ArgumentNullException.ThrowIfNull(whenFalse);

        async ValueTask<object?> Invoke(object? current, ChainContext context, CancellationToken cancellationToken)
        {
            var typed = (TCurrent)current!;
            IChainStep<TCurrent, TOut> chosen = predicate(typed) ? whenTrue : whenFalse;
            return await chosen.ExecuteAsync(typed, context, cancellationToken).ConfigureAwait(false);
        }

        return Append<TOut>(new StepNode { Name = name ?? "Branch", Invoke = Invoke });
    }

    /// <summary>Adds tools the model may call for every prompt step from here on.</summary>
    /// <exception cref="InvalidOperationException">A tool with the same <see cref="AITool.Name"/> was already added.</exception>
    public ChainBuilder<TIn, TCurrent> WithTools(params AITool[] tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        var combined = new List<AITool>(_tools);
        foreach (var tool in tools)
        {
            if (combined.Any(t => string.Equals(t.Name, tool.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"A tool named '{tool.Name}' has already been added to this chain.");
            }

            combined.Add(tool);
        }

        return new ChainBuilder<TIn, TCurrent>(_chatClient, _nodes, combined, _configureOptions, _systemMessages, _reducer, _loggerFactory);
    }

    /// <summary>
    /// Reflects every public instance method of <paramref name="instance"/> into an <see cref="AIFunction"/>
    /// tool. Convenient for grouping related tools (e.g. a "calculator" or "weather" service) into one class.
    /// Methods inherited from <see cref="object"/> and common compiler/record-synthesized members
    /// (<c>ToString</c>, <c>Equals</c>, <c>GetHashCode</c>, <c>Deconstruct</c>, record <c>Clone</c>) are
    /// excluded even when overridden, since they're never meant to be model-callable tools.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over the public methods of an arbitrary type; ensure they're preserved when trimming, or use WithTools(AIFunctionFactory.Create(...)) instead.")]
    public ChainBuilder<TIn, TCurrent> WithToolsFrom(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var tools = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object) && !IsSynthesizedMember(m))
            .Select(m => AIFunctionFactory.Create(m, instance, options: null))
            .ToArray();

        return WithTools(tools);
    }

    private static bool IsSynthesizedMember(MethodInfo method) => method.Name switch
    {
        nameof(ToString) or nameof(Equals) or nameof(GetHashCode) or nameof(GetType) or "Deconstruct" or "PrintMembers" => true,
        _ => method.Name.StartsWith('<'), // e.g. record-synthesized "<Clone>$"
    };

    /// <summary>Adds a callback that configures <see cref="ChatOptions"/> for every prompt step from here on.</summary>
    public ChainBuilder<TIn, TCurrent> WithOptions(Action<ChatOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var previous = _configureOptions;
        void Combined(ChatOptions options)
        {
            previous?.Invoke(options);
            configure(options);
        }

        return new ChainBuilder<TIn, TCurrent>(_chatClient, _nodes, _tools, Combined, _systemMessages, _reducer, _loggerFactory);
    }

    /// <summary>Adds a system message, seeded into every fresh <see cref="ChainContext"/> this chain creates.</summary>
    public ChainBuilder<TIn, TCurrent> WithSystemMessage(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChainBuilder<TIn, TCurrent>(_chatClient, _nodes, _tools, _configureOptions, [.. _systemMessages, content], _reducer, _loggerFactory);
    }

    /// <summary>
    /// Sets the reducer applied to <see cref="ChainContext.Messages"/> before every model call —
    /// use this to bound conversation history for chains reused across multiple <c>RunAsync</c> calls.
    /// Defaults to no reduction (unbounded history) if never called.
    /// </summary>
    public ChainBuilder<TIn, TCurrent> WithHistoryReducer(IChatReducer reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        return new ChainBuilder<TIn, TCurrent>(_chatClient, _nodes, _tools, _configureOptions, _systemMessages, reducer, _loggerFactory);
    }

    /// <summary>Builds the immutable, reusable <see cref="Chain{TIn, TOut}"/>.</summary>
    public Chain<TIn, TCurrent> Build(string? name = null) =>
        new(name ?? "Chain", _chatClient, _nodes, _tools, _configureOptions, _systemMessages, _reducer, _loggerFactory);

    private ChainBuilder<TIn, TNext> Append<TNext>(StepNode node) =>
        new(_chatClient, [.. _nodes, node], _tools, _configureOptions, _systemMessages, _reducer, _loggerFactory);

    private static ChatOptions CloneOptions(ChatOptions options, Action<ChatOptions> configure)
    {
        var clone = options.Clone();
        configure(clone);
        return clone;
    }
}
