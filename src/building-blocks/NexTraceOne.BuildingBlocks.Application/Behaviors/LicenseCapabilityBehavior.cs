using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que verifica se a licença ativa possui a capability
/// exigida antes de executar o request. Aplica-se apenas a requests que
/// implementam <see cref="IRequiresCapability"/>.
///
/// Fluxo:
/// 1. Se o request não implementa IRequiresCapability, segue normalmente.
/// 2. Se a licença não está configurada (LicenseKey vazio), segue (modo dev).
/// 3. Se o ILicenseCapabilityChecker não está registrado, segue (módulo opcional).
/// 4. Caso contrário, verifica a capability e bloqueia se não habilitada.
///
/// Posição no pipeline: após TenantIsolationBehavior, antes de ValidationBehavior.
/// </summary>
public sealed class LicenseCapabilityBehavior<TRequest, TResponse>(
    IConfiguration configuration,
    ILogger<LicenseCapabilityBehavior<TRequest, TResponse>> logger,
    ILicenseCapabilityChecker? capabilityChecker = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequiresCapability requiresCapability)
        {
            return await next();
        }

        var licenseKey = configuration["Licensing:LicenseKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            logger.LogDebug("License key not configured — skipping capability check for {Capability}",
                requiresCapability.RequiredCapability);
            return await next();
        }

        if (capabilityChecker is null)
        {
            logger.LogDebug("License capability checker not registered — skipping check for {Capability}",
                requiresCapability.RequiredCapability);
            return await next();
        }

        var hasCapability = await capabilityChecker.HasCapabilityAsync(
            licenseKey,
            requiresCapability.RequiredCapability,
            cancellationToken);

        if (!hasCapability)
        {
            var maskedKey = licenseKey.Length > 8
                ? string.Concat(licenseKey.AsSpan(0, 4), "****", licenseKey.AsSpan(licenseKey.Length - 4))
                : "****";

            logger.LogWarning(
                "License capability check failed: capability '{Capability}' is not enabled for license '{LicenseKey}'",
                requiresCapability.RequiredCapability,
                maskedKey);

            return ResultResponseFactory.CreateFailureResponse<TResponse>(Error.Forbidden(
                "Licensing.Capability.NotEnabled",
                "The capability '{0}' is not enabled in the current license.",
                requiresCapability.RequiredCapability));
        }

        return await next();
    }
}
