using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NotifyDeploymentFeature = NexTraceOne.ChangeIntelligence.Application.Features.NotifyDeployment.NotifyDeployment;
using UpdateDeploymentStateFeature = NexTraceOne.ChangeIntelligence.Application.Features.UpdateDeploymentState.UpdateDeploymentState;
using RegisterRollbackFeature = NexTraceOne.ChangeIntelligence.Application.Features.RegisterRollback.RegisterRollback;

namespace NexTraceOne.ChangeIntelligence.API.Endpoints;

/// <summary>
/// Endpoints de gestão do ciclo de vida de deployments.
/// Recebe notificações do CI/CD pipeline, atualiza estado de execução
/// e registra rollbacks para rastreabilidade completa.
///
/// Estes endpoints são consumidos primariamente pela integração com
/// pipelines CI/CD (webhooks) e pelo dashboard de operações.
/// </summary>
internal static class DeploymentEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de deployment diretamente no grupo raiz de releases.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            NotifyDeploymentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/releases/{0}", localizer);
        });

        group.MapPut("/{releaseId:guid}/status", async (
            Guid releaseId,
            UpdateDeploymentStateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/{releaseId:guid}/rollback", async (
            Guid releaseId,
            RegisterRollbackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
