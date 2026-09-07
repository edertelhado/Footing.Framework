using System.Diagnostics;
namespace Footing.Framework.Diagnostics;
public static class FootingActivitySource{public static readonly ActivitySource Source=new("Footing.Framework","3.5.5");
public static Activity? StartSqlTemplateRender(string s){var a=Source.StartActivity("SqlTemplate.Render",ActivityKind.Internal);if(a!=null){a.SetTag("sql.snippet",s.Length>100?s[..100]:s);a.SetTag("component","SqlTemplate");}return a;}
public static Activity? StartEventBusPublish(string t){var a=Source.StartActivity("EventBus.Publish",ActivityKind.Internal);if(a!=null){a.SetTag("event.type",t);a.SetTag("component","EventBus");}return a;}
public static Activity? StartSqlBatch(string table,int rows){var a=Source.StartActivity("SqlBatch.Insert",ActivityKind.Internal);if(a!=null){a.SetTag("db.table",table);a.SetTag("db.rows",rows);}return a;}}
