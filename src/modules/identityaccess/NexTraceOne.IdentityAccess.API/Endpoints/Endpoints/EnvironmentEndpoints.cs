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
/// Endpoints de gestão de ambientes (Environment) do módulo Identity.
///
/// Ambientes representam estágios do ciclo de vida configuráveis por tenant.
/// Cada tenant pode definir os seus próprios ambientes (DEV, QA, UAT, PROD, DR, etc.),
/// com perfil operacional, criticidade e designação do ambiente produtivo principal.
///
/// A separação entre Tenant (organização/cliente) e Environment (estágio operacional)
/// é fundamental para a governança enterprise do NexTraceOne.
/// </summary>
internal static class EnvironmentEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de ambiente no subgrupo <c>/environments</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var envGroup = group.MapGroup("/environments");

        // Lista ambientes ativos do tenant — qualquer utilizador autenticado pode visualizar
        envGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListEnvironmentsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Obtém o ambiente produtivo principal do tenant
        envGroup.MapGet("/primary-production", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPrimaryProductionFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Cria um novo ambiente para o tenant — requer permissão administrativa
        envGroup.MapPost("/", async (
            CreateEnvironmentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");

        // Atualiza metadados de um ambiente existente — requer permissão administrativa
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
        }).RequirePermission("identity:users:write");

        // Designa um ambiente como produção principal do tenant — requer permissão administrativa
        envGroup.MapPatch("/{environmentId:guid}/primary-production", async (
            Guid environmentId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SetPrimaryProductionFeature.Command(environmentId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");

        // Concede acesso a um ambiente para um utilizador — requer permissão administrativa
        envGroup.MapPost("/access", async (
            GrantEnvironmentAccessFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");
    }
}
