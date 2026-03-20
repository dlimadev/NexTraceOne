using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using NexTraceOne.IntegrationTests.Infrastructure;

namespace NexTraceOne.IntegrationTests.CriticalFlows;

/// <summary>
/// RH-6 — testes de integração reais no nível de host.
/// Validam handlers/endpoints centrais do produto contra ApiHost + PostgreSQL real,
/// sem mocks de repositório e sem banco in-memory.
/// </summary>
[Collection(ApiHostPostgreSqlCollection.Name)]
public sealed class CoreApiHostIntegrationTests(ApiHostPostgreSqlFixture fixture)
{
    private static readonly Guid PaymentsServiceId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid OrdersApiAssetId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid PaymentsReleaseId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid SeedIncidentId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid SeedAdminUserId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    private static readonly Guid SeedConversationId = Guid.Parse("f0000000-0000-0000-0000-000000000001");
    private const string SeedAuditorEmail = "auditor@nextraceone.dev";
    private const string SeedAuditorPassword = "Admin@123";

    [Fact]
    public async Task IdentityAccess_Should_Login_ListTenants_And_SelectTenant_And_Use_Real_CookieSession()
    {
        var login = await fixture.LoginAsync(ApiHostPostgreSqlFixture.SeedAdminEmail, ApiHostPostgreSqlFixture.SeedAdminPassword);
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.RefreshToken.Should().NotBeNullOrWhiteSpace();
        login.TenantId.Should().Be(ApiHostPostgreSqlFixture.NexTraceCorpTenantId);

        using var authenticatedClient = fixture.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

        var listTenantsResponse = await authenticatedClient.GetAsync("/api/v1/identity/tenants/mine");
        listTenantsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantsPayload = await listTenantsResponse.Content.ReadAsStringAsync();
        tenantsPayload.Should().Contain("NexTrace Corp");
        tenantsPayload.Should().Contain("Acme Fintech");

        var selectTenantResponse = await authenticatedClient.PostAsJsonAsync(
            "/api/v1/identity/auth/select-tenant",
            new { tenantId = ApiHostPostgreSqlFixture.AcmeFintechTenantId });
        selectTenantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var selectTenantPayload = await selectTenantResponse.Content.ReadAsStringAsync();
        selectTenantPayload.Should().Contain(ApiHostPostgreSqlFixture.AcmeFintechTenantId.ToString());
        selectTenantPayload.Should().Contain("Acme Fintech");

        using var cookieClient = fixture.CreateCookieClient();
        var cookieLoginResponse = await cookieClient.PostAsJsonAsync("/api/v1/identity/auth/cookie-session", new
        {
            Email = ApiHostPostgreSqlFixture.SeedAdminEmail,
            Password = ApiHostPostgreSqlFixture.SeedAdminPassword,
        });
        cookieLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var cookieDocument = JsonDocument.Parse(await cookieLoginResponse.Content.ReadAsStringAsync());
        var cookiePayload = cookieDocument.RootElement.TryGetProperty("data", out var nestedCookie)
            ? nestedCookie
            : cookieDocument.RootElement;
        var loginCsrfToken = cookiePayload.GetProperty("csrfToken").GetString();
        loginCsrfToken.Should().NotBeNullOrWhiteSpace();

        var csrfResponse = await cookieClient.GetAsync("/api/v1/identity/auth/cookie-session/csrf-token");
        csrfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var csrfDocument = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var logoutCsrfToken = csrfDocument.RootElement.GetProperty("csrfToken").GetString();
        logoutCsrfToken.Should().NotBeNullOrWhiteSpace();

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/identity/auth/cookie-session");
        logoutRequest.Headers.Add("X-Csrf-Token", logoutCsrfToken);
        var logoutResponse = await cookieClient.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IdentityAccess_Should_Get_Current_User_And_List_Tenant_Users_With_Real_Backend()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var meResponse = await client.GetAsync("/api/v1/identity/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mePayload = await meResponse.Content.ReadAsStringAsync();
        mePayload.Should().Contain(ApiHostPostgreSqlFixture.SeedAdminEmail);
        mePayload.Should().Contain(ApiHostPostgreSqlFixture.NexTraceCorpTenantId.ToString());

        var usersResponse = await client.GetAsync($"/api/v1/identity/tenants/{ApiHostPostgreSqlFixture.NexTraceCorpTenantId}/users?page=1&pageSize=20");
        usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var usersPayload = await usersResponse.Content.ReadAsStringAsync();
        usersPayload.Should().Contain(ApiHostPostgreSqlFixture.SeedAdminEmail);
        usersPayload.Should().Contain(ApiHostPostgreSqlFixture.SeedDeveloperEmail);
    }

    [Fact]
    public async Task IdentityAccess_Should_Enforce_Minimal_Permissions_With_Real_Authorization()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedDeveloperEmail,
            ApiHostPostgreSqlFixture.SeedDeveloperPassword);

        var response = await client.PutAsync($"/api/v1/identity/users/{SeedAdminUserId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "o utilizador Developer não deve conseguir executar mutações administrativas de utilizadores");
    }

    [Fact]
    public async Task Catalog_And_SourceOfTruth_Should_List_Services_Get_Detail_Search_And_Expose_Real_Contract_Catalog_Summary()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var servicesResponse = await client.GetAsync("/api/v1/catalog/services?page=1&pageSize=20");
        servicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var servicesPayload = await servicesResponse.Content.ReadAsStringAsync();
        servicesPayload.Should().Contain("Payments Service");
        servicesPayload.Should().Contain("Orders Service");

        var detailResponse = await client.GetAsync($"/api/v1/catalog/services/{PaymentsServiceId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailPayload = await detailResponse.Content.ReadAsStringAsync();
        detailPayload.Should().Contain("Payments Service");
        detailPayload.Should().Contain("Finance");

        var sourceOfTruthSearchResponse = await client.GetAsync("/api/v1/source-of-truth/search?q=Payments&scope=services&maxResults=10");
        sourceOfTruthSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sourceOfTruthSearchPayload = await sourceOfTruthSearchResponse.Content.ReadAsStringAsync();
        sourceOfTruthSearchPayload.Should().Contain("Payments Service");

        var serviceSourceOfTruthResponse = await client.GetAsync($"/api/v1/source-of-truth/services/{PaymentsServiceId}");
        serviceSourceOfTruthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var serviceSourceOfTruthPayload = await serviceSourceOfTruthResponse.Content.ReadAsStringAsync();
        serviceSourceOfTruthPayload.Should().Contain("Payments Service");
        serviceSourceOfTruthPayload.Should().Contain("Finance");

        var contractsListResponse = await client.GetAsync("/api/v1/contracts/list?page=1&pageSize=20");
        contractsListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contractsListPayload = await contractsListResponse.Content.ReadAsStringAsync();
        contractsListPayload.Should().Contain("Orders API");

        var contractVersionId = await ExtractFirstGuidFromItemsAsync(contractsListResponse, "versionId");
        var contractDetailResponse = await client.GetAsync($"/api/v1/contracts/{contractVersionId}/detail");
        contractDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contractDetailPayload = await contractDetailResponse.Content.ReadAsStringAsync();
        contractDetailPayload.Should().Contain("apiName");
        contractDetailPayload.Should().Contain("routePattern");
        contractDetailPayload.Should().Contain("technicalOwner");

        var contractSourceOfTruthResponse = await client.GetAsync($"/api/v1/source-of-truth/contracts/{contractVersionId}");
        contractSourceOfTruthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contractSourceOfTruthPayload = await contractSourceOfTruthResponse.Content.ReadAsStringAsync();
        contractSourceOfTruthPayload.Should().Contain("OpenApi");
        contractSourceOfTruthPayload.Should().Contain("Approved");

        var globalSearchResponse = await client.GetAsync("/api/v1/source-of-truth/global-search?q=Orders&persona=Engineer&maxResults=10");
        globalSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await globalSearchResponse.Content.ReadAsStringAsync()).Should().Contain("Orders");

        var contractsResponse = await client.GetAsync("/api/v1/contracts/summary");
        contractsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contractsPayload = await contractsResponse.Content.ReadAsStringAsync();
        contractsPayload.Should().Contain("totalVersions");
        contractsPayload.Should().Contain("distinctContracts");
    }

    [Fact]
    public async Task Contracts_Should_Create_Update_Submit_Approve_Publish_And_Reopen_With_Real_Backend()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var draftTitle = $"RH6 Integration Draft {suffix}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/contracts/drafts", new
        {
            title = draftTitle,
            author = ApiHostPostgreSqlFixture.SeedAdminEmail,
            contractType = "RestApi",
            protocol = "OpenApi",
            description = "Created by RH-6 integration suite",
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdDraftId = await ExtractGuidAsync(createResponse, "draftId");

        var listDraftsResponse = await client.GetAsync($"/api/v1/contracts/drafts?status=Editing&author={Uri.EscapeDataString(ApiHostPostgreSqlFixture.SeedAdminEmail)}&page=1&pageSize=20");
        listDraftsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDraftsPayload = await listDraftsResponse.Content.ReadAsStringAsync();
        listDraftsPayload.Should().Contain(draftTitle);
        var draftId = await ExtractFirstGuidFromItemsAsync(listDraftsResponse, "draftId");

        createdDraftId.Should().Be(draftId);

        var getDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{draftId}");
        getDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdDraftPayload = await getDraftResponse.Content.ReadAsStringAsync();
        createdDraftPayload.Should().Contain(draftTitle);
        createdDraftPayload.Should().Contain("createdAt", Exactly.Once());

        var updateContentResponse = await client.PatchAsJsonAsync($"/api/v1/contracts/drafts/{draftId}/content", new
        {
            draftId,
            specContent = """
                          openapi: 3.0.0
                          info:
                            title: RH6 Integration Contract
                            version: 1.0.0
                          paths:
                            /health:
                              get:
                                summary: Health
                                responses:
                                  '200':
                                    description: OK
                          """,
            format = "yaml",
            editedBy = ApiHostPostgreSqlFixture.SeedAdminEmail,
        });
        updateContentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateMetadataResponse = await client.PatchAsJsonAsync($"/api/v1/contracts/drafts/{draftId}/metadata", new
        {
            draftId,
            title = $"{draftTitle} Updated",
            description = "Updated in RH-6 integration suite",
            proposedVersion = "1.0.1",
            serviceId = PaymentsServiceId,
            editedBy = ApiHostPostgreSqlFixture.SeedAdminEmail,
        });
        updateMetadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reloadDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{draftId}");
        reloadDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloadDraftPayload = await reloadDraftResponse.Content.ReadAsStringAsync();
        reloadDraftPayload.Should().Contain("RH6 Integration Contract");
        reloadDraftPayload.Should().Contain($"{draftTitle} Updated");
        reloadDraftPayload.Should().Contain("1.0.1");
        reloadDraftPayload.Should().Contain("lastEditedAt");
        reloadDraftPayload.Should().Contain(PaymentsServiceId.ToString());

        var submitResponse = await client.PostAsync($"/api/v1/contracts/drafts/{draftId}/submit-review", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approveResponse = await client.PostAsJsonAsync($"/api/v1/contracts/drafts/{draftId}/approve", new
        {
            approvedBy = ApiHostPostgreSqlFixture.SeedAdminEmail,
            comment = "Approved by RH-6 integration suite",
        });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishResponse = await client.PostAsJsonAsync($"/api/v1/contracts/drafts/{draftId}/publish", new
        {
            publishedBy = ApiHostPostgreSqlFixture.SeedAdminEmail,
        });
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contractVersionId = await ExtractGuidAsync(publishResponse, "contractVersionId");

        var submittedDraftResponse = await client.GetAsync($"/api/v1/contracts/drafts/{draftId}");
        submittedDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await submittedDraftResponse.Content.ReadAsStringAsync()).Should().Contain("Published");

        var detailResponse = await client.GetAsync($"/api/v1/contracts/{contractVersionId}/detail");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailPayload = await detailResponse.Content.ReadAsStringAsync();
        detailPayload.Should().Contain($"{draftTitle} Updated");
        detailPayload.Should().Contain("Payments Service");
        detailPayload.Should().Contain("Finance");
        detailPayload.Should().Contain("Team Beta");
        detailPayload.Should().Contain("1.0.1");

        var contractsByServiceResponse = await client.GetAsync($"/api/v1/contracts/by-service/{PaymentsServiceId}");
        contractsByServiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await contractsByServiceResponse.Content.ReadAsStringAsync()).Should().Contain("1.0.1");

        var reviewsResponse = await client.GetAsync($"/api/v1/contracts/drafts/{draftId}/reviews");
        reviewsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reviewsResponse.Content.ReadAsStringAsync()).Should().Contain("Approved");
    }

    [Fact]
    public async Task PreviewOnly_Governance_And_DeveloperPortal_Endpoints_Should_Be_Removed_From_Final_Product_Surface()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var simulateResponse = await client.PostAsync($"/api/v1/governance/packs/{Guid.NewGuid():D}/simulate", null);
        simulateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var playgroundResponse = await client.PostAsync("/api/v1/developerportal/playground/execute", null);
        playgroundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeGovernance_And_Incidents_Should_Expose_Real_Read_Write_And_Correlation_Flows()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var releasesResponse = await client.GetAsync($"/api/v1/releases?apiAssetId={OrdersApiAssetId}&page=1&pageSize=20");
        releasesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var releasesPayload = await releasesResponse.Content.ReadAsStringAsync();
        releasesPayload.Should().Contain("Orders Service");
        releasesPayload.Should().Contain("1.3.0");

        var intelligenceResponse = await client.GetAsync($"/api/v1/releases/{PaymentsReleaseId}/intelligence");
        intelligenceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await intelligenceResponse.Content.ReadAsStringAsync()).Should().Contain("Payments Service");

        var reviewStartResponse = await client.PostAsync($"/api/v1/releases/{PaymentsReleaseId}/review/start", null);
        reviewStartResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);

        var incidentsResponse = await client.GetAsync("/api/v1/incidents?page=1&pageSize=20");
        incidentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var incidentsPayload = await incidentsResponse.Content.ReadAsStringAsync();
        incidentsPayload.Should().Contain("INC-2026-0042");
        incidentsPayload.Should().Contain("Payment Gateway");

        var detailResponse = await client.GetAsync($"/api/v1/incidents/{SeedIncidentId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var correlationResponse = await client.GetAsync($"/api/v1/incidents/{SeedIncidentId}/correlation");
        correlationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await correlationResponse.Content.ReadAsStringAsync())
            .Should().Contain("Payment Gateway");

        var createIncidentResponse = await client.PostAsJsonAsync("/api/v1/incidents", new
        {
            title = $"RH6 Incident {Guid.NewGuid():N}"[..20],
            description = "Created by RH-6 integration suite",
            incidentType = "ServiceDegradation",
            severity = "Major",
            serviceId = "svc-rh6-integration",
            serviceDisplayName = "RH6 Integration Service",
            ownerTeam = "platform-core",
            impactedDomain = "Platform",
            environment = "Production",
            detectedAtUtc = DateTimeOffset.UtcNow,
        });
        createIncidentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation_With_Real_Backend()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var conversationTitle = $"RH6 Integration AI {Guid.NewGuid():N}"[..24];

        var createConversationResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/conversations", new
        {
            title = conversationTitle,
            persona = "Engineer",
            clientType = "Web",
            defaultContextScope = "services,incidents,changes",
        });
        createConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdConversationId = await ExtractGuidAsync(createConversationResponse, "conversationId");

        var openedConversationResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}?messagePageSize=20");
        openedConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await openedConversationResponse.Content.ReadAsStringAsync()).Should().Contain(conversationTitle);

        var sendMessageResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/chat", new
        {
            conversationId = createdConversationId,
            message = "Summarize the operational risk for Payments Service in production.",
            contextScope = "services,changes,incidents",
            persona = "Engineer",
            clientType = "Web",
        });
        sendMessageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var sendDocument = JsonDocument.Parse(await sendMessageResponse.Content.ReadAsStringAsync());
        var sendPayload = sendDocument.RootElement;
        sendPayload.GetProperty("conversationId").GetGuid().Should().Be(createdConversationId);
        sendPayload.GetProperty("userMessageId").GetGuid().Should().NotBe(Guid.Empty);
        sendPayload.GetProperty("messageId").GetGuid().Should().NotBe(Guid.Empty);
        sendPayload.GetProperty("conversationMessageCount").GetInt32().Should().Be(2);
        sendPayload.GetProperty("responseState").GetString().Should().BeOneOf("Completed", "Degraded");
        sendPayload.GetProperty("assistantResponse").GetString().Should().NotBeNullOrWhiteSpace();
        sendPayload.GetProperty("assistantResponse").GetString().Should().NotContain("[FALLBACK_PROVIDER_UNAVAILABLE]");

        var listConversationsResponse = await client.GetAsync("/api/v1/ai/assistant/conversations?pageSize=20");
        listConversationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listConversationsPayload = await listConversationsResponse.Content.ReadAsStringAsync();
        listConversationsPayload.Should().Contain(conversationTitle);
        listConversationsPayload.Should().Contain("messageCount");

        var listMessagesResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}/messages?pageSize=20");
        listMessagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var listMessagesDocument = JsonDocument.Parse(await listMessagesResponse.Content.ReadAsStringAsync());
        var messagesRoot = listMessagesDocument.RootElement;
        var items = messagesRoot.GetProperty("items");
        items.GetArrayLength().Should().Be(2);
        items[0].GetProperty("role").GetString().Should().Be("user");
        items[1].GetProperty("role").GetString().Should().Be("assistant");
        items[0].GetProperty("content").GetString().Should().Contain("Payments Service");
        items[1].GetProperty("content").GetString().Should().NotContain("[FALLBACK_PROVIDER_UNAVAILABLE]");
        items[1].GetProperty("responseState").GetString().Should().BeOneOf("Completed", "Degraded");
        items[1].GetProperty("groundingSources").GetArrayLength().Should().BeGreaterThan(0);
        items[1].GetProperty("contextReferences").GetArrayLength().Should().BeGreaterThan(0);
        items[1].GetProperty("content").GetString().Should().Be(sendPayload.GetProperty("assistantResponse").GetString());

        var reopenedConversationResponse = await client.GetAsync($"/api/v1/ai/assistant/conversations/{createdConversationId}?messagePageSize=20");
        reopenedConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var reopenedDocument = JsonDocument.Parse(await reopenedConversationResponse.Content.ReadAsStringAsync());
        var reopenedRoot = reopenedDocument.RootElement;
        reopenedRoot.GetProperty("messageCount").GetInt32().Should().Be(2);
        reopenedRoot.GetProperty("messages")[1].GetProperty("content").GetString().Should().Be(sendPayload.GetProperty("assistantResponse").GetString());
        reopenedRoot.GetProperty("messages")[1].GetProperty("groundingSources").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AI_Should_Not_Silently_Create_A_New_Conversation_When_Send_Targets_Unknown_Id()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var missingConversationId = Guid.NewGuid();

        var sendMessageResponse = await client.PostAsJsonAsync("/api/v1/ai/assistant/chat", new
        {
            conversationId = missingConversationId,
            message = "This should fail instead of creating another conversation.",
            contextScope = "services",
            persona = "Engineer",
            clientType = "Web",
        });

        sendMessageResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AI_Should_Enforce_User_Scoped_Conversation_Access_For_List_Open_Messages_And_Send()
    {
        using var adminClient = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);
        using var developerClient = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedDeveloperEmail,
            ApiHostPostgreSqlFixture.SeedDeveloperPassword);

        var conversationTitle = $"Scoped AI {Guid.NewGuid():N}"[..20];

        var createConversationResponse = await adminClient.PostAsJsonAsync("/api/v1/ai/assistant/conversations", new
        {
            title = conversationTitle,
            persona = "Engineer",
            clientType = "Web",
            defaultContextScope = "services,incidents",
        });
        createConversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversationId = await ExtractGuidAsync(createConversationResponse, "conversationId");

        var developerListResponse = await developerClient.GetAsync("/api/v1/ai/assistant/conversations?pageSize=20");
        developerListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await developerListResponse.Content.ReadAsStringAsync()).Should().NotContain(conversationTitle);

        var developerOpenResponse = await developerClient.GetAsync($"/api/v1/ai/assistant/conversations/{conversationId}?messagePageSize=20");
        developerOpenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var developerMessagesResponse = await developerClient.GetAsync($"/api/v1/ai/assistant/conversations/{conversationId}/messages?pageSize=20");
        developerMessagesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var developerSendResponse = await developerClient.PostAsJsonAsync("/api/v1/ai/assistant/chat", new
        {
            conversationId,
            message = "Attempt to continue another user's conversation.",
            contextScope = "services",
            persona = "Engineer",
            clientType = "Web",
        });
        developerSendResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Incidents_Should_Create_Persist_List_Detail_And_Report_Real_TotalCount()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var title = $"ZR4 Incident {Guid.NewGuid():N}"[..20];

        var createResponse = await client.PostAsJsonAsync("/api/v1/incidents", new
        {
            title,
            description = "Created by ZR-4 integration suite",
            incidentType = "ServiceDegradation",
            severity = "Major",
            serviceId = "svc-zr4-incidents",
            serviceDisplayName = "ZR4 Incidents Service",
            ownerTeam = "platform-core",
            impactedDomain = "Platform",
            environment = "Production",
            detectedAtUtc = DateTimeOffset.UtcNow,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdIncidentId = await ExtractGuidAsync(createResponse, "incidentId");

        var listResponse = await client.GetAsync($"/api/v1/incidents?search={Uri.EscapeDataString(title)}&page=1&pageSize=1");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var listRoot = listDocument.RootElement.TryGetProperty("data", out var nestedList)
            ? nestedList
            : listDocument.RootElement;
        listRoot.GetProperty("totalCount").GetInt32().Should().Be(1);
        listRoot.GetProperty("page").GetInt32().Should().Be(1);
        listRoot.GetProperty("pageSize").GetInt32().Should().Be(1);
        var items = listRoot.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("incidentId").GetGuid().Should().Be(createdIncidentId);
        items[0].GetProperty("title").GetString().Should().Be(title);
        items[0].GetProperty("serviceDisplayName").GetString().Should().Be("ZR4 Incidents Service");

        var detailResponse = await client.GetAsync($"/api/v1/incidents/{createdIncidentId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var detailRoot = detailDocument.RootElement.TryGetProperty("data", out var nestedDetail)
            ? nestedDetail
            : detailDocument.RootElement;
        detailRoot.GetProperty("identity").GetProperty("incidentId").GetGuid().Should().Be(createdIncidentId);
        detailRoot.GetProperty("identity").GetProperty("title").GetString().Should().Be(title);
        detailRoot.GetProperty("ownerTeam").GetString().Should().Be("platform-core");
        detailRoot.GetProperty("impactedEnvironment").GetString().Should().Be("Production");
        detailRoot.GetProperty("linkedServices")[0].GetProperty("serviceId").GetString().Should().Be("svc-zr4-incidents");

        var reopenedResponse = await client.GetAsync($"/api/v1/incidents/{createdIncidentId}");
        reopenedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reopenedResponse.Content.ReadAsStringAsync()).Should().Contain(title);
    }

    [Fact]
    public async Task Incidents_Should_Return_Forbidden_For_ReadOnly_Profile_When_Creating()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            SeedAuditorEmail,
            SeedAuditorPassword);

        var response = await client.PostAsJsonAsync("/api/v1/incidents", new
        {
            title = $"ZR4 Forbidden {Guid.NewGuid():N}"[..20],
            description = "Read-only profiles must not create incidents.",
            incidentType = "ServiceDegradation",
            severity = "Minor",
            serviceId = "svc-zr4-readonly",
            serviceDisplayName = "ZR4 Read Only Service",
            ownerTeam = "audit-team",
            impactedDomain = "Audit",
            environment = "Production",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Audit_Should_Record_Search_And_Verify_Real_Audit_Chain()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(
            ApiHostPostgreSqlFixture.SeedAdminEmail,
            ApiHostPostgreSqlFixture.SeedAdminPassword);

        var initialSearchResponse = await client.GetAsync("/api/v1/audit/search?page=1&pageSize=20");
        initialSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var initialSearchDocument = JsonDocument.Parse(await initialSearchResponse.Content.ReadAsStringAsync());
        var initialSearchRoot = initialSearchDocument.RootElement.TryGetProperty("data", out var nestedSearch)
            ? nestedSearch
            : initialSearchDocument.RootElement;
        var initialItems = initialSearchRoot.GetProperty("items");
        initialItems.GetArrayLength().Should().BeGreaterThan(0);

        var firstAuditItem = initialItems[0];
        var sourceModule = firstAuditItem.GetProperty("sourceModule").GetString();
        var actionType = firstAuditItem.GetProperty("actionType").GetString();

        sourceModule.Should().NotBeNullOrWhiteSpace();
        actionType.Should().NotBeNullOrWhiteSpace();

        var filteredSearchResponse = await client.GetAsync($"/api/v1/audit/search?sourceModule={Uri.EscapeDataString(sourceModule!)}&actionType={Uri.EscapeDataString(actionType!)}&page=1&pageSize=20");
        filteredSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredSearchPayload = await filteredSearchResponse.Content.ReadAsStringAsync();
        filteredSearchPayload.Should().Contain(sourceModule);
        filteredSearchPayload.Should().Contain(actionType);

        var verifyResponse = await client.GetAsync("/api/v1/audit/verify-chain");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyPayload = await verifyResponse.Content.ReadAsStringAsync();
        verifyPayload.Should().MatchRegex("\"(isIntact|IsIntact)\"");
        verifyPayload.Should().MatchRegex("\"(totalLinks|TotalLinks)\"");
    }

    private static async Task<Guid> ExtractFirstGuidFromItemsAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement.TryGetProperty("data", out var nested) ? nested : document.RootElement;

        if (!root.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Could not find an items array in the response payload.");
        }

        foreach (var item in items.EnumerateArray())
        {
            if (TryGetGuid(item, propertyName, out var guid) || TryGetGuid(item, "id", out guid))
            {
                return guid;
            }
        }

        throw new InvalidOperationException($"Could not extract any GUID from the response items using '{propertyName}'.");
    }

    private static async Task<Guid> ExtractGuidAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var payload = document.RootElement.TryGetProperty("data", out var nested) ? nested : document.RootElement;

        if (TryGetGuid(payload, propertyName, out var guid) || TryGetGuid(payload, "id", out guid))
        {
            return guid;
        }

        if (response.Headers.Location is not null)
        {
            var lastSegment = response.Headers.Location.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (Guid.TryParse(lastSegment, out var fromLocation))
            {
                return fromLocation;
            }
        }

        throw new InvalidOperationException($"Could not extract GUID '{propertyName}' from response.");
    }

    private static bool TryGetGuid(JsonElement payload, string propertyName, out Guid guid)
    {
        guid = Guid.Empty;

        foreach (var candidate in GetCandidatePropertyNames(propertyName))
        {
            if (payload.TryGetProperty(candidate, out var property)
                && property.ValueKind == JsonValueKind.String
                && Guid.TryParse(property.GetString(), out guid))
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
}
