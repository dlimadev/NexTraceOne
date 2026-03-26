namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Excepção lançada quando uma operação de escrita deteta um conflito de concorrência otimista.
/// Ocorre quando a linha foi modificada por outro processo entre o momento em que foi lida
/// e o momento em que a escrita tentou persistir.
///
/// Esta excepção é lançada pela camada de infraestrutura (DbContext) para isolar
/// a camada de aplicação da dependência direta em <c>Microsoft.EntityFrameworkCore</c>.
/// Os handlers devem capturá-la e converter para <see cref="NexTraceOne.BuildingBlocks.Core.Results.Error.Conflict"/>.
/// </summary>
public sealed class ConcurrencyException(string entityType, Exception? innerException = null)
    : Exception($"Concurrency conflict detected while saving '{entityType}'. The entity was modified by another process.", innerException)
{
    /// <summary>Tipo da entidade que originou o conflito de concorrência.</summary>
    public string EntityType { get; } = entityType;
}
