using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Examples;

/// <summary>
/// Demonstrates resilient API calls with retry handling.
/// </summary>
public class RetryHandlerExample(ChatClient chatClient) : IExample
{
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Name => "Retry Handler";
    public string Description => "Shows how to use RetryHandler for resilient API calls";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var retryHandler = new RetryHandler(new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            UseJitter = true
        });

        var result = await retryHandler.ExecuteAsync(async () =>
        {
            var response = await _chatClient.CompleteChatAsync("What is Azure Key Vault in one sentence?");
            return response.Value.Content[0].Text;
        });

        Console.WriteLine($"Resilient response: {result}");
    }
}
