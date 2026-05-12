using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Handler de rede que impede chamadas HTTP externas no modo AirGap.
/// Quando Platform:NetworkIsolation:Mode == "AirGap", qualquer tentativa de chamada
/// HTTP de saída é bloqueada e registada como violação de segurança.
/// </summary>
public sealed class AirGapHttpMessageHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AirGapHttpMessageHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AirGapHttpMessageHandler(
        IConfiguration configuration,
        ILogger<AirGapHttpMessageHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var mode = _configuration["Platform:NetworkIsolation:Mode"] ?? "Off";

        if (mode.Equals("AirGap", StringComparison.OrdinalIgnoreCase))
        {
            var destination = request.RequestUri?.Host ?? "unknown";

            // Registar evento de segurança via scope para evitar dependência circular
            await RecordSecurityViolationAsync(destination, request, cancellationToken);

            _logger.LogWarning(
                "[SECURITY] AirGap violation blocked: outbound HTTP request to '{Destination}' was prevented. " +
                "NetworkIsolation.Mode={Mode}. Request={Method} {Uri}",
                destination, mode, request.Method, request.RequestUri);

            throw new InvalidOperationException(
                $"Outbound HTTP request to '{destination}' is blocked by AirGap network isolation policy " +
                $"(Platform:NetworkIsolation:Mode = {mode}).");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task RecordSecurityViolationAsync(
        string destination,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            // Tentar obter o repositório de eventos de segurança dinamicamente
            var securityEventRepositoryType = Type.GetType(
                "NexTraceOne.IdentityAccess.Application.Abstractions.ISecurityEventRepository, NexTraceOne.IdentityAccess.Application");

            if (securityEventRepositoryType is null)
            {
                _logger.LogDebug("ISecurityEventRepository não disponível — violação AirGap apenas em log.");
                return;
            }

            var repository = scope.ServiceProvider.GetService(securityEventRepositoryType);
            if (repository is null)
            {
                _logger.LogDebug("Instância de ISecurityEventRepository não disponível.");
                return;
            }

            // Obter ICurrentTenant para o tenant atual
            var currentTenantType = Type.GetType(
                "NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentTenant, NexTraceOne.BuildingBlocks.Application");

            object? currentTenant = null;
            Guid tenantId = Guid.Empty;

            if (currentTenantType is not null)
            {
                currentTenant = scope.ServiceProvider.GetService(currentTenantType);

                if (currentTenant is not null)
                {
                    var tenantIdProperty = currentTenantType.GetProperty("Id");
                    if (tenantIdProperty?.GetValue(currentTenant) is Guid guid)
                    {
                        tenantId = guid;
                    }
                }
            }

            // Criar SecurityEvent via reflexão
            var securityEventType = Type.GetType(
                "NexTraceOne.IdentityAccess.Domain.Entities.SecurityEvent, NexTraceOne.IdentityAccess.Domain");

            if (securityEventType is null)
            {
                _logger.LogDebug("Tipo SecurityEvent não encontrado.");
                return;
            }

            var createMethod = securityEventType.GetMethod("Create");
            if (createMethod is null)
            {
                _logger.LogDebug("Método Create não encontrado em SecurityEvent.");
                return;
            }

            // Criar instância de TenantId
            var tenantIdType = Type.GetType("NexTraceOne.BuildingBlocks.Core.StronglyTypedIds.TenantId, NexTraceOne.BuildingBlocks.Core");
            var fromMethod = tenantIdType?.GetMethod("From");
            var tenantIdInstance = fromMethod?.Invoke(null, new object[] { tenantId });

            // Criar evento de segurança
            var securityEvent = createMethod.Invoke(null, new[]
            {
                tenantIdInstance,
                null,  // userId
                null,  // sessionId
                "AirGap.Violation",
                $"Tentativa de chamada HTTP externa bloqueada: {request.Method} {request.RequestUri}",
                75,    // riskScore
                null,  // ipAddress
                null,  // userAgent
                $$"""{"destination":"{{destination}}","method":"{{request.Method}}"}""",
                DateTimeOffset.UtcNow
            });

            // Chamar método Add no repositório
            var addMethod = securityEventRepositoryType.GetMethod("Add");
            addMethod?.Invoke(repository, new[] { securityEvent });

            _logger.LogInformation(
                "Evento de segurança AirGap.Violation registado para tenant {TenantId}.",
                tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registar evento de segurança AirGap.Violation.");
        }
    }
}
