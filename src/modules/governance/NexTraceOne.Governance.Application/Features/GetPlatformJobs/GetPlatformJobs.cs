using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetPlatformJobs;

/// <summary>
/// Feature: GetPlatformJobs — listagem dos background jobs conhecidos da plataforma.
/// Os jobs executam no processo BackgroundWorkers (separado do ApiHost).
/// O estado de execução em runtime é honestamente reportado como desconhecido;
/// os nomes e descrições são derivados do catálogo real de jobs da plataforma.
/// </summary>
public static class GetPlatformJobs
{
    /// <summary>Query com filtro opcional de estado e paginação.</summary>
    public sealed record Query(
        string? StatusFilter = null,
        int? Page = null,
        int? PageSize = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo real de background jobs via IPlatformJobStatusProvider.</summary>
    /// <summary>Valida os filtros opcionais da query de jobs de plataforma.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.StatusFilter).MaximumLength(100)
                .When(x => x.StatusFilter is not null);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1).LessThanOrEqualTo(500)
                .When(x => x.Page.HasValue);
        }
    }

    public sealed class Handler(IPlatformJobStatusProvider jobProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshots = await jobProvider.GetJobSnapshotsAsync(cancellationToken);

            var allJobs = snapshots
                .Select(s => new BackgroundJobSummaryDto(
                    JobId: s.JobId,
                    Name: s.Name,
                    Status: BackgroundJobStatus.Stale,
                    LastRunAt: null,
                    NextRunAt: null,
                    ExecutionCount: 0,
                    FailureCount: 0,
                    LastError: s.Description))
                .ToList();

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

            return Result<Response>.Success(new Response(
                Jobs: paged,
                TotalCount: items.Count,
                Page: page,
                PageSize: pageSize));
        }
    }

    /// <summary>Resposta paginada de background jobs da plataforma.</summary>
    public sealed record Response(
        IReadOnlyList<BackgroundJobSummaryDto> Jobs,
        int TotalCount,
        int Page,
        int PageSize);

    /// <summary>
    /// Resumo de um background job.
    /// LastRunAt é null quando o estado de execução não está disponível
    /// (BackgroundWorkers é processo separado do ApiHost).
    /// </summary>
    public sealed record BackgroundJobSummaryDto(
        string JobId,
        string Name,
        BackgroundJobStatus Status,
        DateTimeOffset? LastRunAt,
        DateTimeOffset? NextRunAt,
        long ExecutionCount,
        long FailureCount,
        string? LastError);
}
