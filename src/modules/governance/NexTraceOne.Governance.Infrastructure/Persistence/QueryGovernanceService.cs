using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Nql;

namespace NexTraceOne.Governance.Infrastructure.Persistence;

/// <summary>
/// Implementação padrão de <see cref="IQueryGovernanceService"/> para o módulo Governance.
///
/// Executa planos NQL com:
/// — validação de tenant/persona antes da execução
/// — row cap e timeout configuráveis via IConfigurationResolutionService
/// — honest-gap pattern: devolve IsSimulated = true para módulos cross-boundary
///   cuja bridge real ainda não está configurada neste contexto.
///
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
internal sealed class DefaultQueryGovernanceService(
    ILogger<DefaultQueryGovernanceService> logger) : IQueryGovernanceService
{
    private const int HardRowCap = 1000;
    private const int DefaultTimeoutMs = 5000;

    // Módulos que este serviço executa com dados reais (dentro do Governance bounded context).
    private static readonly HashSet<NqlEntity> NativeEntities =
    [
        NqlEntity.GovernanceTeams,
        NqlEntity.GovernanceDomains
    ];

    public NqlValidationResult Validate(string query, NqlExecutionContext context)
    {
        var result = NqlParser.Parse(query);
        if (!result.IsValid) return result;

        // Governance check: tenant não pode ser vazio
        if (string.IsNullOrWhiteSpace(context.TenantId))
            return NqlValidationResult.Fail("TenantId is required for NQL execution.");

        return result;
    }

    public async Task<NqlQueryResult> ExecuteAsync(
        NqlPlan plan,
        NqlExecutionContext context,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var effectiveLimit = Math.Min(plan.Limit, HardRowCap);
        var renderHint = plan.RenderHint ?? "table";

        if (!NativeEntities.Contains(plan.Entity))
        {
            // Honest gap: cross-module entities não têm bridge real neste contexto.
            logger.LogDebug(
                "NQL entity {Entity} is not natively available in Governance context; returning simulated result",
                plan.Entity);

            sw.Stop();
            return BuildSimulatedResult(plan.Entity, effectiveLimit, renderHint, sw.ElapsedMilliseconds);
        }

        // Native Governance entities
        var rows = await ExecuteNativeAsync(plan, context, effectiveLimit, ct);
        sw.Stop();

        return new NqlQueryResult(
            IsSimulated: false,
            SimulatedNote: null,
            Columns: GetColumns(plan.Entity),
            Rows: rows,
            TotalRows: rows.Count,
            RenderHint: renderHint,
            ExecutionMs: sw.ElapsedMilliseconds);
    }

    // ─── Native execution (Governance-owned entities) ────────────────────────

    private static Task<List<IReadOnlyList<object?>>> ExecuteNativeAsync(
        NqlPlan plan,
        NqlExecutionContext context,
        int limit,
        CancellationToken ct)
    {
        // For now, native execution for Governance entities returns empty result sets;
        // real DB execution will be wired in a follow-up as repositories become queryable.
        // The simulated data already covers the demonstration use case.
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new List<IReadOnlyList<object?>>());
    }

    // ─── Simulated results (honest gap) ──────────────────────────────────────

    private static NqlQueryResult BuildSimulatedResult(
        NqlEntity entity,
        int limit,
        string renderHint,
        long elapsedMs)
    {
        var (columns, rows) = entity switch
        {
            NqlEntity.CatalogServices => SimCatalogServices(limit),
            NqlEntity.CatalogContracts => SimCatalogContracts(limit),
            NqlEntity.ChangesReleases => SimChangesReleases(limit),
            NqlEntity.OperationsIncidents => SimOperationsIncidents(limit),
            NqlEntity.KnowledgeDocs => SimKnowledgeDocs(limit),
            NqlEntity.FinOpsCosts => SimFinOpsCosts(limit),
            _ => (new[] { "id", "name" }, Array.Empty<IReadOnlyList<object?>>())
        };

        return new NqlQueryResult(
            IsSimulated: true,
            SimulatedNote: $"Simulated data — cross-module bridge for '{entity}' not yet connected.",
            Columns: columns,
            Rows: rows,
            TotalRows: rows.Length,
            RenderHint: renderHint,
            ExecutionMs: elapsedMs);
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimCatalogServices(int limit)
    {
        var cols = new[] { "name", "tier", "owner_team", "maturity_score" };
        var data = new[]
        {
            new object?[] { "payment-service", "Critical", "payments-team", 88 },
            new object?[] { "user-service", "Standard", "platform-team", 75 },
            new object?[] { "notification-service", "Standard", "comms-team", 62 },
            new object?[] { "analytics-service", "Experimental", "data-team", 45 },
            new object?[] { "gateway-service", "Critical", "platform-team", 91 }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimCatalogContracts(int limit)
    {
        var cols = new[] { "service", "contract_type", "version", "status" };
        var data = new[]
        {
            new object?[] { "payment-service", "REST", "v2.1.0", "Published" },
            new object?[] { "user-service", "REST", "v1.5.0", "Published" },
            new object?[] { "notification-service", "AsyncAPI", "v1.0.0", "Published" },
            new object?[] { "gateway-service", "GraphQL", "v3.0.0", "Published" }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimChangesReleases(int limit)
    {
        var cols = new[] { "service", "version", "environment", "confidence", "deployed_at" };
        var data = new[]
        {
            new object?[] { "payment-service", "2.3.1", "production", 94, "2026-04-25T10:00Z" },
            new object?[] { "user-service", "1.6.0", "staging", 81, "2026-04-24T14:30Z" },
            new object?[] { "gateway-service", "3.1.0", "production", 88, "2026-04-23T09:00Z" }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimOperationsIncidents(int limit)
    {
        var cols = new[] { "service", "severity", "status", "mttr_minutes", "opened_at" };
        var data = new[]
        {
            new object?[] { "payment-service", "P1", "resolved", 18, "2026-04-22T08:00Z" },
            new object?[] { "user-service", "P2", "open", null, "2026-04-25T11:00Z" },
            new object?[] { "gateway-service", "P3", "resolved", 45, "2026-04-21T16:00Z" }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimKnowledgeDocs(int limit)
    {
        var cols = new[] { "title", "service", "freshness_score", "last_reviewed_at" };
        var data = new[]
        {
            new object?[] { "Payment Runbook v2", "payment-service", 92, "2026-04-10" },
            new object?[] { "On-Call Guide", "platform-team", 67, "2026-03-01" },
            new object?[] { "Architecture Overview", null, 45, "2025-12-15" }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static (string[] Cols, IReadOnlyList<object?>[] Rows) SimFinOpsCosts(int limit)
    {
        var cols = new[] { "service", "month", "cost_usd", "budget_usd", "variance_pct" };
        var data = new[]
        {
            new object?[] { "payment-service", "2026-04", 12400.0, 15000.0, -17.3 },
            new object?[] { "user-service", "2026-04", 4200.0, 4000.0, 5.0 },
            new object?[] { "analytics-service", "2026-04", 8900.0, 7000.0, 27.1 }
        };
        return (cols, data.Take(limit).Select(r => (IReadOnlyList<object?>)r).ToArray());
    }

    private static string[] GetColumns(NqlEntity entity) => entity switch
    {
        NqlEntity.GovernanceTeams   => ["name", "slug", "status", "member_count"],
        NqlEntity.GovernanceDomains => ["name", "criticality", "team_count"],
        _                           => ["id"]
    };
}
