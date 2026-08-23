using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Composes a typed <c>Prompt&lt;T&gt;</c> step with a <c>Then</c> step: the outline's structured
/// output becomes the input to a second model call that writes the full article.
/// </summary>
public sealed class ContentPipelineExample(IChatClient chatClient) : IExample
{
    public string Name => "Content Pipeline";

    public string Description => "Prompt<T> produces a typed outline; Then expands it into an article with a second model call.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .Prompt<Outline>("Create a 3-section outline for a short article about {topic}.")
            .Then<Article>(async (outline, context, ct) =>
            {
                var sections = string.Join("\n- ", outline.Sections);
                var response = await context.ChatClient.GetResponseAsync(
                    $"Write a short article (under 150 words) covering these sections:\n- {sections}", context.Options, ct);
                return new Article(outline.Sections[0], response.Text);
            })
            .Build();

        var result = await chain.RunAsync(new { topic = "cost optimization on Azure" }, cancellationToken);

        Console.WriteLine($"# {result.Value.Title}");
        Console.WriteLine(result.Value.Body);
    }

    private sealed record Outline(IReadOnlyList<string> Sections);

    private sealed record Article(string Title, string Body);
}
