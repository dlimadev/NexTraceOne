using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace NexTraceOne.Selenium.Tests.Infrastructure;

/// <summary>
/// Fixture partilhada que gere o ciclo de vida do ChromeDriver.
/// Uma única instância de browser é reutilizada por collection para
/// velocidade, simulando navegação real de um utilizador.
/// </summary>
public sealed class BrowserFixture : IDisposable
{
    public IWebDriver Driver { get; }

    public BrowserFixture()
    {
        new DriverManager().SetUpDriver(new ChromeConfig());

        var options = new ChromeOptions();

        if (SeleniumSettings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--ignore-certificate-errors");

        // Capturar logs de consola do browser para detetar erros JS
        options.SetLoggingPreference(LogType.Browser, LogLevel.Severe);

        Driver = new ChromeDriver(options);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
