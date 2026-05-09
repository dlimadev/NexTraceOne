using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.IdentityAccess.Infrastructure.Jobs;

/// <summary>
/// SaaS-08: Avalia periodicamente UserAlertRules activas e dispara AlertFiringRecord
/// quando a condição de threshold é violada.
///
/// Executa a cada 60 segundos por defeito.
/// Lê UserAlertRule do ConfigurationDbContext via cross-module-safe SQL directo
/// (read-only, sem referência directa ao ConfigurationDbContext — usa string SQL via parâmetro).
/// </summary>
internal sealed class AlertEvaluationJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AlertEvaluationJob> logger) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _startDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AlertEvaluationJob started — interval {Interval}.", _interval);
        await Task.Delay(_startDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AlertEvaluationJob cycle failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("AlertEvaluationJob stopped.");
    }

    private async Task EvaluateCycleAsync(CancellationToken ct)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var firingRepo = scope.ServiceProvider.GetRequiredService<IAlertFiringRecordRepository>();
        var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRegistrationRepository>();
        var licenseRepo = scope.ServiceProvider.GetRequiredService<ITenantLicenseRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IIdentityAccessUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider>();

        var alertRules = await ReadActiveAlertRulesAsync(context, ct);

        foreach (var rule in alertRules)
        {
            try
            {
                await EvaluateRuleAsync(rule, firingRepo, agentRepo, licenseRepo, uow, clock, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error evaluating alert rule {RuleId}.", rule.RuleId);
            }
        }
    }

    private static async Task<List<AlertRuleDto>> ReadActiveAlertRulesAsync(IdentityDbContext context, CancellationToken ct)
    {
        // Cross-module read via raw SQL — avoids direct reference to Configuration module's DbContext.
        // Uses FormattableString to prevent SQL injection.
        const string sql = """
            SELECT id, tenant_id, name, condition_type, threshold_value, service_name, severity, notification_channels
            FROM cfg_user_alert_rules
            WHERE is_enabled = true
            LIMIT 500
            """;

        var rules = new List<AlertRuleDto>();
        try
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            await context.Database.OpenConnectionAsync(ct);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rules.Add(new AlertRuleDto(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? "Threshold" : reader.GetString(3),
                    reader.IsDBNull(4) ? 0d : reader.GetDouble(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? "Medium" : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7)));
            }
        }
        catch
        {
            // cfg_user_alert_rules table may not exist yet — graceful degradation
        }

        return rules;
    }

    private async Task EvaluateRuleAsync(
        AlertRuleDto rule,
        IAlertFiringRecordRepository firingRepo,
        IAgentRegistrationRepository agentRepo,
        ITenantLicenseRepository licenseRepo,
        IIdentityAccessUnitOfWork uow,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock,
        CancellationToken ct)
    {
        var alreadyFiring = await firingRepo.HasFiringAlertAsync(rule.TenantId, rule.RuleId, ct);
        if (alreadyFiring)
            return;

        var (shouldFire, message) = await EvaluateConditionAsync(rule, agentRepo, licenseRepo, clock, ct);
        if (!shouldFire)
            return;

        var record = AlertFiringRecord.Fire(
            rule.TenantId,
            rule.RuleId,
            rule.Name,
            rule.Severity,
            message,
            rule.ServiceName,
            rule.NotificationChannels,
            clock.UtcNow);

        firingRepo.Add(record);
        await uow.CommitAsync(ct);

        logger.LogInformation("Alert fired: {RuleName} for tenant {TenantId}. Reason: {Message}.",
            rule.Name, rule.TenantId, message);
    }

    private async Task<(bool ShouldFire, string Message)> EvaluateConditionAsync(
        AlertRuleDto rule,
        IAgentRegistrationRepository agentRepo,
        ITenantLicenseRepository licenseRepo,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock,
        CancellationToken ct)
    {
        return rule.ConditionType switch
        {
            "LicenseUtilization" => await EvaluateLicenseUtilizationAsync(rule, agentRepo, licenseRepo, ct),
            "AgentHeartbeatMissed" => await EvaluateHeartbeatAsync(rule, agentRepo, clock, ct),
            _ => LogUnknownAndSkip(rule.ConditionType),
        };
    }

    private async Task<(bool, string)> EvaluateLicenseUtilizationAsync(
        AlertRuleDto rule,
        IAgentRegistrationRepository agentRepo,
        ITenantLicenseRepository licenseRepo,
        CancellationToken ct)
    {
        var license = await licenseRepo.GetByTenantIdAsync(rule.TenantId, ct);
        if (license is null || license.IncludedHostUnits <= 0)
            return (false, string.Empty);

        var activeHostUnits = await agentRepo.SumActiveHostUnitsAsync(rule.TenantId, ct);
        var utilizationPct = (double)activeHostUnits / license.IncludedHostUnits * 100.0;

        if (utilizationPct < rule.ThresholdValue)
            return (false, string.Empty);

        return (true, $"License utilization {utilizationPct:F1}% exceeds threshold {rule.ThresholdValue}% " +
                      $"({activeHostUnits:F1}/{license.IncludedHostUnits} host units)");
    }

    private async Task<(bool, string)> EvaluateHeartbeatAsync(
        AlertRuleDto rule,
        IAgentRegistrationRepository agentRepo,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock,
        CancellationToken ct)
    {
        var agents = await agentRepo.ListByTenantAsync(rule.TenantId, ct);
        var cutoff = clock.UtcNow - TimeSpan.FromMinutes(rule.ThresholdValue);

        var missedAgents = agents
            .Where(a => a.Status == Domain.Entities.AgentRegistrationStatus.Active && a.LastHeartbeatAt < cutoff)
            .ToList();

        if (missedAgents.Count == 0)
            return (false, string.Empty);

        return (true, $"{missedAgents.Count} agent(s) missed heartbeat for over {rule.ThresholdValue} minutes " +
                      $"(oldest: {missedAgents.Min(a => a.LastHeartbeatAt):u})");
    }

    private (bool, string) LogUnknownAndSkip(string conditionType)
    {
        logger.LogWarning("AlertEvaluationJob: unknown condition type '{ConditionType}' — skipping.", conditionType);
        return (false, string.Empty);
    }

    private sealed record AlertRuleDto(
        Guid RuleId,
        Guid TenantId,
        string Name,
        string ConditionType,
        double ThresholdValue,
        string? ServiceName,
        string Severity,
        string? NotificationChannels);
}
