using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o Dashboard principal.
/// Valida que a home page carrega sem erros e mostra conteúdo relevante.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class DashboardNavigationTests : SeleniumTestBase
{
    public DashboardNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void Dashboard_Loads_Successfully()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/", "Dashboard");
    }

    [Fact]
    public void Dashboard_Shows_Navigation_Sidebar()
    {
        MockAuthSessionWithProfileIntercept();
        NavigateTo("/");
        WaitForSuspenseComplete();

        var sidebar = Driver.FindElements(By.CssSelector("nav, aside, [data-testid='sidebar']"));
        sidebar.Should().NotBeEmpty("the dashboard should display a navigation sidebar");
    }
}
