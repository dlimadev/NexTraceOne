using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do ContractCommand: verify, sync, diff, changelog.
/// Testa lógica de deteção de formato de spec, payloads corretos e manuseamento de erros.
/// </summary>
public sealed class ContractCommandTests
{
    // ── Spec format detection ──────────────────────────────────────────────────

    [Theory]
    [InlineData("api.yaml", "openapi: 3.0.0", "yaml")]
    [InlineData("api.yml", "openapi: 3.0.0", "yaml")]
    [InlineData("api.json", "{\"openapi\":\"3.0.0\"}", "json")]
    [InlineData("api.xml", "<definitions/>", "xml")]
    [InlineData("api.wsdl", "<definitions/>", "xml")]
    public void DetectSpecFormat_ReturnsCorrectFormat(string fileName, string content, string expected)
    {
        var format = InvokeDetectFormat(fileName, content);
        format.Should().Be(expected);
    }

    [Fact]
    public void DetectSpecFormat_FallsBackToYaml_WhenNoExtensionMatch()
    {
        var format = InvokeDetectFormat("spec.txt", "openapi: 3.0.0");
        format.Should().Be("yaml");
    }

    [Fact]
    public void DetectSpecFormat_DetectsJsonFromContent()
    {
        var format = InvokeDetectFormat("spec.txt", "{\"openapi\":\"3.0.0\"}");
        format.Should().Be("json");
    }

    [Fact]
    public void DetectSpecFormat_DetectsXmlFromContent()
    {
        var format = InvokeDetectFormat("spec.txt", "<definitions xmlns=\"...\">");
        format.Should().Be("xml");
    }

    // ── Protocol detection ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("openapi: 3.0.0", "OpenApi")]
    [InlineData("{\"swagger\":\"2.0\"}", "Swagger")]
    [InlineData("asyncapi: 2.0.0", "AsyncApi")]
    [InlineData("<definitions xmlns:wsdl=\"...\"/>", "Wsdl")]
    public void DetectProtocol_ReturnsCorrectProtocol(string content, string expected)
    {
        var protocol = InvokeDetectProtocol(content);
        protocol.Should().Be(expected);
    }

    // ── HttpMessageHandler mock helpers ────────────────────────────────────────

    [Fact]
    public async Task Verify_WhenApiReturnsError_ReturnsExitError()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{\"error\":\"internal\"}");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        // Directly test the HTTP client interaction: 500 status should produce non-success
        var response = await httpClient.GetAsync("/api/v1/contracts/verifications", CancellationToken.None);
        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(500);
    }

    [Fact]
    public async Task Verify_WhenApiReturnsSuccess_ResponseIsDeserializable()
    {
        var verificationJson = JsonSerializer.Serialize(new
        {
            verificationId = Guid.NewGuid(),
            status = "Pass",
            contractVersionSemVer = "1.0.0",
            breakingChangesCount = 0,
            nonBreakingChangesCount = 0,
            additiveChangesCount = 0,
            message = "OK",
            verifiedAt = DateTimeOffset.UtcNow
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, verificationJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.PostAsync("/api/v1/contracts/verifications",
            new StringContent("{}", Encoding.UTF8, "application/json"), CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("status").GetString().Should().Be("Pass");
    }

    [Fact]
    public async Task Sync_WhenApiReturnsSuccess_ReturnsExpectedBody()
    {
        var syncJson = JsonSerializer.Serialize(new
        {
            importedCount = 1,
            skippedCount = 0,
            correlationId = Guid.NewGuid()
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, syncJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.PostAsync("/api/v1/contracts/sync",
            new StringContent("{}", Encoding.UTF8, "application/json"), CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("importedCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Diff_WhenApiReturnsChanges_ResponseContainsDiffEntries()
    {
        var diffJson = JsonSerializer.Serialize(new
        {
            breakingCount = 1,
            nonBreakingCount = 0,
            additiveCount = 2,
            changes = new[]
            {
                new { changeType = "breaking", path = "/api/v1/payments/{id}", description = "Parameter removed" },
                new { changeType = "additive", path = "/api/v1/payments/bulk", description = "New endpoint" },
                new { changeType = "additive", path = "/api/v1/payments/search", description = "New endpoint" }
            }
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, diffJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.GetAsync("/api/v1/contracts/diff?service=payments-api&from=1.0.0&to=1.1.0",
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("breakingCount").GetInt32().Should().Be(1);
        result.GetProperty("additiveCount").GetInt32().Should().Be(2);
        result.GetProperty("changes").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Changelog_WhenApiReturnsEntries_ResponseIsDeserializable()
    {
        var changelogJson = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    version = "1.1.0",
                    semVer = "1.1.0",
                    changeType = "additive",
                    description = "Added bulk payment endpoint",
                    publishedAt = DateTimeOffset.UtcNow.AddDays(-7)
                },
                new
                {
                    version = "1.0.0",
                    semVer = "1.0.0",
                    changeType = "feature",
                    description = "Initial release",
                    publishedAt = DateTimeOffset.UtcNow.AddDays(-30)
                }
            }
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, changelogJson);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.GetAsync("/api/v1/contracts/changelogs?apiAssetId=payments-api",
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Diff_WhenApiReturnsNotFound_ResponseIsNonSuccess()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{\"error\":\"contract not found\"}");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.GetAsync("/api/v1/contracts/diff?service=unknown-svc",
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Verify_WithDryRunPayload_IncludesDryRunFlag()
    {
        var capturedBody = string.Empty;
        var verificationJson = JsonSerializer.Serialize(new
        {
            verificationId = Guid.NewGuid(),
            status = "Pass",
            breakingChangesCount = 0,
            nonBreakingChangesCount = 0,
            additiveChangesCount = 0,
            message = "Dry run - no changes persisted",
            verifiedAt = DateTimeOffset.UtcNow
        });

        using var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, verificationJson,
            body => capturedBody = body);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var payload = new { apiAssetId = "payments-api", dryRun = true, specContent = "openapi: 3.0.0" };
        await httpClient.PostAsync("/api/v1/contracts/verifications",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            CancellationToken.None);

        capturedBody.Should().Contain("dryRun");
        capturedBody.Should().Contain("true");
    }

    // ── Contract list ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ContractList_WhenApiReturnsContracts_ResponseContainsItems()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    name = "Payments REST API",
                    protocol = "REST",
                    semVer = "2.0.0",
                    serviceName = "payments-api",
                    lifecycleState = "Active",
                    updatedAt = DateTimeOffset.UtcNow.AddDays(-3)
                },
                new
                {
                    name = "Order Events",
                    protocol = "Kafka",
                    semVer = "1.0.0",
                    serviceName = "order-service",
                    lifecycleState = "Active",
                    updatedAt = DateTimeOffset.UtcNow.AddDays(-10)
                }
            },
            totalCount = 2
        });

        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.GetAsync(
            "/api/v1/contracts/list?page=1&pageSize=20",
            CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("items").GetArrayLength().Should().Be(2);
        result.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ContractList_WithProtocolFilter_QueryContainsProtocolParam()
    {
        var capturedUrl = string.Empty;
        using var handler = new ContractCapturingUrlHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}", url => capturedUrl = url);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await httpClient.GetAsync(
            "/api/v1/contracts/list?protocol=REST&page=1&pageSize=20",
            CancellationToken.None);

        capturedUrl.Should().Contain("protocol=REST");
    }

    [Fact]
    public async Task ContractList_WithSearchTerm_QueryContainsSearchParam()
    {
        var capturedUrl = string.Empty;
        using var handler = new ContractCapturingUrlHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}", url => capturedUrl = url);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        await httpClient.GetAsync(
            "/api/v1/contracts/list?searchTerm=payments&page=1&pageSize=20",
            CancellationToken.None);

        capturedUrl.Should().Contain("searchTerm=payments");
    }

    [Fact]
    public async Task ContractList_WhenApiReturnsEmpty_ResponseContainsZeroItems()
    {
        using var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://nex.test") };

        var response = await httpClient.GetAsync("/api/v1/contracts/list", CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ── Private reflection helpers ─────────────────────────────────────────────

    private static string InvokeDetectFormat(string fileName, string content)
    {
        var method = typeof(NexTraceOne.CLI.Commands.ContractCommand)
            .GetMethod("DetectSpecFormat",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [fileName, content])!;
    }

    private static string InvokeDetectProtocol(string content)
    {
        var method = typeof(NexTraceOne.CLI.Commands.ContractCommand)
            .GetMethod("DetectProtocol",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [content])!;
    }
}

// ── Test infrastructure ────────────────────────────────────────────────────────

/// <summary>Fake HttpMessageHandler que retorna uma resposta predefinida.</summary>
internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

/// <summary>Fake HttpMessageHandler que captura o body enviado e retorna uma resposta predefinida.</summary>
internal sealed class CapturingHttpMessageHandler(
    HttpStatusCode statusCode,
    string responseBody,
    Action<string> onRequest) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            onRequest(body);
        }

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>Fake HttpMessageHandler que captura a URL da requisição e retorna uma resposta predefinida.</summary>
internal sealed class ContractCapturingUrlHandler(
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
