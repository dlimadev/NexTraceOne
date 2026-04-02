using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Contracts — Catálogo, Criação, Studio,
/// Governance, Spectral, Canonical, Publication, Portal.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class ContractsNavigationTests : SeleniumTestBase
{
    public ContractsNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void ContractCatalog_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts");
    }

    [Fact]
    public void CreateContract_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/new");
    }

    [Fact]
    public void ContractGovernance_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/governance");
    }

    [Fact]
    public void SpectralRulesetManager_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/spectral");
    }

    [Fact]
    public void CanonicalEntityCatalog_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/canonical");
    }

    [Fact]
    public void PublicationCenter_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/publication");
    }

    [Fact]
    public void ContractWorkspace_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/sample-contract-version-001");
    }

    [Fact]
    public void ContractPortal_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/portal/sample-contract-version-001");
    }

    [Fact]
    public void DraftStudio_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/contracts/studio/sample-draft-001");
    }
}
