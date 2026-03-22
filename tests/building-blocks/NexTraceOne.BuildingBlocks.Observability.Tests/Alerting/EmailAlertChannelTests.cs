using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Alerting;

/// <summary>
/// Testes do EmailAlertChannel: nome do canal, configuração SMTP ausente e destinatários vazios.
/// </summary>
public sealed class EmailAlertChannelTests
{
    private static AlertPayload CreateTestPayload() => new()
    {
        Title = "Email Test",
        Description = "Testing email dispatch",
        Severity = AlertSeverity.Critical,
        Source = "unit-tests"
    };

    private static EmailAlertChannel CreateChannel(AlertingOptions? options = null)
    {
        var opts = Options.Create(options ?? new AlertingOptions());
        var logger = Substitute.For<ILogger<EmailAlertChannel>>();
        return new EmailAlertChannel(opts, logger);
    }

    [Fact]
    public void ChannelName_IsEmail()
    {
        var channel = CreateChannel();

        channel.ChannelName.Should().Be("Email");
    }

    [Fact]
    public async Task SendAsync_WhenSmtpHostNotConfigured_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Email = new EmailChannelOptions
            {
                SmtpHost = null,
                FromAddress = "alerts@nex.local",
                Recipients = ["ops@nex.local"]
            }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenSmtpHostEmpty_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Email = new EmailChannelOptions
            {
                SmtpHost = "  ",
                FromAddress = "alerts@nex.local",
                Recipients = ["ops@nex.local"]
            }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenNoRecipients_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Email = new EmailChannelOptions
            {
                SmtpHost = "smtp.nex.local",
                FromAddress = "alerts@nex.local",
                Recipients = []
            }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenFromAddressNotConfigured_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Email = new EmailChannelOptions
            {
                SmtpHost = "smtp.nex.local",
                FromAddress = null,
                Recipients = ["ops@nex.local"]
            }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_NullPayload_ThrowsArgumentNullException()
    {
        var channel = CreateChannel();

        Func<Task> act = () => channel.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
