using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.GetSmtpConfiguration;
using NexTraceOne.Notifications.Application.Features.UpsertSmtpConfiguration;
using NexTraceOne.Notifications.Domain.Entities;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes de unidade para os handlers de configuração SMTP (P7.1).
/// </summary>
public sealed class SmtpConfigurationHandlerTests
{
    private readonly ISmtpConfigurationStore _store = Substitute.For<ISmtpConfigurationStore>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public SmtpConfigurationHandlerTests()
    {
        _tenant.Id.Returns(_tenantId);
    }

    // ── GetSmtpConfiguration ───────────────────────────────────────────────

    [Fact]
    public async Task GetSmtp_WhenExists_ReturnsConfiguration()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "smtp.host", 587, true, "from@host.com", "Sender",
            isEnabled: true);

        _store.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(config);

        var handler = new GetSmtpConfiguration.Handler(_store, _tenant);
        var result = await handler.Handle(new GetSmtpConfiguration.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Host.Should().Be("smtp.host");
        result.Value.Port.Should().Be(587);
        result.Value.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetSmtp_WhenNotExists_ReturnsNullValue()
    {
        _store.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns((SmtpConfiguration?)null);

        var handler = new GetSmtpConfiguration.Handler(_store, _tenant);
        var result = await handler.Handle(new GetSmtpConfiguration.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ── UpsertSmtpConfiguration (Create) ──────────────────────────────────

    [Fact]
    public async Task UpsertSmtp_Create_WhenNoExisting_ShouldAddNewConfiguration()
    {
        _store.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns((SmtpConfiguration?)null);

        var handler = new UpsertSmtpConfiguration.Handler(_store, _tenant);
        var command = new UpsertSmtpConfiguration.Command(
            "smtp.host", 587, true, "from@host.com", "Sender",
            "user", "pass", "https://app.com", true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        await _store.Received(1).AddAsync(Arg.Any<SmtpConfiguration>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertSmtp_Update_WhenExisting_ShouldUpdateAndReturnCreatedFalse()
    {
        var existing = SmtpConfiguration.Create(
            _tenantId, "old.host", 25, false, "old@host.com", "Old Sender");

        _store.GetByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertSmtpConfiguration.Handler(_store, _tenant);
        var command = new UpsertSmtpConfiguration.Command(
            "new.host", 587, true, "new@host.com", "New Sender",
            null, null, null, true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeFalse();
        existing.Host.Should().Be("new.host");
        existing.IsEnabled.Should().BeTrue();
        await _store.DidNotReceive().AddAsync(Arg.Any<SmtpConfiguration>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpsertSmtp_Validator_InvalidEmail_ShouldFail()
    {
        var validator = new UpsertSmtpConfiguration.Validator();
        var command = new UpsertSmtpConfiguration.Command(
            "smtp.host", 587, true, "not-an-email", "Sender",
            null, null, null, false);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpsertSmtp_Validator_InvalidPort_ShouldFail()
    {
        var validator = new UpsertSmtpConfiguration.Validator();
        var command = new UpsertSmtpConfiguration.Command(
            "smtp.host", 0, true, "from@host.com", "Sender",
            null, null, null, false);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
