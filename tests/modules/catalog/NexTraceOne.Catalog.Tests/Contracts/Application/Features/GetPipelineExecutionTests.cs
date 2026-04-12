using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetPipelineExecutionFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetPipelineExecution.GetPipelineExecution;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetPipelineExecution — obtém detalhes de uma execução de pipeline.
/// Valida retorno de execução existente, erro quando não encontrada e validação de entrada.
/// </summary>
public sealed class GetPipelineExecutionTests
{
    private static readonly Guid ExecutionGuid = Guid.NewGuid();
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Retorna execução existente ───────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnExecution_When_Exists()
    {
        var execution = PipelineExecution.Create(
            ApiAssetId, "Orders API", "1.0.0",
            """["ServerStubs"]""", "csharp", "aspnet",
            1, "user-123", FixedDate);

        var repository = Substitute.For<IPipelineExecutionRepository>();
        repository.GetByIdAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(execution);

        var sut = new GetPipelineExecutionFeature.Handler(repository);
        var result = await sut.Handle(
            new GetPipelineExecutionFeature.Query(execution.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(ApiAssetId);
        result.Value.ContractName.Should().Be("Orders API");
        result.Value.ContractVersion.Should().Be("1.0.0");
        result.Value.TargetLanguage.Should().Be("csharp");
        result.Value.Status.Should().Be(PipelineExecutionStatus.Running);
        result.Value.TotalStages.Should().Be(1);
        result.Value.InitiatedByUserId.Should().Be("user-123");
    }

    // ── Erro quando não encontrada ───────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_When_ExecutionNotFound()
    {
        var repository = Substitute.For<IPipelineExecutionRepository>();
        repository.GetByIdAsync(Arg.Any<PipelineExecutionId>(), Arg.Any<CancellationToken>())
            .Returns((PipelineExecution?)null);

        var sut = new GetPipelineExecutionFeature.Handler(repository);
        var result = await sut.Handle(
            new GetPipelineExecutionFeature.Query(ExecutionGuid),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("PipelineExecution");
    }

    // ── Validador ────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_Should_Fail_When_ExecutionIdIsEmpty()
    {
        var validator = new GetPipelineExecutionFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetPipelineExecutionFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExecutionId");
    }

    [Fact]
    public async Task Validator_Should_Pass_When_ValidExecutionId()
    {
        var validator = new GetPipelineExecutionFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetPipelineExecutionFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
