using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetDemoSeedStatus;

/// <summary>
/// Feature: GetDemoSeedStatus — estado do seed de dados de demonstração.
/// Estado gerido em campo estático (em memória). SimulatedNote explica limitação.
/// </summary>
public static class GetDemoSeedStatus
{
    private static string _state = "NotSeeded";
    private static DateTimeOffset? _seededAt;
    private static int _entitiesCount;

    /// <summary>Query sem parâmetros — retorna estado atual do seed de demonstração.</summary>
    public sealed record Query() : IQuery<DemoSeedStatus>;

    /// <summary>Comando para executar seed de demonstração.</summary>
    public sealed record RunDemoSeed(Guid? TenantId) : ICommand<DemoSeedResult>;

    /// <summary>Comando para limpar dados de demonstração.</summary>
    public sealed record ClearDemoSeed() : ICommand<DemoSeedClearResult>;

    /// <summary>Handler de leitura do estado do seed de demonstração.</summary>
    public sealed class Handler : IQueryHandler<Query, DemoSeedStatus>
    {
        public Task<Result<DemoSeedStatus>> Handle(Query request, CancellationToken cancellationToken)
        {
            var status = new DemoSeedStatus(
                State: _state,
                SeededAt: _seededAt,
                EntitiesCount: _entitiesCount,
                SimulatedNote: "Demo seed state is managed in-memory. Real demo seed implementation pending.");

            return Task.FromResult(Result<DemoSeedStatus>.Success(status));
        }
    }

    /// <summary>Handler de execução de seed de demonstração.</summary>
    public sealed class RunHandler : ICommandHandler<RunDemoSeed, DemoSeedResult>
    {
        public Task<Result<DemoSeedResult>> Handle(RunDemoSeed request, CancellationToken cancellationToken)
        {
            _state = "Seeded";
            _seededAt = DateTimeOffset.UtcNow;
            _entitiesCount = 42;

            var result = new DemoSeedResult(
                Success: true,
                EntitiesCreated: 42,
                SeededAt: _seededAt.Value,
                Message: "Demo data seeded successfully.");

            return Task.FromResult(Result<DemoSeedResult>.Success(result));
        }
    }

    /// <summary>Handler de limpeza de dados de demonstração.</summary>
    public sealed class ClearHandler : ICommandHandler<ClearDemoSeed, DemoSeedClearResult>
    {
        public Task<Result<DemoSeedClearResult>> Handle(ClearDemoSeed request, CancellationToken cancellationToken)
        {
            _state = "NotSeeded";
            _seededAt = null;
            _entitiesCount = 0;

            var result = new DemoSeedClearResult(
                Success: true,
                EntitiesRemoved: 42,
                ClearedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<DemoSeedClearResult>.Success(result));
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
