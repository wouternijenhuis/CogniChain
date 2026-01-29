using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Examples;

/// <summary>
/// Demonstrates combined prompt template usage.
/// </summary>
public class CombinedPromptTemplateExample(Kernel kernel) : IExample
{
    private readonly Kernel _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

    public string Name => "Combined Prompt Template";
    public string Description => "Shows how to combine CogniChain templates with Semantic Kernel";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cogniChainTemplate = new PromptTemplate(@"
You are analyzing code quality. 

Language: {language}
Focus Areas: {focus}

Code to analyze:
{code}
");

        var formattedPrompt = cogniChainTemplate.Format(new Dictionary<string, string>
        {
            ["language"] = "C#",
            ["focus"] = "performance and readability",
            ["code"] = @"public List<int> GetEvenNumbers(List<int> numbers) 
{
    var result = new List<int>();
    foreach(var n in numbers)
        if(n % 2 == 0) result.Add(n);
    return result;
}"
        });

        var analysisResult = await _kernel.InvokePromptAsync(formattedPrompt, cancellationToken: cancellationToken);
        Console.WriteLine($"Code Analysis:\n{analysisResult}");
    }
}
