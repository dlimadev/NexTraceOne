using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetContractNegotiationFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractNegotiation.GetContractNegotiation;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetContractNegotiation — obtém detalhes de uma negociação de contrato.
/// Valida retorno de negociação existente, erro quando não encontrada e validação de entrada.
/// </summary>
public sealed class GetContractNegotiationTests
{
    private static readonly Guid NegotiationGuid = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Retorna negociação existente ──────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnNegotiation_When_Exists()
    {
        var negotiation = ContractNegotiation.Create(
            ContractId, TeamId, "Platform Team",
            "Update Orders API v2", "Breaking changes proposal",
            FixedDate.AddDays(30),
            """["team-1","team-2"]""", 2,
            """{"openapi":"3.0"}""", "user-123", FixedDate);

        var repository = Substitute.For<IContractNegotiationRepository>();
        repository.GetByIdAsync(Arg.Any<ContractNegotiationId>(), Arg.Any<CancellationToken>())
            .Returns(negotiation);

        var sut = new GetContractNegotiationFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractNegotiationFeature.Query(negotiation.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractId.Should().Be(ContractId);
        result.Value.ProposedByTeamId.Should().Be(TeamId);
        result.Value.ProposedByTeamName.Should().Be("Platform Team");
        result.Value.Title.Should().Be("Update Orders API v2");
        result.Value.Status.Should().Be(NegotiationStatus.Draft);
        result.Value.ParticipantCount.Should().Be(2);
        result.Value.InitiatedByUserId.Should().Be("user-123");
    }

    // ── Erro quando não encontrada ───────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_When_NegotiationNotFound()
    {
        var repository = Substitute.For<IContractNegotiationRepository>();
        repository.GetByIdAsync(Arg.Any<ContractNegotiationId>(), Arg.Any<CancellationToken>())
            .Returns((ContractNegotiation?)null);

        var sut = new GetContractNegotiationFeature.Handler(repository);
        var result = await sut.Handle(
            new GetContractNegotiationFeature.Query(NegotiationGuid),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("Negotiation");
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_NegotiationIdIsEmpty()
    {
        var validator = new GetContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetContractNegotiationFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NegotiationId");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_ValidNegotiationId()
    {
        var validator = new GetContractNegotiationFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetContractNegotiationFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
