using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Changes — Change Catalog, Change Detail,
/// Releases, Workflow, Promotion, Release Calendar.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class ChangesNavigationTests : SeleniumTestBase
{
    public ChangesNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void ChangeCatalog_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/changes");
    }

    [Fact]
    public void ChangeDetail_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/changes/sample-change-001");
    }

    [Fact]
    public void ReleasesPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/releases");
    }

    [Fact]
    public void WorkflowPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/workflow");
    }

    [Fact]
    public void PromotionPage_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/promotion");
    }

    [Fact]
    public void ReleaseCalendar_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/release-calendar");
    }
}
