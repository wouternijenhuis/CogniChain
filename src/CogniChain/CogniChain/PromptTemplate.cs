namespace CogniChain;

/// <summary>
/// Represents a prompt template with variable substitution capabilities.
/// </summary>
public class PromptTemplate
{
    private readonly string _template;
    private readonly List<string> _variables;

    /// <summary>
    /// Gets the raw template string.
    /// </summary>
    public string Template => _template;

    /// <summary>
    /// Gets the list of variable names used in the template.
    /// </summary>
    public IReadOnlyList<string> Variables => _variables.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTemplate"/> class.
    /// </summary>
    /// <param name="template">The template string with variables in {variableName} format.</param>
    public PromptTemplate(string template)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
        _variables = ExtractVariables(template);
    }

    /// <summary>
    /// Formats the template with the provided variable values.
    /// </summary>
    /// <param name="variables">Dictionary of variable names and their values.</param>
    /// <returns>The formatted prompt string.</returns>
    public string Format(Dictionary<string, string> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = _template;
        foreach (var variable in _variables)
        {
            if (!variables.TryGetValue(variable, out var value))
                throw new ArgumentException($"Missing value for variable: {variable}");

            result = result.Replace($"{{{variable}}}", value);
        }

        return result;
    }

    /// <summary>
    /// Formats the template with the provided variable values.
    /// </summary>
    /// <param name="variables">Object with properties matching variable names.</param>
    /// <returns>The formatted prompt string.</returns>
    /// <remarks>
    /// Property values are converted to strings using ToString(). For complex objects,
    /// this may produce unexpected results (e.g., "Namespace.TypeName" instead of meaningful content).
    /// For complex objects, consider using Format(Dictionary&lt;string, string&gt;) and provide
    /// custom string formatting, or ensure your objects override ToString() appropriately.
    /// Only public readable properties are used.
    /// </remarks>
    public string Format(object variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var dict = new Dictionary<string, string>();
        var properties = variables.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(variables)?.ToString() ?? string.Empty;
            dict[prop.Name] = value;
        }

        return Format(dict);
    }

    private static List<string> ExtractVariables(string template)
    {
        var variables = new List<string>();
        var inVariable = false;
        var currentVariable = new System.Text.StringBuilder();

        foreach (var c in template)
        {
            if (c == '{')
            {
                inVariable = true;
                currentVariable.Clear();
            }
            else if (c == '}' && inVariable)
            {
                var varName = currentVariable.ToString();
                if (!string.IsNullOrWhiteSpace(varName) && !variables.Contains(varName))
                {
                    variables.Add(varName);
                }
                inVariable = false;
            }
            else if (inVariable)
            {
                currentVariable.Append(c);
            }
        }

        return variables;
    }

    /// <summary>
    /// Creates a new prompt template from a string.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <returns>A new <see cref="PromptTemplate"/> instance.</returns>
    public static PromptTemplate FromString(string template) => new(template);
}
