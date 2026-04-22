namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração de leitura de atividade de developers na plataforma para um tenant.
///
/// Fornece dados de ações realizadas por utilizador num período lookback configurável,
/// cobrindo: contratos criados/atualizados, runbooks criados/atualizados, releases
/// registados e notas operacionais criadas.
/// Desacopla o handler de atividade de developers das implementações concretas de repositório.
///
/// Wave AC.2 — GetDeveloperActivityReport.
/// </summary>
public interface IDeveloperActivityReader
{
    /// <summary>
    /// Lista todas as entradas de atividade de developers de um tenant no período lookback especificado.
    /// </summary>
    Task<IReadOnlyList<DeveloperActivityEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de atividade de um developer no período lookback.
/// Agrega contagens de ações por tipo para um utilizador.
/// Wave AC.2.
/// </summary>
public sealed record DeveloperActivityEntry(
    /// <summary>Identificador único do utilizador.</summary>
    string UserId,
    /// <summary>Nome de display do utilizador.</summary>
    string UserName,
    /// <summary>Nome da equipa do utilizador, ou null se não atribuído.</summary>
    string? TeamName,
    /// <summary>Número de contratos criados no período.</summary>
    int ContractsCreated,
    /// <summary>Número de contratos atualizados no período.</summary>
    int ContractsUpdated,
    /// <summary>Número de runbooks criados no período.</summary>
    int RunbooksCreated,
    /// <summary>Número de runbooks atualizados no período.</summary>
    int RunbooksUpdated,
    /// <summary>Número de releases registados no período.</summary>
    int ReleasesRegistered,
    /// <summary>Número de notas operacionais criadas no período.</summary>
    int OperationalNotesCreated);
