namespace CogniChain;

/// <summary>
/// Represents a tool that can be called by the LLM.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the tool with the provided input.
    /// </summary>
    /// <param name="input">The input parameters for the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for implementing tools.
/// </summary>
public abstract class ToolBase : ITool
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public abstract Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages a collection of tools available to the LLM.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();

    /// <summary>
    /// Registers a tool.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a tool with the same name is already registered.</exception>
    public void RegisterTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (_tools.ContainsKey(tool.Name))
            throw new InvalidOperationException($"A tool with the name '{tool.Name}' is already registered.");

        _tools.Add(tool.Name, tool);
    }

    /// <summary>
    /// Updates an existing tool registration.
    /// </summary>
    /// <param name="tool">The tool to update.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a tool with the specified name is not registered.</exception>
    public void UpdateTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (!_tools.ContainsKey(tool.Name))
            throw new InvalidOperationException($"Cannot update tool '{tool.Name}' because it is not registered.");
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <returns>The tool if found, null otherwise.</returns>
    public ITool? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>A collection of all registered tools.</returns>
    public IEnumerable<ITool> GetAllTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// Executes a tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="input">The input for the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    public async Task<string> ExecuteToolAsync(string toolName, string input, CancellationToken cancellationToken = default)
    {
        var tool = GetTool(toolName);
        if (tool == null)
            throw new InvalidOperationException($"Tool '{toolName}' not found.");

        return await tool.ExecuteAsync(input, cancellationToken);
    }

    /// <summary>
    /// Gets a formatted list of available tools for the LLM.
    /// </summary>
    /// <returns>A string describing all available tools.</returns>
    public string GetToolDescriptions()
    {
        var descriptions = _tools.Values.Select(t => $"- {t.Name}: {t.Description}");
        return string.Join("\n", descriptions);
    }
}
