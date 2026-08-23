using CogniChain.Examples.Azure.Tools;
using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// <c>WithToolsFrom</c> exposes a plain class's methods as tools; with <c>UseFunctionInvocation()</c>
/// wired into the chat client pipeline (see Program.cs), the model decides whether to call them.
/// </summary>
public sealed class ToolCallingExample(IChatClient chatClient) : IExample
{
    public string Name => "Tool Calling";

    public string Description => "The model calls AzureResourceTool and CostEstimatorTool on its own to answer the question.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var chain = Chain.Create(chatClient)
            .WithToolsFrom(new AzureResourceTool())
            .WithToolsFrom(new CostEstimatorTool())
            .Prompt("{question}")
            .Build();

        var result = await chain.RunAsync(
            new { question = "What's the status of 'my-app-service' and roughly how much does its SKU cost per month?" },
            cancellationToken);

        Console.WriteLine(result.Value);
    }
}
