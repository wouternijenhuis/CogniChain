using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.AI;

namespace CogniChain;

/// <summary>
/// A reusable prompt template with <c>{variable}</c> placeholders and <c>{{</c>/<c>}}</c> escaping
/// for literal braces (so JSON-bearing prompts render correctly).
/// </summary>
/// <remarks>
/// The template is parsed once, at construction, into an immutable list of literal and placeholder
/// segments. Rendering walks the segments and substitutes each placeholder exactly once — unlike a
/// sequence of <see cref="string.Replace(string, string?)"/> calls, a substituted value is never
/// re-scanned for further placeholders.
/// </remarks>
public sealed class PromptTemplate
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly IReadOnlyList<Segment> _segments;

    /// <summary>Gets the original template text.</summary>
    public string Template { get; }

    /// <summary>Gets the distinct placeholder names, in order of first appearance.</summary>
    public IReadOnlyList<string> Variables { get; }

    /// <summary>Initializes a new instance of the <see cref="PromptTemplate"/> class.</summary>
    /// <param name="template">The template text, e.g. <c>"Translate '{text}' to {language}"</c>.</param>
    public PromptTemplate(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
        _segments = Parse(template);
        Variables = _segments.Where(s => s.IsPlaceholder).Select(s => s.Text).Distinct().ToArray();
    }

    /// <summary>Creates a <see cref="PromptTemplate"/> from a string. Equivalent to the constructor.</summary>
    public static PromptTemplate FromString(string template) => new(template);

    /// <summary>Renders the template, substituting each placeholder with the matching value.</summary>
    /// <exception cref="FormatException">A placeholder has no corresponding value.</exception>
    public string Render(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var builder = new StringBuilder();
        foreach (var segment in _segments)
        {
            if (!segment.IsPlaceholder)
            {
                builder.Append(segment.Text);
                continue;
            }

            if (!values.TryGetValue(segment.Text, out var value) || value is null)
            {
                throw new FormatException($"Missing value for placeholder '{{{segment.Text}}}' in prompt template.");
            }

            builder.Append(value);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders the template, reading placeholder values from the public readable properties of
    /// <paramref name="values"/> (an anonymous object, record, or POCO). Property accessors are
    /// cached per type.
    /// </summary>
    [RequiresUnreferencedCode("Reflects over the public properties of an arbitrary object; ensure they're preserved when trimming, or use Render(IReadOnlyDictionary<string, string?>) instead.")]
    public string Render(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var properties = PropertyCache.GetOrAdd(values.GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0).ToArray());

        var dictionary = new Dictionary<string, string?>(properties.Length, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            dictionary[property.Name] = property.GetValue(values)?.ToString();
        }

        return Render(dictionary);
    }

    /// <summary>Renders the template and wraps the result in a <see cref="ChatMessage"/>.</summary>
    public ChatMessage RenderMessage(ChatRole role, object values) => new(role, Render(values));

    /// <summary>Renders the template and wraps the result in a <see cref="ChatMessage"/>.</summary>
    public ChatMessage RenderMessage(ChatRole role, IReadOnlyDictionary<string, string?> values) => new(role, Render(values));

    /// <inheritdoc />
    public override string ToString() => Template;

    private static IReadOnlyList<Segment> Parse(string template)
    {
        var segments = new List<Segment>();
        var literal = new StringBuilder();
        var i = 0;

        while (i < template.Length)
        {
            var c = template[i];

            if (c == '{')
            {
                if (i + 1 < template.Length && template[i + 1] == '{')
                {
                    literal.Append('{');
                    i += 2;
                    continue;
                }

                var close = template.IndexOf('}', i + 1);
                if (close < 0)
                {
                    throw new FormatException($"Unmatched '{{' at position {i} in prompt template.");
                }

                if (literal.Length > 0)
                {
                    segments.Add(Segment.Literal(literal.ToString()));
                    literal.Clear();
                }

                var name = template[(i + 1)..close].Trim();
                if (name.Length == 0)
                {
                    throw new FormatException($"Empty placeholder '{{}}' at position {i} in prompt template.");
                }

                segments.Add(Segment.Placeholder(name));
                i = close + 1;
                continue;
            }

            if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
            {
                literal.Append('}');
                i += 2;
                continue;
            }

            literal.Append(c);
            i++;
        }

        if (literal.Length > 0)
        {
            segments.Add(Segment.Literal(literal.ToString()));
        }

        return segments;
    }

    private readonly struct Segment
    {
        public bool IsPlaceholder { get; }

        public string Text { get; }

        private Segment(bool isPlaceholder, string text)
        {
            IsPlaceholder = isPlaceholder;
            Text = text;
        }

        public static Segment Literal(string text) => new(false, text);

        public static Segment Placeholder(string name) => new(true, name);
    }
}
