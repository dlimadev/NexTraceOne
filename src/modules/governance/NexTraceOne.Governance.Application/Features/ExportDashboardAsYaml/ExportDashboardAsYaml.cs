using System.Text;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ExportDashboardAsYaml;

/// <summary>
/// Feature: ExportDashboardAsYaml — exporta um dashboard como YAML canonicalizado (Dashboard-as-Code).
/// V3.6 — DaC: roundtrip export → import preserva 100% do dashboard.
/// </summary>
public static class ExportDashboardAsYaml
{
    public sealed record Query(Guid DashboardId, string TenantId) : IQuery<Response>;

    public sealed record Response(
        Guid DashboardId,
        string Name,
        string YamlContent,
        string Checksum);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(ICustomDashboardRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "Dashboard.NotFound",
                    "Dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            var yaml = BuildYaml(dashboard);
            var checksum = ComputeChecksum(yaml);

            return Result<Response>.Success(new Response(
                dashboard.Id.Value,
                dashboard.Name,
                yaml,
                checksum));
        }

        private static string BuildYaml(CustomDashboard d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# NexTraceOne Dashboard-as-Code v1");
            sb.AppendLine("# Generated: " + DateTimeOffset.UtcNow.ToString("O"));
            sb.AppendLine("apiVersion: nextraceone.io/v1");
            sb.AppendLine("kind: Dashboard");
            sb.AppendLine("metadata:");
            sb.AppendLine($"  name: {Escape(d.Name)}");
            sb.AppendLine($"  tenantId: {Escape(d.TenantId)}");
            sb.AppendLine($"  persona: {Escape(d.Persona)}");
            if (d.TeamId is not null)
                sb.AppendLine($"  teamId: {Escape(d.TeamId)}");
            sb.AppendLine($"  lifecycleStatus: {d.LifecycleStatus}");
            sb.AppendLine("spec:");
            sb.AppendLine($"  layout: {Escape(d.Layout)}");
            if (d.Description is not null)
                sb.AppendLine($"  description: {Escape(d.Description)}");
            sb.AppendLine($"  sharingScope: {d.SharingPolicy.Scope}");
            sb.AppendLine($"  sharingPermission: {d.SharingPolicy.Permission}");
            sb.AppendLine($"  revisionNumber: {d.CurrentRevisionNumber}");

            if (d.Variables.Count > 0)
            {
                sb.AppendLine("  variables:");
                foreach (var v in d.Variables)
                {
                    sb.AppendLine($"  - key: {Escape(v.Key)}");
                    sb.AppendLine($"    label: {Escape(v.Label)}");
                    sb.AppendLine($"    type: {v.Type}");
                    if (v.DefaultValue is not null)
                        sb.AppendLine($"    default: {Escape(v.DefaultValue)}");
                }
            }

            if (d.Widgets.Count > 0)
            {
                sb.AppendLine("  widgets:");
                foreach (var w in d.Widgets)
                {
                    sb.AppendLine($"  - widgetId: {Escape(w.WidgetId)}");
                    sb.AppendLine($"    type: {Escape(w.Type)}");
                    sb.AppendLine($"    position:");
                    sb.AppendLine($"      x: {w.Position.X}");
                    sb.AppendLine($"      y: {w.Position.Y}");
                    sb.AppendLine($"      width: {w.Position.Width}");
                    sb.AppendLine($"      height: {w.Position.Height}");
                    if (w.Config.ServiceId is not null)
                        sb.AppendLine($"    serviceId: {Escape(w.Config.ServiceId)}");
                    if (w.Config.TeamId is not null)
                        sb.AppendLine($"    teamId: {Escape(w.Config.TeamId)}");
                    if (w.Config.TimeRange is not null)
                        sb.AppendLine($"    timeRange: {Escape(w.Config.TimeRange)}");
                    if (w.Config.CustomTitle is not null)
                        sb.AppendLine($"    customTitle: {Escape(w.Config.CustomTitle)}");
                }
            }

            return sb.ToString();
        }

        private static string Escape(string value) =>
            value.Contains(':') || value.Contains('#') || value.Contains('"')
                ? $"\"{value.Replace("\"", "\\\"")}\""
                : value;

        private static string ComputeChecksum(string content)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
