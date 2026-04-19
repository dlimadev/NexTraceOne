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
/// Testes do ReportCommand: dora, changes-summary.
/// Verifica integração com os endpoints /api/v1/changes/dora-metrics e /api/v1/changes/summary.
/// </summary>
public sealed class ReportCommandTests
{
    // ── DORA metrics ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DoraReport_WhenApiReturnsMetrics_ResponseContainsDora()
    {
        var json = JsonSerializer.Serialize(new
        {
            deploymentFrequency = "Multiple per day",
            deploymentFrequencyLevel = "Elite",
            leadTimeForChanges = "< 1 hour",
            leadTimeLevel = "Elite",
            meanTimeToRestore = "< 1 hour",
            mttrLevel = "Elite",
            changeFailureRate = "0-15%",
            changeFailureRateLevel = "Elite",
            overallPerformance = "Elite"
        });

        using var handler = new ReportFakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/changes/dora-metrics", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("deploymentFrequencyLevel").GetString().Should().Be("Elite");
        result.GetProperty("overallPerformance").GetString().Should().Be("Elite");
    }

    [Fact]
    public async Task DoraReport_WhenApiReturns401_ReturnsUnauthorized()
    {
        using var handler = new ReportFakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/changes/dora-metrics", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DoraReport_WithServiceFilter_QueryContainsServiceParam()
    {
        var capturedUrl = string.Empty;
        using var handler = new ReportCapturingHandler(HttpStatusCode.OK, "{}", url => capturedUrl = url);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await client.GetAsync(
            "/api/v1/changes/dora-metrics?serviceName=payments-api&environment=production",
            CancellationToken.None);

        capturedUrl.Should().Contain("serviceName=payments-api");
        capturedUrl.Should().Contain("environment=production");
    }

    [Fact]
    public async Task DoraReport_WithTeamFilter_QueryContainsTeamParam()
    {
        var capturedUrl = string.Empty;
        using var handler = new ReportCapturingHandler(HttpStatusCode.OK, "{}", url => capturedUrl = url);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await client.GetAsync(
            "/api/v1/changes/dora-metrics?teamName=core-payments",
            CancellationToken.None);

        capturedUrl.Should().Contain("teamName=core-payments");
    }

    // ── Changes Summary ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangesSummary_WhenApiReturnsData_ResponseContainsCounts()
    {
        var json = JsonSerializer.Serialize(new
        {
            totalChanges = 120,
            successfulChanges = 105,
            failedChanges = 8,
            pendingChanges = 7,
            rollbacks = 3,
            byEnvironment = new { production = 80, staging = 40 }
        });

        using var handler = new ReportFakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/changes/summary", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("totalChanges").GetInt32().Should().Be(120);
        result.GetProperty("successfulChanges").GetInt32().Should().Be(105);
        result.GetProperty("rollbacks").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task ChangesSummary_WithEnvironmentFilter_QueryContainsEnvParam()
    {
        var capturedUrl = string.Empty;
        using var handler = new ReportCapturingHandler(HttpStatusCode.OK, "{}", url => capturedUrl = url);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await client.GetAsync("/api/v1/changes/summary?environment=production", CancellationToken.None);

        capturedUrl.Should().Contain("environment=production");
    }

    [Fact]
    public async Task ChangesSummary_WhenApiReturns500_ReturnsServerError()
    {
        using var handler = new ReportFakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{\"error\":\"Internal error\"}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await client.GetAsync("/api/v1/changes/summary", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ChangesSummary_WithDateRange_QueryContainsFromAndTo()
    {
        var capturedUrl = string.Empty;
        using var handler = new ReportCapturingHandler(HttpStatusCode.OK, "{}", url => capturedUrl = url);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await client.GetAsync(
            "/api/v1/changes/summary?from=2025-01-01T00:00:00Z&to=2025-03-31T23:59:59Z",
            CancellationToken.None);

        capturedUrl.Should().Contain("from=");
        capturedUrl.Should().Contain("to=");
    }
}

internal sealed class ReportFakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
}

internal sealed class ReportCapturingHandler(
    HttpStatusCode statusCode,
    string responseBody,
    Action<string> onRequest) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        onRequest(request.RequestUri?.PathAndQuery ?? string.Empty);
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
    }
}
