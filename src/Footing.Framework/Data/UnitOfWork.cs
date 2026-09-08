using System.Data;
using System.Data.Common;
namespace Footing.Framework.Data;

/// <summary>
/// Explicit Dapper transaction: opens the connection, begins the transaction and exposes
/// <see cref="Connection"/> + <see cref="Transaction"/> for Dapper calls.
/// Automatic rollback in <see cref="DisposeAsync"/> if <see cref="CommitAsync"/> was not called.
/// </summary>
public sealed class UnitOfWork : IAsyncDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _committed;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connection = connectionFactory.CreateConnection();
    }

    public IDbConnection Connection => _connection;

    public IDbTransaction? Transaction => _transaction;

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null) return;

        await _connection.OpenAsync(cancellationToken);

        _transaction = _connection.BeginTransaction();
    }

    public Task CommitAsync()
    {
        _transaction?.Commit();
        _committed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_transaction is not null && !_committed)
            _transaction.Rollback();

        _transaction?.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
