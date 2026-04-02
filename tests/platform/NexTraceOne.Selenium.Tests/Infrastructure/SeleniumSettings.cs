namespace NexTraceOne.Selenium.Tests.Infrastructure;

/// <summary>
/// Configuração centralizada para os testes Selenium.
/// Valores podem ser sobrepostos via variáveis de ambiente para CI/CD.
/// </summary>
internal static class SeleniumSettings
{
    /// <summary>
    /// URL base do frontend. Padrão: servidor de preview Vite.
    /// Sobrepor com NXT_SELENIUM_BASE_URL.
    /// </summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("NXT_SELENIUM_BASE_URL") ?? "http://localhost:4173";

    /// <summary>
    /// Timeout padrão para aguardar elementos na página (segundos).
    /// Sobrepor com NXT_SELENIUM_TIMEOUT.
    /// </summary>
    public static int DefaultTimeoutSeconds =>
        int.TryParse(Environment.GetEnvironmentVariable("NXT_SELENIUM_TIMEOUT"), out var t) ? t : 15;

    /// <summary>
    /// Executar o browser em modo headless (sem janela visível).
    /// Sobrepor com NXT_SELENIUM_HEADLESS=false para depuração visual.
    /// </summary>
    public static bool Headless =>
        !string.Equals(
            Environment.GetEnvironmentVariable("NXT_SELENIUM_HEADLESS"),
            "false",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Directoria para guardar screenshots de falha.
    /// </summary>
    public static string ScreenshotDir =>
        Environment.GetEnvironmentVariable("NXT_SELENIUM_SCREENSHOT_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
}
