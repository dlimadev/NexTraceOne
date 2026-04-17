using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetRestorePoints;

/// <summary>
/// Feature: GetRestorePoints — listagem de pontos de restauro e iniciação de recovery.
/// Integração real com sistema de backup é pendente. Retorna lista vazia com SimulatedNote.
/// </summary>
public static class GetRestorePoints
{
    /// <summary>Query sem parâmetros — retorna pontos de restauro disponíveis.</summary>
    public sealed record Query() : IQuery<RestorePointsResponse>;

    /// <summary>Comando para iniciar processo de recovery.</summary>
    public sealed record InitiateRecovery(
        string RestorePointId,
        string Scope,
        IReadOnlyList<string>? Schemas,
        bool DryRun) : ICommand<RecoveryInitiationResult>;

    /// <summary>Handler de listagem de pontos de restauro.</summary>
    public sealed class Handler : IQueryHandler<Query, RestorePointsResponse>
    {
        public Task<Result<RestorePointsResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new RestorePointsResponse(
                RestorePoints: [],
                Total: 0,
                Oldest: null,
                Latest: null,
                SimulatedNote: "Real backup integration pending. Restore points will be populated once a backup provider is configured.");

            return Task.FromResult(Result<RestorePointsResponse>.Success(response));
        }
    }

    /// <summary>Handler de iniciação de recovery.</summary>
    public sealed class InitiateHandler : ICommandHandler<InitiateRecovery, RecoveryInitiationResult>
    {
        public Task<Result<RecoveryInitiationResult>> Handle(InitiateRecovery request, CancellationToken cancellationToken)
        {
            var result = new RecoveryInitiationResult(
                JobId: Guid.NewGuid().ToString(),
                DryRun: request.DryRun,
                Status: request.DryRun ? "DryRunCompleted" : "Initiated",
                Message: request.DryRun
                    ? "Dry run completed. No changes applied."
                    : "Recovery job initiated. Monitor progress via platform jobs.");

            return Task.FromResult(Result<RecoveryInitiationResult>.Success(result));
        }
    }

    /// <summary>Resposta com pontos de restauro.</summary>
    public sealed record RestorePointsResponse(
        IReadOnlyList<RestorePointDto> RestorePoints,
        int Total,
        DateTimeOffset? Oldest,
        DateTimeOffset? Latest,
        string SimulatedNote);

    /// <summary>Ponto de restauro disponível.</summary>
    public sealed record RestorePointDto(
        string Id,
        DateTimeOffset CreatedAt,
        long SizeMb,
        string Status,
        string StorageProvider);

    /// <summary>Resultado da iniciação de recovery.</summary>
    public sealed record RecoveryInitiationResult(
        string JobId,
        bool DryRun,
        string Status,
        string Message);
}
