using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestProfilingSession;

/// <summary>
/// Feature: IngestProfilingSession — ingesta uma sessão de profiling contínuo de um serviço.
///
/// Suporta dotnet-trace (nettrace), pprof (Go/Java/Rust), async-profiler (JVM)
/// e formatos genéricos de stack samples.
///
/// Comportamento:
/// - Cria uma nova ProfilingSession com a janela temporal e métricas fornecidas
/// - Anexa top frames em JSON quando fornecidos
/// - Regista referência para raw data externo (URI + hash)
/// - Associa a sessão a uma release quando fornecida
/// - Marca anomalias automaticamente se HasAnomalies = true no comando
///
/// Wave D backlog — Continuous Profiling ingest contextualizado por serviço.
/// </summary>
public static class IngestProfilingSession
{
    public sealed record Command(
        string TenantId,
        string ServiceName,
        string Environment,
        ProfilingFrameType FrameType,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        long TotalCpuSamples,
        decimal PeakMemoryMb,
        int PeakThreadCount,
        string? TopFramesJson,
        string? RawDataUri,
        string? RawDataHash,
        string? ReleaseVersion,
        string? CommitSha,
        bool HasAnomalies) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.WindowStart).NotEmpty();
            RuleFor(x => x.WindowEnd).GreaterThan(x => x.WindowStart)
                .WithMessage("WindowEnd must be after WindowStart.");
            RuleFor(x => x.TotalCpuSamples).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PeakMemoryMb).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PeakThreadCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TopFramesJson).MaximumLength(50000).When(x => x.TopFramesJson is not null);
            RuleFor(x => x.RawDataUri).MaximumLength(2000).When(x => x.RawDataUri is not null);
            RuleFor(x => x.RawDataHash).MaximumLength(128).When(x => x.RawDataHash is not null);
            RuleFor(x => x.ReleaseVersion).MaximumLength(50).When(x => x.ReleaseVersion is not null);
            RuleFor(x => x.CommitSha).MaximumLength(100).When(x => x.CommitSha is not null);
        }
    }

    public sealed class Handler(
        IProfilingSessionRepository repository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var session = ProfilingSession.Start(
                request.TenantId,
                request.ServiceName,
                request.Environment,
                request.FrameType,
                request.WindowStart,
                request.WindowEnd,
                request.TotalCpuSamples,
                request.PeakMemoryMb,
                request.PeakThreadCount,
                dateTimeProvider.UtcNow);

            if (request.TopFramesJson is not null)
                session.AttachTopFrames(request.TopFramesJson);

            if (request.RawDataUri is not null)
                session.AttachRawDataReference(request.RawDataUri, request.RawDataHash);

            if (request.ReleaseVersion is not null)
                session.LinkToRelease(request.ReleaseVersion, request.CommitSha);

            if (request.HasAnomalies)
                session.MarkAsHavingAnomalies();

            repository.Add(session);
            await unitOfWork.CommitAsync(cancellationToken);

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
                HasAnomalies: session.HasAnomalies,
                HasTopFrames: session.TopFramesJson is not null,
                HasRawData: session.RawDataUri is not null));
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
        bool HasAnomalies,
        bool HasTopFrames,
        bool HasRawData);
}
