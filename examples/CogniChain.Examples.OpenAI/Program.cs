using CogniChain.Examples.OpenAI.Configuration;
using CogniChain.Examples.OpenAI.Examples;
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
    new BasicPromptExample(chatClient),
    new StructuredOutputExample(chatClient),
    new MultiTurnConversationExample(chatClient),
    new ToolCallingExample(chatClient),
    new StreamingExample(chatClient),
];

await ExampleRunner.RunAllAsync(examples);
