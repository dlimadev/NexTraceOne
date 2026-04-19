using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para RecoveryJob.</summary>
public sealed record RecoveryJobId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa um job de recovery de dados iniciado via plataforma.
/// Persiste o histórico auditável de todas as tentativas de restauro, incluindo dry runs.
/// </summary>
public sealed class RecoveryJob : Entity<RecoveryJobId>
{
    private RecoveryJob() { }

    /// <summary>Identificador do ponto de restauro a ser usado (externo ao sistema de backup).</summary>
    public string RestorePointId { get; private init; } = string.Empty;

    /// <summary>Âmbito do recovery: "full", "partial", "schema".</summary>
    public string Scope { get; private init; } = string.Empty;

    /// <summary>Schemas específicos a incluir quando Scope = "partial" (JSON array).</summary>
    public string? SchemasJson { get; private init; }

    /// <summary>Indica se é um dry run (sem aplicar alterações reais).</summary>
    public bool DryRun { get; private init; }

    /// <summary>Estado do job: Initiated, InProgress, Completed, Failed.</summary>
    public string Status { get; private set; } = "Initiated";

    /// <summary>Mensagem de progresso ou resultado.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Data/hora de início do job.</summary>
    public DateTimeOffset InitiatedAt { get; private init; }

    /// <summary>Data/hora de conclusão (sucesso ou falha).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Utilizador ou sistema que iniciou o job.</summary>
    public string? InitiatedBy { get; private init; }

    /// <summary>
    /// Cria um novo RecoveryJob com estado inicial Initiated.
    /// </summary>
    public static RecoveryJob Create(
        string restorePointId,
        string scope,
        string? schemasJson,
        bool dryRun,
        string? initiatedBy,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(restorePointId);
        Guard.Against.NullOrWhiteSpace(scope);

        return new RecoveryJob
        {
            Id = new RecoveryJobId(Guid.NewGuid()),
            RestorePointId = restorePointId,
            Scope = scope,
            SchemasJson = schemasJson,
            DryRun = dryRun,
            Status = dryRun ? "DryRunCompleted" : "Initiated",
            Message = dryRun
                ? "Dry run completed. No changes applied."
                : "Recovery job initiated. Monitor progress via platform jobs.",
            InitiatedAt = now,
            CompletedAt = dryRun ? now : null,
            InitiatedBy = initiatedBy
        };
    }

    /// <summary>Marca o job como concluído com sucesso.</summary>
    public void Complete(string message, DateTimeOffset now)
    {
        Status = "Completed";
        Message = message;
        CompletedAt = now;
    }

    /// <summary>Marca o job como falhado.</summary>
    public void Fail(string reason, DateTimeOffset now)
    {
        Status = "Failed";
        Message = reason;
        CompletedAt = now;
    }
}
