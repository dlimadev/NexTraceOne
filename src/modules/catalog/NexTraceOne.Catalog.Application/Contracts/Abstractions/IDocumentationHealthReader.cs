namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de saúde da documentação por serviço.
/// Por omissão satisfeita por <c>NullDocumentationHealthReader</c> (honest-null).
/// Wave AY.1 — GetDocumentationHealthReport.
/// </summary>
public interface IDocumentationHealthReader
{
    Task<IReadOnlyList<ServiceDocumentationEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de estado de documentação por serviço.</summary>
    public sealed record ServiceDocumentationEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        string DomainName,
        bool HasRunbookUrl,
        DateTimeOffset? RunbookLastUpdatedAt,
        int ContractCount,
        int ContractsWithDescription,
        int ContractsWithExamples,
        int ContractsWithErrorCodes,
        bool HasArchitectureDocUrl,
        bool HasOnboardingDocUrl,
        DateTimeOffset? DocLastUpdatedAt,
        IReadOnlyList<string> ContributorIds);
}
