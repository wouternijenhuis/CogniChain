using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace CogniChain.Examples.SemanticKernel.Plugins;

/// <summary>
/// Native Semantic Kernel plugin for text utilities.
/// </summary>
public class TextUtilityPlugin
{
    [KernelFunction, Description("Reverses the input text")]
    public string ReverseText([Description("The text to reverse")] string input)
    {
        var chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [KernelFunction, Description("Counts words in the input text")]
    public int CountWords([Description("The text to count words in")] string input)
    {
        return input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [KernelFunction, Description("Converts text to uppercase")]
    public string ToUpperCase([Description("The text to convert")] string input)
    {
        return input.ToUpperInvariant();
    }

    [KernelFunction, Description("Converts text to lowercase")]
    public string ToLowerCase([Description("The text to convert")] string input)
    {
        return input.ToLowerInvariant();
    }
}
