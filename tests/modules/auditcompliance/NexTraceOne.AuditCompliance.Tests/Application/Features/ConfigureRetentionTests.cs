using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.ConfigureRetention;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para ConfigureRetention.
/// Valida o handler placeholder e as validações do comando.
/// </summary>
public sealed class ConfigureRetentionTests
{
    private readonly IRetentionPolicyRepository _retentionPolicyRepository = Substitute.For<IRetentionPolicyRepository>();
    private readonly IAuditComplianceUnitOfWork _unitOfWork = Substitute.For<IAuditComplianceUnitOfWork>();

    private ConfigureRetention.Handler CreateHandler() => new(_retentionPolicyRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        var handler = CreateHandler();
        var command = new ConfigureRetention.Command("default-90", 90);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _retentionPolicyRepository.Received(1).Add(Arg.Any<RetentionPolicy>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaxRetention_ShouldReturnSuccess()
    {
        var handler = CreateHandler();
        var command = new ConfigureRetention.Command("long-term", 3650);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _retentionPolicyRepository.Received(1).Add(Arg.Any<RetentionPolicy>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MinRetention_ShouldReturnSuccess()
    {
        var handler = CreateHandler();
        var command = new ConfigureRetention.Command("short-term", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _retentionPolicyRepository.Received(1).Add(Arg.Any<RetentionPolicy>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
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
