using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateOperationalPlaybook;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>Testes unitários do handler CreateOperationalPlaybook.</summary>
public sealed class CreateOperationalPlaybookTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IOperationalPlaybookRepository _repository = Substitute.For<IOperationalPlaybookRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    public CreateOperationalPlaybookTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _tenant.Id.Returns(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        _user.Id.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndReturnSuccess()
    {
        var handler = new CreateOperationalPlaybook.Handler(
            _repository, _unitOfWork, _tenant, _user, _clock);

        var command = new CreateOperationalPlaybook.Command(
            "DB Failover Playbook",
            "Standard failover procedure",
            "[{\"step\":1,\"action\":\"Check replica status\"}]",
            "[\"svc-db-primary\"]",
            "[\"rb-failover-001\"]",
            "[\"database\",\"failover\"]");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("DB Failover Playbook");
        result.Value.Description.Should().Be("Standard failover procedure");
        result.Value.Version.Should().Be(1);
        result.Value.Status.Should().Be("Draft");
        result.Value.CreatedAt.Should().Be(FixedNow);
        result.Value.PlaybookId.Should().NotBe(Guid.Empty);

        await _repository.Received(1).AddAsync(
            Arg.Is<OperationalPlaybook>(p =>
                p.Name == "DB Failover Playbook" &&
                p.TenantId == "00000000-0000-0000-0000-000000000001" &&
                p.Status == PlaybookStatus.Draft &&
                p.Version == 1),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldSucceed()
    {
        var handler = new CreateOperationalPlaybook.Handler(
            _repository, _unitOfWork, _tenant, _user, _clock);

        var command = new CreateOperationalPlaybook.Command(
            "Simple Playbook",
            null,
            "[{\"step\":1}]",
            null,
            null,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Validator_EmptyName_ShouldFail()
    {
        var validator = new CreateOperationalPlaybook.Validator();
        var command = new CreateOperationalPlaybook.Command(
            "",
            null,
            "[{\"step\":1}]",
            null,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptySteps_ShouldFail()
    {
        var validator = new CreateOperationalPlaybook.Validator();
        var command = new CreateOperationalPlaybook.Command(
            "Test Playbook",
            null,
            "",
            null,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidCommand_ShouldPass()
    {
        var validator = new CreateOperationalPlaybook.Validator();
        var command = new CreateOperationalPlaybook.Command(
            "DB Failover Playbook",
            "Steps for failover",
            "[{\"step\":1}]",
            null,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_NameTooLong_ShouldFail()
    {
        var validator = new CreateOperationalPlaybook.Validator();
        var command = new CreateOperationalPlaybook.Command(
            new string('A', 201),
            null,
            "[{\"step\":1}]",
            null,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DescriptionTooLong_ShouldFail()
    {
        var validator = new CreateOperationalPlaybook.Validator();
        var command = new CreateOperationalPlaybook.Command(
            "Test",
            new string('D', 2001),
            "[{\"step\":1}]",
            null,
            null,
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
