using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa um playbook operacional estruturado e versionável.
/// Contém passos ordenados com condições, ligações a serviços e runbooks,
/// workflow de aprovação e rastreio de execução.
///
/// Ciclo de vida: Draft → Active → Deprecated.
/// Apenas playbooks no estado Active podem ser executados.
/// Apenas playbooks no estado Draft podem ter passos atualizados.
///
/// Invariantes:
/// - Name não pode ser nulo ou vazio (max 200).
/// - Steps não pode ser nulo ou vazio.
/// - Version inicia em 1.
/// - TenantId não pode ser nulo ou vazio.
/// </summary>
public sealed class OperationalPlaybook : AuditableEntity<OperationalPlaybookId>
{
    private const int MaxNameLength = 200;
    private const int MaxDescriptionLength = 2000;

    private OperationalPlaybook() { }

    /// <summary>Nome do playbook.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do playbook.</summary>
    public string? Description { get; private set; }

    /// <summary>Versão do playbook, começa em 1.</summary>
    public int Version { get; private set; }

    /// <summary>Passos ordenados com condições (JSONB).</summary>
    public string Steps { get; private set; } = string.Empty;

    /// <summary>Estado atual do playbook.</summary>
    public PlaybookStatus Status { get; private set; }

    /// <summary>Lista de IDs de serviços associados (JSONB).</summary>
    public string? LinkedServiceIds { get; private set; }

    /// <summary>Lista de IDs de runbooks associados (JSONB).</summary>
    public string? LinkedRunbookIds { get; private set; }

    /// <summary>Lista de tags para categorização (JSONB).</summary>
    public string? Tags { get; private set; }

    /// <summary>ID do utilizador que aprovou o playbook.</summary>
    public string? ApprovedByUserId { get; private set; }

    /// <summary>Data/hora da aprovação.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Data/hora de descontinuação.</summary>
    public DateTimeOffset? DeprecatedAt { get; private set; }

    /// <summary>Número total de vezes que o playbook foi executado.</summary>
    public int ExecutionCount { get; private set; }

    /// <summary>Data/hora da última execução.</summary>
    public DateTimeOffset? LastExecutedAt { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista.</summary>
    public uint RowVersion { get; private set; }

    /// <summary>Cria um novo playbook operacional no estado Draft.</summary>
    public static OperationalPlaybook Create(
        string name,
        string? description,
        string steps,
        string? linkedServiceIds,
        string? linkedRunbookIds,
        string? tags,
        string tenantId,
        DateTimeOffset createdAt,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(steps);
        Guard.Against.NullOrWhiteSpace(tenantId);

        if (description is { Length: > MaxDescriptionLength })
            throw new ArgumentException($"Description must not exceed {MaxDescriptionLength} characters.", nameof(description));

        var playbook = new OperationalPlaybook
        {
            Id = OperationalPlaybookId.New(),
            Name = name,
            Description = description,
            Version = 1,
            Steps = steps,
            Status = PlaybookStatus.Draft,
            LinkedServiceIds = linkedServiceIds,
            LinkedRunbookIds = linkedRunbookIds,
            Tags = tags,
            TenantId = tenantId,
            ExecutionCount = 0,
        };
        playbook.SetCreated(createdAt, createdBy);

        return playbook;
    }

    /// <summary>
    /// Ativa o playbook após aprovação. Transição: Draft → Active.
    /// </summary>
    public Result<Unit> Activate(string approvedByUserId, DateTimeOffset approvedAt)
    {
        Guard.Against.NullOrWhiteSpace(approvedByUserId);

        if (Status != PlaybookStatus.Draft)
            return RuntimeIntelligenceErrors.PlaybookInvalidTransition(
                Id.Value.ToString(), Status.ToString(), PlaybookStatus.Active.ToString());

        Status = PlaybookStatus.Active;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = approvedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Descontinua o playbook. Transição: Active → Deprecated.
    /// </summary>
    public Result<Unit> Deprecate(DateTimeOffset deprecatedAt)
    {
        if (Status != PlaybookStatus.Active)
            return RuntimeIntelligenceErrors.PlaybookInvalidTransition(
                Id.Value.ToString(), Status.ToString(), PlaybookStatus.Deprecated.ToString());

        Status = PlaybookStatus.Deprecated;
        DeprecatedAt = deprecatedAt;
        return Unit.Value;
    }

    /// <summary>Incrementa o contador de execuções e atualiza a data da última execução.</summary>
    public void IncrementExecutionCount(DateTimeOffset executedAt)
    {
        ExecutionCount++;
        LastExecutedAt = executedAt;
    }

    /// <summary>
    /// Atualiza os passos do playbook. Só é permitido no estado Draft.
    /// </summary>
    public Result<Unit> UpdateSteps(string steps, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(steps);

        if (Status != PlaybookStatus.Draft)
            return RuntimeIntelligenceErrors.PlaybookInvalidTransition(
                Id.Value.ToString(), Status.ToString(), "Draft (required for editing)");

        Steps = steps;
        Version++;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado para OperationalPlaybook.</summary>
public sealed record OperationalPlaybookId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static OperationalPlaybookId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static OperationalPlaybookId From(Guid id) => new(id);
}
