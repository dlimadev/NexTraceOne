using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetProfilingHistory;

public static class GetProfilingHistory
{
    public sealed record Query(
        string ServiceName,
        string Environment,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IProfilingSessionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sessions = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = sessions.Select(s => new SessionSummary(
                s.Id.Value,
                s.ServiceName,
                s.Environment,
                s.FrameType.ToString(),
                s.WindowStart,
                s.WindowEnd,
                s.DurationSeconds,
                s.TotalCpuSamples,
                s.PeakMemoryMb,
                s.HasAnomalies,
                s.ReleaseVersion,
                s.CommitSha)).ToList();

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: items));
        }
    }

    public sealed record SessionSummary(
        Guid SessionId,
        string ServiceName,
        string Environment,
        string FrameType,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        int DurationSeconds,
        long TotalCpuSamples,
        decimal PeakMemoryMb,
        bool HasAnomalies,
        string? ReleaseVersion,
        string? CommitSha);

    public sealed record Response(
        string ServiceName,
        string Environment,
        int Page,
        int PageSize,
        IReadOnlyList<SessionSummary> Items);
}
