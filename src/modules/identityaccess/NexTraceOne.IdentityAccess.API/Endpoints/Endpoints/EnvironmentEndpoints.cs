using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListEnvironmentsFeature = NexTraceOne.IdentityAccess.Application.Features.ListEnvironments.ListEnvironments;
using GrantEnvironmentAccessFeature = NexTraceOne.IdentityAccess.Application.Features.GrantEnvironmentAccess.GrantEnvironmentAccess;
using CreateEnvironmentFeature = NexTraceOne.IdentityAccess.Application.Features.CreateEnvironment.CreateEnvironment;
using UpdateEnvironmentFeature = NexTraceOne.IdentityAccess.Application.Features.UpdateEnvironment.UpdateEnvironment;
using SetPrimaryProductionFeature = NexTraceOne.IdentityAccess.Application.Features.SetPrimaryProductionEnvironment.SetPrimaryProductionEnvironment;
using GetPrimaryProductionFeature = NexTraceOne.IdentityAccess.Application.Features.GetPrimaryProductionEnvironment.GetPrimaryProductionEnvironment;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de ambientes (Environment) do módulo Environment Management.
///
/// Ambientes representam estágios do ciclo de vida configuráveis por tenant.
/// Cada tenant pode definir os seus próprios ambientes (DEV, QA, UAT, PROD, DR, etc.),
/// com perfil operacional, criticidade e designação do ambiente produtivo principal.
///
/// Permissões seguem o namespace env:* (separado do identity:*):
/// - env:environments:read — listar e consultar ambientes
/// - env:environments:write — criar e editar ambientes
/// - env:environments:admin — designar produção principal, soft-delete
/// - env:access:read — consultar acessos
/// - env:access:admin — conceder e revogar acessos
/// </summary>
internal static class EnvironmentEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de ambiente no subgrupo <c>/environments</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var envGroup = group.MapGroup("/environments");

        // Lista ambientes ativos do tenant
        envGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListEnvironmentsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:environments:read");

        // Obtém o ambiente produtivo principal do tenant
        envGroup.MapGet("/primary-production", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPrimaryProductionFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:environments:read");

        // Cria um novo ambiente para o tenant — requer permissão de escrita
        envGroup.MapPost("/", async (
            CreateEnvironmentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:environments:write");

        // Atualiza metadados de um ambiente existente — requer permissão de escrita
        envGroup.MapPut("/{environmentId:guid}", async (
            Guid environmentId,
            UpdateEnvironmentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            // Garante que o environmentId da rota coincide com o do body
            var cmd = command with { EnvironmentId = environmentId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:environments:write");

        // Designa um ambiente como produção principal do tenant — requer permissão administrativa
        // específica para operações em produção, distinta de operações em ambientes não-produtivos.
        envGroup.MapPatch("/{environmentId:guid}/primary-production", async (
            Guid environmentId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SetPrimaryProductionFeature.Command(environmentId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:environments:production:admin");

        // Concede acesso a um ambiente para um utilizador — requer permissão administrativa de acesso
        envGroup.MapPost("/access", async (
            GrantEnvironmentAccessFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("env:access:admin");
    }
}
