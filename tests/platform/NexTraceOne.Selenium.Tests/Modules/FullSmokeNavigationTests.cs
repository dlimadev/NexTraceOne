using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Smoke test que percorre TODAS as rotas do frontend num único fluxo sequencial.
/// Ideal para execução rápida em CI/CD: deteta páginas que não carregam,
/// error boundaries, redirects inesperados e erros JS graves.
///
/// Para cada rota, o teste:
/// 1. Navega à página
/// 2. Aguarda o carregamento (Suspense + page load)
/// 3. Verifica ausência de error boundary
/// 4. Verifica que não redirecionou para /unauthorized
/// 5. Grava screenshot em caso de falha
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class FullSmokeNavigationTests : SeleniumTestBase
{
    public FullSmokeNavigationTests(BrowserFixture fixture) : base(fixture) { }

    /// <summary>
    /// Todas as rotas estáticas do frontend (sem parâmetros dinâmicos).
    /// </summary>
    public static IEnumerable<object[]> AllStaticRoutes => new List<object[]>
    {
        // ── Auth (public) ──
        new object[] { "/login" },
        new object[] { "/forgot-password" },
        new object[] { "/reset-password" },
        new object[] { "/activate" },
        new object[] { "/mfa" },
        new object[] { "/invitation" },
        new object[] { "/select-tenant" },

        // ── Dashboard ──
        new object[] { "/" },

        // ── Catalog ──
        new object[] { "/search" },
        new object[] { "/source-of-truth" },
        new object[] { "/services" },
        new object[] { "/services/graph" },
        new object[] { "/services/legacy" },
        new object[] { "/services/discovery" },
        new object[] { "/services/maturity" },
        new object[] { "/portal" },

        // ── Contracts ──
        new object[] { "/contracts" },
        new object[] { "/contracts/new" },
        new object[] { "/contracts/governance" },
        new object[] { "/contracts/spectral" },
        new object[] { "/contracts/canonical" },
        new object[] { "/contracts/publication" },

        // ── Knowledge ──
        new object[] { "/knowledge" },
        new object[] { "/knowledge/notes" },

        // ── Changes ──
        new object[] { "/changes" },
        new object[] { "/releases" },
        new object[] { "/workflow" },
        new object[] { "/promotion" },
        new object[] { "/release-calendar" },

        // ── Operations ──
        new object[] { "/operations/incidents" },
        new object[] { "/operations/incidents/timeline" },
        new object[] { "/operations/runbooks" },
        new object[] { "/operations/reliability" },
        new object[] { "/operations/reliability/slos" },
        new object[] { "/operations/automation" },
        new object[] { "/operations/automation/admin" },
        new object[] { "/operations/runtime-comparison" },
        new object[] { "/platform/operations" },

        // ── AI Hub ──
        new object[] { "/ai/assistant" },
        new object[] { "/ai/models" },
        new object[] { "/ai/policies" },
        new object[] { "/ai/routing" },
        new object[] { "/ai/ide" },
        new object[] { "/ai/budgets" },
        new object[] { "/ai/audit" },
        new object[] { "/ai/agents" },
        new object[] { "/ai/analysis" },

        // ── Governance ──
        new object[] { "/governance/executive" },
        new object[] { "/governance/executive/drilldown" },
        new object[] { "/governance/executive/finops" },
        new object[] { "/governance/reports" },
        new object[] { "/governance/compliance" },
        new object[] { "/governance/risk" },
        new object[] { "/governance/risk/heatmap" },
        new object[] { "/governance/finops" },
        new object[] { "/governance/policies" },
        new object[] { "/governance/controls" },
        new object[] { "/governance/evidence" },
        new object[] { "/governance/maturity" },
        new object[] { "/governance/benchmarking" },
        new object[] { "/governance/teams" },
        new object[] { "/governance/domains" },
        new object[] { "/governance/packs" },
        new object[] { "/governance/waivers" },
        new object[] { "/governance/delegated-admin" },

        // ── Admin — Identity & Access ──
        new object[] { "/users" },
        new object[] { "/environments" },
        new object[] { "/break-glass" },
        new object[] { "/jit-access" },
        new object[] { "/delegations" },
        new object[] { "/access-reviews" },
        new object[] { "/my-sessions" },
        new object[] { "/unauthorized" },

        // ── Admin — Audit ──
        new object[] { "/audit" },

        // ── Admin — Notifications ──
        new object[] { "/notifications" },
        new object[] { "/notifications/preferences" },
        new object[] { "/notifications/analytics" },

        // ── Admin — Platform Configuration ──
        new object[] { "/platform/configuration" },
        new object[] { "/platform/configuration/notifications" },
        new object[] { "/platform/configuration/workflows" },
        new object[] { "/platform/configuration/catalog-contracts" },
        new object[] { "/platform/configuration/operations-finops" },
        new object[] { "/platform/configuration/advanced" },
        new object[] { "/platform/configuration/ai-integrations" },
        new object[] { "/platform/configuration/governance" },

        // ── Admin — Integrations ──
        new object[] { "/integrations" },
        new object[] { "/integrations/executions" },
        new object[] { "/integrations/freshness" },

        // ── Admin — Product Analytics ──
        new object[] { "/analytics" },
        new object[] { "/analytics/adoption" },
        new object[] { "/analytics/personas" },
        new object[] { "/analytics/journeys" },
        new object[] { "/analytics/value" },
        new object[] { "/analytics/funnel" },
        new object[] { "/analytics/heatmap" },
        new object[] { "/analytics/time-to-value" },
    };

    [Theory]
    [MemberData(nameof(AllStaticRoutes))]
    public void Page_Loads_Without_Errors(string route)
    {
        // Public auth routes don't need session
        var isPublicRoute = route is "/login" or "/forgot-password" or "/reset-password"
            or "/activate" or "/mfa" or "/invitation" or "/select-tenant";

        if (!isPublicRoute)
        {
            MockAuthSessionWithProfileIntercept();
        }

        try
        {
            NavigateTo(route);
            WaitForSuspenseComplete();
            AssertNoErrorBoundary();

            // Public routes may redirect to login — that's expected
            if (!isPublicRoute && route != "/unauthorized")
            {
                AssertNotUnauthorized();
            }
        }
        catch (Exception)
        {
            CaptureScreenshot($"smoke_failure_{route.Replace("/", "_")}");
            throw;
        }
    }
}
