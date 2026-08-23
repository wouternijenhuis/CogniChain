using CogniChain.Examples.OpenAI.Tools;
using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.OpenAI.Examples;

/// <summary>
/// <c>WithToolsFrom</c> exposes a plain class's methods as tools; with <c>UseFunctionInvocation()</c>
/// wired into the chat client pipeline (see Program.cs), the model decides whether to call them.
/// </summary>
public sealed class ToolCallingExample(IChatClient chatClient) : IExample
{
    public string Name => "Tool Calling";

    public string Description => "The model calls CalculatorTool and WeatherTool on its own to answer the question.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .WithToolsFrom(new CalculatorTool())
            .WithToolsFrom(new WeatherTool())
            .Prompt("{question}")
            .Build();

        var result = await chain.RunAsync(
            new { question = "What's 12.5 times 4, and what's the weather in Lisbon?" },
            cancellationToken);

        Console.WriteLine(result.Value);
    }
}
