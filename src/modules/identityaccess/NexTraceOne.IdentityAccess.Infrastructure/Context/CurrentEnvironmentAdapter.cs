using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Adapter que implementa ICurrentEnvironment expondo o contexto de ambiente
/// resolvido pelo EnvironmentContextAccessor para consumo pelos módulos operacionais.
///
/// Permite que módulos como OperationalIntelligence e ChangeGovernance acessem
/// o ambiente ativo da requisição sem depender diretamente do módulo IdentityAccess.
/// </summary>
internal sealed class CurrentEnvironmentAdapter(EnvironmentContextAccessor accessor)
    : ICurrentEnvironment
{
    /// <inheritdoc />
    public Guid EnvironmentId => accessor.EnvironmentId.Value;

    /// <inheritdoc />
    public bool IsResolved => accessor.IsResolved;

    /// <inheritdoc />
    public bool IsProductionLike => accessor.IsProductionLike;
}
