namespace NexTraceOne.Selenium.Tests.Infrastructure;

/// <summary>
/// Define a collection partilhada para que todos os testes
/// de navegação reutilizem a mesma instância de browser.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SeleniumCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "Selenium";
}
