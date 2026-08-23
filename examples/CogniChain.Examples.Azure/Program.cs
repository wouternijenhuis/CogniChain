using Azure.AI.OpenAI;
using Azure.Identity;
using CogniChain.Examples.Azure.Configuration;
using CogniChain.Examples.Azure.Examples;
using CogniChain.Examples.Shared;
using CogniChain.Middleware;
using Microsoft.Extensions.AI;

var settings = AzureOpenAISettings.FromEnvironment();

AzureOpenAIClient azureClient = settings.UseAzureIdentity
    ? new AzureOpenAIClient(settings.Endpoint, new DefaultAzureCredential())
    : new AzureOpenAIClient(settings.Endpoint, new Azure.AzureKeyCredential(settings.ApiKey!));

IChatClient chatClient = azureClient
    .GetChatClient(settings.Deployment)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseCogniChainRetry()
    .Build();

IExample[] examples =
[
    new BasicPromptExample(chatClient),
    new ContentPipelineExample(chatClient),
    new MultiTurnConversationExample(chatClient),
    new ToolCallingExample(chatClient),
    new StreamingExample(chatClient),
];

await ExampleRunner.RunAllAsync(examples);
