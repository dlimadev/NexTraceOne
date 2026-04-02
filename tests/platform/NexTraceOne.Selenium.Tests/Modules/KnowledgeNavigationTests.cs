using NexTraceOne.Selenium.Tests.Infrastructure;

namespace NexTraceOne.Selenium.Tests.Modules;

/// <summary>
/// Testes de navegação para o módulo Knowledge — Hub, Documentos, Notas Operacionais.
/// </summary>
[Collection(SeleniumCollection.Name)]
public sealed class KnowledgeNavigationTests : SeleniumTestBase
{
    public KnowledgeNavigationTests(BrowserFixture fixture) : base(fixture) { }

    [Fact]
    public void KnowledgeHub_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/knowledge");
    }

    [Fact]
    public void KnowledgeDocument_Loads_With_SampleId()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/knowledge/documents/sample-doc-001");
    }

    [Fact]
    public void OperationalNotes_Loads()
    {
        MockAuthSessionWithProfileIntercept();
        AssertPageLoadsSuccessfully("/knowledge/notes");
    }
}
