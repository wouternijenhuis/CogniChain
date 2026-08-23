using Microsoft.Extensions.AI;

namespace CogniChain.Tests;

public class MessageCountReducerTests
{
    [Fact]
    public async Task ReduceAsync_FewerMessagesThanLimit_ReturnsAllMessages()
    {
        // Arrange
        var reducer = new MessageCountReducer(maxMessages: 10);
        List<ChatMessage> messages = [new(ChatRole.User, "hi"), new(ChatRole.Assistant, "hello")];

        // Act
        var result = (await reducer.ReduceAsync(messages)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReduceAsync_MoreMessagesThanLimit_KeepsOnlyTheMostRecent()
    {
        // Arrange
        var reducer = new MessageCountReducer(maxMessages: 2);
        List<ChatMessage> messages =
        [
            new(ChatRole.User, "one"),
            new(ChatRole.Assistant, "two"),
            new(ChatRole.User, "three"),
            new(ChatRole.Assistant, "four"),
        ];

        // Act
        var result = (await reducer.ReduceAsync(messages)).ToList();

        // Assert
        Assert.Equal(["three", "four"], result.Select(m => m.Text));
    }

    [Fact]
    public async Task ReduceAsync_AlwaysPreservesSystemMessagesEvenWhenOverLimit()
    {
        // Arrange
        var reducer = new MessageCountReducer(maxMessages: 1);
        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are helpful."),
            new(ChatRole.User, "one"),
            new(ChatRole.Assistant, "two"),
            new(ChatRole.User, "three"),
        ];

        // Act
        var result = (await reducer.ReduceAsync(messages)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(ChatRole.System, result[0].Role);
        Assert.Equal("three", result[1].Text);
    }

    [Fact]
    public void Constructor_NonPositiveMaxMessages_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageCountReducer(0));
    }
}
