using NexTraceOne.E2E.Tests.Infrastructure;

namespace NexTraceOne.E2E.Tests.Flows;

/// <summary>
/// Fluxos E2E HTTP reais orientados a negócio.
/// Exercitam backend completo + PostgreSQL real + autenticação real sem mocks,
/// cobrindo os percursos RH-6 mais críticos que antes não tinham prova forte.
/// </summary>
[Collection(ApiE2ECollection.Name)]
public sealed class RealBusinessApiFlowTests(ApiE2EFixture fixture)
{
    private static readonly Guid PaymentsServiceId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid OrdersApiAssetId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid PaymentsReleaseId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid SeedIncidentId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private const string SeedIncidentExternalRef = "INC-2026-0042";
    private const string SeedAdminEmail = "admin@nextraceone.dev";
    private const string SeedAdminPassword = "Admin@123";

    [Fact]
    public async Task Catalog_Should_List_Seeded_Services_And_Return_Service_Detail()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var listResponse = await client.GetAsync("/api/v1/catalog/services?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listPayload = await listResponse.Content.ReadAsStringAsync();
        listPayload.Should().Contain("Payments Service");
        listPayload.Should().Contain("Orders Service");

        var detailResponse = await client.GetAsync($"/api/v1/catalog/services/{PaymentsServiceId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailPayload = await detailResponse.Content.ReadAsStringAsync();
        detailPayload.Should().Contain("Payments Service");
        detailPayload.Should().Contain("Finance");
    }

    [Fact]
    public async Task Contracts_Should_Create_Edit_And_Submit_Draft_With_Real_Backend()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var draftTitle = $"RH6 Draft {suffix}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/contracts/drafts", new
        {
            title = draftTitle,
            author = SeedAdminEmail,
            contractType = "RestApi",
            protocol = "OpenApi",
            description = "Real RH-6 contract draft"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdDraftId = await ExtractGuidAsync(createResponse, "draftId");

        var listDraftsResponse = await client.GetAsync($"/api/v1/contracts/drafts?status=Editing&author={Uri.EscapeDataString(SeedAdminEmail)}&page=1&pageSize=20");
        listDraftsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDraftsPayload = await listDraftsResponse.Content.ReadAsStringAsync();
        listDraftsPayload.Should().Contain(draftTitle);
        var persistedDraftId = await ExtractFirstGuidFromItemsAsync(listDraftsResponse, "draftId");

        createdDraftId.Should().Be(persistedDraftId);

        var getDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{persistedDraftId}");
        getDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await getDraftResponse.Content.ReadAsStringAsync()).Should().Contain(draftTitle);

        var updateContentResponse = await client.PatchAsJsonAsync($"/api/v1/contracts/drafts/{persistedDraftId}/content", new
        {
            draftId = persistedDraftId,
            specContent =
                """
                openapi: 3.0.0
                info:
                  title: RH6 Contracts API
                  version: 1.0.0
                paths:
                  /health:
                    get:
                      summary: Health check
                      responses:
                        '200':
                          description: OK
                """,
            format = "yaml",
            editedBy = SeedAdminEmail
        });

        updateContentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateMetadataResponse = await client.PatchAsJsonAsync($"/api/v1/contracts/drafts/{persistedDraftId}/metadata", new
        {
            draftId = persistedDraftId,
            title = $"RH6 Draft {suffix} Updated",
            description = "Updated via real API E2E flow",
            proposedVersion = "1.0.1",
            serviceId = OrdersApiAssetId,
            editedBy = SeedAdminEmail
        });

        updateMetadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reloadDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{persistedDraftId}");
        reloadDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloadDraftPayload = await reloadDraftResponse.Content.ReadAsStringAsync();
        reloadDraftPayload.Should().Contain("RH6 Contracts API");
        reloadDraftPayload.Should().Contain($"RH6 Draft {suffix} Updated");
        reloadDraftPayload.Should().Contain("1.0.1");

        var submitResponse = await client.PostAsync($"/api/v1/contracts/drafts/{persistedDraftId}/submit-review", content: null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submittedDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{persistedDraftId}");
        submittedDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await submittedDraftResponse.Content.ReadAsStringAsync()).Should().Contain("InReview");
    }

    [Fact]
    public async Task ChangeGovernance_Should_List_Seeded_Releases_And_Start_Review()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var listResponse = await client.GetAsync($"/api/v1/releases?apiAssetId={OrdersApiAssetId}&page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listPayload = await listResponse.Content.ReadAsStringAsync();
        listPayload.Should().Contain("Orders Service");
        listPayload.Should().Contain("1.3.0");

        var intelligenceResponse = await client.GetAsync($"/api/v1/releases/{PaymentsReleaseId}/intelligence");
        intelligenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await intelligenceResponse.Content.ReadAsStringAsync()).Should().Contain("Payments Service");

        var startReviewResponse = await client.PostAsync($"/api/v1/releases/{PaymentsReleaseId}/review/start", content: null);
        startReviewResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);

        var refreshedIntelligenceResponse = await client.GetAsync($"/api/v1/releases/{PaymentsReleaseId}/intelligence");
        refreshedIntelligenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await refreshedIntelligenceResponse.Content.ReadAsStringAsync()).Should().Contain("Payments Service");
    }

    [Fact]
    public async Task Incidents_Should_List_Seeded_Detail_And_Create_New_Incident()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var listResponse = await client.GetAsync("/api/v1/incidents?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listPayload = await listResponse.Content.ReadAsStringAsync();
        listPayload.Should().Contain("Payment Gateway");
        listPayload.Should().Contain(SeedIncidentExternalRef);

        var detailResponse = await client.GetAsync($"/api/v1/incidents/{SeedIncidentId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await detailResponse.Content.ReadAsStringAsync()).Should().Contain("Payment Gateway");

        var createResponse = await client.PostAsJsonAsync("/api/v1/incidents", new
        {
            title = $"RH6 Incident {Guid.NewGuid():N}"[..20],
            description = "Created from RH-6 real API E2E flow",
            incidentType = "ServiceDegradation",
            severity = "Major",
            serviceId = "svc-rh6-api",
            serviceDisplayName = "RH6 API Service",
            ownerTeam = "platform-core",
            impactedDomain = "Platform",
            environment = "Production",
            detectedAtUtc = DateTimeOffset.UtcNow
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdIncidentId = await ExtractGuidAsync(createResponse, "incidentId");

        var createdIncidentDetailResponse = await client.GetAsync($"/api/v1/incidents/{createdIncidentId}");
        createdIncidentDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createdIncidentPayload = await createdIncidentDetailResponse.Content.ReadAsStringAsync();
        createdIncidentPayload.Should().Contain("RH6 API Service");
        createdIncidentPayload.Should().Contain("platform-core");
    }

    [Fact]
    public async Task AI_Should_Create_Conversation_Send_Message_And_List_Persisted_Messages()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var createConversationResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/conversations", new
        {
            title = $"RH6 AI Conversation {Guid.NewGuid():N}"[..24],
            persona = "Engineer",
            clientType = "Web",
            defaultContextScope = "services,incidents,changes"
        });

        createConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdConversationId = await ExtractGuidAsync(createConversationResponse, "conversationId");

        var sendMessageResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/chat", new
        {
            message = "Summarize the likely operational risk for Payments Service in production.",
            contextScope = "services,changes,incidents",
            persona = "Engineer",
            clientType = "Web"
        });

        sendMessageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sendMessagePayload = await sendMessageResponse.Content.ReadAsStringAsync();
        sendMessagePayload.Should().Match(payload =>
            payload.Contains("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.Ordinal)
            || payload.Contains("assistantResponse", StringComparison.OrdinalIgnoreCase)
            || payload.Contains("content", StringComparison.OrdinalIgnoreCase)
            || payload.Contains("message", StringComparison.OrdinalIgnoreCase));

        var responseConversationId = await ExtractGuidAsync(sendMessageResponse, "conversationId");
        responseConversationId.Should().NotBe(Guid.Empty);

        var listMessagesResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{responseConversationId}/messages?pageSize=50");
        listMessagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listMessagesPayload = await listMessagesResponse.Content.ReadAsStringAsync();
        listMessagesPayload.Should().Contain("Summarize the likely operational risk");
        listMessagesPayload.Should().Match(payload =>
            payload.Contains("assistant", StringComparison.OrdinalIgnoreCase)
            || payload.Contains("user", StringComparison.OrdinalIgnoreCase));

        var createdConversationResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}?messagePageSize=20");
        createdConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(SeedAdminEmail, SeedAdminPassword);

        var conversationTitle = $"RH6 AI Conversation {Guid.NewGuid():N}"[..24];

        var createConversationResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/conversations", new
        {
            title = conversationTitle,
            persona = "Engineer",
            clientType = "Web",
            defaultContextScope = "services,incidents,changes"
        });

        createConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdConversationId = await ExtractGuidAsync(createConversationResponse, "conversationId");

        var openConversationResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}?messagePageSize=20");
        openConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await openConversationResponse.Content.ReadAsStringAsync()).Should().Contain(conversationTitle);

        var sendMessageResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/chat", new
        {
            conversationId = createdConversationId,
            message = "Summarize the likely operational risk for Payments Service in production.",
            contextScope = "services,changes,incidents",
            persona = "Engineer",
            clientType = "Web"
        });

        sendMessageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var sendDocument = JsonDocument.Parse(await sendMessageResponse.Content.ReadAsStringAsync());
        var sendPayload = sendDocument.RootElement;
        sendPayload.GetProperty("conversationId").GetGuid().Should().Be(createdConversationId);
        sendPayload.GetProperty("userMessageId").GetGuid().Should().NotBe(Guid.Empty);
        sendPayload.GetProperty("messageId").GetGuid().Should().NotBe(Guid.Empty);
        sendPayload.GetProperty("responseState").GetString().Should().BeOneOf("Completed", "Degraded");
        sendPayload.GetProperty("assistantResponse").GetString().Should().NotBeNullOrWhiteSpace();

        var listConversationsResponse = await client.GetAsync("/api/v1/ai/assistant/conversations?pageSize=20");
        listConversationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await listConversationsResponse.Content.ReadAsStringAsync()).Should().Contain(conversationTitle);

        var listMessagesResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}/messages?pageSize=20");
        listMessagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listMessagesDocument = JsonDocument.Parse(await listMessagesResponse.Content.ReadAsStringAsync());
        var items = listMessagesDocument.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(2);
        items[0].GetProperty("role").GetString().Should().Be("user");
        items[1].GetProperty("role").GetString().Should().Be("assistant");
        items[0].GetProperty("content").GetString().Should().Contain("Payments Service");
        items[1].GetProperty("responseState").GetString().Should().BeOneOf("Completed", "Degraded");

        var reopenedConversationResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}?messagePageSize=20");
        reopenedConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopenedPayload = await reopenedConversationResponse.Content.ReadAsStringAsync();
        reopenedPayload.Should().Contain(conversationTitle);
        reopenedPayload.Should().Contain("Payments Service");
        reopenedPayload.Should().Contain("responseState");
    }

    private static async Task<Guid> ExtractGuidAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        if (TryReadGuid(document.RootElement, propertyName, out var value) || TryReadGuid(document.RootElement, "id", out value))
            return value;

        if (document.RootElement.TryGetProperty("data", out var dataElement)
            && (TryReadGuid(dataElement, propertyName, out value) || TryReadGuid(dataElement, "id", out value)))
        {
            return value;
        }

        throw new InvalidOperationException($"Could not extract GUID property '{propertyName}' from response.");
    }

    private static Guid? ExtractGuidFromLocation(HttpResponseMessage response)
    {
        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(location))
            return null;

        var lastSegment = location.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return Guid.TryParse(lastSegment, out var value) ? value : null;
    }

    private static bool TryReadGuid(JsonElement element, string propertyName, out Guid value)
    {
        value = Guid.Empty;

        foreach (var candidate in GetCandidatePropertyNames(propertyName))
        {
            if (!element.TryGetProperty(candidate, out var property))
                continue;

            if (property.ValueKind == JsonValueKind.String
                && Guid.TryParse(property.GetString(), out value))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCandidatePropertyNames(string propertyName)
    {
        yield return propertyName;
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            yield return char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        }
    }

    private static async Task<Guid> ExtractFirstGuidFromItemsAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!document.RootElement.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Could not extract items array from response.");
        }

        foreach (var item in items.EnumerateArray())
        {
            if (TryReadGuid(item, propertyName, out var value) || TryReadGuid(item, "id", out value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Could not extract GUID property '{propertyName}' from response items.");
    }
}
