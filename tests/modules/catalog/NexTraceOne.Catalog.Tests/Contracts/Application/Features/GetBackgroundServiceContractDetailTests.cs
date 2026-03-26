using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using GetBackgroundServiceContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetBackgroundServiceContractDetail.GetBackgroundServiceContractDetail;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="GetBackgroundServiceContractDetailFeature"/>.
/// Valida que a query retorna os detalhes do processo corretamente ou erro quando não encontrado.
/// </summary>
public sealed class GetBackgroundServiceContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    private static IBackgroundServiceContractDetailRepository CreateRepository() =>
        Substitute.For<IBackgroundServiceContractDetailRepository>();

    private static BackgroundServiceContractDetail CreateDetail(ContractVersionId versionId) =>
        BackgroundServiceContractDetail.Create(
            versionId,
            serviceName: "OrderExpirationJob",
            category: "Job",
            triggerType: "Cron",
            inputsJson: "{}",
            outputsJson: "{}",
            sideEffectsJson: """["Marks expired orders"]""",
            scheduleExpression: "0 * * * *",
            timeoutExpression: "PT30M",
            allowsConcurrency: false).Value;

    [Fact]
    public async Task Handle_Should_Return_Detail_When_Found()
    {
        var repository = CreateRepository();
        var detail = CreateDetail(ValidVersionId);
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var sut = new GetBackgroundServiceContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetBackgroundServiceContractDetailFeature.Query(ValidVersionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("OrderExpirationJob");
        result.Value.Category.Should().Be("Job");
        result.Value.TriggerType.Should().Be("Cron");
        result.Value.ScheduleExpression.Should().Be("0 * * * *");
        result.Value.TimeoutExpression.Should().Be("PT30M");
        result.Value.AllowsConcurrency.Should().BeFalse();
        result.Value.SideEffectsJson.Should().Contain("expired orders");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Detail_Missing()
    {
        var repository = CreateRepository();
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((BackgroundServiceContractDetail?)null);

        var sut = new GetBackgroundServiceContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetBackgroundServiceContractDetailFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.BackgroundService.DetailNotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_Correct_ContractVersionId()
    {
        var versionId = ContractVersionId.From(Guid.NewGuid());
        var repository = CreateRepository();
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(CreateDetail(versionId));

        var sut = new GetBackgroundServiceContractDetailFeature.Handler(repository);
        var result = await sut.Handle(new GetBackgroundServiceContractDetailFeature.Query(versionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(versionId.Value);
    }

    [Fact]
    public void Validator_Should_Fail_For_Empty_ContractVersionId()
    {
        var validator = new GetBackgroundServiceContractDetailFeature.Validator();
        var result = validator.Validate(new GetBackgroundServiceContractDetailFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_ContractVersionId()
    {
        var validator = new GetBackgroundServiceContractDetailFeature.Validator();
        var result = validator.Validate(new GetBackgroundServiceContractDetailFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
