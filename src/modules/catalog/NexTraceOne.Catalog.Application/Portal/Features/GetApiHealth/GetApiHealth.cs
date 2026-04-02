using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetApiHealth;

/// <summary>
/// Feature: GetApiHealth — retorna indicadores de saúde e disponibilidade de uma API.
/// Compõe health status a partir do estado do contrato, deployments e ownership.
/// Métricas de runtime (SLO, latência, error rate) aguardam integração cross-module
/// com RuntimeIntelligence.
/// </summary>
public static class GetApiHealth
{
    /// <summary>Query para obter indicadores de saúde de uma API.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de saúde.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna indicadores de saúde da API.
    /// Constrói health status a partir do contrato e deployments.
    /// Métricas de runtime (SLO, latência, error rate) aguardam IRuntimeIntelligenceModule.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IContractDeploymentRepository contractDeploymentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch latest contract
            var latestContract = await contractVersionRepository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            // Determine health status from contract lifecycle + deployment state
            string healthStatus;
            string? lastDeploymentStatus = null;

            if (latestContract is null)
            {
                healthStatus = "Unknown";
            }
            else
            {
                // Check deployment status for the latest contract
                var deployments = await contractDeploymentRepository.ListByContractVersionAsync(
                    latestContract.Id, cancellationToken);
                var latestDeployment = deployments.Count > 0 ? deployments[0] : null;

                if (latestDeployment is not null)
                {
                    lastDeploymentStatus = latestDeployment.Status.ToString();
                }

                healthStatus = latestContract.LifecycleState switch
                {
                    ContractLifecycleState.Approved or ContractLifecycleState.Locked
                        when latestDeployment?.Status is ContractDeploymentStatus.Success => "Healthy",
                    ContractLifecycleState.Approved or ContractLifecycleState.Locked => "Active",
                    ContractLifecycleState.Deprecated => "Degraded",
                    ContractLifecycleState.Sunset or ContractLifecycleState.Retired => "Critical",
                    _ => "Unknown"
                };
            }

            // SLO compliance from contract SLA if available
            decimal? sloCompliance = latestContract?.Sla is not null
                ? latestContract.Sla.AvailabilityTarget
                : null;

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                HealthStatus: healthStatus,
                SloCompliance: sloCompliance,
                AverageLatencyMs: null, // Requires IRuntimeIntelligenceModule
                ErrorRate: null,        // Requires IRuntimeIntelligenceModule
                LastDeploymentStatus: lastDeploymentStatus,
                LastCheckedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Resposta com indicadores de saúde da API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string HealthStatus,
        decimal? SloCompliance,
        long? AverageLatencyMs,
        decimal? ErrorRate,
        string? LastDeploymentStatus,
        DateTimeOffset? LastCheckedAt);
}
