using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.RecordAuditEvent;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para RecordAuditEvent.
/// Valida criação de evento, encadeamento de hash e validações do comando.
/// </summary>
public sealed class RecordAuditEventTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IAuditChainRepository _chainRepository = Substitute.For<IAuditChainRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public RecordAuditEventTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private RecordAuditEvent.Handler CreateHandler() =>
        new(_eventRepository, _chainRepository, _clock, _unitOfWork);

    private static RecordAuditEvent.Command CreateValidCommand() =>
        new("IdentityAccess", "UserCreated", "user-1", "User", "admin@org.com", Guid.NewGuid());

    [Fact]
    public async Task Handle_FirstEvent_ShouldCreateEventAndChainLink()
    {
        _chainRepository.GetLatestLinkAsync(Arg.Any<CancellationToken>())
            .Returns((AuditChainLink?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuditEventId.Should().NotBeEmpty();
        result.Value.ChainHash.Should().NotBeNullOrEmpty();
        result.Value.SequenceNumber.Should().Be(1);

        _eventRepository.Received(1).Add(Arg.Any<AuditEvent>());
        _chainRepository.Received(1).Add(Arg.Any<AuditChainLink>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubsequentEvent_ShouldChainToLatestLink()
    {
        var existingEvent = AuditEvent.Record("Mod", "Act", "r", "T", "u", DateTimeOffset.UtcNow, Guid.NewGuid());
        var existingLink = AuditChainLink.Create(existingEvent, 5, "prev-hash", DateTimeOffset.UtcNow);

        _chainRepository.GetLatestLinkAsync(Arg.Any<CancellationToken>())
            .Returns(existingLink);

        var handler = CreateHandler();
        var result = await handler.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SequenceNumber.Should().Be(6);
        result.Value.ChainHash.Should().NotBe(existingLink.CurrentHash);
    }

    [Fact]
    public async Task Handle_WithPayload_ShouldPersist()
    {
        _chainRepository.GetLatestLinkAsync(Arg.Any<CancellationToken>())
            .Returns((AuditChainLink?)null);

        var command = new RecordAuditEvent.Command(
            "Catalog", "ServiceUpdated", "svc-1", "Service", "user@org.com", Guid.NewGuid(), """{"change":"value"}""");

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _eventRepository.Received(1).Add(Arg.Is<AuditEvent>(e => e.Payload != null));
    }

    // ── Validator Tests ──

    [Fact]
    public void Validator_ValidCommand_ShouldPass()
    {
        var validator = new RecordAuditEvent.Validator();
        var result = validator.Validate(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptySourceModule_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("", "Action", "r1", "Type", "user", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyActionType_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("Module", "", "r1", "Type", "user", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyResourceId_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("Module", "Action", "", "Type", "user", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyPerformedBy_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("Module", "Action", "r1", "Type", "", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyTenantId_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("Module", "Action", "r1", "Type", "user", Guid.Empty);
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_SourceModuleTooLong_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command(new string('A', 201), "Action", "r1", "Type", "user", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ResourceIdTooLong_ShouldFail()
    {
        var validator = new RecordAuditEvent.Validator();
        var command = new RecordAuditEvent.Command("Module", "Action", new string('R', 501), "Type", "user", Guid.NewGuid());
        validator.Validate(command).IsValid.Should().BeFalse();
    }
}
