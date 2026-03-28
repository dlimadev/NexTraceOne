using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformJobs;

/// <summary>
/// Feature: GetPlatformJobs — listagem e monitorização de background jobs da plataforma.
/// Permite filtro por estado e paginação para operadores e administradores da plataforma.
/// </summary>
public static class GetPlatformJobs
{
    /// <summary>Query com filtro opcional de estado e paginação.</summary>
    public sealed record Query(
        string? StatusFilter = null,
        int? Page = null,
        int? PageSize = null) : IQuery<Response>;

    /// <summary>Handler que retorna lista paginada de background jobs com métricas de execução.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // TODO [P03.5]: Replace static job snapshots with real scheduler/job monitor integration
            // when a platform job status contract is exposed for Governance.
            var now = DateTimeOffset.UtcNow;

            var allJobs = new List<BackgroundJobSummaryDto>
            {
                new(
                    JobId: "job-outbox-processor",
                    Name: "Outbox Processor",
                    Status: BackgroundJobStatus.Running,
                    LastRunAt: now.AddMinutes(-2),
                    NextRunAt: now.AddSeconds(30),
                    ExecutionCount: 14832,
                    FailureCount: 3,
                    LastError: null),
                new(
                    JobId: "job-identity-expiration",
                    Name: "Identity Expiration",
                    Status: BackgroundJobStatus.Completed,
                    LastRunAt: now.AddMinutes(-15),
                    NextRunAt: now.AddMinutes(45),
                    ExecutionCount: 720,
                    FailureCount: 0,
                    LastError: null),
                new(
                    JobId: "job-analytics-aggregation",
                    Name: "Analytics Aggregation",
                    Status: BackgroundJobStatus.Completed,
                    LastRunAt: now.AddMinutes(-5),
                    NextRunAt: now.AddMinutes(55),
                    ExecutionCount: 2160,
                    FailureCount: 7,
                    LastError: null),
                new(
                    JobId: "job-ingestion-pipeline",
                    Name: "Ingestion Pipeline",
                    Status: BackgroundJobStatus.Running,
                    LastRunAt: now.AddMinutes(-1),
                    NextRunAt: now.AddSeconds(15),
                    ExecutionCount: 43210,
                    FailureCount: 12,
                    LastError: null)
            };

            // Aplicar filtro de estado se especificado
            var filtered = allJobs.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.StatusFilter)
                && Enum.TryParse<BackgroundJobStatus>(request.StatusFilter, ignoreCase: true, out var statusFilter))
            {
                filtered = filtered.Where(j => j.Status == statusFilter);
            }

            var page = Math.Max(1, request.Page ?? 1);
            var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
            var items = filtered.ToList();
            var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var response = new Response(
                Jobs: paged,
                TotalCount: items.Count,
                Page: page,
                PageSize: pageSize);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta paginada de background jobs da plataforma.</summary>
    public sealed record Response(
        IReadOnlyList<BackgroundJobSummaryDto> Jobs,
        int TotalCount,
        int Page,
        int PageSize);

    /// <summary>Resumo de execução de um background job.</summary>
    public sealed record BackgroundJobSummaryDto(
        string JobId,
        string Name,
        BackgroundJobStatus Status,
        DateTimeOffset LastRunAt,
        DateTimeOffset? NextRunAt,
        long ExecutionCount,
        long FailureCount,
        string? LastError);
}
