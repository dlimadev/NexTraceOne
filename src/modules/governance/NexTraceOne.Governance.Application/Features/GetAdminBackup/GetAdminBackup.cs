using MediatR;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetAdminBackup;

/// <summary>
/// Feature: GetAdminBackup — coordenação de backups da plataforma.
/// Lê de IConfiguration "Platform:Backup:*".
/// </summary>
public static class GetAdminBackup
{
    /// <summary>Query sem parâmetros — retorna estado atual do coordenador de backups.</summary>
    public sealed record Query() : IQuery<BackupCoordinatorResponse>;

    /// <summary>Comando para atualizar a agenda de backups.</summary>
    public sealed record UpdateBackupSchedule(
        string Frequency,
        int RetentionDays,
        bool Enabled) : ICommand<BackupCoordinatorResponse>;

    /// <summary>Comando para executar backup imediato.</summary>
    public sealed record RunBackupNow() : ICommand<BackupRecord>;

    /// <summary>Handler de leitura do estado de backup.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, BackupCoordinatorResponse>
    {
        public Task<Result<BackupCoordinatorResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var frequency = configuration["Platform:Backup:Frequency"] ?? "Daily";
            var retentionDays = int.TryParse(configuration["Platform:Backup:RetentionDays"], out var rd) ? rd : 30;
            var enabled = !bool.TryParse(configuration["Platform:Backup:Enabled"], out var en) || en;

            var config = new BackupScheduleConfig(
                Frequency: frequency,
                RetentionDays: retentionDays,
                Enabled: enabled);

            var response = new BackupCoordinatorResponse(
                Config: config,
                LastBackup: null,
                NextScheduled: enabled ? DateTimeOffset.UtcNow.AddHours(24) : null,
                History: []);

            return Task.FromResult(Result<BackupCoordinatorResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização de agenda de backup.</summary>
    public sealed class UpdateScheduleHandler : ICommandHandler<UpdateBackupSchedule, BackupCoordinatorResponse>
    {
        public Task<Result<BackupCoordinatorResponse>> Handle(UpdateBackupSchedule request, CancellationToken cancellationToken)
        {
            var config = new BackupScheduleConfig(
                Frequency: request.Frequency,
                RetentionDays: request.RetentionDays,
                Enabled: request.Enabled);

            var response = new BackupCoordinatorResponse(
                Config: config,
                LastBackup: null,
                NextScheduled: request.Enabled ? DateTimeOffset.UtcNow.AddHours(24) : null,
                History: []);

            return Task.FromResult(Result<BackupCoordinatorResponse>.Success(response));
        }
    }

    /// <summary>Handler de execução imediata de backup.</summary>
    public sealed class RunNowHandler : ICommandHandler<RunBackupNow, BackupRecord>
    {
        public Task<Result<BackupRecord>> Handle(RunBackupNow request, CancellationToken cancellationToken)
        {
            var record = new BackupRecord(
                Id: Guid.NewGuid().ToString(),
                StartedAt: DateTimeOffset.UtcNow,
                DurationMs: 0,
                SizeMb: 0,
                Status: "Running",
                StorageProvider: "local");

            return Task.FromResult(Result<BackupRecord>.Success(record));
        }
    }

    /// <summary>Resposta do coordenador de backups.</summary>
    public sealed record BackupCoordinatorResponse(
        BackupScheduleConfig Config,
        BackupRecord? LastBackup,
        DateTimeOffset? NextScheduled,
        IReadOnlyList<BackupRecord> History);

    /// <summary>Configuração da agenda de backups.</summary>
    public sealed record BackupScheduleConfig(string Frequency, int RetentionDays, bool Enabled);

    /// <summary>Registo de execução de backup.</summary>
    public sealed record BackupRecord(
        string Id,
        DateTimeOffset StartedAt,
        long DurationMs,
        double SizeMb,
        string Status,
        string StorageProvider);
}
