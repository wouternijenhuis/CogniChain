using CogniChain;
using OpenAI.Chat;

namespace CogniChain.Examples.Azure.Steps;

/// <summary>
/// Chain step that executes prompts using Azure OpenAI.
/// </summary>
public class AzureOpenAIStep(ChatClient client, string? promptPrefix = null) : IChainStep
{
    private readonly ChatClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly string? _promptPrefix = promptPrefix;

    public async Task<ChainResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var prompt = _promptPrefix != null ? $"{_promptPrefix}\n\n{input}" : input;
        var response = await _client.CompleteChatAsync(prompt);

        return new ChainResult
        {
            Output = response.Value.Content[0].Text,
            Success = true,
            Metadata =
            {
                ["provider"] = "Azure OpenAI",
                ["finishReason"] = response.Value.FinishReason.ToString()
            }
        };
    }
}
