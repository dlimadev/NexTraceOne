using NexTraceOne.Notifications.Application.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class NotificationChannelOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect()
    {
        NotificationChannelOptions.SectionName.Should().Be("Notifications:Channels");
    }

    [Fact]
    public void Email_DefaultsToDisabled()
    {
        var options = new NotificationChannelOptions();
        options.Email.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Teams_DefaultsToDisabled()
    {
        var options = new NotificationChannelOptions();
        options.Teams.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Email_DefaultSmtpPort_Is587()
    {
        var settings = new EmailChannelSettings();
        settings.SmtpPort.Should().Be(587);
    }

    [Fact]
    public void Email_DefaultUseSsl_IsTrue()
    {
        var settings = new EmailChannelSettings();
        settings.UseSsl.Should().BeTrue();
    }

    [Fact]
    public void Email_DefaultFromName_IsNexTraceOne()
    {
        var settings = new EmailChannelSettings();
        settings.FromName.Should().Be("NexTraceOne");
    }

    [Fact]
    public void Teams_DefaultTimeout_Is30Seconds()
    {
        var settings = new TeamsChannelSettings();
        settings.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void DeliveryRetryOptions_SectionName_IsCorrect()
    {
        DeliveryRetryOptions.SectionName.Should().Be("Notifications:Retry");
    }

    [Fact]
    public void DeliveryRetryOptions_DefaultMaxAttempts_Is3()
    {
        var options = new DeliveryRetryOptions();
        options.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void DeliveryRetryOptions_DefaultBaseDelay_Is30()
    {
        var options = new DeliveryRetryOptions();
        options.BaseDelaySeconds.Should().Be(30);
    }

    [Fact]
    public void Email_SmtpHost_CanBeConfigured()
    {
        var settings = new EmailChannelSettings { SmtpHost = "smtp.example.com" };
        settings.SmtpHost.Should().Be("smtp.example.com");
    }

    [Fact]
    public void Email_Credentials_CanBeConfigured()
    {
        var settings = new EmailChannelSettings
        {
            Username = "user@example.com",
            Password = "secret"
        };
        settings.Username.Should().Be("user@example.com");
        settings.Password.Should().Be("secret");
    }

    [Fact]
    public void Teams_WebhookUrl_CanBeConfigured()
    {
        var settings = new TeamsChannelSettings { WebhookUrl = "https://teams.hook/webhook" };
        settings.WebhookUrl.Should().Be("https://teams.hook/webhook");
    }

    [Fact]
    public void Email_NullSmtpHost_IsAllowed()
    {
        var settings = new EmailChannelSettings();
        settings.SmtpHost.Should().BeNull();
    }

    [Fact]
    public void Teams_NullWebhookUrl_IsAllowed()
    {
        var settings = new TeamsChannelSettings();
        settings.WebhookUrl.Should().BeNull();
    }
}
