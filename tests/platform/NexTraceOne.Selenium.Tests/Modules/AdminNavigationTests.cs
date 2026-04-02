using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Admin — Identity & Access, Audit,
/// Notifications, Platform Configuration, Integrations, Product Analytics.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class AdminNavigationTests : SeleniumTestBase
{
    public AdminNavigationTests(BrowserFixture fixture) : base(fixture) { }

    // ── Identity & Access ──

    [Fact]
    public void UsersPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/users");
    }

    [Fact]
    public void EnvironmentsPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/environments");
    }

    [Fact]
    public void BreakGlassPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/break-glass");
    }

    [Fact]
    public void JitAccessPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/jit-access");
    }

    [Fact]
    public void DelegationPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/delegations");
    }

    [Fact]
    public void AccessReviewPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/access-reviews");
    }

    [Fact]
    public void MySessionsPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/my-sessions");
    }

    [Fact]
    public void UnauthorizedPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/unauthorized");
    }

    // ── Audit ──

    [Fact]
    public void AuditPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/audit");
    }

    // ── Notifications ──

    [Fact]
    public void NotificationCenter_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/notifications");
    }

    [Fact]
    public void NotificationPreferences_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/notifications/preferences");
    }

    [Fact]
    public void NotificationAnalytics_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/notifications/analytics");
    }

    // ── Platform Configuration ──

    [Fact]
    public void ConfigurationAdmin_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration");
    }

    [Fact]
    public void NotificationConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/notifications");
    }

    [Fact]
    public void WorkflowConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/workflows");
    }

    [Fact]
    public void CatalogContractsConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/catalog-contracts");
    }

    [Fact]
    public void OperationsFinOpsConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/operations-finops");
    }

    [Fact]
    public void AdvancedConfigurationConsole_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/advanced");
    }

    // ── Integrations ──

    [Fact]
    public void IntegrationHub_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/integrations");
    }

    [Fact]
    public void ConnectorDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/integrations/sample-connector-001");
    }

    [Fact]
    public void IngestionExecutions_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/integrations/executions");
    }

    [Fact]
    public void IngestionFreshness_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/integrations/freshness");
    }

    // ── Product Analytics ──

    [Fact]
    public void ProductAnalyticsOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics");
    }

    [Fact]
    public void ModuleAdoption_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/adoption");
    }

    [Fact]
    public void PersonaUsage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/personas");
    }

    [Fact]
    public void JourneyFunnel_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/journeys");
    }

    [Fact]
    public void ValueTracking_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/value");
    }

    [Fact]
    public void AdoptionFunnel_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/funnel");
    }

    [Fact]
    public void FeatureHeatmap_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/heatmap");
    }

    [Fact]
    public void TimeToValue_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/analytics/time-to-value");
    }
}
