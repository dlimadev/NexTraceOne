using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Identificador fortemente tipado para BreakingChangeProposal.
/// </summary>
public sealed record BreakingChangeProposalId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Estados possíveis de uma proposta de breaking change.
/// </summary>
public enum BreakingChangeProposalStatus
{
    /// <summary>Proposta criada mas ainda não enviada para consulta.</summary>
    Draft = 0,
    /// <summary>Consulta aberta — consumidores a ser notificados e aguardam resposta.</summary>
    ConsultationOpen = 1,
    /// <summary>Pelo menos um consumidor respondeu.</summary>
    ConsumerResponded = 2,
    /// <summary>Aguarda aprovação final pelo owner.</summary>
    ApprovalPending = 3,
    /// <summary>Proposta aprovada — pode publicar versão com breaking changes.</summary>
    Approved = 4,
    /// <summary>Proposta rejeitada — não deve publicar breaking changes conforme proposta.</summary>
    Rejected = 5
}

/// <summary>
/// Proposta formal de breaking change num contrato.
/// Cria um workflow de consulta de consumidores activos antes de publicar uma versão com quebras,
/// define janela de migração, e gera um DeprecationPlan automático.
///
/// Fluxo: Draft → ConsultationOpen → ConsumerResponded → ApprovalPending → Approved|Rejected.
/// Referência: CC-06, FUTURE-ROADMAP.md Wave A.2.
/// Owner: módulo Catalog (Contracts).
/// </summary>
public sealed class BreakingChangeProposal : Entity<BreakingChangeProposalId>
{
    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do contrato (ApiAsset) com breaking changes propostos.</summary>
    public Guid ContractId { get; private init; }

    /// <summary>Estado actual da proposta.</summary>
    public BreakingChangeProposalStatus Status { get; private set; }

    /// <summary>Descrição das breaking changes propostas (JSON estruturado com tipo + path + razão).</summary>
    public string ProposedBreakingChangesJson { get; private set; } = "[]";

    /// <summary>Número de dias de janela de migração proposta para os consumidores.</summary>
    public int MigrationWindowDays { get; private set; }

    /// <summary>Identificador do DeprecationPlan gerado automaticamente (se existir).</summary>
    public Guid? DeprecationPlanId { get; private set; }

    /// <summary>Identificador do utilizador que criou a proposta.</summary>
    public string ProposedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC em que a consulta foi aberta.</summary>
    public DateTimeOffset? ConsultationOpenedAt { get; private set; }

    /// <summary>Data/hora UTC em que a proposta foi aprovada ou rejeitada.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Notas de decisão (razão de aprovação ou rejeição).</summary>
    public string? DecisionNotes { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private BreakingChangeProposal() { }

    /// <summary>Cria uma nova proposta de breaking change.</summary>
    public static BreakingChangeProposal Create(
        string tenantId,
        Guid contractId,
        string proposedBreakingChangesJson,
        int migrationWindowDays,
        string proposedBy,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Default(contractId, nameof(contractId));
        Guard.Against.NullOrWhiteSpace(proposedBreakingChangesJson, nameof(proposedBreakingChangesJson));
        Guard.Against.NegativeOrZero(migrationWindowDays, nameof(migrationWindowDays));
        Guard.Against.NullOrWhiteSpace(proposedBy, nameof(proposedBy));

        return new BreakingChangeProposal
        {
            Id = new BreakingChangeProposalId(Guid.NewGuid()),
            TenantId = tenantId,
            ContractId = contractId,
            Status = BreakingChangeProposalStatus.Draft,
            ProposedBreakingChangesJson = proposedBreakingChangesJson,
            MigrationWindowDays = migrationWindowDays,
            ProposedBy = proposedBy.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>Abre a consulta de consumidores — transição Draft → ConsultationOpen.</summary>
    public void OpenConsultation(DateTimeOffset utcNow)
    {
        if (Status != BreakingChangeProposalStatus.Draft)
            throw new InvalidOperationException($"Cannot open consultation from state {Status}.");

        Status = BreakingChangeProposalStatus.ConsultationOpen;
        ConsultationOpenedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Regista resposta de consumidor — transição para ConsumerResponded se ainda não estava.</summary>
    public void RecordConsumerResponse(DateTimeOffset utcNow)
    {
        if (Status == BreakingChangeProposalStatus.ConsultationOpen)
            Status = BreakingChangeProposalStatus.ConsumerResponded;
        UpdatedAt = utcNow;
    }

    /// <summary>Submete para aprovação — transição para ApprovalPending.</summary>
    public void SubmitForApproval(DateTimeOffset utcNow)
    {
        if (Status is not (BreakingChangeProposalStatus.ConsultationOpen or
                           BreakingChangeProposalStatus.ConsumerResponded))
            throw new InvalidOperationException($"Cannot submit for approval from state {Status}.");

        Status = BreakingChangeProposalStatus.ApprovalPending;
        UpdatedAt = utcNow;
    }

    /// <summary>Aprova a proposta — transição para Approved.</summary>
    public void Approve(string? notes, DateTimeOffset utcNow)
    {
        Status = BreakingChangeProposalStatus.Approved;
        DecidedAt = utcNow;
        DecisionNotes = notes;
        UpdatedAt = utcNow;
    }

    /// <summary>Rejeita a proposta — transição para Rejected.</summary>
    public void Reject(string? notes, DateTimeOffset utcNow)
    {
        Status = BreakingChangeProposalStatus.Rejected;
        DecidedAt = utcNow;
        DecisionNotes = notes;
        UpdatedAt = utcNow;
    }

    /// <summary>Associa um DeprecationPlan gerado automaticamente.</summary>
    public void SetDeprecationPlan(Guid deprecationPlanId)
        => DeprecationPlanId = deprecationPlanId;
}
