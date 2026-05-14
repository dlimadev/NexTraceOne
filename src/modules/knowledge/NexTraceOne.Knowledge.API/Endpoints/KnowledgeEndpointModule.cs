using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using CreateKnowledgeDocumentFeature = NexTraceOne.Knowledge.Application.Features.CreateKnowledgeDocument.CreateKnowledgeDocument;
using CreateOperationalNoteFeature = NexTraceOne.Knowledge.Application.Features.CreateOperationalNote.CreateOperationalNote;
using CreateKnowledgeRelationFeature = NexTraceOne.Knowledge.Application.Features.CreateKnowledgeRelation.CreateKnowledgeRelation;
using GetKnowledgeByRelationTargetFeature = NexTraceOne.Knowledge.Application.Features.GetKnowledgeByRelationTarget.GetKnowledgeByRelationTarget;
using GetKnowledgeRelationsBySourceFeature = NexTraceOne.Knowledge.Application.Features.GetKnowledgeRelationsBySource.GetKnowledgeRelationsBySource;
using ListKnowledgeDocumentsFeature = NexTraceOne.Knowledge.Application.Features.ListKnowledgeDocuments.ListKnowledgeDocuments;
using GetKnowledgeDocumentByIdFeature = NexTraceOne.Knowledge.Application.Features.GetKnowledgeDocumentById.GetKnowledgeDocumentById;
using ListOperationalNotesFeature = NexTraceOne.Knowledge.Application.Features.ListOperationalNotes.ListOperationalNotes;
using UpdateKnowledgeDocumentFeature = NexTraceOne.Knowledge.Application.Features.UpdateKnowledgeDocument.UpdateKnowledgeDocument;
using UpdateOperationalNoteFeature = NexTraceOne.Knowledge.Application.Features.UpdateOperationalNote.UpdateOperationalNote;
using GetKnowledgeGraphOverviewFeature = NexTraceOne.Knowledge.Application.Features.GetKnowledgeGraphOverview.GetKnowledgeGraphOverview;
using GenerateAutoDocumentationFeature = NexTraceOne.Knowledge.Application.Features.GenerateAutoDocumentation.GenerateAutoDocumentation;
using GetServiceOperationalTimelineFeature = NexTraceOne.Knowledge.Application.Features.GetServiceOperationalTimeline.GetServiceOperationalTimeline;

using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.Knowledge.Domain.Enums;
using NexTraceOne.BuildingBlocks.Security.Extensions;

namespace NexTraceOne.Knowledge.API.Endpoints;

/// <summary>
/// Endpoints do Knowledge Hub — gestão de documentos de conhecimento,
/// notas operacionais e relações entre objectos de conhecimento e outros contextos.
///
/// Módulo nativo de Knowledge. Ownership real do módulo Knowledge.
/// As rotas /api/v1/knowledge pertencem ao módulo Knowledge.
///
/// P10.1: Endpoint module mínimo — endpoints CRUD serão adicionados em P10.2.
/// P10.2: Adicionado endpoint de pesquisa /api/v1/knowledge/search.
/// </summary>
public sealed class KnowledgeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var knowledge = app.MapGroup("/api/v1/knowledge");

        // P10.1: Endpoint de health-check mínimo do módulo Knowledge.
        knowledge.MapGet("/status", () =>
            Results.Ok(new { module = "Knowledge", status = "active", version = "10.3" }))
            .WithTags("Knowledge")
            .WithSummary("Knowledge module status check")
            .AllowAnonymous();

        // P10.2: Endpoint de pesquisa no Knowledge Hub.
        knowledge.MapGet("/search", async (
            string q,
            string? scope,
            int? maxResults,
            [FromServices] IKnowledgeSearchProvider searchProvider,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
                return Results.BadRequest(new { error = "Search term is required and must be at most 200 characters." });

            var max = maxResults is > 0 and <= 100 ? maxResults.Value : 25;
            var results = await searchProvider.SearchAsync(q, scope, max, cancellationToken);
            return Results.Ok(new
            {
                items = results,
                totalResults = results.Count
            });
        })
        .WithTags("Knowledge")
        .WithSummary("Search knowledge documents and operational notes")
        .RequirePermission("knowledge:read");

        // P10.3: Criação mínima de documento para fluxo de ligação contextual.
        knowledge.MapPost("/documents", async (
            CreateKnowledgeDocumentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(id => $"/api/v1/knowledge/documents/{id.DocumentId}", localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Create knowledge document")
        .RequirePermission("knowledge:write");

        // P10.3: Criação mínima de nota operacional contextual.
        knowledge.MapPost("/operational-notes", async (
            CreateOperationalNoteFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(id => $"/api/v1/knowledge/operational-notes/{id.NoteId}", localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Create operational note")
        .RequirePermission("knowledge:write");

        // P10.3: Criação de relação entre objeto de conhecimento e entidade-alvo.
        knowledge.MapPost("/relations", async (
            CreateKnowledgeRelationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(id => $"/api/v1/knowledge/relations/{id.RelationId}", localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Create knowledge relation")
        .RequirePermission("knowledge:write");

        // P10.3: Consulta conhecimento contextual por alvo (service/contract/change/incident).
        knowledge.MapGet("/relations/by-target/{targetType}/{targetEntityId:guid}", async (
            string targetType,
            Guid targetEntityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<NexTraceOne.Knowledge.Domain.Enums.RelationType>(targetType, true, out var parsedType))
            {
                return Results.BadRequest(new
                {
                    error = "targetType must be one of: service, contract, change, incident, knowledgedocument, runbook, other."
                });
            }

            var result = await sender.Send(
                new GetKnowledgeByRelationTargetFeature.Query(parsedType, targetEntityId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Get knowledge linked to a target entity")
        .RequirePermission("knowledge:read");

        // P10.3: Consulta de relações por origem (documento/nota) para navegação contextual mínima.
        knowledge.MapGet("/relations/by-source/{sourceEntityId:guid}", async (
            Guid sourceEntityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetKnowledgeRelationsBySourceFeature.Query(sourceEntityId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Get target relations for a knowledge source entity")
        .RequirePermission("knowledge:read");

        // P10.4: Listagem paginada de documentos de conhecimento.
        knowledge.MapGet("/documents", async (
            string? category,
            string? status,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            DocumentCategory? parsedCategory = null;
            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<DocumentCategory>(category, true, out var cat))
                parsedCategory = cat;

            DocumentStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var st))
                parsedStatus = st;

            var result = await sender.Send(
                new ListKnowledgeDocumentsFeature.Query(parsedCategory, parsedStatus, page < 1 ? 1 : page, pageSize < 1 ? 25 : pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("List knowledge documents with pagination")
        .RequirePermission("knowledge:read");

        // P10.4: Detalhe completo de um documento de conhecimento.
        knowledge.MapGet("/documents/{documentId:guid}", async (
            Guid documentId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetKnowledgeDocumentByIdFeature.Query(documentId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Get knowledge document detail")
        .RequirePermission("knowledge:read");

        // P10.4: Listagem paginada de notas operacionais.
        knowledge.MapGet("/operational-notes", async (
            string? severity,
            string? contextType,
            Guid? contextEntityId,
            bool? isResolved,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            NoteSeverity? parsedSeverity = null;
            if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<NoteSeverity>(severity, true, out var sev))
                parsedSeverity = sev;

            var result = await sender.Send(
                new ListOperationalNotesFeature.Query(
                    parsedSeverity,
                    string.IsNullOrWhiteSpace(contextType) ? null : contextType,
                    contextEntityId,
                    isResolved,
                    page < 1 ? 1 : page,
                    pageSize < 1 ? 25 : pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("List operational notes with pagination")
        .RequirePermission("knowledge:read");

        // P10.5: Atualização de documento de conhecimento.
        knowledge.MapPut("/documents/{documentId:guid}", async (
            Guid documentId,
            UpdateKnowledgeDocumentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var enriched = command with { DocumentId = documentId };
            var result = await sender.Send(enriched, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Update knowledge document")
        .RequirePermission("knowledge:write");

        // P10.5: Atualização de nota operacional (inclui resolve/reopen).
        knowledge.MapPut("/operational-notes/{noteId:guid}", async (
            Guid noteId,
            UpdateOperationalNoteFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var enriched = command with { NoteId = noteId };
            var result = await sender.Send(enriched, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Update operational note")
        .RequirePermission("knowledge:write");

        // ── GET /api/v1/knowledge/graph — Knowledge Graph Overview ──
        knowledge.MapGet("/graph", async (
            string? centerEntityType,
            string? centerEntityId,
            int? maxDepth,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetKnowledgeGraphOverviewFeature.Query(centerEntityType, centerEntityId, maxDepth ?? 2),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Get knowledge graph overview — nodes, edges and connected components")
        .RequirePermission("knowledge:read");

        // ── GET /api/v1/knowledge/auto-documentation/{serviceName} ──
        knowledge.MapGet("/auto-documentation/{serviceName}", async (
            string serviceName,
            string? sections,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var sectionList = string.IsNullOrWhiteSpace(sections)
                ? null
                : (IReadOnlyList<string>)sections.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var result = await sender.Send(
                new GenerateAutoDocumentationFeature.Query(serviceName, sectionList),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Generate auto documentation for a service from Knowledge Hub data")
        .RequirePermission("knowledge:read");

        // ── GET /api/v1/knowledge/services/{serviceId}/operational-timeline ──
        knowledge.MapGet("/services/{serviceId:guid}/operational-timeline", async (
            Guid serviceId,
            string? severity,
            bool? isResolved,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            NoteSeverity? parsedSeverity = null;
            if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<NoteSeverity>(severity, true, out var sev))
                parsedSeverity = sev;

            var result = await sender.Send(
                new GetServiceOperationalTimelineFeature.Query(
                    ServiceId: serviceId,
                    Severity: parsedSeverity,
                    IsResolved: isResolved,
                    Page: page ?? 1,
                    PageSize: pageSize ?? 25),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithTags("Knowledge")
        .WithSummary("Get operational notes timeline for a specific service")
        .RequirePermission("knowledge:read");
    }
}
