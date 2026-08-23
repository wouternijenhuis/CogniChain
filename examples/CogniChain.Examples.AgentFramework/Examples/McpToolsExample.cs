using CogniChain.Examples.Shared;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace CogniChain.Examples.AgentFramework.Examples;

/// <summary>
/// Pulls tools from a Model Context Protocol server straight into a chain — <c>McpClientTool</c> already
/// derives from <see cref="AIFunction"/>, so no adapter is needed.
/// </summary>
/// <remarks>
/// Needs Node.js (<c>npx</c>) and network access, to launch the public MCP "everything" demo server
/// over stdio. If either is unavailable, this example reports why it was skipped instead of failing.
/// </remarks>
public sealed class McpToolsExample(IChatClient chatClient) : IExample
{
    public string Name => "MCP Tools";

    public string Description => "Tools discovered from an MCP server are wired straight into WithTools().";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "Everything",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            });

            await using var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

            Console.WriteLine($"Discovered {tools.Count} MCP tools: {string.Join(", ", tools.Select(t => t.Name))}");

            var chain = Chain.Create(chatClient)
                .WithTools([.. tools])
                .Prompt("{question}")
                .Build();

            var result = await chain.RunAsync(new { question = "Use the 'echo' tool to repeat back: CogniChain + MCP" }, cancellationToken);

            Console.WriteLine(result.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skipped — needs Node.js/npx and network access to fetch the demo MCP server: {ex.Message}");
        }
    }
}
