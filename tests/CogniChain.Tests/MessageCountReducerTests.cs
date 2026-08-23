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
    public async Task ReduceAsync_PreservesSystemMessageOriginalRelativePosition()
    {
        // Arrange: a system message added mid-conversation (a legal operation on the public
        // IList<ChatMessage>) must not be hoisted to the front of the reduced list.
        var reducer = new MessageCountReducer(maxMessages: 10);
        List<ChatMessage> messages =
        [
            new(ChatRole.User, "one"),
            new(ChatRole.Assistant, "two"),
            new(ChatRole.System, "New instruction mid-conversation."),
            new(ChatRole.User, "three"),
        ];

        // Act
        var result = (await reducer.ReduceAsync(messages)).ToList();

        // Assert
        Assert.Equal(["one", "two", "New instruction mid-conversation.", "three"], result.Select(m => m.Text));
    }

    [Fact]
    public async Task ReduceAsync_CutPointOnToolResult_ExtendsWindowToIncludePrecedingAssistantCall()
    {
        // Arrange: a naive count-based cut landing exactly on a tool-result message would sever it from
        // the assistant message that requested it, producing a malformed history most providers reject.
        var reducer = new MessageCountReducer(maxMessages: 2);
        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are helpful."),
            new(ChatRole.User, "one"),
            new(ChatRole.Assistant, "calling a tool"),
            new(ChatRole.Tool, "tool result"),
            new(ChatRole.Assistant, "two"),
        ];

        // Act
        var result = (await reducer.ReduceAsync(messages)).ToList();

        // Assert: the window is extended backward to keep the assistant call and its tool result together.
        Assert.Equal(["You are helpful.", "calling a tool", "tool result", "two"], result.Select(m => m.Text));
    }

    [Fact]
    public void Constructor_NonPositiveMaxMessages_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MessageCountReducer(0));
    }
}
