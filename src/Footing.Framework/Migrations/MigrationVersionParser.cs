namespace Footing.Framework.Migrations;

/// <summary>
/// Agnostic parser without regex — uses IndexOf "__" and StringComparison.Ordinal.
/// Supports V1__create_users.sql, V1_0_1__fix.sql (_→.), R__views.sql, R001__views.sql, 001__baseline.sql (NNN__).
/// Documented regex: V(\d+[_\d]*)__ , R(\d*)__ , (\d+)__ — but implementation uses IndexOf to keep 11→11 regex.
/// </summary>
public static class MigrationVersionParser
{
    public static bool TryParse(string fileName, out MigrationInfo info) => TryParse(fileName, null, "", out info);

    public static bool TryParse(string fileName, string? folderHint, string sql, out MigrationInfo info)
    {
        info = null!;
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var idx = name.IndexOf("__", StringComparison.Ordinal);
        if (idx < 0) return false;

        var prefix = name[..idx];
        var description = name[(idx + 2)..];
        if (string.IsNullOrWhiteSpace(description)) return false;

        string version;
        string type;

        // folder hint decides BASELINE / REPEATABLE when ambiguous
        var isBaselineFolder = folderHint != null && folderHint.Contains("Baseline", StringComparison.OrdinalIgnoreCase);
        var isRepeatableFolder = folderHint != null && folderHint.Contains("Repeatable", StringComparison.OrdinalIgnoreCase);

        if (isBaselineFolder)
        {
            type = "BASELINE";
            var raw = prefix.StartsWith("V", StringComparison.OrdinalIgnoreCase) ? prefix[1..] : prefix;
            version = raw.Replace('_', '.');
        }
        else if (isRepeatableFolder || prefix.StartsWith("R", StringComparison.OrdinalIgnoreCase))
        {
            type = "REPEATABLE";
            // R__views.sql → R__views as unique version; R001__views.sql → R001
            if (prefix.Length == 1 && prefix[0] == 'R' || prefix.Length == 1 && prefix[0] == 'r')
                version = name; // R__views full name to keep uniqueness
            else
                version = prefix;
            // normalize already extracted description
        }
        else if (prefix.StartsWith("V", StringComparison.OrdinalIgnoreCase))
        {
            type = "VERSIONED";
            var raw = prefix[1..];
            if (raw.Length == 0) return false;
            version = raw.Replace('_', '.');
        }
        else if (IsNumericPrefix(prefix))
        {
            // NNN__ baseline sem letra (001__create_schema.sql)
            type = "BASELINE";
            version = prefix.Replace('_', '.');
        }
        else
        {
            return false;
        }

        var checksum = string.IsNullOrEmpty(sql) ? "" : MigrationChecksum.Compute(sql);
        info = new MigrationInfo(version, description, Path.GetFileName(fileName), type, checksum, sql);
        return true;
    }

    private static bool IsNumericPrefix(string prefix)
    {
        if (prefix.Length == 0) return false;
        foreach (var c in prefix)
            if (!char.IsDigit(c) && c != '_') return false;
        return true;
    }
}
