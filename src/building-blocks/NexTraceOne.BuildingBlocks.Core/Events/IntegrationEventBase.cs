namespace NexTraceOne.BuildingBlocks.Core;

/// <summary>
/// Implementação base para Integration Events publicados entre módulos.
/// Preenche automaticamente EventId, OccurredAt e SourceModule.
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
}
