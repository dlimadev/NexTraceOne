using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Governance — Executive, Reports, Compliance,
/// Risk, FinOps, Policies, Controls, Evidence, Maturity, Benchmarking,
/// Teams, Domains, Governance Packs, Waivers, Delegated Admin, Configuration.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class GovernanceNavigationTests : SeleniumTestBase
{
    public GovernanceNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void ExecutiveOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/executive");
    }

    [Fact]
    public void ExecutiveDrillDown_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/executive/drilldown");
    }

    [Fact]
    public void ExecutiveFinOps_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/executive/finops");
    }

    [Fact]
    public void ReportsPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/reports");
    }

    [Fact]
    public void CompliancePage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/compliance");
    }

    [Fact]
    public void RiskCenter_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/risk");
    }

    [Fact]
    public void RiskHeatmap_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/risk/heatmap");
    }

    [Fact]
    public void FinOpsOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/finops");
    }

    [Fact]
    public void ServiceFinOps_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/finops/service/sample-service-001");
    }

    [Fact]
    public void TeamFinOps_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/finops/team/sample-team-001");
    }

    [Fact]
    public void DomainFinOps_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/finops/domain/sample-domain-001");
    }

    [Fact]
    public void PolicyCatalog_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/policies");
    }

    [Fact]
    public void EnterpriseControls_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/controls");
    }

    [Fact]
    public void EvidencePackages_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/evidence");
    }

    [Fact]
    public void MaturityScorecards_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/maturity");
    }

    [Fact]
    public void Benchmarking_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/benchmarking");
    }

    [Fact]
    public void TeamsOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/teams");
    }

    [Fact]
    public void TeamDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/teams/sample-team-001");
    }

    [Fact]
    public void DomainsOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/domains");
    }

    [Fact]
    public void DomainDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/domains/sample-domain-001");
    }

    [Fact]
    public void GovernancePacksOverview_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/packs");
    }

    [Fact]
    public void GovernancePackDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/packs/sample-pack-001");
    }

    [Fact]
    public void WaiversPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/waivers");
    }

    [Fact]
    public void DelegatedAdmin_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/governance/delegated-admin");
    }

    [Fact]
    public void GovernanceConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/governance");
    }
}
