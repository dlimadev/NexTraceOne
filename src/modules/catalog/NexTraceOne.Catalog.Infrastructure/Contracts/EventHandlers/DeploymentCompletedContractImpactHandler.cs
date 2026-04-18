using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.EventHandlers;

/// <summary>
/// Consome o evento DeploymentCompleted e analisa o impacto sobre contratos do serviço deployado.
///
/// Resolve gap INOVACAO-ROADMAP.md §1.2 — Change-to-Contract Impact (Automático).
///
/// Quando um deploy é bem-sucedido, o handler:
/// 1. Localiza o serviço pelo nome no catálogo
/// 2. Lista todos os API assets do serviço
/// 3. Para cada API asset, obtém a versão de contrato mais recente publicada
/// 4. Regista um deployment de contrato associando o evento ao contrato activo
/// 5. Emite log estruturado com impacto para alimentar alertas e dashboards
///
/// NOTA: O registo automático de deployment de contrato permite que a feature
/// GetContractEnvironmentVersionDrift detecte automaticamente divergências após cada deploy.
/// </summary>
internal sealed class DeploymentCompletedContractImpactHandler(
    IServiceAssetRepository serviceAssetRepository,
    IApiAssetRepository apiAssetRepository,
    IContractVersionRepository contractVersionRepository,
    IContractDeploymentRepository contractDeploymentRepository,
    IContractsUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<DeploymentCompletedContractImpactHandler> logger)
    : IIntegrationEventHandler<DeploymentCompletedIntegrationEvent>
{
    public async Task HandleAsync(DeploymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        // Apenas processar deployments bem-sucedidos — falhas não alteram o estado activo de contratos
        if (!@event.IsSuccess)
        {
            logger.LogDebug(
                "Skipping contract impact analysis for failed deployment {ChangeId}, service {ServiceName}",
                @event.ChangeId, @event.ServiceName);
            return;
        }

        logger.LogInformation(
            "Analysing contract impact for deployment {ChangeId}, service {ServiceName}, environment {Environment}",
            @event.ChangeId, @event.ServiceName, @event.EnvironmentName);

        // Encontrar o serviço no catálogo pelo nome
        var service = await serviceAssetRepository.GetByNameAsync(@event.ServiceName, ct);
        if (service is null)
        {
            logger.LogWarning(
                "Service {ServiceName} not found in catalog for deployment {ChangeId}. Contract impact analysis skipped.",
                @event.ServiceName, @event.ChangeId);
            return;
        }

        // Listar todos os API assets do serviço
        var apiAssets = await apiAssetRepository.ListByServiceIdAsync(service.Id, ct);
        if (apiAssets.Count == 0)
        {
            logger.LogInformation(
                "No API assets found for service {ServiceName} (Id={ServiceId}). No contract deployment records created.",
                @event.ServiceName, service.Id);
            return;
        }

        var contractsRegistered = 0;

        foreach (var apiAsset in apiAssets)
        {
            // Obtém a versão de contrato mais recente publicada para este API asset
            var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(apiAsset.Id.Value, ct);
            if (latestVersion is null)
            {
                logger.LogDebug(
                    "No published contract version found for API asset {ApiAssetId} ({ApiAssetName}). Skipping.",
                    apiAsset.Id.Value, apiAsset.Name);
                continue;
            }

            // Verificar se já existe deployment registado para esta combinação nos últimos 5 minutos
            // (idempotência básica para evitar duplicados em re-entrega de eventos)
            var recentDeployments = await contractDeploymentRepository.ListByContractVersionAsync(
                latestVersion.Id, ct);

            var alreadyRegistered = recentDeployments.Any(d =>
                string.Equals(d.Environment, @event.EnvironmentName, StringComparison.OrdinalIgnoreCase) &&
                d.DeployedAt > clock.UtcNow.AddMinutes(-5) &&
                d.Status == ContractDeploymentStatus.Success);

            if (alreadyRegistered)
            {
                logger.LogDebug(
                    "Deployment for contract version {ContractVersionId} in environment {Environment} already registered within last 5 minutes. Skipping duplicate.",
                    latestVersion.Id.Value, @event.EnvironmentName);
                continue;
            }

            var deployedBy = @event.OwnerUserId?.ToString() ?? "system/change-governance";

            var deployment = ContractDeployment.Create(
                contractVersionId: latestVersion.Id,
                apiAssetId: apiAsset.Id.Value,
                environment: @event.EnvironmentName,
                semVer: latestVersion.SemVer,
                status: ContractDeploymentStatus.Success,
                deployedAt: clock.UtcNow,
                deployedBy: deployedBy,
                sourceSystem: "ChangeGovernance/DeploymentCompleted",
                notes: $"Auto-registered from ChangeId={@event.ChangeId}. Service={@event.ServiceName}.");

            contractDeploymentRepository.Add(deployment);
            contractsRegistered++;
        }

        if (contractsRegistered > 0)
        {
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Contract impact registered: {ContractsRegistered} deployment records created for service {ServiceName} " +
                "in environment {Environment} (ChangeId={ChangeId})",
                contractsRegistered, @event.ServiceName, @event.EnvironmentName, @event.ChangeId);
        }
        else
        {
            logger.LogInformation(
                "No new contract deployment records created for service {ServiceName} (ChangeId={ChangeId}). " +
                "Either no contracts are published or deployments were already registered.",
                @event.ServiceName, @event.ChangeId);
        }
    }
}
