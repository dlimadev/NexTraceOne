using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Implementação scoped do accessor de contexto de ambiente ativo.
/// Populado pelo EnvironmentResolutionMiddleware após resolução e validação.
/// </summary>
public sealed class EnvironmentContextAccessor : IEnvironmentContextAccessor
{
    private EnvironmentId _environmentId = new(Guid.Empty);
    private EnvironmentProfile _profile = EnvironmentProfile.Development;
    private bool _isProductionLike;
    private bool _isResolved;

    /// <inheritdoc />
    public EnvironmentId EnvironmentId => _environmentId;

    /// <inheritdoc />
    public EnvironmentProfile Profile => _profile;

    /// <inheritdoc />
    public bool IsProductionLike => _isProductionLike;

    /// <inheritdoc />
    public bool IsResolved => _isResolved;

    /// <summary>
    /// Configura o contexto de ambiente resolvido.
    /// Chamado pelo EnvironmentResolutionMiddleware após validação.
    /// </summary>
    public void Set(EnvironmentId environmentId, EnvironmentProfile profile, bool isProductionLike)
    {
        _environmentId = environmentId;
        _profile = profile;
        _isProductionLike = isProductionLike;
        _isResolved = true;
    }
}
