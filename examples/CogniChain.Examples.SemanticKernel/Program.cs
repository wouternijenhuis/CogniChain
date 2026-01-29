using CogniChain.Examples.SemanticKernel.Configuration;
using CogniChain.Examples.SemanticKernel.Examples;
using CogniChain.Examples.SemanticKernel.Services;

// Load configuration
var settings = SemanticKernelSettings.FromEnvironment();

// Create services
var kernelFactory = new KernelFactory(settings);
var kernel = kernelFactory.CreateKernel();

Console.WriteLine($"Using {kernelFactory.ProviderDescription}\n");

// Create examples - each focused on a single responsibility
var examples = new IExample[]
{
    new BasicChainStepExample(kernel),
    new CombinedPromptTemplateExample(kernel),
    new MultiTurnConversationExample(kernel),
    new ChainPipelineExample(kernel),
    new RetryHandlerExample(kernel),
    new StreamingHandlerExample(kernel),
    new ToolIntegrationExample(kernel)
};

// Run all examples
var runner = new ExampleRunner(examples);
await runner.RunAllAsync();
