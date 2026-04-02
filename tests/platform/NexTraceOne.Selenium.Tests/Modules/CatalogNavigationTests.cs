using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Catalog — Service Catalog, Source of Truth,
/// Search, Developer Portal, Legacy Assets, Discovery, Maturity.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class CatalogNavigationTests : SeleniumTestBase
{
    public CatalogNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void GlobalSearch_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/search");
    }

    [Fact]
    public void SourceOfTruthExplorer_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/source-of-truth");
    }

    [Fact]
    public void ServiceCatalogList_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services");
    }

    [Fact]
    public void ServiceCatalogGraph_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services/graph");
    }

    [Fact]
    public void ServiceDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services/sample-service-001");
    }

    [Fact]
    public void LegacyAssetCatalog_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services/legacy");
    }

    [Fact]
    public void ServiceDiscovery_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services/discovery");
    }

    [Fact]
    public void ServiceMaturity_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/services/maturity");
    }

    [Fact]
    public void DeveloperPortal_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/portal");
    }
}
