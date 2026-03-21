namespace NexTraceOne.BuildingBlocks.Core.Events;

/// <summary>
/// Implementação base para Integration Events publicados entre módulos.
/// Preenche automaticamente EventId, OccurredAt, SourceModule e, a partir da Fase 5,
/// TenantId e EnvironmentId para propagação de contexto distribuído.
/// Todo Integration Event da plataforma deve herdar desta classe.
/// Exemplo:
/// public sealed record UserCreatedIntegrationEvent(Guid UserId, string Email)
///     : IntegrationEventBase("Identity");
/// </summary>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    /// <summary>Inicializa o Integration Event com o módulo de origem.</summary>
    protected IntegrationEventBase(string sourceModule)
    {
        SourceModule = sourceModule;
    }

    /// <inheritdoc/>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public string SourceModule { get; init; }

    /// <summary>
    /// Identificador do tenant que gerou este evento.
    /// Nullable para compatibilidade com eventos anteriores à Fase 5
    /// e com eventos de infraestrutura que não têm contexto de tenant.
    /// Obrigatório em eventos operacionais — usar TenantContextualEventBase quando disponível.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Identificador do ambiente onde o evento ocorreu.
    /// Nullable para compatibilidade e para eventos globais que não têm contexto de ambiente.
    /// </summary>
    public Guid? EnvironmentId { get; init; }
}
