using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateScheduledReport;

/// <summary>Feature: CreateScheduledReport — cria um relatório programado para o utilizador.</summary>
public static class CreateScheduledReport
{
    private static readonly string[] ValidFormats = ["pdf", "csv", "json"];
    private static readonly string[] ValidSchedules = ["daily", "weekly", "monthly"];

    public sealed record Command(
        string Name,
        string ReportType,
        string FiltersJson,
        string Schedule,
        string RecipientsJson,
        string Format) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ReportType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Schedule).NotEmpty()
                .Must(s => ValidSchedules.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Schedule must be one of: {string.Join(", ", ValidSchedules)}");
            RuleFor(x => x.Format).NotEmpty()
                .Must(f => ValidFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Format must be one of: {string.Join(", ", ValidFormats)}");
        }
    }

    public sealed class Handler(
        IScheduledReportRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var report = ScheduledReport.Create(
                currentTenant.Id.ToString(),
                currentUser.Id,
                request.Name,
                request.ReportType,
                request.FiltersJson,
                request.Schedule,
                request.RecipientsJson,
                request.Format,
                clock.UtcNow);

            await repository.AddAsync(report, cancellationToken);

            return new Response(
                report.Id.Value,
                report.Name,
                report.ReportType,
                report.Schedule,
                report.Format,
                report.IsEnabled,
                report.CreatedAt);
        }
    }

    public sealed record Response(
        Guid ReportId,
        string Name,
        string ReportType,
        string Schedule,
        string Format,
        bool IsEnabled,
        DateTimeOffset CreatedAt);
}
