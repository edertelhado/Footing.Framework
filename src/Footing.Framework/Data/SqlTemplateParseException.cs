namespace Footing.Framework.Data;

/// <summary>
/// Structural failure in SqlTemplate — unclosed tag, unbalanced braces.
/// Fail-fast: thrown in Parse/Render before SQL generation, with line/col and suggestion.
/// Distinguishes from parameter errors (fail-safe omitted).
/// </summary>
public sealed class SqlTemplateParseException : InvalidOperationException
{
    public string? Tag { get; }
    public string? Expected { get; }
    public int Line { get; }
    public int Column { get; }
    public string? Snippet { get; }
    public string? Suggestion { get; }

    public SqlTemplateParseException(string message, string? tag, string? expected, int line, int column, string? snippet, string? suggestion)
        : base(message)
    {
        Tag = tag;
        Expected = expected;
        Line = line;
        Column = column;
        Snippet = snippet;
        Suggestion = suggestion;
    }

    public SqlTemplateParseException(string message, Exception inner) : base(message, inner) { }
}
