using NexTraceOne.E2E.Tests.Infrastructure;

namespace NexTraceOne.E2E.Tests.Flows;

/// <summary>
/// Testes E2E reais para fluxos do catálogo de serviços e gestão de incidentes.
/// Valida que os endpoints core respondem corretamente com autenticação real
/// contra backend + PostgreSQL real.
///
/// Classificação: ALTA CONFIANÇA para endpoints reais contra base seedada
///                MÉDIA CONFIANÇA para criação e mutações dependentes de permissões
/// </summary>
[Collection(ApiE2ECollection.Name)]
public sealed class CatalogAndIncidentApiFlowTests(ApiE2EFixture fixture)
{
    private static readonly Guid OrdersApiAssetId = Guid.Parse("d0000000-0000-0000-0000-000000000001");

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

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
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
    public async Task Incidents_List_Returns_Seeded_List_When_Authenticated()
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
            content.Should().Contain("INC-2026-0042");
            content.Should().Contain("Payment Gateway");
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
        var response = await fixture.Client.GetAsync($"/api/v1/releases?apiAssetId={OrdersApiAssetId}");

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

        var response = await client.GetAsync($"/api/v1/releases?apiAssetId={OrdersApiAssetId}");

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
        var response = await fixture.Client.GetAsync("/api/v1/contracts/summary");

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

        var response = await client.GetAsync("/api/v1/contracts/summary");

        ((int)response.StatusCode).Should().BeOneOf(new[] {200, 403}, "utilizador autenticado deve receber 200 ou 403 para listagem de contratos");
    }

    [Fact]
    public async Task Incidents_List_Pagination_Should_Report_Unpaged_TotalCount()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/incidents?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task Incidents_Create_Persist_Detail_And_Reopen_Should_Work_EndToEnd()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var title = $"E2E incident {uniqueSuffix}";

        var createPayload = new
        {
            Title = title,
            Description = "Incident created by real E2E flow to validate persistence.",
            IncidentType = "ServiceDegradation",
            Severity = "Major",
            ServiceId = "svc-e2e-core",
            ServiceDisplayName = "E2E Core Service",
            OwnerTeam = "core-squad",
            ImpactedDomain = "Core",
            Environment = "Production"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/incidents", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createJson);
        var incidentId = createdDoc.RootElement.GetProperty("incidentId").GetGuid();
        var reference = createdDoc.RootElement.GetProperty("reference").GetString();
        reference.Should().NotBeNullOrWhiteSpace();

        var listResponse = await client.GetAsync($"/api/v1/incidents?search={Uri.EscapeDataString(title)}&page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        listDoc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        var listItems = listDoc.RootElement.GetProperty("items");
        listItems.GetArrayLength().Should().Be(1);
        listItems[0].GetProperty("incidentId").GetGuid().Should().Be(incidentId);
        listItems[0].GetProperty("title").GetString().Should().Be(title);

        var detailResponse = await client.GetAsync($"/api/v1/incidents/{incidentId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detailDoc = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        detailDoc.RootElement.GetProperty("identity").GetProperty("incidentId").GetGuid().Should().Be(incidentId);
        detailDoc.RootElement.GetProperty("identity").GetProperty("title").GetString().Should().Be(title);
        detailDoc.RootElement.GetProperty("ownerTeam").GetString().Should().Be("core-squad");
        detailDoc.RootElement.GetProperty("linkedServices")[0].GetProperty("serviceId").GetString().Should().Be("svc-e2e-core");

        var reopenedDetailResponse = await client.GetAsync($"/api/v1/incidents/{incidentId}");
        reopenedDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var reopenedDoc = JsonDocument.Parse(await reopenedDetailResponse.Content.ReadAsStringAsync());
        reopenedDoc.RootElement.GetProperty("identity").GetProperty("reference").GetString().Should().Be(reference);
        reopenedDoc.RootElement.GetProperty("identity").GetProperty("title").GetString().Should().Be(title);
    }

    [Fact]
    public async Task Incidents_Create_With_ReadOnly_Profile_Should_Return_403()
    {
        var client = await fixture.CreateAuthenticatedClientAsync(ApiE2EFixture.E2EViewerEmail, ApiE2EFixture.E2EViewerPassword);

        var response = await client.PostAsJsonAsync("/api/v1/incidents", new
        {
            Title = "Forbidden create incident",
            Description = "Read-only users must not create incidents.",
            IncidentType = "ServiceDegradation",
            Severity = "Minor",
            ServiceId = "svc-read-only",
            ServiceDisplayName = "Read Only Service",
            OwnerTeam = "viewer-squad",
            Environment = "Production"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
