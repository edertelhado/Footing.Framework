// Exemplo docs — NpgsqlOutboxStore (não pack) — app escolhe NPgSQL
using System.Data;using Dapper;using Footing.Framework.Data;using Footing.Framework.Outbox;
public class NpgsqlOutboxStore(IDbConnectionFactory f):IOutboxStore{
 public async Task SaveAsync(OutboxMessage m,IDbTransaction? tx=null,CancellationToken ct=default){var c=tx?.Connection??f.CreateConnection();await c.ExecuteAsync("INSERT INTO Outbox (Id,Type,Payload,CreatedAt) VALUES (@Id,@Type,@Payload::jsonb,@CreatedAt)",m,tx);}
 public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default){using var c=f.CreateConnection();return (await c.QueryAsync<OutboxMessage>("SELECT * FROM Outbox WHERE ProcessedAt IS NULL ORDER BY CreatedAt LIMIT @b FOR UPDATE SKIP LOCKED",new{b})).AsList();}
 public Task MarkProcessedAsync(Guid id,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET ProcessedAt=NOW() WHERE Id=@id",new{id});}
 public Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET Attempts=Attempts+1,LastError=@e WHERE Id=@id",new{id,e});}
}
