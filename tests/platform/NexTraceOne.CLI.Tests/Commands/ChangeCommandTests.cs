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
/// Testes do ChangeCommand: report, blast-radius, list.
/// Foca nos novos campos: externalReleaseId, externalSystem (Natural Key Routing).
/// </summary>
public sealed class ChangeCommandTests
{
    // ── REPORT with natural key routing ─────────────────────────────────────────

    [Fact]
    public async Task ChangeReport_WithExternalId_PayloadContainsExternalFields()
    {
        var capturedBody = string.Empty;

        using var handler = new ChangeCapturingHandler(
            HttpStatusCode.Created,
            JsonSerializer.Serialize(new { releaseId = Guid.NewGuid(), status = "Reported" }),
            body => capturedBody = body);

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var payload = new
        {
            serviceName = "payments-api",
            semVer = "2.1.0",
            environment = "production",
            changeType = "Deploy",
            externalReleaseId = "gh-run-12345",
            externalSystem = "github"
        };

        await client.PostAsync(
            "/api/v1/releases",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            CancellationToken.None);

        capturedBody.Should().Contain("gh-run-12345");
        capturedBody.Should().Contain("github");
    }

    [Fact]
    public async Task ChangeReport_WhenApiReturnsCreated_IsSuccess()
    {
        var json = JsonSerializer.Serialize(new
        {
            releaseId = Guid.NewGuid(),
            status = "Reported",
            serviceName = "payments-api",
            semVer = "2.1.0",
            environment = "production"
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.PostAsync(
            "/api/v1/releases",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeReport_WhenApiReturnsError_ReturnsNonSuccess()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "{\"error\":\"validation failed\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.PostAsync(
            "/api/v1/releases",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task BlastRadius_WhenApiReturnsData_ResponseIsDeserializable()
    {
        var json = JsonSerializer.Serialize(new
        {
            releaseId = Guid.NewGuid(),
            service = "payments-api",
            directDependentsCount = 3,
            indirectDependentsCount = 12,
            riskScore = 0.67,
            affectedServices = new[] { "checkout-service", "reporting-api", "billing-service" }
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var id = Guid.NewGuid();
        var response = await client.GetAsync(
            $"/api/v1/changes/{id}/blast-radius", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("directDependentsCount").GetInt32().Should().Be(3);
        result.GetProperty("riskScore").GetDouble().Should().Be(0.67);
    }

    [Fact]
    public async Task ChangeList_WhenApiReturnsData_ResponseContainsItems()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    changeId = Guid.NewGuid(),
                    serviceName = "payments-api",
                    semVer = "2.1.0",
                    environment = "production",
                    changeType = "Deploy",
                    status = "Validated",
                    confidence = 0.92,
                    reportedAt = DateTimeOffset.UtcNow.AddHours(-1)
                }
            },
            totalCount = 1
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/changes?service=payments-api", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("items").GetArrayLength().Should().Be(1);
        result.GetProperty("totalCount").GetInt32().Should().Be(1);
    }
}

/// <summary>HttpMessageHandler de captura para ChangeCommand tests.</summary>
internal sealed class ChangeCapturingHandler(
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
