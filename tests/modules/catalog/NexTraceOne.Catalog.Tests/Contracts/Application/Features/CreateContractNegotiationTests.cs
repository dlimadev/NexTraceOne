using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using CreateContractNegotiationFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractNegotiation.CreateContractNegotiation;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler CreateContractNegotiation — cria uma negociação cross-team de contrato.
/// Valida criação com estado Draft, persistência via UnitOfWork e validação de entrada.
/// </summary>
public sealed class CreateContractNegotiationTests
{
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Cria negociação com sucesso ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_CreateNegotiation_When_ValidCommand()
    {
        var repository = Substitute.For<IContractNegotiationRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new CreateContractNegotiationFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new CreateContractNegotiationFeature.Command(
                ContractId, TeamId, "Platform Team",
                "Update Orders API v2", "Breaking changes proposal",
                FixedNow.AddDays(30),
                """["team-1","team-2"]""", 2,
                """{"openapi":"3.0"}""", "user-123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractId.Should().Be(ContractId);
        result.Value.ProposedByTeamId.Should().Be(TeamId);
        result.Value.ProposedByTeamName.Should().Be("Platform Team");
        result.Value.Title.Should().Be("Update Orders API v2");
        result.Value.Status.Should().Be(NegotiationStatus.Draft);
        result.Value.ParticipantCount.Should().Be(2);
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_Should_PersistNegotiation_Via_Repository()
    {
        var repository = Substitute.For<IContractNegotiationRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new CreateContractNegotiationFeature.Handler(repository, unitOfWork, dateTimeProvider);
        await handler.Handle(
            new CreateContractNegotiationFeature.Command(
                null, TeamId, "Platform Team",
                "New API", "New API proposal",
                null, """["team-1"]""", 1, null, "user-456"),
            CancellationToken.None);

        await repository.Received(1).AddAsync(Arg.Any<ContractNegotiation>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_TeamIdIsEmpty()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, Guid.Empty, "Team", "Title", "Desc",
                null, """["t"]""", 1, null, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProposedByTeamId");
    }

    [Fact]
    public async Task Validator_Should_Fail_When_TitleIsEmpty()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, TeamId, "Team", "", "Desc",
                null, """["t"]""", 1, null, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Validator_Should_Fail_When_ParticipantCountIsZero()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, TeamId, "Team", "Title", "Desc",
                null, """["t"]""", 0, null, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ParticipantCount");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_AllValid()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                ContractId, TeamId, "Platform Team",
                "Update Orders API v2", "Breaking changes proposal",
                FixedNow.AddDays(30),
                """["team-1","team-2"]""", 2,
                """{"openapi":"3.0"}""", "user-123"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_Should_Fail_When_ContractIdIsEmptyGuid()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                Guid.Empty, TeamId, "Team", "Title", "Desc",
                null, """["t"]""", 1, null, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContractId");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_ContractIdIsNull()
    {
        var validator = new CreateContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, TeamId, "Team", "Title", "Desc",
                null, """["t"]""", 1, null, "user-123"));

        result.IsValid.Should().BeTrue();
    }
}
