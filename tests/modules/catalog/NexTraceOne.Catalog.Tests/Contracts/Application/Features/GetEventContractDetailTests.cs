using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using GetEventContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractDetail.GetEventContractDetail;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="GetEventContractDetailFeature"/>.
/// Valida que a query retorna os detalhes AsyncAPI corretamente ou erro quando não encontrado.
/// </summary>
public sealed class GetEventContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    private static IEventContractDetailRepository CreateRepository() =>
        Substitute.For<IEventContractDetailRepository>();

    private static EventContractDetail CreateDetail(ContractVersionId versionId) =>
        EventContractDetail.Create(
            versionId,
            title: "UserEventService",
            asyncApiVersion: "2.6.0",
            channelsJson: """{"user/signedup":["PUBLISH"]}""",
            messagesJson: """{"UserSignedUp":["userId","email"]}""",
            serversJson: """{"production":"kafka.example.com:9092"}""",
            defaultContentType: "application/json").Value;

    [Fact]
    public async Task Handle_Should_Return_Detail_When_Found()
    {
        var repository = CreateRepository();
        var detail = CreateDetail(ValidVersionId);
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var sut = new GetEventContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetEventContractDetailFeature.Query(ValidVersionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("UserEventService");
        result.Value.AsyncApiVersion.Should().Be("2.6.0");
        result.Value.DefaultContentType.Should().Be("application/json");
        result.Value.ChannelsJson.Should().Contain("user/signedup");
        result.Value.ServersJson.Should().Contain("kafka.example.com:9092");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Detail_Missing()
    {
        var repository = CreateRepository();
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((EventContractDetail?)null);

        var sut = new GetEventContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetEventContractDetailFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Event.DetailNotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_Correct_ContractVersionId()
    {
        var versionId = ContractVersionId.From(Guid.NewGuid());
        var repository = CreateRepository();
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(CreateDetail(versionId));

        var sut = new GetEventContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetEventContractDetailFeature.Query(versionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(versionId.Value);
    }

    [Fact]
    public void Validator_Should_Fail_For_Empty_ContractVersionId()
    {
        var validator = new GetEventContractDetailFeature.Validator();
        var result = validator.Validate(new GetEventContractDetailFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_ContractVersionId()
    {
        var validator = new GetEventContractDetailFeature.Validator();
        var result = validator.Validate(new GetEventContractDetailFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
