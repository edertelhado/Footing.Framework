using System.Data;
using System.Data.Common;
namespace Footing.Framework.Data;

/// <summary>
/// Helper agnóstico para IDbConnection. Evita cast para NpgsqlConnection/FbConnection no app.
/// Dapper já auto-abre em QueryAsync/ExecuteAsync, mas para UnitOfWork/BeginTransaction precisamos abrir.
/// </summary>
public static class DbConnectionExtensions
{
    public static Task OpenAsync(this IDbConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.State == ConnectionState.Open) return Task.CompletedTask;
        if (connection is DbConnection db) return db.OpenAsync(ct);
        connection.Open();
        return Task.CompletedTask;
    }

    public static async ValueTask<IAsyncDisposable> OpenAsyncDisposable(this IDbConnection connection, CancellationToken ct = default)
    {
        await connection.OpenAsync(ct);
        return new ConnectionDisposable(connection);
    }

    private sealed class ConnectionDisposable(IDbConnection conn) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() { conn.Dispose(); return ValueTask.CompletedTask; }
    }
}
