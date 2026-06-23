using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NexTrace.Sdk;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do SecurityCommand: estrutura, validação de severidade e comportamento de gate
/// (deps/vulnerable) com API mockada.
/// </summary>
public sealed class SecurityCommandTests
{
    private const int ExitSuccess = 0;
    private const int ExitGateFailed = 1;
    private const int ExitError = 2;

    [Fact]
    public void Create_Returns_Command_With_Deps_And_Vulnerable()
    {
        var command = NexTraceOne.CLI.Commands.SecurityCommand.Create();

        command.Name.Should().Be("security");
        command.Subcommands.Should().Contain(c => c.Name == "deps");
        command.Subcommands.Should().Contain(c => c.Name == "vulnerable");
    }

    [Fact]
    public async Task DepsAsync_WithInvalidFailOn_Returns_ExitError()
    {
        var exitCode = await InvokeDepsAsync(
            "11111111-1111-1111-1111-111111111111", "extreme", "http://localhost", "text", null);

        exitCode.Should().Be(ExitError);
    }

    [Fact]
    public async Task DepsAsync_WithCriticalVuln_AndFailOnCritical_FailsGate()
    {
        using var sdk = CreateMockSdk(_ => HealthJson(criticalVulnCount: 1));

        var exitCode = await InvokeDepsAsync(
            "11111111-1111-1111-1111-111111111111", "critical", "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitGateFailed);
    }

    [Fact]
    public async Task DepsAsync_WithNoVulns_AndFailOnHigh_Passes()
    {
        using var sdk = CreateMockSdk(_ => HealthJson());

        var exitCode = await InvokeDepsAsync(
            "11111111-1111-1111-1111-111111111111", "high", "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitSuccess);
    }

    [Fact]
    public async Task VulnerableAsync_WithFailOnFound_AndResults_FailsGate()
    {
        using var sdk = CreateMockSdk(_ => """
            [{"profileId":"p1","serviceId":"22222222-2222-2222-2222-222222222222","healthScore":40,"criticalCount":1,"highCount":2,"mediumCount":0,"lastScanAt":"2026-06-23T10:00:00Z"}]
            """);

        var exitCode = await InvokeVulnerableAsync("high", failOnFound: true, "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitGateFailed);
    }

    [Fact]
    public async Task VulnerableAsync_WithInvalidSeverity_Returns_ExitError()
    {
        var exitCode = await InvokeVulnerableAsync("extreme", failOnFound: false, "http://localhost", "text", null);

        exitCode.Should().Be(ExitError);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static async Task<int> InvokeDepsAsync(
        string serviceId, string? failOn, string url, string format, NexTraceSdkClient? injectedClient)
    {
        var method = typeof(NexTraceOne.CLI.Commands.SecurityCommand)
            .GetMethod("DepsAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            serviceId, failOn, url, null, format, CancellationToken.None, injectedClient
        ])!;
        return await task;
    }

    private static async Task<int> InvokeVulnerableAsync(
        string minSeverity, bool failOnFound, string url, string format, NexTraceSdkClient? injectedClient)
    {
        var method = typeof(NexTraceOne.CLI.Commands.SecurityCommand)
            .GetMethod("VulnerableAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            minSeverity, failOnFound, url, null, format, CancellationToken.None, injectedClient
        ])!;
        return await task;
    }

    private static NexTraceSdkClient CreateMockSdk(Func<HttpRequestMessage, string> responder)
    {
        var handler = new FuncHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new NexTraceSdkClient(httpClient);
    }

    private static string HealthJson(int criticalVulnCount = 0) => $$"""
        {
            "serviceId": "11111111-1111-1111-1111-111111111111",
            "healthScore": 90,
            "lastScanAt": "2026-06-23T10:00:00Z",
            "totalDeps": 10,
            "directDeps": 5,
            "transitiveDeps": 5,
            "criticalVulnCount": {{criticalVulnCount}},
            "highVulnCount": 0,
            "mediumVulnCount": 0,
            "lowVulnCount": 0,
            "outdatedCount": 0,
            "deprecatedCount": 0,
            "licenseRiskCounts": {}
        }
        """;

    private sealed class FuncHandler(Func<HttpRequestMessage, string> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responder(request), System.Text.Encoding.UTF8, "application/json")
            });
    }
}
</content>
