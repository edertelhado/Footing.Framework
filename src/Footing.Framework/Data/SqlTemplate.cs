using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Footing.Framework.Data;

public partial class SqlTemplate
{
    private readonly string _template;

    private static readonly ConcurrentDictionary<string, string> FragmentCache = new(StringComparer.OrdinalIgnoreCase);

    public static void RegisterFragment(string name, string sql)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sql);
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^\w+$"))
            throw new ArgumentException("Fragment name must match ^\\w+$", nameof(name));
        FragmentCache[name] = sql;
    }

    public static void ClearFragments() => FragmentCache.Clear();

    private SqlTemplate(string template) => _template = template;

    public static SqlTemplate Parse(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ValidateTemplate(sql);
        return new(sql);
    }

    public SqlResult Render(object? parameters = null)
    {
        var paramDict = ToDictionary(parameters);
        var sql = RenderTemplate(_template, paramDict);
        // Return paramDict so that BIND injected vars are visible (required for DoD) — Dapper accepts IDictionary
        return new SqlResult(sql, paramDict);
    }

    private static string RenderTemplate(string template, Dictionary<string, object?> paramDict)
    {
        ValidateTemplate(template);
        template = RenderBindBlocks(template, paramDict);
        template = RenderIncludeBlocks(template);
        template = RenderTrimBlocks(template, paramDict);
        template = RenderSetBlocks(template, paramDict);
        template = RenderWhereBlocks(template, paramDict);
        template = RenderIfDefinedBlocks(template, paramDict);
        template = RenderIfBlocks(template, paramDict);
        template = RenderInBlocks(template, paramDict);
        template = RenderChooseBlocks(template, paramDict);
        template = RenderBetweenBlocks(template, paramDict);
        template = CleanupEmptyLines(template);
        return template.Trim();
    }

    private static string RenderBindBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = BindRegex();
        return regex.Replace(template, match =>
        {
            var varName = match.Groups[1].Value.Trim();
            var valueExpr = match.Groups[2].Value.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(varName, @"^[A-Za-z_]\w*$"))
                return "";
            if (valueExpr.Contains(';') || valueExpr.Contains("--") || valueExpr.Contains("/*"))
                return "";
            var evaluated = EvaluateBindValue(valueExpr, paramDict);
            paramDict[varName] = evaluated;
            return "";
        });
    }

    private static string EvaluateBindValue(string expr, Dictionary<string, object?> paramDict)
    {
        var parts = new List<string>();
        bool inSingle = false, inDouble = false;
        var sb = new StringBuilder();
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '\'' && !inDouble) { inSingle = !inSingle; sb.Append(c); continue; }
            if (c == '"' && !inSingle) { inDouble = !inDouble; sb.Append(c); continue; }
            if (!inSingle && !inDouble && c == '+')
            {
                parts.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }
            sb.Append(c);
        }
        parts.Add(sb.ToString().Trim());
        var result = new StringBuilder();
        foreach (var raw in parts)
        {
            var p = raw.Trim();
            if (string.IsNullOrEmpty(p)) continue;
            if (p.Length >= 2 && ((p.StartsWith("'") && p.EndsWith("'")) || (p.StartsWith("\"") && p.EndsWith("\""))))
            {
                result.Append(p.Substring(1, p.Length - 2));
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(p, @"^\w+$"))
            {
                if (paramDict.TryGetValue(p, out var val) && val != null)
                    result.Append(val.ToString());
                else
                    result.Append("");
            }
            else
            {
                // unexpected token -> ignore fail-safe
                // check for injection again
                if (p.Contains(';') || p.Contains("--") || p.Contains("/*")) continue;
                result.Append(p);
            }
        }
        return result.ToString();
    }

    private static string RenderIncludeBlocks(string template)
        => RenderIncludeBlocksInternal(template, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);

    private static string RenderIncludeBlocksInternal(string template, HashSet<string> visited, int depth)
    {
        if (depth > 5) return template;
        var regex = IncludeRegex();
        return regex.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (visited.Contains(name)) return match.Value; // loop guard -> literal
            if (!FragmentCache.TryGetValue(name, out var fragmentSql)) return match.Value; // miss -> fail-safe literal
            visited.Add(name);
            var expanded = RenderIncludeBlocksInternal(fragmentSql, new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase), depth + 1);
            visited.Remove(name);
            return expanded;
        });
    }

    private static string RenderTrimBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = TrimRegex();
        return regex.Replace(template, match =>
        {
            string opening = match.Value.Substring(0, match.Value.IndexOf('}') + 1);
            // content is last group (inner)
            string content = match.Groups[match.Groups.Count - 1].Value;
            string? prefix = ExtractTrimAttr(opening, "prefix");
            string? prefixOverrides = ExtractTrimAttr(opening, "prefixOverrides");
            string? suffix = ExtractTrimAttr(opening, "suffix");
            string? suffixOverrides = ExtractTrimAttr(opening, "suffixOverrides");
            var rendered = RenderConditionsInBlock(content, paramDict);
            if (string.IsNullOrWhiteSpace(rendered)) return "";
            return TrimContent(rendered, prefix, prefixOverrides, suffix, suffixOverrides);
        });
    }

    private static string RenderSetBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = SetRegexManual;
        return regex.Replace(template, match =>
        {
            var content = match.Groups[1].Value;
            var rendered = RenderConditionsInBlock(content, paramDict);
            if (string.IsNullOrWhiteSpace(rendered)) return "";
            return TrimContent(rendered, "SET", null, null, ",");
        });
    }

    private static string? ExtractTrimAttr(string opening, string attrName)
    {
        var m = System.Text.RegularExpressions.Regex.Match(opening, attrName + @"=""([^""]*)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string TrimContent(string content, string? prefix, string? prefixOverrides, string? suffix, string? suffixOverrides)
    {
        if (string.IsNullOrWhiteSpace(content)) return "";
        // WHERE-specific path preserves original joining with " AND "
        if (string.Equals(prefix?.Trim(), "WHERE", StringComparison.OrdinalIgnoreCase))
        {
            var lines = content.Split('\n');
            var result = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (!string.IsNullOrEmpty(prefixOverrides))
                {
                    var overrides = prefixOverrides.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var ov in overrides)
                    {
                        var ovTrim = ov.Trim();
                        if (string.IsNullOrEmpty(ovTrim)) continue;
                        if (trimmed.StartsWith(ovTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            trimmed = trimmed.Substring(ovTrim.Length).Trim();
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            content = string.Join(" AND ", result);
            if (!string.IsNullOrEmpty(suffixOverrides))
            {
                var overrides = suffixOverrides.Split('|', StringSplitOptions.RemoveEmptyEntries);
                bool removed;
                do
                {
                    removed = false;
                    var t = content.TrimEnd();
                    foreach (var ov in overrides)
                    {
                        var ovTrim = ov.Trim();
                        if (t.EndsWith(ovTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            content = t.Substring(0, t.Length - ovTrim.Length).TrimEnd();
                            removed = true;
                            break;
                        }
                    }
                } while (removed);
            }
            if (!string.IsNullOrWhiteSpace(content) && !string.IsNullOrEmpty(prefix))
                content = prefix.Trim() + " " + content.Trim();
            if (!string.IsNullOrWhiteSpace(content) && !string.IsNullOrEmpty(suffix))
                content = content.Trim() + " " + suffix.Trim();
            return content.Trim();
        }
        else
        {
            content = content.Trim();
            if (!string.IsNullOrEmpty(prefixOverrides))
            {
                var overrides = prefixOverrides.Split('|', StringSplitOptions.RemoveEmptyEntries);
                bool removed;
                do
                {
                    removed = false;
                    var trimmed = content.TrimStart();
                    foreach (var ov in overrides)
                    {
                        var ovTrim = ov.Trim();
                        if (string.IsNullOrEmpty(ovTrim)) continue;
                        if (trimmed.StartsWith(ovTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            content = trimmed.Substring(ovTrim.Length).TrimStart();
                            removed = true;
                            break;
                        }
                    }
                } while (removed);
            }
            if (!string.IsNullOrEmpty(suffixOverrides))
            {
                var overrides = suffixOverrides.Split('|', StringSplitOptions.RemoveEmptyEntries);
                bool removed;
                do
                {
                    removed = false;
                    var trimmed = content.TrimEnd();
                    foreach (var ov in overrides)
                    {
                        var ovTrim = ov.Trim();
                        if (trimmed.EndsWith(ovTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            content = trimmed.Substring(0, trimmed.Length - ovTrim.Length).TrimEnd();
                            removed = true;
                            break;
                        }
                    }
                } while (removed);
            }
            // Normalize whitespace (collapse newlines)
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();
            if (!string.IsNullOrWhiteSpace(content) && !string.IsNullOrEmpty(prefix))
                content = prefix.Trim() + " " + content.Trim();
            if (!string.IsNullOrWhiteSpace(content) && !string.IsNullOrEmpty(suffix))
                content = content.Trim() + " " + suffix.Trim();
            return content.Trim();
        }
    }

    private static void ValidateTemplate(string template)
    {
        var stack = new Stack<(string Tag, string Expected, int Line, int Column, int Index)>();

        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] != '{') continue;

            // literal {{ -> skip escaped
            if (i + 1 < template.Length && template[i + 1] == '{')
            {
                i++; // skip second {
                continue;
            }

            int close = template.IndexOf('}', i + 1);
            if (close == -1)
            {
                int line = CountNewLines(template, i) + 1;
                int col = ComputeColumn(template, i);
                int snippetStart = Math.Max(0, i - 30);
                int snippetLen = Math.Min(60, template.Length - snippetStart);
                string snippet = template.Substring(snippetStart, snippetLen);
                string tagText = template.Substring(i, Math.Min(30, template.Length - i));
                string message = $"Unclosed tag at line {line} col {col}: '{tagText}' — missing '}}'. Snippet: \"{snippet}\" Suggestion: Add '}}'.";
                throw new SqlTemplateParseException(message, tagText, "}", line, col, snippet, "Add '}'");
            }

            // If inner contains '{' before '}' => outer tag not closed (e.g., "{IF:Name AND x=1 {END}")
            int nextOpen = template.IndexOf('{', i + 1);
            if (nextOpen != -1 && nextOpen < close)
            {
                int line = CountNewLines(template, i) + 1;
                int col = ComputeColumn(template, i);
                int snippetStart = Math.Max(0, i - 30);
                int snippetLen = Math.Min(60, template.Length - snippetStart);
                string snippet = template.Substring(snippetStart, snippetLen);
                string tagText = template.Substring(i, Math.Min(30, nextOpen - i));
                // garantir que tagText contenha '{' inicial
                if (!tagText.StartsWith("{")) tagText = "{" + tagText;
                string message = $"Unclosed tag at line {line} col {col}: '{tagText}' — missing '}}'. Snippet: \"{snippet}\" Suggestion: Add '}}'.";
                throw new SqlTemplateParseException(message, tagText, "}", line, col, snippet, "Add '}'");
            }

            string inner = template.Substring(i + 1, close - i - 1);
            string innerTrim = inner.Trim();

            bool isOpeningIf = innerTrim.StartsWith("IF:", StringComparison.OrdinalIgnoreCase);
            bool isOpeningIfDefined = innerTrim.StartsWith("IFDEFINED:", StringComparison.OrdinalIgnoreCase) || innerTrim.StartsWith("IFNOTDEFINED:", StringComparison.OrdinalIgnoreCase);
            bool isOpeningWhere = innerTrim.Equals("WHERE", StringComparison.OrdinalIgnoreCase);
            bool isOpeningChoose = innerTrim.Equals("CHOOSE", StringComparison.OrdinalIgnoreCase);
            bool isOpeningWhen = innerTrim.StartsWith("WHEN:", StringComparison.OrdinalIgnoreCase);
            bool isOpeningBetween = innerTrim.StartsWith("BETWEEN:", StringComparison.OrdinalIgnoreCase);
            bool isOpeningTrim = innerTrim.StartsWith("TRIM", StringComparison.OrdinalIgnoreCase);
            bool isOpeningSet = innerTrim.Equals("SET", StringComparison.OrdinalIgnoreCase);
            bool isOpeningInclude = innerTrim.StartsWith("INCLUDE:", StringComparison.OrdinalIgnoreCase) || innerTrim.StartsWith("SQL:", StringComparison.OrdinalIgnoreCase);
            bool isOpeningBind = innerTrim.StartsWith("BIND:", StringComparison.OrdinalIgnoreCase);

            bool isClosingEnd = innerTrim.Equals("END", StringComparison.OrdinalIgnoreCase);
            bool isClosingEndWhere = innerTrim.Equals("ENDWHERE", StringComparison.OrdinalIgnoreCase);
            bool isClosingEndChoose = innerTrim.Equals("ENDCHOOSE", StringComparison.OrdinalIgnoreCase);
            bool isClosingEndWhen = innerTrim.Equals("ENDWHEN", StringComparison.OrdinalIgnoreCase);
            bool isClosingEndTrim = innerTrim.Equals("ENDTRIM", StringComparison.OrdinalIgnoreCase);
            bool isClosingEndSet = innerTrim.Equals("ENDSET", StringComparison.OrdinalIgnoreCase);

            string tagTextFull = "{" + inner + "}";
            int tagLine = CountNewLines(template, i) + 1;
            int tagCol = ComputeColumn(template, i);

            if (isOpeningIf)
            {
                stack.Push((tagTextFull, "{END}", tagLine, tagCol, i));
            }
            else if (isOpeningIfDefined)
            {
                stack.Push((tagTextFull, "{END}", tagLine, tagCol, i));
            }
            else if (isOpeningWhere)
            {
                stack.Push((tagTextFull, "{ENDWHERE}", tagLine, tagCol, i));
            }
            else if (isOpeningChoose)
            {
                stack.Push((tagTextFull, "{ENDCHOOSE}", tagLine, tagCol, i));
            }
            else if (isOpeningWhen)
            {
                stack.Push((tagTextFull, "{ENDWHEN}", tagLine, tagCol, i));
            }
            else if (isOpeningBetween)
            {
                stack.Push((tagTextFull, "{END}", tagLine, tagCol, i));
            }
            else if (isOpeningTrim)
            {
                stack.Push((tagTextFull, "{ENDTRIM}", tagLine, tagCol, i));
            }
            else if (isOpeningSet)
            {
                stack.Push((tagTextFull, "{ENDSET}", tagLine, tagCol, i));
            }
            else if (isOpeningInclude || isOpeningBind)
            {
                // self-closing, no stack
            }
            else if (isClosingEnd || isClosingEndWhere || isClosingEndChoose || isClosingEndWhen || isClosingEndTrim || isClosingEndSet)
            {
                string closingFull = "{" + innerTrim + "}";
                if (stack.Count == 0)
                {
                    // stray closing without opening — ignore (fail-safe for stray)
                }
                else
                {
                    var top = stack.Peek();
                    if (string.Equals(top.Expected, closingFull, StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Pop();
                    }
                    else
                    {
                        int snippetStart2 = Math.Max(0, i - 30);
                        int snippetLen2 = Math.Min(60, template.Length - snippetStart2);
                        string snippet2 = template.Substring(snippetStart2, snippetLen2);
                        string message2 = $"Missing {top.Expected} for {top.Tag} opened at line {top.Line} col {top.Column}. Found {closingFull} at line {tagLine} col {tagCol} instead. Snippet: \"{snippet2}\" Suggestion: add '{top.Expected}' before '{closingFull}'.";
                        throw new SqlTemplateParseException(message2, top.Tag, top.Expected, top.Line, top.Column, snippet2, $"add '{top.Expected}'");
                    }
                }
            }
            else
            {
                // unknown tag like {FOO:Bar} or {OTHERWISE} -> ignore forward-compat
            }

            i = close;
        }

        if (stack.Count > 0)
        {
            var top = stack.Peek();
            int snippetStart = Math.Max(0, top.Index - 30);
            int snippetLen = Math.Min(60, template.Length - snippetStart);
            string snippet = template.Substring(snippetStart, snippetLen);
            string message = $"Missing {top.Expected} for {top.Tag} opened at line {top.Line} col {top.Column}. Expected '{top.Expected}' to close block before EOF. Snippet: \"{snippet}\" Suggestion: add '{top.Expected}'.";
            throw new SqlTemplateParseException(message, top.Tag, top.Expected, top.Line, top.Column, snippet, $"add '{top.Expected}'");
        }
    }

    private static int CountNewLines(string s, int upToIndex)
    {
        int c = 0;
        for (int i = 0; i < upToIndex && i < s.Length; i++) if (s[i] == '\n') c++;
        return c;
    }

    private static int ComputeColumn(string s, int index)
    {
        int lastNl = -1;
        for (int i = index - 1; i >= 0; i--) if (s[i] == '\n') { lastNl = i; break; }
        return lastNl == -1 ? index + 1 : index - lastNl;
    }

    private static string RenderWhereBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = WhereRegex();

        return regex.Replace(template, match =>
        {
            var content = match.Groups[1].Value.Trim();
            var rendered = RenderConditionsInBlock(content, paramDict);
            if (string.IsNullOrWhiteSpace(rendered))
                return "";

            rendered = TrimContent(rendered, "WHERE", "AND|OR ", null, null);
            return rendered;
        });
    }

    private static string RenderConditionsInBlock(string content, Dictionary<string, object?> paramDict)
    {
        content = RenderIfDefinedBlocks(content, paramDict);
        content = RenderIfBlocks(content, paramDict);
        content = RenderInBlocks(content, paramDict);
        content = RenderChooseBlocks(content, paramDict);
        content = RenderBetweenBlocks(content, paramDict);
        return content;
    }

    private static string RenderIfDefinedBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = IfDefinedRegex();
        return regex.Replace(template, match =>
        {
            // match.Groups[0] = full, [1] = param name (with optional NOT prefix inside regex?), but our regex \{(?:NOT)?... has param in group 1
            // For \{IF(?:NOT)?DEFINED:(\w+)\} group 1 is param
            var fullTag = match.Value;
            bool isNotDefined = fullTag.StartsWith("{IFNOTDEFINED", StringComparison.OrdinalIgnoreCase);
            var paramName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            bool isDefined = IsDefined(paramName, paramDict);
            bool shouldRender = isNotDefined ? !isDefined : isDefined;
            return shouldRender ? content : "";
        });
    }

    private static bool IsDefined(string paramName, Dictionary<string, object?> paramDict)
        => paramDict.ContainsKey(paramName);

    private static string NormalizeWhereContent(string content)
    {
        var lines = content.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.StartsWith("AND ", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[4..].Trim();
            else if (trimmed.StartsWith("OR ", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[3..].Trim();

            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }

        return string.Join(" AND ", result);
    }

    private static string RenderIfBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = IfRegex();
        return regex.Replace(template, match =>
        {
            var expr = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            return EvaluateCondition(expr, paramDict) ? content : "";
        });
    }

    private static string RenderInBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = InRegex();

        return regex.Replace(template, match =>
        {
            var paramName = match.Groups[1].Value;
            var content = match.Groups[2].Value.Trim();

            if (!paramDict.TryGetValue(paramName, out var val) || val == null)
                return "";

            if (val is string) return "";
            if (val is IEnumerable enumerable && val is not string)
            {
                var hasItems = false;
                var enumerator = enumerable.GetEnumerator();
                using (enumerator as IDisposable)
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != null) { hasItems = true; break; }
                    }
                }
                return hasItems ? content : "";
            }
            return "";
        });
    }

    private static string RenderChooseBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = ChooseRegex();

        return regex.Replace(template, match =>
        {
            var body = match.Groups[1].Value;
            var whenRegex = WhenRegex();
            var whenMatches = whenRegex.Matches(body);
            var otherwiseMatch = OtherwiseRegex().Match(body);

            foreach (System.Text.RegularExpressions.Match whenMatch in whenMatches)
            {
                var expr = whenMatch.Groups[1].Value;
                var content = whenMatch.Groups[2].Value;
                if (EvaluateCondition(expr, paramDict))
                    return content;
            }

            return otherwiseMatch.Success ? otherwiseMatch.Groups[1].Value : "";
        });
    }

    private static string RenderBetweenBlocks(string template, Dictionary<string, object?> paramDict)
    {
        var regex = BetweenRegex();
        return regex.Replace(template, match =>
        {
            var baseName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            bool hasMin = paramDict.TryGetValue(baseName + "Min", out var min) && min != null && !(min is string sm && string.IsNullOrEmpty(sm));
            bool hasMax = paramDict.TryGetValue(baseName + "Max", out var max) && max != null && !(max is string sx && string.IsNullOrEmpty(sx));
            if (hasMin && hasMax) return content;
            return "";
        });
    }

    private static string CleanupEmptyLines(string template)
    {
        var lines = template.Split('\n');
        var result = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.AppendLine(line);
        }

        return result.ToString();
    }

    private static Dictionary<string, object?> ToDictionary(object? parameters)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters == null) return dict;

        if (parameters is IDictionary<string, object?> d1)
        {
            foreach (var kv in d1) dict[kv.Key] = kv.Value;
            return dict;
        }
        if (parameters is IDictionary<string, object> d2)
        {
            foreach (var kv in d2) dict[kv.Key] = kv.Value;
            return dict;
        }
        if (parameters is System.Collections.IDictionary d)
        {
            foreach (System.Collections.DictionaryEntry kv in d)
                if (kv.Key is string k) dict[k] = kv.Value;
            return dict;
        }

        var props = PropsCacheHelper.Get(parameters.GetType());
        foreach (var p in props) dict[p.Name] = p.GetValue(parameters);
        return dict;
    }

    private static bool EvaluateCondition(string expr, Dictionary<string, object?> p)
    {
        expr = expr.Trim();
        // Won't 3.0 — rejects or/()/! with Warning false (keeps fail-safe omitted)
        if (ContainsOrOutsideQuotes(expr))
        {
            System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] 'or' operator not supported (Won't 3.0) → false. Expr: {expr}");
            return false;
        }
        if (ContainsUnsupportedParensOutsideQuotes(expr))
        {
            System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] parentheses '()' not supported (Won't 3.0) → false. Expr: {expr}");
            return false;
        }
        if (ContainsUnsupportedExclamationOutsideQuotes(expr))
        {
            System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] '!' operator not supported (Won't 3.0) → false. Expr: {expr}");
            return false;
        }
        var andIndices = FindAndIndicesOutsideQuotes(expr);
        if (andIndices.Count == 1)
        {
            var idx = andIndices[0];
            var left = expr.Substring(0, idx).Trim();
            var right = expr.Substring(idx + 3).Trim();
            // short-circuit: if left false, does not evaluate right
            if (!EvaluateSingle(left, p)) return false;
            return EvaluateSingle(right, p);
        }
        else if (andIndices.Count > 1)
        {
            System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] 'and' with 3+ conditions not supported (only 1 and, 2 factors) → false. Expr: {expr}");
            return false;
        }
        return EvaluateSingle(expr, p);
    }

    private static bool EvaluateSingle(string expr, Dictionary<string, object?> p)
    {
        expr = expr.Trim();
        // defined(Param) helper — ContainsKey distinction missing vs null
        var mDef = Regex.Match(expr, @"^defined\s*\(\s*(\w+)\s*\)$", RegexOptions.IgnoreCase);
        if (mDef.Success)
        {
            var defName = mDef.Groups[1].Value;
            return IsDefined(defName, p);
        }
        var mLen = Regex.Match(expr, @"^length\s*\(\s*(\w+)\s*\)\s*(==|!=|<>|>=|<=|>|<|=|(?i:gte|lte|gt|lt|ge|le))\s*(.+)$", RegexOptions.IgnoreCase);
        if (mLen.Success)
        {
            var paramName = mLen.Groups[1].Value;
            var opLen = mLen.Groups[2].Value.ToLowerInvariant();
            var rightRawLen = mLen.Groups[3].Value.Trim();
            var leftLenVal = (decimal)EvaluateLengthParam(paramName, p);
            object? rightValLen; string? litLen = null; bool isNullLen = false;
            if (string.Equals(rightRawLen, "null", StringComparison.OrdinalIgnoreCase)) { isNullLen = true; rightValLen = null; }
            else if ((rightRawLen.StartsWith("'") && rightRawLen.EndsWith("'") && rightRawLen.Length >= 2) || (rightRawLen.StartsWith("\"") && rightRawLen.EndsWith("\"") && rightRawLen.Length >= 2)) { litLen = rightRawLen[1..^1]; rightValLen = litLen; }
            else if (rightRawLen.StartsWith("@")) { var n = rightRawLen[1..]; p.TryGetValue(n, out rightValLen); }
            else if (decimal.TryParse(rightRawLen, NumberStyles.Any, CultureInfo.InvariantCulture, out var numLen)) { rightValLen = numLen; }
            else if (string.Equals(rightRawLen, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(rightRawLen, "false", StringComparison.OrdinalIgnoreCase)) { rightValLen = bool.Parse(rightRawLen); }
            else if (Regex.IsMatch(rightRawLen, @"^\w+$")) { p.TryGetValue(rightRawLen, out rightValLen); }
            else { litLen = rightRawLen; rightValLen = rightRawLen; }
            if (opLen == "=") opLen = "==";
            if (opLen == "<>") opLen = "!=";
            if (opLen == "gt") opLen = ">";
            if (opLen == "lt") opLen = "<";
            if (opLen == "gte" || opLen == "ge") opLen = ">=";
            if (opLen == "lte" || opLen == "le") opLen = "<=";
            if (isNullLen) return opLen == "!=";
            if (rightValLen == null) return opLen == "!=";
            if (opLen is ">" or "<" or ">=" or "<=")
            {
                if (!IsOrderingComparable(leftLenVal, rightValLen))
                {
                    System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] length({paramName}) {opLen} incompatible types → false");
                    return false;
                }
            }
            return opLen switch { "==" => AreEqual(leftLenVal, rightValLen, litLen), "!=" => !AreEqual(leftLenVal, rightValLen, litLen), ">" => Compare(leftLenVal, rightValLen) > 0, "<" => Compare(leftLenVal, rightValLen) < 0, ">=" => Compare(leftLenVal, rightValLen) >= 0, "<=" => Compare(leftLenVal, rightValLen) <= 0, _ => false };
        }
        if (Regex.IsMatch(expr, @"^\w+$"))
        {
            if (!p.TryGetValue(expr, out var v) || v == null) return false;
            if (v is string s && string.IsNullOrEmpty(s)) return false;
            return true;
        }
        var m = Regex.Match(expr, @"^(\w+)\s*(==|!=|<>|>=|<=|>|<|=|(?i:gte|lte|gt|lt|ge|le))\s*(.+)$", RegexOptions.IgnoreCase);
        if (!m.Success) return false;
        var leftName = m.Groups[1].Value;
        var op = m.Groups[2].Value.ToLowerInvariant();
        var rightRaw = m.Groups[3].Value.Trim();
        if (!p.TryGetValue(leftName, out var leftVal)) leftVal = null;
        object? rightVal; string? lit = null; bool isNull = false;
        if (string.Equals(rightRaw, "null", StringComparison.OrdinalIgnoreCase)) { isNull = true; rightVal = null; }
        else if ((rightRaw.StartsWith("'") && rightRaw.EndsWith("'") && rightRaw.Length >= 2) || (rightRaw.StartsWith("\"") && rightRaw.EndsWith("\"") && rightRaw.Length >= 2)) { lit = rightRaw[1..^1]; rightVal = lit; }
        else if (rightRaw.StartsWith("@")) { var n = rightRaw[1..]; p.TryGetValue(n, out rightVal); }
        else if (decimal.TryParse(rightRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num)) { rightVal = num; }
        else if (string.Equals(rightRaw, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(rightRaw, "false", StringComparison.OrdinalIgnoreCase)) { rightVal = bool.Parse(rightRaw); }
        else if (Regex.IsMatch(rightRaw, @"^\w+$")) { p.TryGetValue(rightRaw, out rightVal); }
        else { lit = rightRaw; rightVal = rightRaw; }
        if (op == "=") op = "==";
        if (op == "<>") op = "!=";
        if (op == "gt") op = ">";
        if (op == "lt") op = "<";
        if (op == "gte" || op == "ge") op = ">=";
        if (op == "lte" || op == "le") op = "<=";
        if (isNull) return op == "==" ? leftVal == null : op == "!=" ? leftVal != null : false;
        if (leftVal == null && rightVal != null) return op == "!=";
        if (leftVal != null && rightVal == null) return op == "!=";
        if (leftVal == null && rightVal == null) return op == "=="; // C fix: aligned to SQL UNKNOWN, only == is true
        // C fix: > < >= <= only true if IsOrderingComparable
        if (op is ">" or "<" or ">=" or "<=")
        {
            if (!IsOrderingComparable(leftVal, rightVal))
            {
                System.Diagnostics.Debug.WriteLine($"[SqlTemplate Warning] Operator '{op}' with incompatible types ({leftVal?.GetType().Name ?? "null"} vs {rightVal?.GetType().Name ?? "null"}) — returns false. Use ==/!= for strings or convert to numeric/date. Expr: {expr}");
                return false;
            }
        }
        return op switch { "==" => AreEqual(leftVal, rightVal, lit), "!=" => !AreEqual(leftVal, rightVal, lit), ">" => Compare(leftVal, rightVal) > 0, "<" => Compare(leftVal, rightVal) < 0, ">=" => Compare(leftVal, rightVal) >= 0, "<=" => Compare(leftVal, rightVal) <= 0, _ => false };
    }

    // Helpers — split " and " case-insensitive outside quotes ('', ""), short-circuit, keeps 7 regex
    private static List<int> FindAndIndicesOutsideQuotes(string expr)
    {
        var indices = new List<int>();
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (c == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (inSingle || inDouble) continue;
            if (i + 3 <= expr.Length && string.Equals(expr.Substring(i, 3), "and", StringComparison.OrdinalIgnoreCase))
            {
                bool leftOk = i == 0 || char.IsWhiteSpace(expr[i - 1]);
                bool rightOk = i + 3 == expr.Length || char.IsWhiteSpace(expr[i + 3]);
                if (leftOk && rightOk)
                {
                    indices.Add(i);
                    i += 2; // advance to avoid overlapping (for +1 loop)
                }
            }
        }
        return indices;
    }

    private static bool ContainsOrOutsideQuotes(string expr)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (c == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (inSingle || inDouble) continue;
            if (i + 2 <= expr.Length && string.Equals(expr.Substring(i, 2), "or", StringComparison.OrdinalIgnoreCase))
            {
                bool leftOk = i == 0 || char.IsWhiteSpace(expr[i - 1]);
                bool rightOk = i + 2 == expr.Length || char.IsWhiteSpace(expr[i + 2]);
                if (leftOk && rightOk) return true;
            }
        }
        return false;
    }

    private static bool ContainsUnsupportedParensOutsideQuotes(string expr)
    {
        // Remove valid length(Param) and defined(Param) constructs before checking '('
        var withoutLength = Regex.Replace(expr, @"(?i)length\s*\(\s*\w+\s*\)", "");
        withoutLength = Regex.Replace(withoutLength, @"(?i)defined\s*\(\s*\w+\s*\)", "");
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < withoutLength.Length; i++)
        {
            char c = withoutLength[i];
            if (c == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (c == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (inSingle || inDouble) continue;
            if (c == '(' || c == ')') return true;
        }
        return false;
    }

    private static bool ContainsUnsupportedExclamationOutsideQuotes(string expr)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (c == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (inSingle || inDouble) continue;
            if (c == '!')
            {
                // "!=" is valid — next char '=' indicates != operator; ignore
                if (i + 1 < expr.Length && expr[i + 1] == '=') continue;
                return true;
            }
        }
        return false;
    }

    private static bool IsOrderingComparable(object? a, object? b) =>
        (TryToDecimal(a, out _) && TryToDecimal(b, out _)) ||
        (TryToDateTime(a, out _) && TryToDateTime(b, out _)) ||
        (a is string && b is string) ||
        (a != null && b != null && a.GetType() == b.GetType() && a is IComparable);

    private static bool AreEqual(object? a, object? b, string? literal)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
            return da == db;
        if (a is bool ba && b is bool bb) return ba == bb;
        // if literal was quoted, compare as string ordinal
        if (literal != null)
            return string.Equals(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
        // fallback ordinal string compare
        return string.Equals(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
    }

    private static int Compare(object? a, object? b)
    {
        // 1) numeric (includes numeric string)
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
            return da.CompareTo(db);
        // 2) DateTime (covers DateTime, DateTimeOffset and string date)
        if (TryToDateTime(a, out var dta) && TryToDateTime(b, out var dtb))
            return dta.CompareTo(dtb);
        // 3) both strings → intentional Ordinal lexical (e.g., 'eder' > 'adam')
        if (a is string sa && b is string sb)
            return string.Compare(sa, sb, StringComparison.Ordinal);
        // 4) same IComparable type (e.g., bool, DateTime already handled) → try
        if (a != null && b != null && a.GetType() == b.GetType() && a is IComparable ca)
        {
            try { return ca.CompareTo(b); } catch { }
        }
        return string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
    }

    private static bool IsNumeric(object? v) => v is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool TryToDecimal(object? v, out decimal d)
    {
        if (v is decimal dec) { d = dec; return true; }
        if (IsNumeric(v)) { try { d = Convert.ToDecimal(v, CultureInfo.InvariantCulture); return true; } catch { } }
        if (v is string s && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) { d = parsed; return true; }
        d = 0;
        return false;
    }

    private static bool TryToDateTime(object? v, out DateTime d)
    {
        if (v is DateTime dt) { d = dt; return true; }
        if (v is DateTimeOffset dto) { d = dto.DateTime; return true; }
        if (v is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) { d = parsed; return true; }
        if (v is string s2 && DateTime.TryParse(s2, CultureInfo.CurrentCulture, DateTimeStyles.None, out var p2)) { d = p2; return true; }
        d = default;
        return false;
    }

    private static int EvaluateLengthParam(string paramName, Dictionary<string, object?> p)
    {
        if (!p.TryGetValue(paramName, out var v) || v == null) return 0;
        if (v is string s) return s.Length;
        return v.ToString()!.Length;
    }

    [GeneratedRegex(@"\{IF:([^}]+)\}(.*?)\{END\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex IfRegex();

    [GeneratedRegex(@"\{IN:(\w+)\}(.*?)\{END\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex InRegex();

    [GeneratedRegex(@"\{WHERE\}(.*?)\{ENDWHERE\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex WhereRegex();

    [GeneratedRegex(@"\{CHOOSE\}(.*?)\{ENDCHOOSE\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ChooseRegex();

    [GeneratedRegex(@"\{WHEN:([^}]+)\}(.*?)\{ENDWHEN\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex WhenRegex();

    [GeneratedRegex(@"\{OTHERWISE\}(.*?)\{ENDOTHERWISE\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex OtherwiseRegex();

    [GeneratedRegex(@"\{BETWEEN:(\w+)\}(.*?)\{END\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BetweenRegex();

    [GeneratedRegex(@"\{(?:INCLUDE|SQL):(\w+)\}", RegexOptions.IgnoreCase)]
    private static partial Regex IncludeRegex();

    [GeneratedRegex(@"\{TRIM(\s+prefix=""[^""]*"")?(\s+prefixOverrides=""[^""]*"")?(\s+suffix=""[^""]*"")?(\s+suffixOverrides=""[^""]*"")?\}(.*?)\{ENDTRIM\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TrimRegex();

    private static readonly System.Text.RegularExpressions.Regex SetRegexManual = new(@"\{SET\}(.*?)\{ENDSET\}", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    [GeneratedRegex(@"\{BIND:(\w+),\s*value=(.+?)\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BindRegex();

    [GeneratedRegex(@"\{IF(?:NOT)?DEFINED:(\w+)\}(.*?)\{END\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex IfDefinedRegex();
}
