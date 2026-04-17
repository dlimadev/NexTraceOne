using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetNonProdSchedules;

/// <summary>
/// Feature: GetNonProdSchedules — agendas de ambientes não produtivos para otimização de custos.
/// Utiliza IEnvironmentBehaviorService para verificar chaves de comportamento.
/// Retorna agendas estáticas em memória com SimulatedNote.
/// </summary>
public static class GetNonProdSchedules
{
    /// <summary>Query sem parâmetros — retorna agendas de ambientes não produtivos.</summary>
    public sealed record Query() : IQuery<NonProdSchedulesResponse>;

    /// <summary>Comando para atualizar agenda de um ambiente não produtivo.</summary>
    public sealed record UpdateNonProdSchedule(
        string EnvironmentId,
        bool Enabled,
        IReadOnlyList<string> ActiveDaysOfWeek,
        int ActiveFromHour,
        int ActiveToHour,
        string Timezone) : ICommand<NonProdSchedulesResponse>;

    /// <summary>Comando para aplicar override temporário numa agenda.</summary>
    public sealed record OverrideNonProdSchedule(
        string EnvironmentId,
        DateTimeOffset KeepActiveUntil,
        string Reason) : ICommand<NonProdScheduleDto>;

    /// <summary>Handler de leitura de agendas de não-produção.</summary>
    public sealed class Handler(IEnvironmentBehaviorService behaviorService) : IQueryHandler<Query, NonProdSchedulesResponse>
    {
        public async Task<Result<NonProdSchedulesResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var nonProdSchedulerEnabled = await behaviorService.IsEnabledAsync(
                "env.behavior.jobs.non_prod_scheduler.enabled", null, cancellationToken);

            var schedules = new List<NonProdScheduleDto>
            {
                new(
                    EnvironmentId: "staging",
                    EnvironmentName: "Staging",
                    Enabled: true,
                    ActiveDaysOfWeek: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                    ActiveFromHour: 8,
                    ActiveToHour: 20,
                    Timezone: "UTC",
                    Override: null,
                    EstimatedSavingPct: 33),
                new(
                    EnvironmentId: "qa",
                    EnvironmentName: "QA",
                    Enabled: true,
                    ActiveDaysOfWeek: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                    ActiveFromHour: 9,
                    ActiveToHour: 18,
                    Timezone: "UTC",
                    Override: null,
                    EstimatedSavingPct: 40)
            };

            var response = new NonProdSchedulesResponse(
                Schedules: schedules,
                TotalEstimatedSavingPercent: 36,
                NonProdSchedulerEnabled: nonProdSchedulerEnabled,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "Non-prod schedule data is in-memory. Real environment scheduling integration pending.");

            return Result<NonProdSchedulesResponse>.Success(response);
        }
    }

    /// <summary>Handler de atualização de agenda.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateNonProdSchedule, NonProdSchedulesResponse>
    {
        public Task<Result<NonProdSchedulesResponse>> Handle(UpdateNonProdSchedule request, CancellationToken cancellationToken)
        {
            var updated = new NonProdScheduleDto(
                EnvironmentId: request.EnvironmentId,
                EnvironmentName: request.EnvironmentId,
                Enabled: request.Enabled,
                ActiveDaysOfWeek: request.ActiveDaysOfWeek,
                ActiveFromHour: request.ActiveFromHour,
                ActiveToHour: request.ActiveToHour,
                Timezone: request.Timezone,
                Override: null,
                EstimatedSavingPct: 0);

            var response = new NonProdSchedulesResponse(
                Schedules: [updated],
                TotalEstimatedSavingPercent: 0,
                NonProdSchedulerEnabled: true,
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<NonProdSchedulesResponse>.Success(response));
        }
    }

    /// <summary>Handler de override de agenda.</summary>
    public sealed class OverrideHandler : ICommandHandler<OverrideNonProdSchedule, NonProdScheduleDto>
    {
        public Task<Result<NonProdScheduleDto>> Handle(OverrideNonProdSchedule request, CancellationToken cancellationToken)
        {
            var overrideEntry = new ScheduleOverrideDto(
                KeepActiveUntil: request.KeepActiveUntil,
                Reason: request.Reason,
                AppliedAt: DateTimeOffset.UtcNow);

            var dto = new NonProdScheduleDto(
                EnvironmentId: request.EnvironmentId,
                EnvironmentName: request.EnvironmentId,
                Enabled: true,
                ActiveDaysOfWeek: [],
                ActiveFromHour: 0,
                ActiveToHour: 23,
                Timezone: "UTC",
                Override: overrideEntry,
                EstimatedSavingPct: 0);

            return Task.FromResult(Result<NonProdScheduleDto>.Success(dto));
        }
    }

    /// <summary>Resposta com agendas de ambientes não produtivos.</summary>
    public sealed record NonProdSchedulesResponse(
        IReadOnlyList<NonProdScheduleDto> Schedules,
        double TotalEstimatedSavingPercent,
        bool NonProdSchedulerEnabled,
        DateTimeOffset GeneratedAt,
        string SimulatedNote);

    /// <summary>Agenda de ambiente não produtivo.</summary>
    public sealed record NonProdScheduleDto(
        string EnvironmentId,
        string EnvironmentName,
        bool Enabled,
        IReadOnlyList<string> ActiveDaysOfWeek,
        int ActiveFromHour,
        int ActiveToHour,
        string Timezone,
        ScheduleOverrideDto? Override,
        double EstimatedSavingPct);

    /// <summary>Override temporário de agenda.</summary>
    public sealed record ScheduleOverrideDto(
        DateTimeOffset KeepActiveUntil,
        string Reason,
        DateTimeOffset AppliedAt);
}
