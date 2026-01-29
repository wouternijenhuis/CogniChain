using CogniChain.Examples.SemanticKernel.Configuration;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Services;

/// <summary>
/// Factory for creating Semantic Kernel instances.
/// </summary>
public class KernelFactory(SemanticKernelSettings settings)
{
    private readonly SemanticKernelSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public string ProviderDescription { get; } = settings.UseAzure
            ? $"Azure OpenAI - Endpoint: {settings.AzureEndpoint}, Deployment: {settings.AzureDeployment}"
            : $"OpenAI with {settings.OpenAIModel}";

    public Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        if (_settings.UseAzure)
        {
            builder.AddAzureOpenAIChatCompletion(
                _settings.AzureDeployment,
                _settings.AzureEndpoint!,
                _settings.AzureApiKey!);
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                _settings.OpenAIModel,
                _settings.OpenAIApiKey!);
        }

        return builder.Build();
    }
}
