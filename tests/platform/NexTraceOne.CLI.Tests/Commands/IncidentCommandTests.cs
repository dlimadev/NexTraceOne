using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do IncidentCommand: list, get, report.
/// Verifica contratos HTTP, serialização de resposta e tratamento de erros.
/// </summary>
public sealed class IncidentCommandTests
{
    // ── LIST tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IncidentList_WhenApiReturnsSuccess_ResponseIsDeserializable()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    incidentId = Guid.NewGuid(),
                    title = "Payment service degraded",
                    serviceName = "payments-api",
                    severity = "High",
                    status = "Open",
                    reportedAt = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new
                {
                    incidentId = Guid.NewGuid(),
                    title = "DB connection pool exhausted",
                    serviceName = "user-service",
                    severity = "Critical",
                    status = "Investigating",
                    reportedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                }
            },
            totalCount = 2
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/incidents?pageSize=20", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("items").GetArrayLength().Should().Be(2);
        result.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task IncidentList_WithServiceFilter_SendsFilterInQuery()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync(
            "/api/v1/incidents?pageSize=20&serviceId=payments-api&status=Open",
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task IncidentList_WhenApiReturnsError_ReturnsNonSuccess()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/incidents", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(401);
    }

    // ── GET tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task IncidentGet_WhenApiReturnsSuccess_ResponseContainsExpectedFields()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new
        {
            incidentId = id,
            title = "Payment service degraded",
            serviceName = "payments-api",
            severity = "High",
            status = "Investigating",
            environment = "production",
            description = "Elevated error rate detected",
            reportedAt = DateTimeOffset.UtcNow.AddHours(-1),
            resolvedAt = (DateTimeOffset?)null,
            correlatedChangesCount = 2
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync($"/api/v1/incidents/{id}", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("severity").GetString().Should().Be("High");
        result.GetProperty("status").GetString().Should().Be("Investigating");
        result.GetProperty("correlatedChangesCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task IncidentGet_WhenNotFound_Returns404()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/incidents/nonexistent-id", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(404);
    }

    // ── REPORT tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task IncidentReport_WhenApiReturnsCreated_ResponseContainsIncidentId()
    {
        var newId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { incidentId = newId });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var payload = new
        {
            title = "Payment service degraded",
            serviceId = "payments-api",
            severity = "High",
            environment = "production",
            description = "Elevated error rate detected",
            externalIncidentId = "PD-12345",
            externalSystem = "pagerduty",
            reportedFrom = "cli",
            reportedAt = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsync(
            "/api/v1/incidents",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("incidentId").GetGuid().Should().Be(newId);
    }

    [Fact]
    public async Task IncidentReport_WhenApiReturnsBadRequest_ReturnsNonSuccess()
    {
        using var handler = new FakeHttpMessageHandler(
            HttpStatusCode.BadRequest, "{\"error\":\"title is required\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.PostAsync(
            "/api/v1/incidents",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task IncidentReport_WithExternalId_PayloadContainsExternalFields()
    {
        var capturedBody = string.Empty;
        var handler = new IncidentCapturingHandler(
            HttpStatusCode.Created,
            JsonSerializer.Serialize(new { incidentId = Guid.NewGuid() }),
            body => capturedBody = body);

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var payload = new
        {
            title = "Incident test",
            serviceId = "payments-api",
            severity = "High",
            externalIncidentId = "JIRA-4567",
            externalSystem = "jira"
        };

        await client.PostAsync(
            "/api/v1/incidents",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            CancellationToken.None);

        capturedBody.Should().Contain("JIRA-4567");
        capturedBody.Should().Contain("jira");
    }
}

/// <summary>HttpMessageHandler de captura que permite inspecionar o body enviado.</summary>
internal sealed class IncidentCapturingHandler(
    HttpStatusCode statusCode,
    string responseBody,
    Action<string> onRequestBody) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            onRequestBody(body);
        }

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}
