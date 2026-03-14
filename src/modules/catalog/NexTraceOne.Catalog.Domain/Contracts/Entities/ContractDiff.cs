using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Entidade que representa o resultado de um diff semântico entre duas versões de contrato.
/// Classifica as mudanças em breaking, non-breaking e aditivas, e sugere a próxima versão semântica.
/// Suporta diffs específicos por protocolo (OpenAPI, WSDL, AsyncAPI).
/// </summary>
public sealed class ContractDiff : Entity<ContractDiffId>
{
    private ContractDiff() { }

    /// <summary>Identificador da versão de contrato à qual este diff está associado.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Versão base (mais antiga) usada no diff.</summary>
    public ContractVersionId BaseVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Versão alvo (mais nova) usada no diff.</summary>
    public ContractVersionId TargetVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Identificador do ativo de API ao qual pertencem as versões comparadas.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Protocolo do contrato comparado (permite diff semântico adequado ao tipo).</summary>
    public ContractProtocol Protocol { get; private set; }

    /// <summary>Nível de mudança geral calculado para este diff.</summary>
    public ChangeLevel ChangeLevel { get; private set; }

    /// <summary>Lista de mudanças breaking detectadas.</summary>
    public IReadOnlyList<ChangeEntry> BreakingChanges { get; private set; } = [];

    /// <summary>Lista de mudanças non-breaking detectadas.</summary>
    public IReadOnlyList<ChangeEntry> NonBreakingChanges { get; private set; } = [];

    /// <summary>Lista de mudanças aditivas detectadas.</summary>
    public IReadOnlyList<ChangeEntry> AdditiveChanges { get; private set; } = [];

    /// <summary>Sugestão de próxima versão semântica baseada no nível de mudança.</summary>
    public string SuggestedSemVer { get; private set; } = string.Empty;

    /// <summary>Nível de confiança da sugestão de semver (0.0 a 1.0).</summary>
    public decimal Confidence { get; private set; }

    /// <summary>Data/hora em que o diff foi computado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>
    /// Cria um novo diff semântico entre duas versões de contrato.
    /// </summary>
    public static ContractDiff Create(
        ContractVersionId contractVersionId,
        ContractVersionId baseVersionId,
        ContractVersionId targetVersionId,
        Guid apiAssetId,
        ChangeLevel changeLevel,
        IReadOnlyList<ChangeEntry> breakingChanges,
        IReadOnlyList<ChangeEntry> nonBreakingChanges,
        IReadOnlyList<ChangeEntry> additiveChanges,
        string suggestedSemVer,
        DateTimeOffset computedAt,
        ContractProtocol protocol = ContractProtocol.OpenApi,
        decimal confidence = 0.8m)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.Null(baseVersionId);
        Guard.Against.Null(targetVersionId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.Null(breakingChanges);
        Guard.Against.Null(nonBreakingChanges);
        Guard.Against.Null(additiveChanges);
        Guard.Against.NullOrWhiteSpace(suggestedSemVer);

        return new ContractDiff
        {
            Id = ContractDiffId.New(),
            ContractVersionId = contractVersionId,
            BaseVersionId = baseVersionId,
            TargetVersionId = targetVersionId,
            ApiAssetId = apiAssetId,
            Protocol = protocol,
            ChangeLevel = changeLevel,
            BreakingChanges = breakingChanges,
            NonBreakingChanges = nonBreakingChanges,
            AdditiveChanges = additiveChanges,
            SuggestedSemVer = suggestedSemVer,
            Confidence = Math.Clamp(confidence, 0m, 1m),
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractDiff.</summary>
public sealed record ContractDiffId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractDiffId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractDiffId From(Guid id) => new(id);
}
