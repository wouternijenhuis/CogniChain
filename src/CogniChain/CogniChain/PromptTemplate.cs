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

        // Validate that all required variables are provided
        foreach (var variable in _variables)
        {
            if (!variables.ContainsKey(variable))
                throw new ArgumentException($"Missing value for variable: {variable}");
        }

        // Build the result in a single pass to avoid repeated string replacements
        var template = _template;
        var sb = new System.Text.StringBuilder(template.Length);

        for (int i = 0; i < template.Length; i++)
        {
            var c = template[i];
            if (c == '{')
            {
                int end = template.IndexOf('}', i + 1);
                if (end > i + 1)
                {
                    var name = template.Substring(i + 1, end - i - 1);
                    if (variables.TryGetValue(name, out var value))
                    {
                        sb.Append(value);
                        i = end;
                        continue;
                    }
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats the template with the provided variable values.
    /// </summary>
    /// <param name="variables">Object with properties matching variable names.</param>
    /// <returns>The formatted prompt string.</returns>
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
        var currentVariable = string.Empty;

        foreach (var c in template)
        {
            if (c == '{')
            {
                inVariable = true;
                currentVariable = string.Empty;
            }
            else if (c == '}' && inVariable)
            {
                if (!string.IsNullOrWhiteSpace(currentVariable) && !variables.Contains(currentVariable))
                {
                    variables.Add(currentVariable);
                }
                inVariable = false;
            }
            else if (inVariable)
            {
                currentVariable += c;
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
