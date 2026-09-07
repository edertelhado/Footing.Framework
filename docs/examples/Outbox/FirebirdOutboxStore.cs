// Exemplo docs — FirebirdOutboxStore (não pack)
using System.Data;using Dapper;using Footing.Framework.Data;using Footing.Framework.Outbox;
using FirebirdSql.Data.FirebirdClient;
public class FirebirdOutboxStore(FbConnectionFactory f):IOutboxStore{
 public async Task SaveAsync(OutboxMessage m,IDbTransaction? tx=null,CancellationToken ct=default){var c=tx?.Connection as FbConnection??f.CreateConnection();await c.ExecuteAsync("INSERT INTO OUTBOX (ID,TYPE,PAYLOAD,CREATED_AT) VALUES (@Id,@Type,@Payload,@CreatedAt)",m,transaction:tx as FbTransaction);}
 public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default){using var c=f.CreateConnection();return (await c.QueryAsync<OutboxMessage>("SELECT FIRST @b * FROM OUTBOX WHERE PROCESSED_AT IS NULL ORDER BY CREATED_AT",new{b})).AsList();}
 public Task MarkProcessedAsync(Guid id,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE OUTBOX SET PROCESSED_AT=CURRENT_TIMESTAMP WHERE ID=@id",new{id});}
 public Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE OUTBOX SET ATTEMPTS=ATTEMPTS+1,LAST_ERROR=@e WHERE ID=@id",new{id,e});}
}
public class FbConnectionFactory(string cs):IDbConnectionFactory{public IDbConnection CreateConnection()=>new FbConnection(cs);}
