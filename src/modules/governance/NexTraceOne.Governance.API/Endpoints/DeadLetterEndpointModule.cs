using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Security.Extensions;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints Platform Admin para gestão da Dead Letter Queue (DLQ) do Outbox.
/// Permite listar, reprocessar e descartar mensagens que esgotaram as tentativas de entrega.
/// </summary>
public sealed class DeadLetterEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/dead-letters");

        // ── GET /api/v1/platform/dead-letters ─────────────────────────────────────
        group.MapGet("/", async (
            Guid? tenantId,
            string? status,
            int page,
            int pageSize,
            [FromServices] IDeadLetterRepository repository,
            CancellationToken cancellationToken) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

            DlqMessageStatus? parsedStatus = null;
            if (status is not null)
            {
                if (!Enum.TryParse<DlqMessageStatus>(status, ignoreCase: true, out var s))
                    return Results.BadRequest(new { error = $"Invalid status '{status}'. Valid values: Pending, Reprocessing, Resolved, Discarded." });
                parsedStatus = s;
            }

            var result = await repository.ListAsync(tenantId, parsedStatus, page, pageSize, cancellationToken);

            return Results.Ok(new DeadLetterListResponse(
                Items: result.Items.Select(ToDto).ToList(),
                Total: result.Total,
                Page: result.Page,
                PageSize: result.PageSize));
        })
        .RequirePermission("platform:admin:read");

        // ── POST /api/v1/platform/dead-letters/{id}/reprocess ─────────────────────
        group.MapPost("/{id:guid}/reprocess", async (
            Guid id,
            [FromServices] IDeadLetterRepository repository,
            CancellationToken cancellationToken) =>
        {
            var message = await repository.FindByIdAsync(id, cancellationToken);
            if (message is null)
                return Results.NotFound(new { error = $"Dead letter message '{id}' not found." });

            if (message.Status == DlqMessageStatus.Resolved || message.Status == DlqMessageStatus.Discarded)
                return Results.Conflict(new
                {
                    error = $"Cannot reprocess a message with status '{message.Status}'. Only Pending or Reprocessing messages can be reprocessed."
                });

            message.MarkReprocessing(DateTimeOffset.UtcNow);
            await repository.UpdateAsync(message, cancellationToken);

            return Results.Ok(ToDto(message));
        })
        .RequirePermission("platform:admin:write");

        // ── POST /api/v1/platform/dead-letters/{id}/discard ──────────────────────
        group.MapPost("/{id:guid}/discard", async (
            Guid id,
            DiscardRequest request,
            [FromServices] IDeadLetterRepository repository,
            CancellationToken cancellationToken) =>
        {
            var message = await repository.FindByIdAsync(id, cancellationToken);
            if (message is null)
                return Results.NotFound(new { error = $"Dead letter message '{id}' not found." });

            if (message.Status == DlqMessageStatus.Discarded)
                return Results.Conflict(new { error = "Message is already discarded." });

            message.MarkDiscarded(request.Reason ?? "Manually discarded by platform admin.");
            await repository.UpdateAsync(message, cancellationToken);

            return Results.Ok(ToDto(message));
        })
        .RequirePermission("platform:admin:write");
    }

    private static DeadLetterDto ToDto(DeadLetterMessage m) => new(
        Id: m.Id,
        TenantId: m.TenantId,
        MessageType: m.MessageType,
        FailureReason: m.FailureReason,
        LastException: m.LastException,
        AttemptCount: m.AttemptCount,
        ExhaustedAt: m.ExhaustedAt,
        ReprocessedAt: m.ReprocessedAt,
        Status: m.Status.ToString());
}

internal sealed record DeadLetterListResponse(
    IReadOnlyList<DeadLetterDto> Items,
    int Total,
    int Page,
    int PageSize);

internal sealed record DeadLetterDto(
    Guid Id,
    Guid TenantId,
    string MessageType,
    string FailureReason,
    string? LastException,
    int AttemptCount,
    DateTimeOffset ExhaustedAt,
    DateTimeOffset? ReprocessedAt,
    string Status);

internal sealed record DiscardRequest(string? Reason);
