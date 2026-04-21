using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetProfilingAnalysis;

public static class GetProfilingAnalysis
{
    public sealed record Query(Guid SessionId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }

    public sealed class Handler(IProfilingSessionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var session = await repository.GetByIdAsync(
                ProfilingSessionId.From(request.SessionId), cancellationToken);

            if (session is null)
                return RuntimeIntelligenceErrors.ProfilingSessionNotFound(request.SessionId.ToString());

            return Result<Response>.Success(new Response(
                SessionId: session.Id.Value,
                ServiceName: session.ServiceName,
                Environment: session.Environment,
                FrameType: session.FrameType.ToString(),
                WindowStart: session.WindowStart,
                WindowEnd: session.WindowEnd,
                DurationSeconds: session.DurationSeconds,
                TotalCpuSamples: session.TotalCpuSamples,
                PeakMemoryMb: session.PeakMemoryMb,
                PeakThreadCount: session.PeakThreadCount,
                HasAnomalies: session.HasAnomalies,
                TopFramesJson: session.TopFramesJson,
                RawDataUri: session.RawDataUri,
                RawDataHash: session.RawDataHash,
                ReleaseVersion: session.ReleaseVersion,
                CommitSha: session.CommitSha));
        }
    }

    public sealed record Response(
        Guid SessionId,
        string ServiceName,
        string Environment,
        string FrameType,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        int DurationSeconds,
        long TotalCpuSamples,
        decimal PeakMemoryMb,
        int PeakThreadCount,
        bool HasAnomalies,
        string? TopFramesJson,
        string? RawDataUri,
        string? RawDataHash,
        string? ReleaseVersion,
        string? CommitSha);
}
