using System.Net;
using CogniChain.Middleware;
using CogniChain.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace CogniChain.Tests;

public class RetryingChatClientTests
{
    private static readonly RetryPolicy FastPolicy = new()
    {
        MaxAttempts = 3,
        InitialDelay = TimeSpan.FromMilliseconds(1),
        MaxDelay = TimeSpan.FromMilliseconds(5),
    };

    [Fact]
    public async Task GetResponseAsync_TransientFailureThenSuccess_RetriesUntilSuccess()
    {
        // Arrange
        var inner = new FakeChatClient();
        inner.EnqueueThrow(new HttpRequestException("busy", null, HttpStatusCode.TooManyRequests));
        inner.EnqueueResponse("ok");
        var client = new RetryingChatClient(inner, FastPolicy);

        // Act
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        Assert.Equal("ok", response.Text);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_NonTransientFailure_DoesNotRetry()
    {
        // Arrange
        var inner = new FakeChatClient();
        inner.EnqueueThrow(new HttpRequestException("bad request", null, HttpStatusCode.BadRequest));
        var client = new RetryingChatClient(inner, FastPolicy);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_ExhaustsAttempts_PropagatesOriginalExceptionUnwrapped()
    {
        // Arrange
        var inner = new FakeChatClient();
        for (var i = 0; i < FastPolicy.MaxAttempts; i++)
        {
            inner.EnqueueThrow(new HttpRequestException("still busy", null, HttpStatusCode.ServiceUnavailable));
        }

        var client = new RetryingChatClient(inner, FastPolicy);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
        Assert.Equal("still busy", exception.Message);
        Assert.Equal(FastPolicy.MaxAttempts, inner.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_OperationCanceled_PropagatesImmediatelyWithoutRetrying()
    {
        // Arrange
        var inner = new FakeChatClient();
        inner.EnqueueThrow(new OperationCanceledException());
        var client = new RetryingChatClient(inner, FastPolicy);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_ExceptionWithStatusProperty_IsClassifiedTransientByDuckTyping()
    {
        // Arrange: covers SDK exception types (e.g. Azure/OpenAI) that expose "Status" without deriving
        // from HttpRequestException.
        var inner = new FakeChatClient();
        inner.EnqueueThrow(new FakeStatusException(429, "rate limited"));
        inner.EnqueueResponse("ok");
        var client = new RetryingChatClient(inner, FastPolicy);

        // Act
        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        Assert.Equal("ok", response.Text);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_TransientFailureOnConnect_RetriesAndThenStreams()
    {
        // Arrange
        var inner = new FakeChatClient();
        inner.EnqueueStreamingThrow(new HttpRequestException("busy", null, HttpStatusCode.TooManyRequests));
        inner.EnqueueStreaming("a", "b");
        var client = new RetryingChatClient(inner, FastPolicy);

        // Act
        var tokens = new List<string>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            tokens.Add(update.Text);
        }

        // Assert
        Assert.Equal(["a", "b"], tokens);
        Assert.Equal(2, inner.StreamingCallCount);
    }

    [Fact]
    public void Constructor_MaxAttemptsLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var inner = new FakeChatClient();
        var policy = new RetryPolicy { MaxAttempts = 0 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryingChatClient(inner, policy));
    }
}
