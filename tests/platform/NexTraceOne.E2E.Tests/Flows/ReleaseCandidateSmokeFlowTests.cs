using NexTraceOne.E2E.Tests.Infrastructure;

namespace NexTraceOne.E2E.Tests.Flows;

/// <summary>
/// Smoke tests reais e rápidos para gate de build/release candidate.
/// Exercitam o caminho feliz mínimo dos módulos produtivos com backend real.
/// </summary>
[Collection(ApiE2ECollection.Name)]
public sealed class ReleaseCandidateSmokeFlowTests(ApiE2EFixture fixture)
{
    [Fact]
    public async Task Smoke_Health_Should_Return_200()
    {
        var response = await fixture.Client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Smoke_Login_And_Current_User_Should_Work()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/identity/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain(ApiE2EFixture.E2EAdminEmail);
    }

    [Fact]
    public async Task Smoke_Catalog_And_SourceOfTruth_Should_Return_Seeded_Data()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();

        var catalogResponse = await client.GetAsync("/api/v1/catalog/services?page=1&pageSize=10");
        catalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await catalogResponse.Content.ReadAsStringAsync()).Should().Contain("Payments Service");

        var sourceOfTruthResponse = await client.GetAsync("/api/v1/source-of-truth/search?q=Payments&scope=services&maxResults=10");
        sourceOfTruthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await sourceOfTruthResponse.Content.ReadAsStringAsync()).Should().Contain("Payments Service");
    }

    [Fact]
    public async Task Smoke_Contracts_Should_Return_Summary()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/v1/contracts/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("totalVersions");
    }

    [Fact]
    public async Task Smoke_ChangeGovernance_And_Incidents_Should_Return_Core_Data()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();

        var releasesResponse = await client.GetAsync("/api/v1/releases?apiAssetId=d0000000-0000-0000-0000-000000000001&page=1&pageSize=10");
        releasesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await releasesResponse.Content.ReadAsStringAsync()).Should().Contain("1.3.0");

        var incidentsResponse = await client.GetAsync("/api/v1/incidents?page=1&pageSize=10");
        incidentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await incidentsResponse.Content.ReadAsStringAsync()).Should().Contain("INC-2026-0042");
    }

    [Fact]
    public async Task Smoke_Audit_Should_Search_And_Verify_Chain()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();

        var searchResponse = await client.GetAsync("/api/v1/audit/search?page=1&pageSize=20");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await searchResponse.Content.ReadAsStringAsync()).Should().Contain("items");

        var verifyResponse = await client.GetAsync("/api/v1/audit/verify-chain");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await verifyResponse.Content.ReadAsStringAsync()).Should().MatchRegex("\"(isIntact|IsIntact)\"");
    }

    [Fact]
    public async Task Smoke_AI_Assistant_Should_Create_Conversation()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/ai/assistant/conversations", new
        {
            title = $"Smoke AI {Guid.NewGuid():N}"[..17],
            persona = "Engineer",
            clientType = "Web",
            defaultContextScope = "services,incidents",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().MatchRegex("\"(conversationId|ConversationId)\"");
    }
}
