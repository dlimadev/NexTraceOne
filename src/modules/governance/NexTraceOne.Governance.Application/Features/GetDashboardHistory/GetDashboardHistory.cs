using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetDashboardHistory;

/// <summary>
/// Feature: GetDashboardHistory — lista o histórico de revisões de um dashboard.
/// Retorna revisões ordenadas da mais recente para a mais antiga.
/// V3.1 — Dashboard Intelligence Foundation.
/// </summary>
public static class GetDashboardHistory
{
    /// <summary>Query para obter o histórico de revisões de um dashboard.</summary>
    public sealed record Query(
        Guid DashboardId,
        string TenantId,
        int MaxResults = 20) : IQuery<Response>;

    /// <summary>DTO de revisão para o histórico.</summary>
    public sealed record RevisionDto(
        int RevisionNumber,
        string Name,
        string? Description,
        string Layout,
        string AuthorUserId,
        string? ChangeNote,
        DateTimeOffset CreatedAt,
        int WidgetCount);

    /// <summary>Resposta com lista de revisões e metadados.</summary>
    public sealed record Response(
        Guid DashboardId,
        int TotalRevisions,
        IReadOnlyList<RevisionDto> Revisions);

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 100)
                .WithMessage("MaxResults must be between 1 and 100.");
        }
    }

    /// <summary>Handler que retorna o histórico de revisões do dashboard.</summary>
    public sealed class Handler(
        ICustomDashboardRepository dashboardRepository,
        IDashboardRevisionRepository revisionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dashboard = await dashboardRepository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "CustomDashboard.NotFound",
                    "Custom dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden(
                    "CustomDashboard.Forbidden",
                    "Access to dashboard '{0}' is not allowed.",
                    request.DashboardId);

            var totalRevisions = await revisionRepository.CountByDashboardIdAsync(
                dashboard.Id, cancellationToken);

            var revisions = await revisionRepository.ListByDashboardIdAsync(
                dashboard.Id, request.MaxResults, cancellationToken);

            var dtos = revisions.Select(r =>
            {
                // Conta widgets a partir do JSON sem deserializar o objeto completo
                var widgetCount = CountJsonArrayItems(r.WidgetsJson);
                return new RevisionDto(
                    r.RevisionNumber,
                    r.Name,
                    r.Description,
                    r.Layout,
                    r.AuthorUserId,
                    r.ChangeNote,
                    r.CreatedAt,
                    widgetCount);
            }).ToList();

            return Result<Response>.Success(new Response(
                request.DashboardId,
                totalRevisions,
                dtos));
        }

        private static int CountJsonArrayItems(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return 0;
            var count = 0;
            var depth = 0;
            var inString = false;
            var escaped = false;
            foreach (var c in json)
            {
                if (escaped) { escaped = false; continue; }
                if (c == '\\' && inString) { escaped = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (c == '{') { if (depth == 1) count++; depth++; }
                else if (c == '}') depth--;
                else if (c == '[') depth++;
                else if (c == ']') depth--;
            }
            return count;
        }
    }
}
