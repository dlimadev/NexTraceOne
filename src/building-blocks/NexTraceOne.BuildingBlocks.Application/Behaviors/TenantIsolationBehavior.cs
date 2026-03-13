using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

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
        if (request is IPublicRequest)
        {
            return await next();
        }

        if (currentTenant.Id == Guid.Empty)
        {
            return ResultResponseFactory.CreateFailureResponse<TResponse>(Error.Security(
                "Tenant.Isolation.NoTenant",
                "Tenant context was not provided."));
        }

        if (!currentTenant.IsActive)
        {
            return ResultResponseFactory.CreateFailureResponse<TResponse>(Error.Forbidden(
                "Tenant.Isolation.Inactive",
                "Tenant '{0}' is inactive.",
                currentTenant.Name));
        }

        return await next();
    }
}
