using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa o resultado da avaliação de um gate de compliance
/// contra uma versão de contrato específica. Regista se o contrato passou,
/// gerou avisos ou foi bloqueado, juntamente com as violações detetadas.
/// </summary>
public sealed class ContractComplianceResult : Entity<ContractComplianceResultId>
{
    private ContractComplianceResult() { }

    /// <summary>Identificador do gate de compliance avaliado.</summary>
    public Guid GateId { get; private set; }

    /// <summary>Identificador da versão de contrato avaliada.</summary>
    public string ContractVersionId { get; private set; } = string.Empty;

    /// <summary>Identificador da mudança associada (opcional, para correlação com Change Intelligence).</summary>
    public string? ChangeId { get; private set; }

    /// <summary>Resultado da avaliação: Pass, Warn ou Block.</summary>
    public ComplianceEvaluationResult Result { get; private set; }

    /// <summary>Detalhes das violações detetadas em formato JSON (JSONB).</summary>
    public string? Violations { get; private set; }

    /// <summary>Identificador do pacote de evidências associado (opcional).</summary>
    public string? EvidencePackId { get; private set; }

    /// <summary>Momento em que a avaliação foi realizada.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>Identificador do tenant para isolamento multi-tenant.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo resultado de avaliação de compliance contratual.
    /// </summary>
    public static ContractComplianceResult Evaluate(
        Guid gateId,
        string contractVersionId,
        string? changeId,
        ComplianceEvaluationResult result,
        string? violations,
        string? evidencePackId,
        DateTimeOffset evaluatedAt,
        string? tenantId)
    {
        Guard.Against.Default(gateId);
        Guard.Against.NullOrWhiteSpace(contractVersionId);
        Guard.Against.StringTooLong(contractVersionId, 200);
        Guard.Against.EnumOutOfRange(result);

        if (changeId is not null)
            Guard.Against.StringTooLong(changeId, 200);

        if (evidencePackId is not null)
            Guard.Against.StringTooLong(evidencePackId, 200);

        return new ContractComplianceResult
        {
            Id = ContractComplianceResultId.New(),
            GateId = gateId,
            ContractVersionId = contractVersionId,
            ChangeId = changeId,
            Result = result,
            Violations = violations,
            EvidencePackId = evidencePackId,
            EvaluatedAt = evaluatedAt,
            TenantId = tenantId
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractComplianceResult.</summary>
public sealed record ContractComplianceResultId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractComplianceResultId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractComplianceResultId From(Guid id) => new(id);
}
