using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Behaviors;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação thread-safe e scoped do rastreador de eventos de segurança.
///
/// Mantém em memória os <see cref="SecurityEvent"/>s criados durante uma requisição
/// para que o <see cref="SecurityEventAuditBehavior{TRequest,TResponse}"/>
/// possa propagá-los ao módulo Audit central após a execução do handler.
///
/// Tempo de vida: Scoped (uma instância por requisição HTTP).
/// Não persiste dados — apenas acumula referências em memória durante o request pipeline.
/// </summary>
internal sealed class SecurityEventTracker : ISecurityEventTracker
{
    private readonly List<SecurityEvent> _events = [];

    /// <inheritdoc />
    public void Track(SecurityEvent securityEvent)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        _events.Add(securityEvent);
    }

    /// <inheritdoc />
    public IReadOnlyList<SecurityEvent> GetTrackedEvents() => _events.AsReadOnly();

    /// <inheritdoc />
    public bool HasPendingEvents => _events.Count > 0;

    /// <inheritdoc />
    public void Clear() => _events.Clear();
}
