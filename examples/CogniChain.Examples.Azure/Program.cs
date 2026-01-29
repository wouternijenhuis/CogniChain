using CogniChain.Examples.Azure.Configuration;
using CogniChain.Examples.Azure.Examples;
using CogniChain.Examples.Azure.Services;

// Load configuration
var settings = AzureOpenAISettings.FromEnvironment();

// Create services
var clientFactory = new AzureChatClientFactory(settings);
var chatClient = clientFactory.CreateChatClient();

Console.WriteLine($"Using {clientFactory.AuthenticationMethod} authentication");
Console.WriteLine($"Endpoint: {settings.Endpoint}");
Console.WriteLine($"Deployment: {settings.DeploymentName}\n");

// Create examples - each focused on a single responsibility
var examples = new IExample[]
{
    new BasicChainStepExample(chatClient),
    new MultiTurnConversationExample(chatClient),
    new ContentPipelineExample(chatClient),
    new StreamingExample(chatClient),
    new RetryHandlerExample(chatClient),
    new ToolIntegrationExample(),
    new StructuredOutputExample(chatClient),
    new GenerationParametersExample(chatClient)
};

// Run all examples
var runner = new ExampleRunner(examples);
await runner.RunAllAsync();
