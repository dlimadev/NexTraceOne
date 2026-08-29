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
/// Testes do ArtifactCommand: estrutura e comportamento de sign/verify com API mockada.
/// </summary>
public sealed class ArtifactCommandTests
{
    private const int ExitSuccess = 0;
    private const int ExitInvalid = 1;

    [Fact]
    public void Create_Returns_Command_With_Sign_And_Verify()
    {
        var command = NexTraceOne.CLI.Commands.ArtifactCommand.Create();

        command.Name.Should().Be("artifact");
        command.Subcommands.Should().Contain(c => c.Name == "sign");
        command.Subcommands.Should().Contain(c => c.Name == "verify");
    }

    [Fact]
    public async Task VerifyAsync_WithValidSignature_Returns_Success()
    {
        using var sdk = CreateMockSdk(_ =>
            """{"isValid":true,"artifactId":"art-1","verifiedAt":"2026-06-30T10:00:00Z","signerIdentity":"ci@nextraceone","errors":[],"warnings":[]}""");

        var exitCode = await InvokeVerifyAsync("art-1", "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitSuccess);
    }

    [Fact]
    public async Task VerifyAsync_WithInvalidSignature_Returns_Invalid()
    {
        using var sdk = CreateMockSdk(_ =>
            """{"isValid":false,"artifactId":"art-1","verifiedAt":null,"signerIdentity":"","errors":["mismatch"],"warnings":[]}""");

        var exitCode = await InvokeVerifyAsync("art-1", "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitInvalid);
    }

    [Fact]
    public async Task SignAsync_WithMockApi_Returns_Success()
    {
        using var sdk = CreateMockSdk(_ =>
            """{"artifactId":"art-1","artifactName":"payments:1.2.3","checksum":"sha256:abc","signature":"sig","signedAt":"2026-06-30T10:00:00Z","signerIdentity":"ci@nextraceone","sbomJson":null,"transparencyLogEntry":"rekor-1"}""");

        var exitCode = await InvokeSignAsync("payments:1.2.3", "docker-image", "1.2.3", null, "http://localhost", "text", sdk);

        exitCode.Should().Be(ExitSuccess);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static async Task<int> InvokeVerifyAsync(
        string artifactId, string url, string format, NexTraceSdkClient injectedClient)
    {
        var method = typeof(NexTraceOne.CLI.Commands.ArtifactCommand)
            .GetMethod("VerifyAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            artifactId, url, null, format, CancellationToken.None, injectedClient
        ])!;
        return await task;
    }

    private static async Task<int> InvokeSignAsync(
        string path, string type, string version, string? metadata, string url, string format, NexTraceSdkClient injectedClient)
    {
        var method = typeof(NexTraceOne.CLI.Commands.ArtifactCommand)
            .GetMethod("SignAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task<int>)method!.Invoke(null, [
            path, type, version, metadata, url, null, format, CancellationToken.None, injectedClient
        ])!;
        return await task;
    }

    private static NexTraceSdkClient CreateMockSdk(Func<HttpRequestMessage, string> responder)
    {
        var handler = new FuncHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new NexTraceSdkClient(httpClient);
    }

    private sealed class FuncHandler(Func<HttpRequestMessage, string> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responder(request), System.Text.Encoding.UTF8, "application/json")
            });
    }
}
