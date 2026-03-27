using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade DeliveryChannelConfiguration.
/// Valida criação, atualização e habilitação/desabilitação de configurações de canal.
/// </summary>
public sealed class DeliveryChannelConfigurationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldCreateConfiguration()
    {
        var config = DeliveryChannelConfiguration.Create(
            _tenantId,
            DeliveryChannel.Email,
            "Email Channel",
            true,
            """{"smtpHost":"smtp.example.com"}""");

        config.Should().NotBeNull();
        config.Id.Value.Should().NotBeEmpty();
        config.TenantId.Should().Be(_tenantId);
        config.ChannelType.Should().Be(DeliveryChannel.Email);
        config.DisplayName.Should().Be("Email Channel");
        config.IsEnabled.Should().BeTrue();
        config.ConfigurationJson.Should().Contain("smtp.example.com");
        config.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_DefaultIsEnabled_ShouldBeFalse()
    {
        var config = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.MicrosoftTeams, "Teams Channel");

        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        var config = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.Email, "Email Channel");

        config.IsEnabled.Should().BeFalse();
        config.Enable();
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var config = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.Email, "Email Channel", isEnabled: true);

        config.Disable();
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldChangeAllFields()
    {
        var config = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.MicrosoftTeams, "Old Name", false, null);

        var before = config.UpdatedAt;
        config.Update("New Name", true, """{"webhookUrl":"https://new.webhook"}""");

        config.DisplayName.Should().Be("New Name");
        config.IsEnabled.Should().BeTrue();
        config.ConfigurationJson.Should().Contain("new.webhook");
        config.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
