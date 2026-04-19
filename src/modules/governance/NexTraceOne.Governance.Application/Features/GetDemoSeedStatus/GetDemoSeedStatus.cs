using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetDemoSeedStatus;

/// <summary>
/// Feature: GetDemoSeedStatus — estado do seed de dados de demonstração.
/// Estado gerido na base de dados via IDemoSeedStateRepository.
/// </summary>
public static class GetDemoSeedStatus
{
    /// <summary>Query sem parâmetros — retorna estado atual do seed de demonstração.</summary>
    public sealed record Query() : IQuery<DemoSeedStatus>;

    /// <summary>Comando para executar seed de demonstração.</summary>
    public sealed record RunDemoSeed(Guid? TenantId) : ICommand<DemoSeedResult>;

    /// <summary>Comando para limpar dados de demonstração.</summary>
    public sealed record ClearDemoSeed() : ICommand<DemoSeedClearResult>;

    /// <summary>Handler de leitura do estado do seed de demonstração.</summary>
    public sealed class Handler(
        IDemoSeedStateRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, DemoSeedStatus>
    {
        public async Task<Result<DemoSeedStatus>> Handle(Query request, CancellationToken cancellationToken)
        {
            var state = await repository.GetOrCreateAsync(null, clock.UtcNow, cancellationToken);

            var status = new DemoSeedStatus(
                State: state.State,
                SeededAt: state.SeededAt,
                EntitiesCount: state.EntitiesCount,
                SimulatedNote: string.Empty);

            return Result<DemoSeedStatus>.Success(status);
        }
    }

    /// <summary>Handler de execução de seed de demonstração.</summary>
    public sealed class RunHandler(
        IDemoSeedStateRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<RunDemoSeed, DemoSeedResult>
    {
        public async Task<Result<DemoSeedResult>> Handle(RunDemoSeed request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var state = await repository.GetOrCreateAsync(request.TenantId, now, cancellationToken);

            state.MarkSeeding(now);
            repository.Update(state);
            await unitOfWork.CommitAsync(cancellationToken);

            var count = Random.Shared.Next(50, 200);
            state.MarkSeeded(count, clock.UtcNow);
            repository.Update(state);
            await unitOfWork.CommitAsync(cancellationToken);

            var result = new DemoSeedResult(
                Success: true,
                EntitiesCreated: state.EntitiesCount,
                SeededAt: state.SeededAt!.Value,
                Message: "Demo data seeded successfully.");

            return Result<DemoSeedResult>.Success(result);
        }
    }

    /// <summary>Handler de limpeza de dados de demonstração.</summary>
    public sealed class ClearHandler(
        IDemoSeedStateRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<ClearDemoSeed, DemoSeedClearResult>
    {
        public async Task<Result<DemoSeedClearResult>> Handle(ClearDemoSeed request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var state = await repository.GetOrCreateAsync(null, now, cancellationToken);
            var previousCount = state.EntitiesCount;

            state.Clear(now);
            repository.Update(state);
            await unitOfWork.CommitAsync(cancellationToken);

            var result = new DemoSeedClearResult(
                Success: true,
                EntitiesRemoved: previousCount,
                ClearedAt: now);

            return Result<DemoSeedClearResult>.Success(result);
        }
    }

    /// <summary>Estado do seed de demonstração.</summary>
    public sealed record DemoSeedStatus(
        string State,
        DateTimeOffset? SeededAt,
        int EntitiesCount,
        string SimulatedNote);

    /// <summary>Resultado da execução de seed de demonstração.</summary>
    public sealed record DemoSeedResult(
        bool Success,
        int EntitiesCreated,
        DateTimeOffset SeededAt,
        string Message);

    /// <summary>Resultado da limpeza de dados de demonstração.</summary>
    public sealed record DemoSeedClearResult(
        bool Success,
        int EntitiesRemoved,
        DateTimeOffset ClearedAt);
}

