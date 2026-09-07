using System.Security.Cryptography;
using System.Text;

namespace Footing.Framework.Migrations;

public static class MigrationChecksum
{
    public static string Compute(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var normalized = sql.Replace("\r\n", "\n").Replace("\r", "\n");
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
