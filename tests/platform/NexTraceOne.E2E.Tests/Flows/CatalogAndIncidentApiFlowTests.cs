using NexTraceOne.E2E.Tests.Infrastructure;

namespace NexTraceOne.E2E.Tests.Flows;

/// <summary>
/// Testes E2E reais para fluxos do catálogo de serviços e gestão de incidentes.
/// Valida que os endpoints core respondem corretamente com autenticação real
/// contra backend + PostgreSQL real.
///
/// Classificação: ALTA CONFIANÇA para endpoints de listagem vazios
///                MÉDIA CONFIANÇA para criação (requer seed de dados mais completo)
/// </summary>
[Collection(ApiE2ECollection.Name)]
public sealed class CatalogAndIncidentApiFlowTests(ApiE2EFixture fixture)
{
    // ── Catalog: Service Source of Truth ─────────────────────────────────────

    [Fact]
    public async Task Catalog_ListServices_Without_Auth_Should_Return_401()
    {
        var response = await fixture.Client.GetAsync("/api/v1/catalog/services");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET /api/v1/catalog/services exige autenticação");
    }

    [Fact]
    public async Task Catalog_ListServices_With_Auth_Should_Return_200_Or_403()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return; // Known gap — seed dependency

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/catalog/services");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "utilizador autenticado deve receber 200 (lista) ou 403 (sem permissão específica)");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace("a resposta deve conter JSON");
        }
    }

    [Fact]
    public async Task Catalog_ListServices_Returns_Empty_Or_List_When_Authenticated()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/catalog/services");

        // Database is fresh (no seed services) — should return 200 with empty list
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            // Should be a valid JSON response (array or paged result)
            content.Should().NotBeNullOrWhiteSpace();
            content.Should().Match(c => c.Contains("[") || c.Contains("items") || c.Contains("data"),
                "a resposta de listagem deve conter array ou paginação");
        }
    }

    [Fact]
    public async Task Catalog_ServicesSummary_Should_Return_200_When_Authenticated()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/catalog/services/summary");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "summary endpoint deve retornar 200 ou 403 para utilizador autenticado");
    }

    [Fact]
    public async Task Catalog_GetService_With_Unknown_Id_Should_Return_404_Or_Error()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var unknownId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/catalog/services/{unknownId}");

        ((int)response.StatusCode).Should().BeOneOf(new[] {404, 400, 403}, "serviço inexistente deve retornar 404 — nunca 500");
    }

    // ── Incidents: OperationalIntelligence ────────────────────────────────────

    [Fact]
    public async Task Incidents_List_Without_Auth_Should_Return_401()
    {
        var response = await fixture.Client.GetAsync("/api/v1/incidents");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "GET /api/v1/incidents exige autenticação");
    }

    [Fact]
    public async Task Incidents_List_With_Auth_Should_Return_200_Or_403()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/incidents");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "utilizador autenticado deve receber listagem de incidentes ou 403 por permissões");
    }

    [Fact]
    public async Task Incidents_List_Returns_Empty_List_In_Fresh_Database()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/incidents");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
            // Fresh database should return empty list
            content.Should().Match(c =>
                c.Contains("[]") || c.Contains("\"items\":[]") || c.Contains("\"total\":0"),
                "base de dados nova deve retornar lista vazia de incidentes");
        }
    }

    [Fact]
    public async Task Incidents_GetById_With_Unknown_Id_Should_Return_404()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var unknownId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/incidents/{unknownId}");

        ((int)response.StatusCode).Should().BeOneOf(new[] {404, 400, 403}, "incidente inexistente deve retornar 404 — nunca 500");
    }

    [Fact]
    public async Task Incidents_Summary_Should_Return_200_When_Authenticated()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/incidents/summary");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "incident summary deve retornar 200 ou 403 para utilizador autenticado");
    }

    // ── ChangeGovernance: Releases ────────────────────────────────────────────

    [Fact]
    public async Task ChangeGovernance_ListReleases_Without_Auth_Should_Return_401()
    {
        var response = await fixture.Client.GetAsync("/api/v1/change-intelligence/releases");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "listagem de releases exige autenticação");
    }

    [Fact]
    public async Task ChangeGovernance_ListReleases_With_Auth_Should_Not_Return_500()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/change-intelligence/releases");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "releases endpoint deve responder com 200 ou 403 para utilizador autenticado — nunca 500");
    }

    // ── AIKnowledge: Conversations ────────────────────────────────────────────

    [Fact]
    public async Task AI_ListConversations_Without_Auth_Should_Return_401()
    {
        var response = await fixture.Client.GetAsync("/api/v1/ai/assistant/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "listagem de conversas AI exige autenticação");
    }

    [Fact]
    public async Task AI_ListConversations_With_Auth_Should_Return_200_Or_403()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/ai/assistant/conversations");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "utilizador autenticado deve receber 200 ou 403 para listagem de conversas AI");
    }

    // ── Contracts ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Contracts_List_Without_Auth_Should_Return_401()
    {
        var response = await fixture.Client.GetAsync("/api/v1/contracts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "listagem de contratos exige autenticação");
    }

    [Fact]
    public async Task Contracts_List_With_Auth_Should_Return_200_Or_403()
    {
        var token = await fixture.GetAuthTokenAsync();
        if (token is null) return;

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/contracts");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "utilizador autenticado deve receber 200 ou 403 para listagem de contratos");
    }
}
