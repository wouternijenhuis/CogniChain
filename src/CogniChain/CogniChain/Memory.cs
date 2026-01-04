namespace CogniChain;

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the role of the message sender (e.g., "user", "assistant", "system").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata for the message.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Manages conversation history and context.
/// </summary>
public class ConversationMemory
{
    private readonly List<Message> _messages = new();
    private readonly int _maxMessages;

    /// <summary>
    /// Gets the messages in the conversation.
    /// </summary>
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationMemory"/> class.
    /// </summary>
    /// <param name="maxMessages">Maximum number of messages to retain. Use -1 for unlimited.</param>
    public ConversationMemory(int maxMessages = -1)
    {
        _maxMessages = maxMessages;
    }

    /// <summary>
    /// Adds a message to the conversation history.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The content of the message.</param>
    public void AddMessage(string role, string content)
    {
        var message = new Message
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        _messages.Add(message);

        // Trim to max messages if needed (keep system messages)
        if (_maxMessages > 0)
        {
            var systemMessages = _messages.Where(m => m.Role == "system").ToList();
            var otherMessages = _messages.Where(m => m.Role != "system").ToList();

            if (otherMessages.Count > _maxMessages)
            {
                var toRemove = otherMessages.Count - _maxMessages;
                otherMessages.RemoveRange(0, toRemove);
            }

            _messages.Clear();
            _messages.AddRange(systemMessages);
            _messages.AddRange(otherMessages);
        }
    }

    /// <summary>
    /// Adds a user message to the conversation history.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    public void AddUserMessage(string content) => AddMessage("user", content);

    /// <summary>
    /// Adds an assistant message to the conversation history.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    public void AddAssistantMessage(string content) => AddMessage("assistant", content);

    /// <summary>
    /// Adds a system message to the conversation history.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    public void AddSystemMessage(string content) => AddMessage("system", content);

    /// <summary>
    /// Gets the conversation history as a formatted string.
    /// </summary>
    /// <returns>A formatted string of the conversation history.</returns>
    public string GetFormattedHistory()
    {
        return string.Join("\n", _messages.Select(m => $"{m.Role}: {m.Content}"));
    }

    /// <summary>
    /// Clears all messages from the conversation history.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }

    /// <summary>
    /// Gets the last N messages from the conversation.
    /// </summary>
    /// <param name="count">The number of messages to retrieve.</param>
    /// <returns>The last N messages.</returns>
    public IEnumerable<Message> GetLastMessages(int count)
    {
        return _messages.TakeLast(count);
    }

    /// <summary>
    /// Gets messages by role.
    /// </summary>
    /// <param name="role">The role to filter by.</param>
    /// <returns>Messages matching the specified role.</returns>
    public IEnumerable<Message> GetMessagesByRole(string role)
    {
        return _messages.Where(m => m.Role == role);
    }
}
