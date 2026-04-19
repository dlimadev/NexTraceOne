using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetNonProdSchedules;

/// <summary>
/// Feature: GetNonProdSchedules — agendas de ambientes não produtivos para otimização de custos.
/// Utiliza IEnvironmentBehaviorService para verificar chaves de comportamento.
/// Lê agendas persistidas na base de dados; semeia defaults quando não há registos.
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
    public sealed class Handler(
        IEnvironmentBehaviorService behaviorService,
        INonProdScheduleRepository scheduleRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : IQueryHandler<Query, NonProdSchedulesResponse>
    {
        public async Task<Result<NonProdSchedulesResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var nonProdSchedulerEnabled = await behaviorService.IsEnabledAsync(
                "env.behavior.jobs.non_prod_scheduler.enabled", null, cancellationToken);

            var dbSchedules = await scheduleRepository.ListAllAsync(cancellationToken);

            if (dbSchedules.Count == 0)
            {
                var now = clock.UtcNow;
                var staging = NonProdSchedule.Create(
                    environmentId: "staging",
                    environmentName: "Staging",
                    enabled: true,
                    activeDaysOfWeek: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                    activeFromHour: 8,
                    activeToHour: 20,
                    timezone: "UTC",
                    estimatedSavingPct: 33,
                    now: now);

                var qa = NonProdSchedule.Create(
                    environmentId: "qa",
                    environmentName: "QA",
                    enabled: true,
                    activeDaysOfWeek: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                    activeFromHour: 9,
                    activeToHour: 18,
                    timezone: "UTC",
                    estimatedSavingPct: 40,
                    now: now);

                await scheduleRepository.AddAsync(staging, cancellationToken);
                await scheduleRepository.AddAsync(qa, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                dbSchedules = await scheduleRepository.ListAllAsync(cancellationToken);
            }

            var schedules = dbSchedules.Select(s =>
            {
                var days = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(s.ActiveDaysOfWeekJson) ?? [];
                ScheduleOverrideDto? overrideDto = s.KeepActiveUntil.HasValue
                    ? new ScheduleOverrideDto(s.KeepActiveUntil.Value, s.OverrideReason ?? string.Empty, s.UpdatedAt)
                    : null;

                return new NonProdScheduleDto(
                    EnvironmentId: s.EnvironmentId,
                    EnvironmentName: s.EnvironmentName,
                    Enabled: s.Enabled,
                    ActiveDaysOfWeek: days,
                    ActiveFromHour: s.ActiveFromHour,
                    ActiveToHour: s.ActiveToHour,
                    Timezone: s.Timezone,
                    Override: overrideDto,
                    EstimatedSavingPct: s.EstimatedSavingPct);
            }).ToList();

            var totalSaving = schedules.Count > 0
                ? schedules.Average(s => s.EstimatedSavingPct)
                : 0;

            var response = new NonProdSchedulesResponse(
                Schedules: schedules,
                TotalEstimatedSavingPercent: Math.Round(totalSaving, 0),
                NonProdSchedulerEnabled: nonProdSchedulerEnabled,
                GeneratedAt: clock.UtcNow,
                SimulatedNote: string.Empty);

            return Result<NonProdSchedulesResponse>.Success(response);
        }
    }

    /// <summary>Handler de atualização de agenda.</summary>
    public sealed class UpdateHandler(
        INonProdScheduleRepository scheduleRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<UpdateNonProdSchedule, NonProdSchedulesResponse>
    {
        public async Task<Result<NonProdSchedulesResponse>> Handle(UpdateNonProdSchedule request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var existing = await scheduleRepository.GetByEnvironmentIdAsync(request.EnvironmentId, cancellationToken);

            if (existing is not null)
            {
                existing.Update(request.Enabled, request.ActiveDaysOfWeek, request.ActiveFromHour, request.ActiveToHour, request.Timezone, now);
                scheduleRepository.Update(existing);
            }
            else
            {
                var created = NonProdSchedule.Create(
                    environmentId: request.EnvironmentId,
                    environmentName: request.EnvironmentId,
                    enabled: request.Enabled,
                    activeDaysOfWeek: request.ActiveDaysOfWeek,
                    activeFromHour: request.ActiveFromHour,
                    activeToHour: request.ActiveToHour,
                    timezone: request.Timezone,
                    estimatedSavingPct: 0,
                    now: now);
                await scheduleRepository.AddAsync(created, cancellationToken);
                existing = created;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            var days = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(existing.ActiveDaysOfWeekJson) ?? [];
            var updated = new NonProdScheduleDto(
                EnvironmentId: existing.EnvironmentId,
                EnvironmentName: existing.EnvironmentName,
                Enabled: existing.Enabled,
                ActiveDaysOfWeek: days,
                ActiveFromHour: existing.ActiveFromHour,
                ActiveToHour: existing.ActiveToHour,
                Timezone: existing.Timezone,
                Override: null,
                EstimatedSavingPct: existing.EstimatedSavingPct);

            var response = new NonProdSchedulesResponse(
                Schedules: [updated],
                TotalEstimatedSavingPercent: existing.EstimatedSavingPct,
                NonProdSchedulerEnabled: true,
                GeneratedAt: now,
                SimulatedNote: string.Empty);

            return Result<NonProdSchedulesResponse>.Success(response);
        }
    }

    /// <summary>Handler de override de agenda.</summary>
    public sealed class OverrideHandler(
        INonProdScheduleRepository scheduleRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<OverrideNonProdSchedule, NonProdScheduleDto>
    {
        public async Task<Result<NonProdScheduleDto>> Handle(OverrideNonProdSchedule request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var schedule = await scheduleRepository.GetByEnvironmentIdAsync(request.EnvironmentId, cancellationToken);

            if (schedule is null)
            {
                schedule = NonProdSchedule.Create(
                    environmentId: request.EnvironmentId,
                    environmentName: request.EnvironmentId,
                    enabled: true,
                    activeDaysOfWeek: [],
                    activeFromHour: 0,
                    activeToHour: 23,
                    timezone: "UTC",
                    estimatedSavingPct: 0,
                    now: now);
                await scheduleRepository.AddAsync(schedule, cancellationToken);
            }

            schedule.ApplyOverride(request.KeepActiveUntil, request.Reason, now);
            scheduleRepository.Update(schedule);
            await unitOfWork.CommitAsync(cancellationToken);

            var days = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<string>>(schedule.ActiveDaysOfWeekJson) ?? [];
            var overrideEntry = new ScheduleOverrideDto(
                KeepActiveUntil: request.KeepActiveUntil,
                Reason: request.Reason,
                AppliedAt: now);

            var dto = new NonProdScheduleDto(
                EnvironmentId: schedule.EnvironmentId,
                EnvironmentName: schedule.EnvironmentName,
                Enabled: schedule.Enabled,
                ActiveDaysOfWeek: days,
                ActiveFromHour: schedule.ActiveFromHour,
                ActiveToHour: schedule.ActiveToHour,
                Timezone: schedule.Timezone,
                Override: overrideEntry,
                EstimatedSavingPct: schedule.EstimatedSavingPct);

            return Result<NonProdScheduleDto>.Success(dto);
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

