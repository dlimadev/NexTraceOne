using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateUserModelPolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateUserModelPolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteUserModelPolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListUserModelPolicies;

namespace NexTraceOne.AIKnowledge.API.Endpoints.UserModelPolicies;

/// <summary>
/// Endpoints de gestão de políticas de acesso a modelos por utilizador.
/// Permitem definir quais modelos cada utilizador pode utilizar (allowlist/denylist).
/// </summary>
public sealed class UserModelPoliciesEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/user-model-policies")
            .WithTags("AI Governance - User Model Policies")
            .RequireAuthorization();

        // GET /api/v1/ai/user-model-policies — Lista políticas de modelos por utilizador
        group.MapGet("/", async (
            bool? isActive,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListUserModelPolicies.Query(isActive), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListUserModelPolicies")
        .WithSummary("Lista políticas de acesso a modelos de IA por utilizador");

        // POST /api/v1/ai/user-model-policies — Cria política para utilizador
        group.MapPost("/", async (
            CreateUserModelPolicy.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                $"/api/v1/ai/user-model-policies/{result.Value?.PolicyId}");
        })
        .WithName("CreateUserModelPolicy")
        .WithSummary("Cria uma política de acesso a modelos de IA para um utilizador específico");

        // PUT /api/v1/ai/user-model-policies/{id} — Atualiza política
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserModelPolicy.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var commandWithId = command with { PolicyId = id };
            var result = await sender.Send(commandWithId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateUserModelPolicy")
        .WithSummary("Atualiza a política de acesso a modelos de um utilizador");

        // DELETE /api/v1/ai/user-model-policies/{id} — Desativa política
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteUserModelPolicy.Command(id), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteUserModelPolicy")
        .WithSummary("Desativa a política de acesso a modelos de um utilizador");
    }
}
