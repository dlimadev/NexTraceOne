using System.Text;

using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.AdviseOnServiceDependencies;

/// <summary>
/// Feature: AdviseOnServiceDependencies — usa LLM para gerar resumo executivo
/// e upgrade path prioritizado a partir de um ServiceDependencyProfile enriquecido.
/// </summary>
public static class AdviseOnServiceDependencies
{
    public sealed record Command(Guid ServiceId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    public sealed record Response(
        Guid ServiceId,
        string ExecutiveSummary,
        List<UpgradeRecommendation> UpgradePath,
        string? RawLlmResponse = null);

    public sealed record UpgradeRecommendation(
        string PackageName,
        string CurrentVersion,
        string TargetVersion,
        string Priority, // Critical, High, Medium, Low
        string Rationale);

    public sealed class Handler(
        IServiceDependencyProfileRepository repository,
        ILlmCompletionClient llmClient,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"Dependency profile not found for service {request.ServiceId}.");

            var prompt = BuildPrompt(profile);
            var llmResponse = await llmClient.CompleteAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(llmResponse))
            {
                logger.LogWarning("LLM unavailable for service {ServiceId}. Returning rule-based advice.", request.ServiceId);
                var fallback = BuildRuleBasedAdvice(profile);
                return Result<Response>.Success(fallback with { ServiceId = request.ServiceId });
            }

            var parsed = TryParseLlmResponse(llmResponse, profile);
            return Result<Response>.Success(parsed with
            {
                ServiceId = request.ServiceId,
                RawLlmResponse = llmResponse
            });
        }

        private static string BuildPrompt(ServiceDependencyProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a Dependency Security Advisor. Analyze the following service dependency profile and provide a concise executive summary and prioritized upgrade path.");
            sb.AppendLine();
            sb.AppendLine($"Service ID: {profile.ServiceId}");
            sb.AppendLine($"Health Score: {profile.HealthScore}/100");
            sb.AppendLine($"Total Dependencies: {profile.TotalDependencies}");
            sb.AppendLine($"Direct: {profile.DirectDependencies}, Transitive: {profile.TransitiveDependencies}");
            sb.AppendLine();

            foreach (var dep in profile.Dependencies)
            {
                sb.AppendLine($"- {dep.PackageName}@{dep.Version}");
                if (!string.IsNullOrEmpty(dep.LatestStableVersion))
                    sb.AppendLine($"  Latest: {dep.LatestStableVersion}");
                if (dep.IsOutdated)
                    sb.AppendLine($"  Status: OUTDATED");
                if (!string.IsNullOrEmpty(dep.License))
                    sb.AppendLine($"  License: {dep.License}");
                foreach (var vuln in dep.Vulnerabilities)
                {
                    sb.AppendLine($"  VULNERABILITY: {vuln.CveId} | Severity: {vuln.Severity} | CVSS: {vuln.CvssScore} | Fixed in: {vuln.FixedInVersion ?? "unknown"}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Respond ONLY with valid JSON. No markdown, no explanations.");
            sb.AppendLine();
            sb.AppendLine("Expected JSON format:");
            sb.AppendLine("{ \"executiveSummary\": \"...\", \"upgradePath\": [{ \"packageName\": \"...\", \"currentVersion\": \"...\", \"targetVersion\": \"...\", \"priority\": \"...\", \"rationale\": \"...\" }] }");

            return sb.ToString();
        }

        private static Response BuildRuleBasedAdvice(ServiceDependencyProfile profile)
        {
            var upgrades = new List<UpgradeRecommendation>();

            foreach (var dep in profile.Dependencies.Where(d => d.Vulnerabilities.Any()))
            {
                var maxSeverity = dep.Vulnerabilities.Max(v => v.Severity);
                var priority = maxSeverity switch
                {
                    Domain.DependencyGovernance.Enums.VulnerabilitySeverity.Critical => "Critical",
                    Domain.DependencyGovernance.Enums.VulnerabilitySeverity.High => "High",
                    _ => "Medium"
                };

                var fixedVersion = dep.Vulnerabilities
                    .Select(v => v.FixedInVersion)
                    .FirstOrDefault(v => !string.IsNullOrEmpty(v))
                    ?? dep.LatestStableVersion
                    ?? "latest";

                upgrades.Add(new UpgradeRecommendation(
                    dep.PackageName,
                    dep.Version,
                    fixedVersion,
                    priority,
                    $"Vulnerabilities: {string.Join(", ", dep.Vulnerabilities.Select(v => v.CveId))}"));
            }

            foreach (var dep in profile.Dependencies.Where(d => d.IsOutdated && !d.Vulnerabilities.Any()))
            {
                upgrades.Add(new UpgradeRecommendation(
                    dep.PackageName,
                    dep.Version,
                    dep.LatestStableVersion ?? "latest",
                    "Low",
                    "Package is outdated."));
            }

            var ordered = upgrades
                .OrderBy(u => u.Priority switch { "Critical" => 0, "High" => 1, "Medium" => 2, _ => 3 })
                .ToList();

            var summary = profile.HealthScore >= 80
                ? "Dependency health is good. Minor updates recommended."
                : profile.HealthScore >= 50
                    ? $"Dependency health is moderate ({profile.HealthScore}/100). {ordered.Count} packages need attention."
                    : $"Dependency health is critical ({profile.HealthScore}/100). {ordered.Count} packages require immediate action.";

            return new Response(profile.ServiceId, summary, ordered);
        }

        private static Response TryParseLlmResponse(string json, ServiceDependencyProfile profile)
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var summary = doc.RootElement.GetProperty("executiveSummary").GetString()
                    ?? "LLM response parsed but summary missing.";

                var upgrades = new List<UpgradeRecommendation>();
                if (doc.RootElement.TryGetProperty("upgradePath", out var path))
                {
                    foreach (var item in path.EnumerateArray())
                    {
                        upgrades.Add(new UpgradeRecommendation(
                            item.GetProperty("packageName").GetString() ?? "unknown",
                            item.GetProperty("currentVersion").GetString() ?? "unknown",
                            item.GetProperty("targetVersion").GetString() ?? "unknown",
                            item.GetProperty("priority").GetString() ?? "Medium",
                            item.GetProperty("rationale").GetString() ?? ""));
                    }
                }

                return new Response(profile.ServiceId, summary, upgrades);
            }
            catch (Exception)
            {
                return BuildRuleBasedAdvice(profile);
            }
        }
    }
}
