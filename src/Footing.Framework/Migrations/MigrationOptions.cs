namespace Footing.Framework.Migrations;

public sealed class MigrationOptions
{
    public string HistoryTable { get; set; } = "__migrations";

    /// <summary>Assembly + prefix para EmbeddedResource. Default: entry assembly, filtrado por .sql.</summary>
    public System.Reflection.Assembly? EmbeddedAssembly { get; set; }
    public string? EmbeddedPrefix { get; set; }

    /// <summary>Pasta FileSystem alternativa (ex: "Migrations"). Se setado, usa FileSystem provider.</summary>
    public string? FileSystemFolder { get; set; }

    /// <summary>Auto-migrate no HostedService StartAsync (default true).</summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>Valida checksum drift antes/depois do migrate (default true).</summary>
    public bool ValidateOnMigrate { get; set; } = true;

    /// <summary>Repair (DELETE WHERE success='N') antes do migrate (default false).</summary>
    public bool RepairOnMigrate { get; set; } = false;

    /// <summary>Baseline se history vazia (INSERT version baseline, default false).</summary>
    public bool BaselineOnMigrate { get; set; } = false;
    public string BaselineVersion { get; set; } = "0";

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
