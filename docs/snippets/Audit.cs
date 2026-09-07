// Snippet docs — Audit helper (Won't 3.1.1 / Could 3.2 0.1) — copy-paste se app quiser
// Não shipa no pack Footing.Framework — manter agnóstico
namespace App.Data;
public interface IAuditable{DateTime CreatedAt{get;set;}DateTime UpdatedAt{get;set;}string? CreatedBy{get;set;}string? UpdatedBy{get;set;}}
public interface ISoftDelete{bool IsDeleted{get;set;}DateTime? DeletedAt{get;set;}}
public static class AuditExtensions{public static void ApplyAudit(this IAuditable e,string? u=null,bool n=false){var now=DateTime.UtcNow;e.UpdatedAt=now;e.UpdatedBy=u;if(n){e.CreatedAt=now;e.CreatedBy=u;}}public static void SoftDelete(this ISoftDelete e){e.IsDeleted=true;e.DeletedAt=DateTime.UtcNow;}}
