namespace CogniChain.Tests;

public class PromptTemplateTests
{
    [Fact]
    public void Format_WithDictionary_ReplacesVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}, you are {age} years old.");
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Alice",
            ["age"] = "30"
        };

        // Act
        var result = template.Format(variables);

        // Assert
        Assert.Equal("Hello Alice, you are 30 years old.", result);
    }

    [Fact]
    public void Format_WithObject_ReplacesVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello {Name}, you are {Age} years old.");
        var variables = new { Name = "Bob", Age = 25 };

        // Act
        var result = template.Format(variables);

        // Assert
        Assert.Equal("Hello Bob, you are 25 years old.", result);
    }

    [Fact]
    public void ExtractVariables_FindsAllVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}, you are {age} years old. {name} is great!");

        // Act
        var variables = template.Variables;

        // Assert
        Assert.Equal(2, variables.Count);
        Assert.Contains("name", variables);
        Assert.Contains("age", variables);
    }

    [Fact]
    public void Format_MissingVariable_ThrowsException()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}");
        var variables = new Dictionary<string, string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => template.Format(variables));
    }

    [Fact]
    public void FromString_CreatesTemplate()
    {
        // Arrange & Act
        var template = PromptTemplate.FromString("Test {variable}");

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Variables);
    }
}
