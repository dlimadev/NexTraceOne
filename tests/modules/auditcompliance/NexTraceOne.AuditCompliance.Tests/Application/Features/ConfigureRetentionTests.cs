using NexTraceOne.AuditCompliance.Application.Features.ConfigureRetention;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para ConfigureRetention.
/// Valida o handler placeholder e as validações do comando.
/// </summary>
public sealed class ConfigureRetentionTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        var handler = new ConfigureRetention.Handler();
        var command = new ConfigureRetention.Command("default-90", 90);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MaxRetention_ShouldReturnSuccess()
    {
        var handler = new ConfigureRetention.Handler();
        var command = new ConfigureRetention.Command("long-term", 3650);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MinRetention_ShouldReturnSuccess()
    {
        var handler = new ConfigureRetention.Handler();
        var command = new ConfigureRetention.Command("short-term", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Validator Tests ──

    [Fact]
    public void Validator_ValidCommand_ShouldPass()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("default", 90)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyPolicyName_ShouldFail()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("", 90)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_PolicyNameTooLong_ShouldFail()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command(new string('A', 201), 90)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ZeroRetentionDays_ShouldFail()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("test", 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_NegativeRetentionDays_ShouldFail()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("test", -1)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_RetentionDaysTooHigh_ShouldFail()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("test", 3651)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_MaxRetentionDays_ShouldPass()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("test", 3650)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_MinRetentionDays_ShouldPass()
    {
        var validator = new ConfigureRetention.Validator();
        validator.Validate(new ConfigureRetention.Command("test", 1)).IsValid.Should().BeTrue();
    }
}
