namespace Footing.Framework.Migrations;

public sealed class MigrationOptions
{
    public string HistoryTable { get; set; } = "__migrations";
    public Dictionary<string, string> Placeholders { get; set; } = new();

    private bool _useTransaction;

    /// <summary>
    /// Definitive Won't — agnostic transactional DDL is not supported, even with BUY.
    /// Keeps AutoTransaction=false agnostic; caller controls BEGIN/COMMIT inside the .sql script if needed (e.g., PostgreSQL).
    /// Setter throws NotSupportedException when set to true.
    /// </summary>
    public bool UseTransaction
    {
        get => _useTransaction;
        set
        {
            if (value) throw new NotSupportedException("DDL transactions are not supported. Use BEGIN/COMMIT inside the .sql script if your database supports transactional DDL.");
            _useTransaction = value;
        }
    }
}
