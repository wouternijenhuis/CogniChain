using System.ComponentModel;

namespace CogniChain.Examples.OpenAI.Tools;

/// <summary>A tiny calculator, wired up via <c>WithToolsFrom(new CalculatorTool())</c>.</summary>
public sealed class CalculatorTool
{
    [Description("Adds two numbers together.")]
    public double Add([Description("The first number.")] double a, [Description("The second number.")] double b) => a + b;

    [Description("Multiplies two numbers together.")]
    public double Multiply([Description("The first number.")] double a, [Description("The second number.")] double b) => a * b;
}
