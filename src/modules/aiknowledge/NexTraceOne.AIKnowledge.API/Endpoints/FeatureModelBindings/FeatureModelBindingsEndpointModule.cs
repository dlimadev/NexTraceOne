using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateFeatureModelBinding;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateFeatureModelBinding;
using NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteFeatureModelBinding;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListFeatureModelBindings;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetFeatureModelBinding;

namespace NexTraceOne.AIKnowledge.API.Endpoints.FeatureModelBindings;

/// <summary>
/// Endpoints de gestão de vinculações feature → modelo de IA.
/// Permitem configurar qual modelo de IA é utilizado para cada funcionalidade da plataforma por tenant.
/// </summary>
public sealed class FeatureModelBindingsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/feature-model-bindings")
            .WithTags("AI Governance - Feature Model Bindings")
            .RequireAuthorization();

        // GET /api/v1/ai/feature-model-bindings — Lista vinculações do tenant
        group.MapGet("/", async (
            bool? isActive,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeatureModelBindings.Query(isActive), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ListFeatureModelBindings")
        .WithSummary("Lista vinculações de funcionalidade para modelo de IA do tenant");

        // GET /api/v1/ai/feature-model-bindings/{id} — Obtém vinculação por ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetFeatureModelBinding.Query(id), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GetFeatureModelBinding")
        .WithSummary("Obtém uma vinculação feature → modelo por ID");

        // POST /api/v1/ai/feature-model-bindings — Cria nova vinculação
        group.MapPost("/", async (
            CreateFeatureModelBinding.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                $"/api/v1/ai/feature-model-bindings/{result.Value?.BindingId}");
        })
        .WithName("CreateFeatureModelBinding")
        .WithSummary("Cria uma nova vinculação entre funcionalidade e modelo de IA");

        // PUT /api/v1/ai/feature-model-bindings/{id} — Atualiza vinculação
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateFeatureModelBinding.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var commandWithId = command with { BindingId = id };
            var result = await sender.Send(commandWithId, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateFeatureModelBinding")
        .WithSummary("Atualiza a vinculação feature → modelo");

        // DELETE /api/v1/ai/feature-model-bindings/{id} — Desativa vinculação
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeatureModelBinding.Command(id), cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteFeatureModelBinding")
        .WithSummary("Desativa uma vinculação feature → modelo");
    }
}
