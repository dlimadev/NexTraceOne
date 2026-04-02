using OpenQA.Selenium.Support.Extensions;

namespace NexTraceOne.Selenium.Tests.Infrastructure;

/// <summary>
/// Classe base para todos os testes de navegação Selenium.
/// Fornece helpers comuns: navegação, espera, sessão mock, screenshots e validação de erros JS.
/// </summary>
public abstract class SeleniumTestBase : IClassFixture<BrowserFixture>
{
    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }
    protected string BaseUrl { get; }

    protected SeleniumTestBase(BrowserFixture fixture)
    {
        Driver = fixture.Driver;
        BaseUrl = SeleniumSettings.BaseUrl;
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(SeleniumSettings.DefaultTimeoutSeconds));
    }

    /// <summary>
    /// Navega para uma rota relativa e aguarda que a página esteja loaded.
    /// </summary>
    protected void NavigateTo(string relativePath)
    {
        var url = $"{BaseUrl}{relativePath}";
        Driver.Navigate().GoToUrl(url);
        WaitForPageLoad();
    }

    /// <summary>
    /// Injeta tokens de sessão mock no sessionStorage para simular
    /// utilizador autenticado, conforme a estratégia do frontend (nxt_at, nxt_tid, nxt_uid).
    /// </summary>
    protected void MockAuthSession()
    {
        // Navigate to the app first so we can set sessionStorage on the correct origin
        Driver.Navigate().GoToUrl(BaseUrl);
        WaitForPageLoad();

        var js = (IJavaScriptExecutor)Driver;
        js.ExecuteScript(@"
            sessionStorage.setItem('nxt_at', 'mock-selenium-token');
            sessionStorage.setItem('nxt_tid', 'tenant-selenium-001');
            sessionStorage.setItem('nxt_uid', 'user-selenium-001');
        ");
    }

    /// <summary>
    /// Injeta mock de sessão e intercepta chamadas à API de perfil.
    /// Para cenários onde o frontend faz fetch do utilizador após login.
    /// </summary>
    protected void MockAuthSessionWithProfileIntercept()
    {
        MockAuthSession();

        // Inject a service worker or XHR interceptor to mock /api/v1/identity/users/ responses
        var js = (IJavaScriptExecutor)Driver;
        js.ExecuteScript(@"
            const origFetch = window.fetch;
            window.fetch = function(url, opts) {
                if (typeof url === 'string' && url.includes('/api/v1/identity/users/')) {
                    return Promise.resolve(new Response(JSON.stringify({
                        id: 'user-selenium-001',
                        email: 'admin@acme.com',
                        fullName: 'Selenium Test User',
                        roles: ['Admin'],
                        tenantId: 'tenant-selenium-001'
                    }), { status: 200, headers: { 'Content-Type': 'application/json' } }));
                }
                return origFetch.apply(this, arguments);
            };
        ");
    }

    /// <summary>
    /// Aguarda que o document.readyState seja 'complete'.
    /// </summary>
    protected void WaitForPageLoad()
    {
        Wait.Until(d =>
            ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
    }

    /// <summary>
    /// Aguarda que o React Suspense termine de carregar (loader desaparece).
    /// </summary>
    protected void WaitForSuspenseComplete()
    {
        // Wait for the spinner to disappear, indicating lazy load is complete
        try
        {
            Wait.Until(d =>
            {
                var spinners = d.FindElements(By.CssSelector(".animate-spin"));
                return spinners.Count == 0;
            });
        }
        catch (WebDriverTimeoutException)
        {
            // If timeout, page may have loaded without a spinner or spinner stayed — continue
        }
    }

    /// <summary>
    /// Verifica se não existem erros graves de JavaScript na consola do browser.
    /// </summary>
    protected void AssertNoJavaScriptErrors()
    {
        try
        {
            var logs = Driver.Manage().Logs.GetLog(LogType.Browser);
            var severeErrors = logs
                .Where(l => l.Level == LogLevel.Severe)
                .Where(l => !IsExpectedError(l.Message))
                .ToList();

            severeErrors.Should().BeEmpty(
                "the page should not have JavaScript console errors, but found: {0}",
                string.Join(Environment.NewLine, severeErrors.Select(e => e.Message)));
        }
        catch (NullReferenceException)
        {
            // Some drivers don't support log retrieval — skip check
        }
    }

    /// <summary>
    /// Verifica se a URL actual corresponde ao caminho esperado.
    /// </summary>
    protected void AssertCurrentPath(string expectedPath)
    {
        Wait.Until(d => new Uri(d.Url).AbsolutePath == expectedPath);
        var currentPath = new Uri(Driver.Url).AbsolutePath;
        currentPath.Should().Be(expectedPath);
    }

    /// <summary>
    /// Verifica se a página contém um heading visível (h1-h6) com o texto fornecido.
    /// </summary>
    protected void AssertHeadingVisible(string textPattern)
    {
        var headings = Driver.FindElements(By.CssSelector("h1, h2, h3, h4, h5, h6"));
        headings.Should().Contain(
            h => h.Displayed && h.Text.Contains(textPattern, StringComparison.OrdinalIgnoreCase),
            $"expected a visible heading containing '{textPattern}'");
    }

    /// <summary>
    /// Verifica que a página não mostra um ecrã de erro genérico (error boundary).
    /// </summary>
    protected void AssertNoErrorBoundary()
    {
        var errorElements = Driver.FindElements(By.CssSelector("[data-testid='error-boundary'], .error-boundary"));
        errorElements.Where(e => e.Displayed).Should().BeEmpty(
            "the page should not display an error boundary");
    }

    /// <summary>
    /// Verifica que a página não está a mostrar a página 'Unauthorized'.
    /// </summary>
    protected void AssertNotUnauthorized()
    {
        var url = new Uri(Driver.Url).AbsolutePath;
        url.Should().NotBe("/unauthorized",
            "the page should not redirect to /unauthorized with admin session");
    }

    /// <summary>
    /// Captura screenshot com nome descritivo para diagnóstico de falhas.
    /// </summary>
    protected string CaptureScreenshot(string testName)
    {
        var dir = SeleniumSettings.ScreenshotDir;
        Directory.CreateDirectory(dir);

        var sanitized = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(dir, $"{sanitized}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

        var screenshot = Driver.TakeScreenshot();
        screenshot.SaveAsFile(filePath);

        return filePath;
    }

    /// <summary>
    /// Executa a navegação completa para uma rota e valida:
    /// - a página carregou
    /// - não há error boundary
    /// - não redirecionou para unauthorized
    /// - não há erros JS graves
    /// </summary>
    protected void AssertPageLoadsSuccessfully(string route, string? expectedHeading = null)
    {
        NavigateTo(route);
        WaitForSuspenseComplete();

        AssertNoErrorBoundary();
        AssertNotUnauthorized();
        AssertNoJavaScriptErrors();

        if (expectedHeading is not null)
        {
            AssertHeadingVisible(expectedHeading);
        }
    }

    /// <summary>
    /// Filtra erros JS esperados que não representam bugs reais.
    /// </summary>
    private static bool IsExpectedError(string message)
    {
        // API calls that fail because there is no real backend during testing
        if (message.Contains("ERR_CONNECTION_REFUSED", StringComparison.OrdinalIgnoreCase))
            return true;

        if (message.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase))
            return true;

        // 401/403 from mocked session
        if (message.Contains("401") || message.Contains("403"))
            return true;

        return false;
    }
}
