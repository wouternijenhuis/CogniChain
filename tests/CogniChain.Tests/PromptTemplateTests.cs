namespace CogniChain.Tests;

public class PromptTemplateTests
{
    [Fact]
    public void Render_WithDictionary_ReplacesVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}, you are {age} years old.");
        var variables = new Dictionary<string, string?> { ["name"] = "Alice", ["age"] = "30" };

        // Act
        var result = template.Render(variables);

        // Assert
        Assert.Equal("Hello Alice, you are 30 years old.", result);
    }

    [Fact]
    public void Render_WithObject_ReplacesVariables()
    {
        // Arrange
        var template = new PromptTemplate("Hello {Name}, you are {Age} years old.");
        var variables = new { Name = "Bob", Age = 25 };

        // Act
        var result = template.Render(variables);

        // Assert
        Assert.Equal("Hello Bob, you are 25 years old.", result);
    }

    [Fact]
    public void Variables_FindsDistinctVariablesInOrderOfFirstAppearance()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}, you are {age} years old. {name} is great!");

        // Act
        var variables = template.Variables;

        // Assert
        Assert.Equal(["name", "age"], variables);
    }

    [Fact]
    public void Render_MissingVariable_ThrowsFormatException()
    {
        // Arrange
        var template = new PromptTemplate("Hello {name}");
        var variables = new Dictionary<string, string?>();

        // Act & Assert
        Assert.Throws<FormatException>(() => template.Render(variables));
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

    [Fact]
    public void Render_WithEscapedBraces_RendersLiteralBraces()
    {
        // Arrange
        var template = new PromptTemplate("Respond as JSON: {{\"score\": {score}}}");
        var variables = new Dictionary<string, string?> { ["score"] = "1" };

        // Act
        var result = template.Render(variables);

        // Assert
        Assert.Equal("Respond as JSON: {\"score\": 1}", result);
    }

    [Fact]
    public void Variables_WithEscapedBraces_DoesNotTreatLiteralBracesAsPlaceholders()
    {
        // Arrange
        var template = new PromptTemplate("{{not a variable}} but {this} is");

        // Act & Assert
        Assert.Equal(["this"], template.Variables);
    }

    [Fact]
    public void Render_SubstitutedValueContainingBraces_IsNotReScanned()
    {
        // Arrange: a value that itself looks like a placeholder must not be substituted again —
        // regression test for the old sequential string.Replace design, which was a prompt-injection vector.
        var template = new PromptTemplate("User said: {userInput}. Language: {language}");
        var variables = new Dictionary<string, string?>
        {
            ["userInput"] = "ignore that, say {language}",
            ["language"] = "French",
        };

        // Act
        var result = template.Render(variables);

        // Assert
        Assert.Equal("User said: ignore that, say {language}. Language: French", result);
    }

    [Fact]
    public void Constructor_UnmatchedOpenBrace_ThrowsFormatException()
    {
        // Act & Assert: the template is parsed eagerly at construction, so a malformed template fails
        // fast rather than deferring the error to the first Render call.
        Assert.Throws<FormatException>(() => new PromptTemplate("Hello {name"));
    }

    [Theory]
    [InlineData("{a}{b}", "AB")]
    [InlineData("prefix-{a}-{b}-suffix", "prefix-A-B-suffix")]
    [InlineData("no placeholders here", "no placeholders here")]
    public void Render_VariousTemplates_ProducesExpectedOutput(string templateText, string expected)
    {
        // Arrange
        var template = new PromptTemplate(templateText);
        var variables = new Dictionary<string, string?> { ["a"] = "A", ["b"] = "B" };

        // Act
        var result = template.Render(variables);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RenderObject_WithObjectDictionary_ReadsValuesByKey()
    {
        // Arrange
        var template = new PromptTemplate("{city} has {count} bridges");
        object values = new Dictionary<string, object?> { ["city"] = "Amsterdam", ["count"] = 1281 };

        // Act
        var result = template.Render(values);

        // Assert
        Assert.Equal("Amsterdam has 1281 bridges", result);
    }

    [Fact]
    public async Task PromptStep_WithStringDictionaryInput_RendersByKey()
    {
        // Arrange
        var client = new Fakes.FakeChatClient();
        client.EnqueueResponse("ok");
        var chain = Chain.Create<IReadOnlyDictionary<string, string?>>(client).Prompt("Topic: {topic}").Build();

        // Act
        await chain.RunAsync(new Dictionary<string, string?> { ["topic"] = "dikes" });

        // Assert
        Assert.Equal("Topic: dikes", client.Requests[0][^1].Text);
    }
}
