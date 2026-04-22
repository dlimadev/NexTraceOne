namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração de leitura de padrões de acesso de utilizadores para detecção de anomalias.
///
/// Fornece dados agregados por utilizador num período lookback, cobrindo:
/// volume total de pedidos, pedidos fora de horário, acessos a recursos sensíveis,
/// acessos a recursos incomuns e exportações em larga escala.
/// Desacopla o handler de anomalias de acesso das implementações concretas de repositório.
///
/// Wave AD.3 — GetAccessPatternAnomalyReport.
/// </summary>
public interface IAccessPatternReader
{
    /// <summary>
    /// Lista todas as entradas de padrões de acesso de utilizadores de um tenant no período lookback.
    /// </summary>
    Task<IReadOnlyList<UserAccessEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de padrão de acesso de um utilizador no período lookback.
/// Agrega contagens de sinais anómalos por tipo.
/// Wave AD.3.
/// </summary>
public sealed record UserAccessEntry(
    /// <summary>Identificador único do utilizador.</summary>
    string UserId,
    /// <summary>Nome de display do utilizador.</summary>
    string UserName,
    /// <summary>Nome da equipa, ou null se não atribuído.</summary>
    string? TeamName,
    /// <summary>Total de pedidos no período.</summary>
    int TotalRequests,
    /// <summary>Pedidos fora do horário 08:00–20:00 UTC.</summary>
    int OffHoursRequests,
    /// <summary>Acessos pela primeira vez a recursos marcados como Restricted ou Partner.</summary>
    int SensitiveResourceAccesses,
    /// <summary>Acessos a tipos de recurso nunca acedidos anteriormente pelo utilizador.</summary>
    int UnusualResourceAccesses,
    /// <summary>Número de eventos de exportação em larga escala na sessão.</summary>
    int BulkExportCount,
    /// <summary>Média diária de pedidos no período.</summary>
    double AvgDailyRequests,
    /// <summary>Máximo diário de pedidos no período.</summary>
    double MaxDailyRequests);
