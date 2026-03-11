using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que garante isolamento de tenant em cada request.
/// Verifica se o TenantId está presente no contexto antes de executar
/// qualquer Command ou Query. Requests sem tenant ativo são rejeitados
/// com erro de segurança, exceto para endpoints públicos marcados com IPublicRequest.
/// </summary>
public sealed class TenantIsolationBehavior<TRequest, TResponse>(
    ICurrentTenant currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Verificar tenant ativo e rejeitar se ausente (exceto IPublicRequest)
        throw new NotImplementedException();
    }
}
