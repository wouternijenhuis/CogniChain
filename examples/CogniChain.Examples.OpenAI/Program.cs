using CogniChain.Examples.OpenAI.Configuration;
using CogniChain.Examples.OpenAI.Examples;
using CogniChain.Examples.OpenAI.Services;

// Load configuration
var settings = OpenAISettings.FromEnvironment();

// Create services
var clientFactory = new OpenAIChatClientFactory(settings);
var chatClient = clientFactory.CreateChatClient();

Console.WriteLine($"Using model: {settings.Model}\n");

// Create examples - each focused on a single responsibility
var examples = new IExample[]
{
    new BasicChainStepExample(chatClient),
    new MultiTurnConversationExample(chatClient),
    new MultiStepChainExample(chatClient),
    new StreamingExample(chatClient),
    new ToolIntegrationExample(),
    new StructuredOutputExample(chatClient),
    new TemperatureControlExample(chatClient)
};

// Run all examples
var runner = new ExampleRunner(examples);
await runner.RunAllAsync();
