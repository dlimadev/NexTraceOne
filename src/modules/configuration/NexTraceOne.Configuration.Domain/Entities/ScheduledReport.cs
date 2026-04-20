using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Relatório programado por utilizador ou admin.
/// Executado via Quartz.NET com base no schedule configurado.
/// Formatos suportados: PDF, CSV, JSON.
/// </summary>
public sealed class ScheduledReport : AuditableEntity<ScheduledReportId>
{
    private const int MaxNameLength = 100;
    private static readonly string[] ValidFormats = ["pdf", "csv", "json"];
    private static readonly string[] ValidSchedules = ["daily", "weekly", "monthly"];

    private ScheduledReport() { }

    public string TenantId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ReportType { get; private set; } = string.Empty;
    public string FiltersJson { get; private set; } = "{}";
    public string Schedule { get; private set; } = "weekly";
    public string RecipientsJson { get; private set; } = "[]";
    public string Format { get; private set; } = "pdf";
    public bool IsEnabled { get; private set; } = true;
    public DateTimeOffset? LastSentAt { get; private set; }

    public static ScheduledReport Create(
        string tenantId,
        string userId,
        string name,
        string reportType,
        string filtersJson,
        string schedule,
        string recipientsJson,
        string format,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(reportType);

        var normalizedSchedule = ValidSchedules.Contains(schedule, StringComparer.OrdinalIgnoreCase)
            ? schedule.ToLowerInvariant() : "weekly";
        var normalizedFormat = ValidFormats.Contains(format, StringComparer.OrdinalIgnoreCase)
            ? format.ToLowerInvariant() : "pdf";

        var report = new ScheduledReport
        {
            Id = new ScheduledReportId(Guid.NewGuid()),
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            ReportType = reportType,
            FiltersJson = string.IsNullOrWhiteSpace(filtersJson) ? "{}" : filtersJson,
            Schedule = normalizedSchedule,
            RecipientsJson = string.IsNullOrWhiteSpace(recipientsJson) ? "[]" : recipientsJson,
            Format = normalizedFormat,
            IsEnabled = true,
        };
        report.SetCreated(createdAt, string.Empty);
        report.SetUpdated(createdAt, string.Empty);
        return report;
    }

    public void MarkSent(DateTimeOffset sentAt) { LastSentAt = sentAt; SetUpdated(sentAt, string.Empty); }
    public void Toggle(bool enabled, DateTimeOffset updatedAt) { IsEnabled = enabled; SetUpdated(updatedAt, string.Empty); }
}
