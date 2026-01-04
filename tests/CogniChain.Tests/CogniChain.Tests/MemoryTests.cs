namespace CogniChain.Tests;

public class MemoryTests
{
    [Fact]
    public void AddMessage_AddsToHistory()
    {
        // Arrange
        var memory = new ConversationMemory();

        // Act
        memory.AddMessage("user", "Hello");
        memory.AddMessage("assistant", "Hi there!");

        // Assert
        Assert.Equal(2, memory.Messages.Count);
        Assert.Equal("user", memory.Messages[0].Role);
        Assert.Equal("Hello", memory.Messages[0].Content);
    }

    [Fact]
    public void AddUserMessage_AddsWithCorrectRole()
    {
        // Arrange
        var memory = new ConversationMemory();

        // Act
        memory.AddUserMessage("Test message");

        // Assert
        Assert.Single(memory.Messages);
        Assert.Equal("user", memory.Messages[0].Role);
    }

    [Fact]
    public void MaxMessages_TrimsHistory()
    {
        // Arrange
        var memory = new ConversationMemory(maxMessages: 2);

        // Act
        memory.AddUserMessage("Message 1");
        memory.AddUserMessage("Message 2");
        memory.AddUserMessage("Message 3");
        memory.AddUserMessage("Message 4");

        // Assert
        Assert.Equal(2, memory.Messages.Count);
        Assert.Equal("Message 3", memory.Messages[0].Content);
        Assert.Equal("Message 4", memory.Messages[1].Content);
    }

    [Fact]
    public void MaxMessages_PreservesSystemMessages()
    {
        // Arrange
        var memory = new ConversationMemory(maxMessages: 2);

        // Act
        memory.AddSystemMessage("System instruction");
        memory.AddUserMessage("Message 1");
        memory.AddUserMessage("Message 2");
        memory.AddUserMessage("Message 3");

        // Assert
        Assert.Equal(3, memory.Messages.Count);
        Assert.Equal("system", memory.Messages[0].Role);
        Assert.Equal("Message 2", memory.Messages[1].Content);
        Assert.Equal("Message 3", memory.Messages[2].Content);
    }

    [Fact]
    public void GetFormattedHistory_ReturnsFormattedString()
    {
        // Arrange
        var memory = new ConversationMemory();
        memory.AddUserMessage("Hello");
        memory.AddAssistantMessage("Hi!");

        // Act
        var formatted = memory.GetFormattedHistory();

        // Assert
        Assert.Contains("user: Hello", formatted);
        Assert.Contains("assistant: Hi!", formatted);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        // Arrange
        var memory = new ConversationMemory();
        memory.AddUserMessage("Test");

        // Act
        memory.Clear();

        // Assert
        Assert.Empty(memory.Messages);
    }

    [Fact]
    public void GetLastMessages_ReturnsCorrectCount()
    {
        // Arrange
        var memory = new ConversationMemory();
        memory.AddUserMessage("Message 1");
        memory.AddUserMessage("Message 2");
        memory.AddUserMessage("Message 3");

        // Act
        var last = memory.GetLastMessages(2).ToList();

        // Assert
        Assert.Equal(2, last.Count);
        Assert.Equal("Message 2", last[0].Content);
    }

    [Fact]
    public void GetMessagesByRole_FiltersCorrectly()
    {
        // Arrange
        var memory = new ConversationMemory();
        memory.AddUserMessage("User 1");
        memory.AddAssistantMessage("Assistant 1");
        memory.AddUserMessage("User 2");

        // Act
        var userMessages = memory.GetMessagesByRole("user").ToList();

        // Assert
        Assert.Equal(2, userMessages.Count);
        Assert.All(userMessages, m => Assert.Equal("user", m.Role));
    }
}
