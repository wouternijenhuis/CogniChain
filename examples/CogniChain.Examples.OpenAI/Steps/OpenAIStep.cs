using CogniChain;
using OpenAI.Chat;

namespace CogniChain.Examples.OpenAI.Steps;

/// <summary>
/// Chain step that executes prompts using OpenAI.
/// </summary>
public class OpenAIStep(ChatClient client, string? promptPrefix = null) : IChainStep
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
                ["model"] = "gpt-4o-mini",
                ["finishReason"] = response.Value.FinishReason.ToString()
            }
        };
    }
}
