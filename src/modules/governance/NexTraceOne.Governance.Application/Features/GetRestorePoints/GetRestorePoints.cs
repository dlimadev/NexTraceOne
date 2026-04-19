using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Application.Features.GetRestorePoints;

/// <summary>
/// Feature: GetRestorePoints — listagem de pontos de restauro e iniciação de recovery.
/// Delega para IBackupProvider. Quando nenhum sistema de backup está configurado,
/// IBackupProvider.IsConfigured é false e a lista retornada é vazia com SimulatedNote.
/// Histórico de jobs de recovery é persistido em base de dados via IRecoveryJobRepository.
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
        bool DryRun,
        string? InitiatedBy = null) : ICommand<RecoveryInitiationResult>;

    /// <summary>Handler de listagem de pontos de restauro via IBackupProvider.</summary>
    public sealed class Handler(IBackupProvider backupProvider) : IQueryHandler<Query, RestorePointsResponse>
    {
        public async Task<Result<RestorePointsResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var points = await backupProvider.ListRestorePointsAsync(cancellationToken);

            var dtos = points.Select(p => new RestorePointDto(
                Id: p.Id,
                CreatedAt: p.CreatedAt,
                SizeMb: p.SizeMb,
                Status: p.Status,
                StorageProvider: p.StorageProvider)).ToList();

            var simulatedNote = backupProvider.IsConfigured
                ? string.Empty
                : "No backup provider configured. Restore points will be available once a backup system (pg_dump, pgBackRest, Barman) is set up.";

            var response = new RestorePointsResponse(
                RestorePoints: dtos,
                Total: dtos.Count,
                Oldest: dtos.Count > 0 ? dtos.Min(p => p.CreatedAt) : null,
                Latest: dtos.Count > 0 ? dtos.Max(p => p.CreatedAt) : null,
                SimulatedNote: simulatedNote);

            return Result<RestorePointsResponse>.Success(response);
        }
    }

    /// <summary>Handler de iniciação de recovery — persiste job auditável em base de dados.</summary>
    public sealed class InitiateHandler(
        IBackupProvider backupProvider,
        IRecoveryJobRepository jobRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<InitiateRecovery, RecoveryInitiationResult>
    {
        public async Task<Result<RecoveryInitiationResult>> Handle(InitiateRecovery request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            // When the provider is configured, verify the restore point exists before creating a job.
            if (backupProvider.IsConfigured)
            {
                var point = await backupProvider.GetRestorePointAsync(request.RestorePointId, cancellationToken);
                if (point is null)
                    return Error.NotFound("RecoveryJob.RestorePointNotFound",
                        $"Restore point '{request.RestorePointId}' was not found.");
            }

            var schemasJson = request.Schemas is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(request.Schemas)
                : null;

            var job = RecoveryJob.Create(
                restorePointId: request.RestorePointId,
                scope: request.Scope,
                schemasJson: schemasJson,
                dryRun: request.DryRun,
                initiatedBy: request.InitiatedBy,
                now: now);

            await jobRepository.AddAsync(job, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var result = new RecoveryInitiationResult(
                JobId: job.Id.Value.ToString(),
                DryRun: request.DryRun,
                Status: job.Status,
                Message: job.Message);

            return Result<RecoveryInitiationResult>.Success(result);
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
