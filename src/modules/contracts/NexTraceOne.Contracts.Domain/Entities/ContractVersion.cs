using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma versão versionada de um contrato OpenAPI.
/// Gerencia o ciclo de vida da versão, incluindo importação, bloqueio e diffs associados.
/// </summary>
public sealed class ContractVersion : AuditableEntity<ContractVersionId>
{
    private readonly List<ContractDiff> _diffs = [];

    private ContractVersion() { }

    /// <summary>Identificador do ativo de API correspondente no módulo EngineeringGraph.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão semântica do contrato, ex: "1.2.3".</summary>
    public string SemVer { get; private set; } = string.Empty;

    /// <summary>Conteúdo bruto da especificação OpenAPI (JSON ou YAML, máx. 1MB).</summary>
    public string SpecContent { get; private set; } = string.Empty;

    /// <summary>Formato da especificação: "json" ou "yaml".</summary>
    public string Format { get; private set; } = string.Empty;

    /// <summary>Origem do import: URL ou "upload".</summary>
    public string ImportedFrom { get; private set; } = string.Empty;

    /// <summary>Indica se esta versão está bloqueada contra novas alterações.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>Data/hora em que a versão foi bloqueada, se aplicável.</summary>
    public DateTimeOffset? LockedAt { get; private set; }

    /// <summary>Usuário que bloqueou a versão, se aplicável.</summary>
    public string? LockedBy { get; private set; }

    /// <summary>Diffs computados associados a esta versão.</summary>
    public IReadOnlyList<ContractDiff> Diffs => _diffs.AsReadOnly();

    /// <summary>
    /// Importa uma nova versão de contrato OpenAPI para o sistema.
    /// Retorna falha se a versão semântica for inválida ou o conteúdo estiver vazio.
    /// </summary>
    public static Result<ContractVersion> Import(
        Guid apiAssetId,
        string semVer,
        string specContent,
        string format,
        string importedFrom)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(format);
        Guard.Against.NullOrWhiteSpace(importedFrom);

        if (string.IsNullOrWhiteSpace(semVer) || SemanticVersion.Parse(semVer) is null)
            return ContractsErrors.InvalidSemVer(semVer ?? string.Empty);

        if (string.IsNullOrWhiteSpace(specContent))
            return ContractsErrors.EmptySpecContent();

        return new ContractVersion
        {
            Id = ContractVersionId.New(),
            ApiAssetId = apiAssetId,
            SemVer = semVer,
            SpecContent = specContent,
            Format = format.ToLowerInvariant(),
            ImportedFrom = importedFrom,
            IsLocked = false
        };
    }

    /// <summary>
    /// Bloqueia esta versão do contrato, impedindo novas alterações.
    /// Retorna falha se a versão já estiver bloqueada.
    /// </summary>
    public Result<Unit> Lock(string lockedBy, DateTimeOffset lockedAt)
    {
        Guard.Against.NullOrWhiteSpace(lockedBy);

        if (IsLocked)
            return ContractsErrors.AlreadyLocked(SemVer);

        IsLocked = true;
        LockedAt = lockedAt;
        LockedBy = lockedBy;
        return Unit.Value;
    }

    /// <summary>Associa um diff computado a esta versão do contrato.</summary>
    public void AddDiff(ContractDiff diff)
    {
        Guard.Against.Null(diff);
        _diffs.Add(diff);
    }
}

/// <summary>Identificador fortemente tipado de ContractVersion.</summary>
public sealed record ContractVersionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractVersionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractVersionId From(Guid id) => new(id);
}
