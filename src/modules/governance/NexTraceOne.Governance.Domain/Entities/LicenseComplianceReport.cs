using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade LicenseComplianceReport.
/// Garante que nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record LicenseComplianceReportId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Relatório de compliance de licenças de dependências para um escopo específico (serviço, equipa ou domínio).
/// Permite responder "Quantas dependências do serviço X estão em conformidade com as políticas de licença?"
/// ou "Qual equipa tem maior risco de licenciamento?".
/// Inclui detalhamento JSONB de dependências, conflitos e recomendações de remediação.
/// </summary>
public sealed class LicenseComplianceReport : Entity<LicenseComplianceReportId>
{
    /// <summary>Escopo de análise (Service, Team, Domain).</summary>
    public LicenseComplianceScope Scope { get; private init; }

    /// <summary>Chave da entidade específica (nome ou identificador) dentro do escopo.</summary>
    public string ScopeKey { get; private init; } = string.Empty;

    /// <summary>Label legível para exibição na UI (opcional).</summary>
    public string? ScopeLabel { get; private init; }

    /// <summary>Número total de dependências analisadas.</summary>
    public int TotalDependencies { get; private init; }

    /// <summary>Número de dependências em conformidade.</summary>
    public int CompliantCount { get; private init; }

    /// <summary>Número de dependências não conformes.</summary>
    public int NonCompliantCount { get; private init; }

    /// <summary>Número de dependências com aviso.</summary>
    public int WarningCount { get; private init; }

    /// <summary>Nível de risco global do relatório.</summary>
    public LicenseRiskLevel OverallRiskLevel { get; private init; }

    /// <summary>Percentagem de compliance (0–100). Calculado automaticamente.</summary>
    public int CompliancePercent { get; private init; }

    /// <summary>Detalhamento completo de dependências e licenças (JSONB).</summary>
    public string? LicenseDetails { get; private init; }

    /// <summary>Conflitos de licença detetados (JSONB).</summary>
    public string? Conflicts { get; private init; }

    /// <summary>Recomendações de remediação (JSONB).</summary>
    public string? Recommendations { get; private init; }

    /// <summary>Data/hora UTC em que o scan foi executado.</summary>
    public DateTimeOffset ScannedAt { get; private init; }

    /// <summary>Identificador do tenant proprietário (nullable para multi-tenant).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private LicenseComplianceReport() { }

    /// <summary>
    /// Gera um novo relatório de compliance de licenças para um escopo específico.
    /// Valida invariantes de domínio e calcula automaticamente a percentagem de compliance.
    /// </summary>
    /// <param name="scope">Escopo de análise (Service, Team, Domain).</param>
    /// <param name="scopeKey">Chave da entidade (nome/ID).</param>
    /// <param name="scopeLabel">Label legível para UI (opcional).</param>
    /// <param name="totalDependencies">Número total de dependências analisadas.</param>
    /// <param name="compliantCount">Número de dependências conformes.</param>
    /// <param name="nonCompliantCount">Número de dependências não conformes.</param>
    /// <param name="warningCount">Número de dependências com aviso.</param>
    /// <param name="overallRiskLevel">Nível de risco global.</param>
    /// <param name="licenseDetails">Detalhamento JSONB de dependências e licenças (opcional).</param>
    /// <param name="conflicts">Conflitos JSONB detetados (opcional).</param>
    /// <param name="recommendations">Recomendações JSONB de remediação (opcional).</param>
    /// <param name="tenantId">Identificador do tenant (opcional).</param>
    /// <param name="now">Data/hora UTC do scan.</param>
    /// <returns>Nova instância válida de LicenseComplianceReport.</returns>
    public static LicenseComplianceReport Generate(
        LicenseComplianceScope scope,
        string scopeKey,
        string? scopeLabel,
        int totalDependencies,
        int compliantCount,
        int nonCompliantCount,
        int warningCount,
        LicenseRiskLevel overallRiskLevel,
        string? licenseDetails,
        string? conflicts,
        string? recommendations,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(scopeKey, nameof(scopeKey));
        Guard.Against.StringTooLong(scopeKey, 200, nameof(scopeKey));

        if (scopeLabel is not null)
            Guard.Against.StringTooLong(scopeLabel, 300, nameof(scopeLabel));

        Guard.Against.Negative(totalDependencies, nameof(totalDependencies));
        Guard.Against.Negative(compliantCount, nameof(compliantCount));
        Guard.Against.Negative(nonCompliantCount, nameof(nonCompliantCount));
        Guard.Against.Negative(warningCount, nameof(warningCount));

        var compliancePercent = totalDependencies > 0
            ? (compliantCount * 100) / totalDependencies
            : 100;

        if (compliancePercent < 0 || compliancePercent > 100)
            throw new ArgumentException(
                $"Computed compliance percent ({compliancePercent}) must be between 0 and 100.",
                nameof(compliantCount));

        return new LicenseComplianceReport
        {
            Id = new LicenseComplianceReportId(Guid.NewGuid()),
            Scope = scope,
            ScopeKey = scopeKey.Trim(),
            ScopeLabel = scopeLabel?.Trim(),
            TotalDependencies = totalDependencies,
            CompliantCount = compliantCount,
            NonCompliantCount = nonCompliantCount,
            WarningCount = warningCount,
            OverallRiskLevel = overallRiskLevel,
            CompliancePercent = compliancePercent,
            LicenseDetails = licenseDetails,
            Conflicts = conflicts,
            Recommendations = recommendations,
            TenantId = tenantId?.Trim(),
            ScannedAt = now
        };
    }
}
