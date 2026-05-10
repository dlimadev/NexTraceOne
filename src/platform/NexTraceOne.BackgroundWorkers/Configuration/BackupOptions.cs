namespace NexTraceOne.BackgroundWorkers.Configuration;

/// <summary>
/// Opções de configuração para o BackupCoordinatorJob.
/// Secção: "Backup" em appsettings.
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>Directório de saída para ficheiros de backup.</summary>
    public string OutputDirectory { get; set; } = "/var/nextraceone/backups";

    /// <summary>Número de dias de retenção. Backups mais antigos são eliminados.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Nomes das bases de dados a incluir no backup.
    /// Usa a connection string "NexTraceOne" como referência de host/porta/credenciais.
    /// </summary>
    public List<string> Databases { get; set; } =
    [
        "nextraceone",
        "nextraceone_identity",
        "nextraceone_catalog",
        "nextraceone_operations",
        "nextraceone_ai",
    ];

    /// <summary>Timeout máximo por base de dados (minutos).</summary>
    public int DumpTimeoutMinutes { get; set; } = 30;
}
