using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateUserTokenQuota;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateUserTokenQuota;
using NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteUserTokenQuota;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListUserTokenQuotas;

namespace NexTraceOne.AIKnowledge.API.Endpoints.UserTokenQuotas;

/// <summary>
/// Endpoints de gestão de quotas de tokens por utilizador.
/// Permitem configurar limites de consumo de tokens de IA por utilizador, provider e modelo.
/// </summary>
public sealed class UserTokenQuotasEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/user-token-quotas")
            .WithTags("AI Governance - User Token Quotas")
            .RequireAuthorization();

        // GET /api/v1/ai/user-token-quotas — Lista quotas de tokens por utilizador
        group.MapGet("/", async (
            string? userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListUserTokenQuotas.Query(userId), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListUserTokenQuotas")
        .WithSummary("Lista quotas de tokens de IA por utilizador");

        // POST /api/v1/ai/user-token-quotas — Cria quota para utilizador
        group.MapPost("/", async (
            CreateUserTokenQuota.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                $"/api/v1/ai/user-token-quotas/{result.Value?.QuotaId}");
        })
        .WithName("CreateUserTokenQuota")
        .WithSummary("Cria uma quota de tokens de IA para um utilizador específico");

        // PUT /api/v1/ai/user-token-quotas/{id} — Atualiza quota
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserTokenQuota.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var commandWithId = command with { QuotaId = id };
            var result = await sender.Send(commandWithId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateUserTokenQuota")
        .WithSummary("Atualiza a quota de tokens de um utilizador");

        // DELETE /api/v1/ai/user-token-quotas/{id} — Desativa quota
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteUserTokenQuota.Command(id), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteUserTokenQuota")
        .WithSummary("Desativa a quota de tokens de um utilizador");
    }
}
