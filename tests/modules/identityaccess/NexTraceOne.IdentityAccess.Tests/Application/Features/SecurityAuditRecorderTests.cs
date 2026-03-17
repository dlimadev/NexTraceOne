using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Tests.TestDoubles;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes do serviço SecurityAuditRecorder.
/// Valida que cada método cria o SecurityEvent correto com o tipo e risk score esperados.
/// </summary>
public sealed class SecurityAuditRecorderTests
{
    private readonly ISecurityEventRepository _securityEventRepository = Substitute.For<ISecurityEventRepository>();
    private readonly TestDateTimeProvider _dateTimeProvider = new(new DateTimeOffset(2025, 03, 12, 10, 0, 0, TimeSpan.Zero));
    private readonly TestCurrentTenant _currentTenant = new(Guid.NewGuid());

    private SecurityAuditRecorder CreateSut() => new(
        _securityEventRepository,
        _dateTimeProvider,
        _currentTenant);

    [Fact]
    public void RecordAuthenticationSuccess_Should_AddEventWithCorrectType()
    {
        var sut = CreateSut();
        var tenantId = TenantId.From(Guid.NewGuid());
        var userId = UserId.New();

        sut.RecordAuthenticationSuccess(tenantId, userId, "127.0.0.1", "TestAgent");

        _securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.AuthenticationSucceeded &&
            e.RiskScore == 0));
    }

    [Fact]
    public void RecordAuthenticationFailure_Should_AddEventWithRiskScore30()
    {
        var sut = CreateSut();
        var tenantId = TenantId.From(Guid.NewGuid());

        sut.RecordAuthenticationFailure(tenantId, null, "Invalid credentials", "127.0.0.1", "TestAgent");

        _securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.AuthenticationFailed &&
            e.RiskScore == 30));
    }

    [Fact]
    public void RecordAccountLocked_Should_AddEventWithRiskScore70()
    {
        var sut = CreateSut();
        var tenantId = TenantId.From(Guid.NewGuid());
        var userId = UserId.New();

        sut.RecordAccountLocked(tenantId, userId, "127.0.0.1", "TestAgent");

        _securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.AccountLocked &&
            e.RiskScore == 70));
    }

    [Fact]
    public void RecordOidcCallbackSuccess_Should_AddEventWithProviderMetadata()
    {
        var sut = CreateSut();
        var tenantId = TenantId.From(Guid.NewGuid());
        var userId = UserId.New();
        var sessionId = SessionId.New();

        sut.RecordOidcCallbackSuccess(tenantId, userId, sessionId, "google", "ext-123", "127.0.0.1", "TestAgent");

        _securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.OidcCallbackSuccess &&
            e.RiskScore == 0));
    }

    [Fact]
    public void RecordOidcCallbackFailure_Should_AddEventWithRiskScore50()
    {
        var sut = CreateSut();
        var tenantId = TenantId.From(Guid.NewGuid());

        sut.RecordOidcCallbackFailure(tenantId, "google", "Token exchange failed", "127.0.0.1", "TestAgent");

        _securityEventRepository.Received(1).Add(Arg.Is<SecurityEvent>(e =>
            e.EventType == SecurityEventType.OidcCallbackFailed &&
            e.RiskScore == 50));
    }

    [Fact]
    public void ResolveTenantIdForAudit_Should_ReturnCurrentTenantId_When_TenantIsAvailable()
    {
        var sut = CreateSut();

        var result = sut.ResolveTenantIdForAudit();

        result.Value.Should().Be(_currentTenant.Id);
    }

    [Fact]
    public void ResolveTenantIdForAudit_Should_ReturnEmptyGuid_When_TenantIsNotAvailable()
    {
        var emptyTenant = new TestCurrentTenant(Guid.Empty);
        var sut = new SecurityAuditRecorder(_securityEventRepository, _dateTimeProvider, emptyTenant);

        var result = sut.ResolveTenantIdForAudit();

        result.Value.Should().Be(Guid.Empty);
    }
}
