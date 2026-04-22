using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação nula de <see cref="IContractVersionHistoryReader"/> com dados de teste codificados.
///
/// Devolve dois contratos com três versões cada para validar o comportamento
/// do handler <c>GetContractLineageReport</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AB.2 — GetContractLineageReport.
/// </summary>
internal sealed class NullContractVersionHistoryReader : IContractVersionHistoryReader
{
    private static readonly DateTimeOffset BaseDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<ContractVersionEntry> AllEntries =
    [
        // Contrato 1 — order-api (REST, estável)
        new ContractVersionEntry(
            ContractId: "order-api",
            ContractName: "Order API",
            Version: "v1.0.0",
            LifecycleState: "Deprecated",
            AuthorName: "alice@example.com",
            ApproverName: "tech-lead@example.com",
            PublishedAt: BaseDate,
            DeprecatedAt: BaseDate.AddDays(90),
            BreakingChangesFromPreviousVersion: 0,
            ActiveConsumersAtDeprecation: 3,
            Protocol: "REST"),

        new ContractVersionEntry(
            ContractId: "order-api",
            ContractName: "Order API",
            Version: "v1.1.0",
            LifecycleState: "Deprecated",
            AuthorName: "alice@example.com",
            ApproverName: "tech-lead@example.com",
            PublishedAt: BaseDate.AddDays(90),
            DeprecatedAt: BaseDate.AddDays(200),
            BreakingChangesFromPreviousVersion: 0,
            ActiveConsumersAtDeprecation: 5,
            Protocol: "REST"),

        new ContractVersionEntry(
            ContractId: "order-api",
            ContractName: "Order API",
            Version: "v2.0.0",
            LifecycleState: "Published",
            AuthorName: "bob@example.com",
            ApproverName: "tech-lead@example.com",
            PublishedAt: BaseDate.AddDays(200),
            DeprecatedAt: null,
            BreakingChangesFromPreviousVersion: 2,
            ActiveConsumersAtDeprecation: 0,
            Protocol: "REST"),

        // Contrato 2 — inventory-events (AsyncAPI, com breaking changes)
        new ContractVersionEntry(
            ContractId: "inventory-events",
            ContractName: "Inventory Events",
            Version: "v1.0.0",
            LifecycleState: "Deprecated",
            AuthorName: "carol@example.com",
            ApproverName: null,
            PublishedAt: BaseDate.AddDays(10),
            DeprecatedAt: BaseDate.AddDays(60),
            BreakingChangesFromPreviousVersion: 0,
            ActiveConsumersAtDeprecation: 2,
            Protocol: "AsyncAPI"),

        new ContractVersionEntry(
            ContractId: "inventory-events",
            ContractName: "Inventory Events",
            Version: "v1.1.0",
            LifecycleState: "Deprecated",
            AuthorName: "carol@example.com",
            ApproverName: "arch@example.com",
            PublishedAt: BaseDate.AddDays(60),
            DeprecatedAt: BaseDate.AddDays(120),
            BreakingChangesFromPreviousVersion: 1,
            ActiveConsumersAtDeprecation: 4,
            Protocol: "AsyncAPI"),

        new ContractVersionEntry(
            ContractId: "inventory-events",
            ContractName: "Inventory Events",
            Version: "v2.0.0",
            LifecycleState: "Published",
            AuthorName: "carol@example.com",
            ApproverName: "arch@example.com",
            PublishedAt: BaseDate.AddDays(120),
            DeprecatedAt: null,
            BreakingChangesFromPreviousVersion: 3,
            ActiveConsumersAtDeprecation: 0,
            Protocol: "AsyncAPI"),
    ];

    /// <inheritdoc />
    public Task<IReadOnlyList<ContractVersionEntry>> ListByContractAsync(
        string tenantId,
        string contractId,
        int lookbackDays,
        CancellationToken ct)
    {
        IReadOnlyList<ContractVersionEntry> result = AllEntries
            .Where(e => string.Equals(e.ContractId, contractId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListContractIdsAsync(string tenantId, CancellationToken ct)
    {
        IReadOnlyList<string> ids = AllEntries
            .Select(e => e.ContractId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(ids);
    }
}
