using System;
using System.CommandLine;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NexTrace.Sdk;
using NexTrace.Sdk.Clients;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do IntegrationCommand: estrutura do comando, helpers privados e
/// comportamento de alto nível em cenários de erro de conexão.
/// </summary>
public sealed class IntegrationCommandTests
{
    [Fact]
    public void Create_Returns_Command_With_Scaffold_And_Register()
    {
        var command = NexTraceOne.CLI.Commands.IntegrationCommand.Create();

        command.Should().NotBeNull();
        command.Name.Should().Be("integration");
        command.Subcommands.Should().Contain(c => c.Name == "scaffold");
        command.Subcommands.Should().Contain(c => c.Name == "register");
    }

    [Theory]
    [InlineData("Payments API", "Payments API")]
    [InlineData("Payments/API", "Payments_API")]
    [InlineData("", "contract")]
    [InlineData(null, "contract")]
    public void ToSafeDirectoryName_Sanitizes_Input(string? input, string expected)
    {
        var result = InvokeToSafeDirectoryName(input);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task RegisterAsync_WhenApiIsUnreachable_Returns_ExitError()
    {
        var method = typeof(NexTraceOne.CLI.Commands.IntegrationCommand)
            .GetMethod("RegisterAsync", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "orders-consumer",
            "Service",
            "Production",
            "cli",
            "ref",
            0.95m,
            "http://192.0.2.1:1",
            null,
            "text",
            CancellationToken.None
        ])!;

        var exitCode = await task;

        exitCode.Should().Be(1);
    }

    [Theory]
    [InlineData("typescript")]
    [InlineData("python")]
    [InlineData("java")]
    public async Task ScaffoldAsync_WithUnsupportedLanguage_Returns_ExitError(string language)
    {
        var method = typeof(NexTraceOne.CLI.Commands.IntegrationCommand)
            .GetMethod("ScaffoldAsync", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var outputDir = Path.Combine(Path.GetTempPath(), $"nex-int-lang-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        try
        {
            var task = (Task<int>)method!.Invoke(null, [
                "payments-api",
                "orders-consumer",
                null,
                language,
                outputDir,
                null,
                false,
                0.95m,
                "http://localhost",
                null,
                "text",
                CancellationToken.None,
                null
            ])!;

            var exitCode = await task;

            exitCode.Should().Be(1);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task ScaffoldAsync_WhenApiIsUnreachable_Returns_ExitError()
    {
        var method = typeof(NexTraceOne.CLI.Commands.IntegrationCommand)
            .GetMethod("ScaffoldAsync", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var outputDir = Path.Combine(Path.GetTempPath(), $"nex-int-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        try
        {
            var task = (Task<int>)method!.Invoke(null, [
                "payments-api",
                "orders-consumer",
                null,
                "csharp",
                outputDir,
                null,
                false,
                0.95m,
                "http://192.0.2.1:1",
                null,
                "text",
                CancellationToken.None,
                null
            ])!;

            var exitCode = await task;

            exitCode.Should().Be(1);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task ScaffoldAsync_WithMockApi_GeneratesFilesAndManifest()
    {
        var handler = new IntegrationMockHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        using var sdkClient = new NexTraceSdkClient(httpClient);

        var method = typeof(NexTraceOne.CLI.Commands.IntegrationCommand)
            .GetMethod("ScaffoldAsync", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var outputDir = Path.Combine(Path.GetTempPath(), $"nex-int-mock-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        try
        {
            var task = (Task<int>)method!.Invoke(null, [
                "payments-api",
                "orders-consumer",
                null,
                "csharp",
                outputDir,
                null,
                false,
                0.95m,
                "http://localhost",
                null,
                "json",
                CancellationToken.None,
                sdkClient
            ])!;

            var exitCode = await task;

            exitCode.Should().Be(0);

            var manifestPath = Path.Combine(outputDir, "nexone-integration.json");
            File.Exists(manifestPath).Should().BeTrue();

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<JsonElement>(manifestJson);
            manifest.GetProperty("ProviderName").GetString().Should().Be("payments-api");
            manifest.GetProperty("ConsumerName").GetString().Should().Be("orders-consumer");
            manifest.GetProperty("TotalFiles").GetInt32().Should().Be(1);
            manifest.GetProperty("TotalOperations").GetInt32().Should().Be(3);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { /* best effort */ }
        }
    }

    private static string InvokeToSafeDirectoryName(string? input)
    {
        var method = typeof(NexTraceOne.CLI.Commands.IntegrationCommand)
            .GetMethod("ToSafeDirectoryName", BindingFlags.NonPublic | BindingFlags.Static);

        return (string)method!.Invoke(null, [input])!;
    }

    /// <summary>Handler HTTP mock para testes de integração do CLI.</summary>
    private sealed class IntegrationMockHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            var json = path switch
            {
                "/api/v1/catalog/services/search?q=payments-api" => """
                    {"items":[{"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api"}]}
                    """,
                "/api/v1/catalog/services/11111111-1111-1111-1111-111111111111" => """
                    {"serviceId":"11111111-1111-1111-1111-111111111111","name":"payments-api","apis":[]}
                    """,
                "/api/v1/catalog/impact/11111111-1111-1111-1111-111111111111?maxDepth=2" => """
                    {"rootNodeId":"11111111-1111-1111-1111-111111111111","affectedNodes":[],"totalAffected":0}
                    """,
                "/api/v1/contracts/by-service/11111111-1111-1111-1111-111111111111" => """
                    {"contracts":[{"versionId":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","apiName":"Payments API","semVer":"1.0.0","protocol":"REST","lifecycleState":"Active"}]}
                    """,
                "/api/v1/contracts/cccccccc-cccc-cccc-cccc-cccccccccccc/detail" => """
                    {"id":"cccccccc-cccc-cccc-cccc-cccccccccccc","apiAssetId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","semVer":"1.0.0","specContent":"openapi: 3.0.0\ninfo:\n  title: Payments API\n  version: 1.0.0\npaths:\n  /api/v1/payments:\n    get:\n      operationId: listPayments\n      responses:\n        '200':\n          description: OK\n"}
                    """,
                "/api/v1/contracts/generate-code" => """
                    {"serviceName":"orders-consumer","title":"Payments API","schemaCount":2,"operationCount":3,"files":[{"path":"src/OrdersConsumer.Contracts/PaymentDto.cs","content":"public sealed record PaymentDto {}"}]}
                    """,
                _ => "{}"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
