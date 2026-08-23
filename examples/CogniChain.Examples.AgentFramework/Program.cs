using CogniChain.Examples.AgentFramework.Configuration;
using CogniChain.Examples.AgentFramework.Examples;
using CogniChain.Examples.Shared;
using CogniChain.Middleware;
using Microsoft.Extensions.AI;
using OpenAI;

var settings = OpenAISettings.FromEnvironment();

IChatClient chatClient = new OpenAIClient(settings.ApiKey)
    .GetChatClient(settings.Model)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseCogniChainRetry()
    .Build();

IExample[] examples =
[
    new ChainAsAgentExample(chatClient),
    new AgentAsChainStepExample(chatClient),
    new McpToolsExample(chatClient),
];

await ExampleRunner.RunAllAsync(examples);
