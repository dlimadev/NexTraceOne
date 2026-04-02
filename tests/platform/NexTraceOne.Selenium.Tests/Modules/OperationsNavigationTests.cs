using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Operations — Incidents, Runbooks, Reliability,
/// Automation, Environment Comparison, Platform Operations.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class OperationsNavigationTests : SeleniumTestBase
{
    public OperationsNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void IncidentsPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/incidents");
    }

    [Fact]
    public void IncidentTimeline_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/incidents/timeline");
    }

    [Fact]
    public void IncidentDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/incidents/sample-incident-001");
    }

    [Fact]
    public void RunbooksPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/runbooks");
    }

    [Fact]
    public void TeamReliability_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/reliability");
    }

    [Fact]
    public void ReliabilitySloManagement_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/reliability/slos");
    }

    [Fact]
    public void ServiceReliabilityDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/reliability/sample-service-001");
    }

    [Fact]
    public void AutomationWorkflows_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/automation");
    }

    [Fact]
    public void AutomationAdmin_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/automation/admin");
    }

    [Fact]
    public void AutomationWorkflowDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/automation/sample-workflow-001");
    }

    [Fact]
    public void EnvironmentComparison_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/operations/runtime-comparison");
    }

    [Fact]
    public void PlatformOperations_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/operations");
    }
}
