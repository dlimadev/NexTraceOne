namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Leitor de dados de lead time de mudanças para o relatório GetChangeLeadTimeReport.
/// Por omissão satisfeita por <c>NullChangeLeadTimeReader</c> (honest-null).
/// Wave AW.2 — Change Lead Time Report.
/// </summary>
public interface IChangeLeadTimeReader
{
    /// <summary>Lista entries de lead time de release por tenant numa janela temporal.</summary>
    Task<IReadOnlyList<LeadTimeEntry>> ListReleaseLeadTimesByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

/// <summary>
/// Dados de lead time de uma release por estágio do pipeline de entrega.
/// </summary>
/// <param name="ReleaseId">Identificador da release.</param>
/// <param name="ServiceName">Nome canónico do serviço.</param>
/// <param name="TeamName">Equipa responsável.</param>
/// <param name="Environment">Ambiente alvo.</param>
/// <param name="CreatedAt">Timestamp de criação da release.</param>
/// <param name="ApprovalRequestedAt">Timestamp do pedido de aprovação.</param>
/// <param name="ApprovedAt">Timestamp da aprovação.</param>
/// <param name="PreProdDeployedAt">Timestamp do deploy em pré-produção.</param>
/// <param name="ProductionDeployedAt">Timestamp do deploy em produção.</param>
/// <param name="VerifiedAt">Timestamp da verificação pós-deploy.</param>
public sealed record LeadTimeEntry(
    Guid ReleaseId,
    string ServiceName,
    string TeamName,
    string Environment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovalRequestedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PreProdDeployedAt,
    DateTimeOffset? ProductionDeployedAt,
    DateTimeOffset? VerifiedAt);
