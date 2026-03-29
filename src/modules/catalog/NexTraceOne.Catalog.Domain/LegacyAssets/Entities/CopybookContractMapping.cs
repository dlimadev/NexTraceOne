using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Mapeamento entre copybook COBOL e contrato OpenAPI/AsyncAPI (referência cruzada).
/// Permite rastrear que contratos modernos correspondem a que layouts legacy.
/// </summary>
public sealed class CopybookContractMapping : Entity<CopybookContractMappingId>
{
    private CopybookContractMapping() { }

    /// <summary>Copybook de origem do mapeamento.</summary>
    public CopybookId CopybookId { get; private set; } = null!;

    /// <summary>Identificador da versão de contrato mapeada.</summary>
    public Guid ContractVersionId { get; private set; }

    /// <summary>Tipo de mapeamento (ex.: "REST-to-COBOL", "Event-to-COBOL").</summary>
    public string MappingType { get; private set; } = string.Empty;

    /// <summary>Notas adicionais sobre o mapeamento.</summary>
    public string? Notes { get; private set; }

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria um novo mapeamento copybook–contrato.</summary>
    public static CopybookContractMapping Create(
        CopybookId copybookId, Guid contractVersionId, string mappingType)
    {
        Guard.Against.Null(copybookId);
        Guard.Against.Default(contractVersionId);
        Guard.Against.NullOrWhiteSpace(mappingType);

        return new CopybookContractMapping
        {
            Id = CopybookContractMappingId.New(),
            CopybookId = copybookId,
            ContractVersionId = contractVersionId,
            MappingType = mappingType.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Identificador fortemente tipado de CopybookContractMapping.</summary>
public sealed record CopybookContractMappingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookContractMappingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookContractMappingId From(Guid id) => new(id);
}
