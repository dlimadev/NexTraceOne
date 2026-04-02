using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo AI Hub — Assistant, Models, Policies,
/// Routing, IDE, Budgets, Audit, Agents, Analysis, AI Integrations Config.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class AiHubNavigationTests : SeleniumTestBase
{
    public AiHubNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void AiAssistant_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/assistant");
    }

    [Fact]
    public void ModelRegistry_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/models");
    }

    [Fact]
    public void AiPolicies_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/policies");
    }

    [Fact]
    public void AiRouting_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/routing");
    }

    [Fact]
    public void IdeIntegrations_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/ide");
    }

    [Fact]
    public void TokenBudget_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/budgets");
    }

    [Fact]
    public void AiAudit_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/audit");
    }

    [Fact]
    public void AiAgents_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/agents");
    }

    [Fact]
    public void AgentDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/agents/sample-agent-001");
    }

    [Fact]
    public void AiAnalysis_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/ai/analysis");
    }

    [Fact]
    public void AiIntegrationsConfiguration_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/platform/configuration/ai-integrations");
    }
}
