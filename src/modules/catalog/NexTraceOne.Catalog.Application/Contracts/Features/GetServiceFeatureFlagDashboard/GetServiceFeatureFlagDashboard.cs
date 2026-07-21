using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetServiceFeatureFlagDashboard;

/// <summary>
/// Feature: GetServiceFeatureFlagDashboard — vista agregada das feature flags do tenant
/// consumida pela aba "Feature Flags" do detalhe do serviço e pelo portefólio.
///
/// Devolve a lista achatada de flags (com estado actual) mais contadores globais
/// (total, activas, inactivas, serviços afectados). O frontend filtra por serviço
/// do lado do cliente. Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
public static class GetServiceFeatureFlagDashboard
{
    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query do dashboard de feature flags por tenant.</summary>
    public sealed record Query(string TenantId) : IQuery<Dashboard>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
    }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Flag individual escopada por serviço.</summary>
    public sealed record ServiceFeatureFlag(
        string Id,
        string ServiceId,
        string ServiceName,
        string FlagKey,
        string DisplayName,
        string? Description,
        bool Enabled,
        string Environment,
        DateTimeOffset UpdatedAt,
        string? UpdatedBy);

    /// <summary>Dashboard agregado de feature flags.</summary>
    public sealed record Dashboard(
        int TotalFlags,
        int EnabledFlags,
        int DisabledFlags,
        int AffectedServices,
        IReadOnlyList<ServiceFeatureFlag> Flags);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(IFeatureFlagRepository repository) : IQueryHandler<Query, Dashboard>
    {
        public async Task<Result<Dashboard>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var records = await repository.ListByTenantAsync(request.TenantId, cancellationToken);

            var flags = records
                .OrderBy(f => f.ServiceId)
                .ThenBy(f => f.FlagKey)
                .Select(f => new ServiceFeatureFlag(
                    f.Id.ToString(),
                    f.ServiceId,
                    f.ServiceId, // sem reader de nome — usa serviceId como fallback (ver inventory report)
                    f.FlagKey,
                    f.FlagKey,
                    Description: null,
                    f.IsEnabled,
                    ResolveEnvironment(f.EnabledEnvironmentsJson),
                    f.LastToggledAt ?? f.CreatedAt,
                    f.OwnerId))
                .ToList();

            var dashboard = new Dashboard(
                TotalFlags: flags.Count,
                EnabledFlags: flags.Count(f => f.Enabled),
                DisabledFlags: flags.Count(f => !f.Enabled),
                AffectedServices: flags.Select(f => f.ServiceId).Distinct().Count(),
                Flags: flags);

            return Result<Dashboard>.Success(dashboard);
        }

        private static string ResolveEnvironment(string? enabledEnvironmentsJson)
        {
            if (string.IsNullOrWhiteSpace(enabledEnvironmentsJson))
                return "default";
            try
            {
                var envs = JsonSerializer.Deserialize<List<string>>(enabledEnvironmentsJson);
                return envs is { Count: > 0 } ? string.Join(", ", envs) : "default";
            }
            catch
            {
                return "default";
            }
        }
    }
}
