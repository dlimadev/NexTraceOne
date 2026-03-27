using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade SmtpConfiguration.
/// Valida criação, atualização e habilitação/desabilitação da configuração SMTP.
/// </summary>
public sealed class SmtpConfigurationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldCreateConfiguration()
    {
        var config = SmtpConfiguration.Create(
            _tenantId,
            "smtp.empresa.com",
            587,
            true,
            "noreply@empresa.com",
            "NexTraceOne",
            "user@empresa.com",
            null,
            "https://app.nextraceone.com",
            false);

        config.Should().NotBeNull();
        config.Id.Value.Should().NotBeEmpty();
        config.TenantId.Should().Be(_tenantId);
        config.Host.Should().Be("smtp.empresa.com");
        config.Port.Should().Be(587);
        config.UseSsl.Should().BeTrue();
        config.FromAddress.Should().Be("noreply@empresa.com");
        config.FromName.Should().Be("NexTraceOne");
        config.Username.Should().Be("user@empresa.com");
        config.EncryptedPassword.Should().BeNull();
        config.BaseUrl.Should().Be("https://app.nextraceone.com");
        config.IsEnabled.Should().BeFalse();
        config.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "smtp.host", 587, true, "from@host.com", "Sender");

        config.IsEnabled.Should().BeFalse();
        config.Enable();
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "smtp.host", 587, true, "from@host.com", "Sender",
            isEnabled: true);

        config.Disable();
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateServer_ShouldChangeHostPortAndSsl()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "old.host", 25, false, "from@host.com", "Sender");

        config.UpdateServer("new.host", 465, true);

        config.Host.Should().Be("new.host");
        config.Port.Should().Be(465);
        config.UseSsl.Should().BeTrue();
    }

    [Fact]
    public void UpdateCredentials_ShouldChangeUsernameAndPassword()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "smtp.host", 587, true, "from@host.com", "Sender",
            username: "olduser");

        config.UpdateCredentials("newuser", "encryptedpwd");

        config.Username.Should().Be("newuser");
        config.EncryptedPassword.Should().Be("encryptedpwd");
    }

    [Fact]
    public void UpdateSender_ShouldChangeFromAddressNameAndBaseUrl()
    {
        var config = SmtpConfiguration.Create(
            _tenantId, "smtp.host", 587, true, "old@host.com", "Old Name");

        config.UpdateSender("new@host.com", "New Name", "https://new.url");

        config.FromAddress.Should().Be("new@host.com");
        config.FromName.Should().Be("New Name");
        config.BaseUrl.Should().Be("https://new.url");
    }
}
