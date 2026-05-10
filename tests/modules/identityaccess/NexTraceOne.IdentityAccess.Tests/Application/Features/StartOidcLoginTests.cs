using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.StartOidcLogin;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários do handler StartOidcLogin.
/// Cobre: provider não configurado, geração de state com CSRF nonce, open redirect protection,
/// codificação de returnTo no state, registo de evento de segurança.
/// </summary>
public sealed class StartOidcLoginTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string ConfiguredProvider = "azure";

    private static (
        IOidcProvider oidcProvider,
        ISecurityEventRepository evtRepo,
        ISecurityEventTracker evtTracker,
        StartOidcLogin.Handler handler) CreateHandler()
    {
        var oidcProvider = Substitute.For<IOidcProvider>();
        oidcProvider.IsConfigured(ConfiguredProvider).Returns(true);
        oidcProvider.IsConfigured(Arg.Is<string>(p => p != ConfiguredProvider)).Returns(false);
        oidcProvider.BuildAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => $"https://login.microsoftonline.com/auth?state={ci.ArgAt<string>(1)}");

        var evtRepo = Substitute.For<ISecurityEventRepository>();
        var evtTracker = Substitute.For<ISecurityEventTracker>();
        var clock = new TestDateTimeProvider(FixedNow);
        var currentTenant = new TestCurrentTenant(TenantId);

        var handler = new StartOidcLogin.Handler(
            oidcProvider, evtRepo, evtTracker, clock, currentTenant);

        return (oidcProvider, evtRepo, evtTracker, handler);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenProviderNotConfigured()
    {
        var (_, _, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new StartOidcLogin.Command("okta"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OidcProvider");
    }

    [Fact]
    public async Task Handle_Should_ReturnAuthorizationUrl_WhenProviderConfigured()
    {
        var (oidcProvider, _, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new StartOidcLogin.Command(ConfiguredProvider),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorizationUrl.Should().Contain("https://login.microsoftonline.com");
        oidcProvider.Received(1).BuildAuthorizationUrl(
            ConfiguredProvider, Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_EncodeReturnToInState_WhenReturnToProvided()
    {
        var (_, _, _, handler) = CreateHandler();
        const string returnTo = "/dashboard";

        var result = await handler.Handle(
            new StartOidcLogin.Command(ConfiguredProvider, returnTo),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var state = result.Value.State;
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
        // State format: "nonce:urlEncodedReturnTo"
        decoded.Should().Contain(Uri.EscapeDataString(returnTo));
    }

    [Fact]
    public async Task Handle_Should_UseDefaultReturnTo_WhenReturnToOmitted()
    {
        var (_, _, _, handler) = CreateHandler();

        var result = await handler.Handle(
            new StartOidcLogin.Command(ConfiguredProvider, ReturnTo: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.Value.State));
        // Default returnTo is "/" — encoded as "%2F" or "/"
        decoded.Should().EndWith(":" + Uri.EscapeDataString("/"));
    }

    [Fact]
    public async Task Handle_Should_GenerateUniqueStatePerRequest()
    {
        var (_, _, _, handler) = CreateHandler();
        var cmd = new StartOidcLogin.Command(ConfiguredProvider);

        var result1 = await handler.Handle(cmd, CancellationToken.None);
        var result2 = await handler.Handle(cmd, CancellationToken.None);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.State.Should().NotBe(result2.Value.State,
            because: "each flow must have a unique nonce to prevent CSRF replay");
    }

    [Fact]
    public async Task Handle_Should_RecordSecurityEvent_WhenFlowStarted()
    {
        var (_, evtRepo, evtTracker, handler) = CreateHandler();

        await handler.Handle(new StartOidcLogin.Command(ConfiguredProvider), CancellationToken.None);

        evtRepo.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.OidcFlowStarted));
        evtTracker.Received(1).Track(Arg.Any<SecurityEvent>());
    }

    [Theory]
    [InlineData("https://evil.com/steal")]
    [InlineData("//evil.com")]
    [InlineData("http://external.com")]
    public async Task Validator_Should_RejectExternalReturnTo_ToPreventOpenRedirect(string maliciousReturnTo)
    {
        var validator = new StartOidcLogin.Validator();

        var result = validator.Validate(new StartOidcLogin.Command(ConfiguredProvider, maliciousReturnTo));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(StartOidcLogin.Command.ReturnTo));
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("http://localhost:3000/app")]
    [InlineData("https://localhost/settings")]
    public async Task Validator_Should_AcceptSafeReturnTo(string safeReturnTo)
    {
        var validator = new StartOidcLogin.Validator();

        var result = validator.Validate(new StartOidcLogin.Command(ConfiguredProvider, safeReturnTo));

        result.IsValid.Should().BeTrue();
    }
}
