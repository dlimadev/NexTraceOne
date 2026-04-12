using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ExecuteContractPipelineFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExecuteContractPipeline.ExecuteContractPipeline;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler ExecuteContractPipeline — inicia a execução de um pipeline de geração de código.
/// Valida criação com estado Running, persistência via UnitOfWork e validação de entrada.
/// </summary>
public sealed class ExecuteContractPipelineTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Cria execução com sucesso ────────────────────────────────────

    [Fact]
    public async Task Handle_Should_CreateExecution_When_ValidCommand()
    {
        var repository = Substitute.For<IPipelineExecutionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ExecuteContractPipelineFeature.Handler(repository, unitOfWork, dateTimeProvider);
        var result = await handler.Handle(
            new ExecuteContractPipelineFeature.Command(
                ApiAssetId, "Orders API", "1.0.0",
                """["ServerStubs","ClientSdk"]""",
                "csharp", "aspnet", 2, "user-123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(ApiAssetId);
        result.Value.ContractName.Should().Be("Orders API");
        result.Value.ContractVersion.Should().Be("1.0.0");
        result.Value.Status.Should().Be(PipelineExecutionStatus.Running);
        result.Value.TotalStages.Should().Be(2);
        result.Value.StartedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_Should_PersistExecution_Via_Repository()
    {
        var repository = Substitute.For<IPipelineExecutionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ExecuteContractPipelineFeature.Handler(repository, unitOfWork, dateTimeProvider);
        await handler.Handle(
            new ExecuteContractPipelineFeature.Command(
                ApiAssetId, "Orders API", "1.0.0",
                """["ServerStubs"]""",
                "typescript", null, 1, "user-456"),
            CancellationToken.None);

        await repository.Received(1).AddAsync(Arg.Any<PipelineExecution>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_ApiAssetIdIsEmpty()
    {
        var validator = new ExecuteContractPipelineFeature.Validator();
        var result = await validator.ValidateAsync(
            new ExecuteContractPipelineFeature.Command(
                Guid.Empty, "Orders API", "1.0.0",
                """["ServerStubs"]""", "csharp", null, 1, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    [Fact]
    public async Task Validator_Should_Fail_When_ContractNameIsEmpty()
    {
        var validator = new ExecuteContractPipelineFeature.Validator();
        var result = await validator.ValidateAsync(
            new ExecuteContractPipelineFeature.Command(
                ApiAssetId, "", "1.0.0",
                """["ServerStubs"]""", "csharp", null, 1, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContractName");
    }

    [Fact]
    public async Task Validator_Should_Fail_When_TotalStagesIsZero()
    {
        var validator = new ExecuteContractPipelineFeature.Validator();
        var result = await validator.ValidateAsync(
            new ExecuteContractPipelineFeature.Command(
                ApiAssetId, "Orders API", "1.0.0",
                """["ServerStubs"]""", "csharp", null, 0, "user-123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalStages");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_AllValid()
    {
        var validator = new ExecuteContractPipelineFeature.Validator();
        var result = await validator.ValidateAsync(
            new ExecuteContractPipelineFeature.Command(
                ApiAssetId, "Orders API", "1.0.0",
                """["ServerStubs","ClientSdk"]""",
                "csharp", "aspnet", 2, "user-123"));

        result.IsValid.Should().BeTrue();
    }
}
