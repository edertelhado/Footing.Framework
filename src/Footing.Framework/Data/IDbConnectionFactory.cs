using System.Data;
namespace Footing.Framework.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
