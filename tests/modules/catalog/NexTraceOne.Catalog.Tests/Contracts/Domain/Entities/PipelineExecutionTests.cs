using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="PipelineExecution"/>.
/// Valida criação, conclusão, falha, estados parciais e validação de entrada.
/// </summary>
public sealed class PipelineExecutionTests
{
    private static readonly Guid ValidApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);
    private const string ValidStages = """["ServerStubs","ClientSdk"]""";
    private const string ValidLanguage = "csharp";
    private const string ValidFramework = "aspnet";
    private const string ValidUserId = "user-123";
    private const string ValidContractName = "Orders API";
    private const string ValidContractVersion = "1.0.0";

    // ── Create com valores válidos ───────────────────────────────────

    [Fact]
    public void Create_Should_SetAllProperties_When_ValidValues()
    {
        var execution = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        execution.Id.Value.Should().NotBeEmpty();
        execution.ApiAssetId.Should().Be(ValidApiAssetId);
        execution.ContractName.Should().Be(ValidContractName);
        execution.ContractVersion.Should().Be(ValidContractVersion);
        execution.RequestedStages.Should().Be(ValidStages);
        execution.TargetLanguage.Should().Be(ValidLanguage);
        execution.TargetFramework.Should().Be(ValidFramework);
        execution.Status.Should().Be(PipelineExecutionStatus.Running);
        execution.TotalStages.Should().Be(2);
        execution.CompletedStages.Should().Be(0);
        execution.FailedStages.Should().Be(0);
        execution.StartedAt.Should().Be(FixedDate);
        execution.CompletedAt.Should().BeNull();
        execution.DurationMs.Should().BeNull();
        execution.ErrorMessage.Should().BeNull();
        execution.InitiatedByUserId.Should().Be(ValidUserId);
    }

    [Fact]
    public void Create_Should_AcceptNullTargetFramework()
    {
        var execution = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, null,
            2, ValidUserId, FixedDate);

        execution.TargetFramework.Should().BeNull();
    }

    // ── Complete ─────────────────────────────────────────────────────

    [Fact]
    public void Complete_Should_SetCompletedStatus()
    {
        var execution = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        var completedAt = FixedDate.AddMinutes(5);
        var stageResults = """{"ServerStubs":"ok","ClientSdk":"ok"}""";
        var artifacts = """["output.zip"]""";

        execution.Complete(stageResults, artifacts, 2, completedAt);

        execution.Status.Should().Be(PipelineExecutionStatus.Completed);
        execution.StageResults.Should().Be(stageResults);
        execution.GeneratedArtifacts.Should().Be(artifacts);
        execution.CompletedStages.Should().Be(2);
        execution.FailedStages.Should().Be(0);
        execution.CompletedAt.Should().Be(completedAt);
        execution.DurationMs.Should().Be((long)(completedAt - FixedDate).TotalMilliseconds);
    }

    // ── Fail ─────────────────────────────────────────────────────────

    [Fact]
    public void Fail_Should_SetFailedStatus_When_NoStagesCompleted()
    {
        var execution = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        var completedAt = FixedDate.AddMinutes(1);

        execution.Fail("Generation error", null, 0, 2, completedAt);

        execution.Status.Should().Be(PipelineExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be("Generation error");
        execution.CompletedStages.Should().Be(0);
        execution.FailedStages.Should().Be(2);
        execution.CompletedAt.Should().Be(completedAt);
        execution.DurationMs.Should().Be((long)(completedAt - FixedDate).TotalMilliseconds);
    }

    [Fact]
    public void Fail_Should_SetPartiallyCompletedStatus_When_SomeStagesCompleted()
    {
        var execution = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        var completedAt = FixedDate.AddMinutes(3);
        var stageResults = """{"ServerStubs":"ok","ClientSdk":"error"}""";

        execution.Fail("Partial failure", stageResults, 1, 1, completedAt);

        execution.Status.Should().Be(PipelineExecutionStatus.PartiallyCompleted);
        execution.ErrorMessage.Should().Be("Partial failure");
        execution.StageResults.Should().Be(stageResults);
        execution.CompletedStages.Should().Be(1);
        execution.FailedStages.Should().Be(1);
    }

    // ── Validação com valores inválidos ──────────────────────────────

    [Fact]
    public void Create_Should_Throw_When_ApiAssetIdIsDefault()
    {
        var act = () => PipelineExecution.Create(
            Guid.Empty, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_ContractNameIsNullOrWhiteSpace(string? invalidName)
    {
        var act = () => PipelineExecution.Create(
            ValidApiAssetId, invalidName!, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_TargetLanguageIsNullOrWhiteSpace(string? invalidLanguage)
    {
        var act = () => PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, invalidLanguage!, ValidFramework,
            2, ValidUserId, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Throw_When_TotalStagesIsNotPositive(int invalidStages)
    {
        var act = () => PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            invalidStages, ValidUserId, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    // ── IDs únicos ──────────────────────────────────────────────────

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var exec1 = PipelineExecution.Create(
            ValidApiAssetId, ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);
        var exec2 = PipelineExecution.Create(
            Guid.NewGuid(), ValidContractName, ValidContractVersion,
            ValidStages, ValidLanguage, ValidFramework,
            2, ValidUserId, FixedDate);

        exec1.Id.Should().NotBe(exec2.Id);
    }

    // ── PipelineExecutionId ─────────────────────────────────────────

    [Fact]
    public void PipelineExecutionId_New_Should_CreateUniqueId()
    {
        var id1 = PipelineExecutionId.New();
        var id2 = PipelineExecutionId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineExecutionId_From_Should_PreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = PipelineExecutionId.From(guid);

        id.Value.Should().Be(guid);
    }
}
