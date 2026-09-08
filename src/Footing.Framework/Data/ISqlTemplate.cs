namespace Footing.Framework.Data;

/// <summary>
/// Contrato para SqlTemplate — facilita mock em testes de repositório.
/// Implementação: SqlTemplate : ISqlTemplate.
/// </summary>
public interface ISqlTemplate
{
    SqlResult Render(object? parameters = null);
}
