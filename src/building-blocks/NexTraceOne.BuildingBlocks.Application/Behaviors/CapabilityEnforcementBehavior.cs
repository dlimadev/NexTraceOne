using System.Collections.Concurrent;
using System.Reflection;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que aplica o enforcement de capabilities de licença.
/// Requests anotados com <see cref="RequiresCapabilityAttribute"/> só executam
/// se o tenant ativo possuir TODAS as capabilities declaradas; caso contrário
/// retornam erro Forbidden mapeado para HTTP 403.
/// Executa após o TenantIsolationBehavior (tenant já garantido no contexto).
/// </summary>
public sealed class CapabilityEnforcementBehavior<TRequest, TResponse>(
    ICurrentTenant currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Cache por tipo de request — reflexão de atributos só na primeira execução.
    private static readonly ConcurrentDictionary<Type, string[]> RequiredCapabilitiesCache = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requiredCapabilities = RequiredCapabilitiesCache.GetOrAdd(
            typeof(TRequest),
            static type => type
                .GetCustomAttributes<RequiresCapabilityAttribute>(inherit: false)
                .Select(a => a.Capability)
                .ToArray());

        if (requiredCapabilities.Length == 0 || request is IPublicRequest)
        {
            return await next();
        }

        var missing = requiredCapabilities.FirstOrDefault(c => !currentTenant.HasCapability(c));
        if (missing is not null)
        {
            return ResultResponseFactory.CreateFailureResponse<TResponse>(Error.Forbidden(
                "CapabilityRequired",
                "This feature requires the '{0}' capability. Upgrade your plan to enable it.",
                missing));
        }

        return await next();
    }
}
