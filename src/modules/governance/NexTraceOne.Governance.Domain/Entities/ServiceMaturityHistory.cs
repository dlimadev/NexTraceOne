using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ServiceMaturityHistory.
/// </summary>
public sealed record ServiceMaturityHistoryId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo histórico imutável de um snapshot de maturidade de serviço.
/// Criado automaticamente a cada reavaliação de ServiceMaturityAssessment,
/// permitindo o trending do nível de maturidade ao longo do tempo.
/// Append-only — nunca é actualizado após inserção.
/// </summary>
public sealed class ServiceMaturityHistory : Entity<ServiceMaturityHistoryId>
{
    /// <summary>Identificador do serviço avaliado.</summary>
    public Guid ServiceId { get; private init; }

    /// <summary>Nome desnormalizado do serviço para exibição sem join.</summary>
    public string ServiceName { get; private init; } = string.Empty;

    /// <summary>Identificador da avaliação que originou este snapshot.</summary>
    public ServiceMaturityAssessmentId AssessmentId { get; private init; } = null!;

    /// <summary>Nível de maturidade registado neste snapshot.</summary>
    public ServiceMaturityLevel Level { get; private init; }

    /// <summary>Número de reavaliação correspondente a este snapshot (0 = avaliação inicial).</summary>
    public int ReassessmentCount { get; private init; }

    /// <summary>Data/hora UTC em que este snapshot foi capturado.</summary>
    public DateTimeOffset RecordedAt { get; private init; }

    /// <summary>Identificador do tenant proprietário.</summary>
    public string? TenantId { get; private init; }

    /// <summary>Construtor privado para EF Core.</summary>
    private ServiceMaturityHistory() { }

    /// <summary>
    /// Cria um snapshot histórico a partir de uma avaliação existente.
    /// Tipicamente chamado após cada reavaliação para preservar o histórico.
    /// </summary>
    public static ServiceMaturityHistory Snapshot(
        ServiceMaturityAssessment assessment,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        return new ServiceMaturityHistory
        {
            Id = new ServiceMaturityHistoryId(Guid.NewGuid()),
            ServiceId = assessment.ServiceId,
            ServiceName = assessment.ServiceName,
            AssessmentId = assessment.Id,
            Level = assessment.CurrentLevel,
            ReassessmentCount = assessment.ReassessmentCount,
            RecordedAt = now,
            TenantId = assessment.TenantId
        };
    }
}
