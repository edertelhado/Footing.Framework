// Exemplo docs — DbfOutboxStore (VFP OleDb file dbf, não pack)
using System.Data;using System.Data.OleDb;using Footing.Framework.Outbox;
public class DbfOutboxStore(string folder):IOutboxStore{
 private OleDbConnection Conn()=>new($"Provider=VFPOLEDB;Data Source={folder};");
 public Task SaveAsync(OutboxMessage m,IDbTransaction? tx=null,CancellationToken ct=default){using var c=Conn();c.Open();using var cmd=new OleDbCommand("INSERT INTO outbox.dbf (Id,Type,Payload,CreatedAt) VALUES (?,?,?,?)",c);cmd.Parameters.AddWithValue("p1",m.Id.ToString());cmd.Parameters.AddWithValue("p2",m.Type);cmd.Parameters.AddWithValue("p3",m.Payload);cmd.Parameters.AddWithValue("p4",m.CreatedAt);cmd.ExecuteNonQuery();return Task.CompletedTask;}
 public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default){using var c=Conn();c.Open();using var cmd=new OleDbCommand($"SELECT TOP {b} * FROM outbox WHERE ProcessedAt IS NULL",c);using var r=cmd.ExecuteReader();var l=new List<OutboxMessage>();while(r.Read())l.Add(new OutboxMessage(Guid.Parse(r["Id"].ToString()!),r["Type"].ToString()!,r["Payload"].ToString()!, (DateTime)r["CreatedAt"]));return Task.FromResult<IReadOnlyList<OutboxMessage>>(l);}
 public Task MarkProcessedAsync(Guid id,CancellationToken ct=default){using var c=Conn();c.Open();new OleDbCommand($"UPDATE outbox.dbf SET ProcessedAt=Date() WHERE Id='{id}'",c).ExecuteNonQuery();return Task.CompletedTask;}
 public Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default)=>Task.CompletedTask;
}
