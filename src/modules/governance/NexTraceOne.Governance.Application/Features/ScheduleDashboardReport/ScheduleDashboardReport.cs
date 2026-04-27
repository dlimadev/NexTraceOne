using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ScheduleDashboardReport;

/// <summary>
/// Feature: ScheduleDashboardReport — cria ou actualiza um agendamento de relatório para um dashboard.
/// V3.6 — Governance, Reports &amp; Embedding.
/// </summary>
public static class ScheduleDashboardReport
{
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId,
        string CronExpression,
        string Format,
        IReadOnlyList<string> Recipients,
        int RetentionDays,
        string? WebhookUrl = null) : ICommand<Response>;

    public sealed record Response(
        Guid ReportId,
        Guid DashboardId,
        string CronExpression,
        string Format,
        int RetentionDays,
        bool IsActive);

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> ValidFormats = ["pdf", "png"];

        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.CronExpression).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Format).Must(f => ValidFormats.Contains(f.ToLowerInvariant()))
                .WithMessage("Format must be 'pdf' or 'png'.");
            RuleFor(x => x.RetentionDays).InclusiveBetween(1, 3650);
            RuleFor(x => x.Recipients).NotNull();
            RuleForEach(x => x.Recipients).EmailAddress();
        }
    }

    public sealed class Handler(
        ICustomDashboardRepository dashboardRepository,
        IScheduledDashboardReportRepository reportRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await dashboardRepository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "Dashboard.NotFound",
                    "Dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            var recipientsJson = System.Text.Json.JsonSerializer.Serialize(request.Recipients);

            var report = ScheduledDashboardReport.Create(
                dashboardId: request.DashboardId,
                tenantId: request.TenantId,
                userId: request.UserId,
                cronExpression: request.CronExpression,
                format: request.Format,
                recipientsJson: recipientsJson,
                retentionDays: request.RetentionDays,
                now: clock.UtcNow,
                webhookUrl: request.WebhookUrl);

            await reportRepository.AddAsync(report, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                report.Id.Value,
                report.DashboardId,
                report.CronExpression,
                report.Format,
                report.RetentionDays,
                report.IsActive));
        }
    }
}
