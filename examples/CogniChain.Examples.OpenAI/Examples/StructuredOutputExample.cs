using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary><c>Prompt&lt;T&gt;</c>: the model's response is deserialized straight into a record via JSON schema.</summary>
public sealed class StructuredOutputExample(IChatClient chatClient) : IExample
{
    public string Name => "Structured Output";

    public string Description => "Prompt<T> deserializes the model's response into a typed record.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .Prompt<MovieSuggestion>("Suggest one movie about {theme}.")
            .Build();

        var result = await chain.RunAsync(new { theme = "time travel" }, cancellationToken);

        Console.WriteLine($"{result.Value.Title} ({result.Value.Year}) — {result.Value.Reason}");
    }

    private sealed record MovieSuggestion(string Title, int Year, string Reason);
}
