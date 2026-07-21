using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractValidationSummary;
using NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractSpectral;
using NexTraceOne.Catalog.Application.Contracts.Validation;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para o linting nativo de contratos: ContractLintRunner + as features
/// ValidateContractSpectral e GetContractValidationSummary.
/// </summary>
public sealed class ContractValidationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private const string GoodSpec =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"Payments\",\"version\":\"1.0.0\",\"description\":\"d\"}," +
        "\"servers\":[{\"url\":\"https://api.x\"}],\"paths\":{\"/p\":{}},\"components\":{\"schemas\":{\"P\":{}}}}";
    private const string BadSpec =
        "{\"servers\":[{\"url\":\"http://insecure\"}]}";

    private static ContractVersion MakeContract(string spec) =>
        ContractVersion.Import(Guid.NewGuid(), "1.0.0", spec, "json", "test").Value!;

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── ContractLintRunner ─────────────────────────────────────────────────

    [Fact]
    public void Runner_CleanSpec_HasNoErrors_IsPublishReady()
    {
        var result = ContractLintRunner.Run(GoodSpec, "json", FixedNow);

        result.Summary.ErrorCount.Should().Be(0);
        result.Summary.IsPublishReady.Should().BeTrue();
        result.Summary.OverallStatus.Should().Be("Valid");
        result.Summary.Fingerprint.Should().NotBeNullOrWhiteSpace();
        result.Summary.Sources.Should().Contain("internal");
    }

    [Fact]
    public void Runner_MissingTitle_ProducesError_NotPublishReady()
    {
        var result = ContractLintRunner.Run(BadSpec, "json", FixedNow);

        result.Issues.Should().Contain(i => i.RuleId == "info-title" && i.Severity == ValidationSeverity.Error);
        result.Summary.ErrorCount.Should().BeGreaterThan(0);
        result.Summary.IsPublishReady.Should().BeFalse();
        result.Summary.OverallStatus.Should().Be("Invalid");
    }

    [Fact]
    public void Runner_HttpServer_ProducesWarning()
    {
        var result = ContractLintRunner.Run(BadSpec, "json", FixedNow);
        result.Issues.Should().Contain(i => i.RuleId == "no-http-server" && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Runner_InvalidJson_ProducesError()
    {
        var result = ContractLintRunner.Run("not json", "json", FixedNow);
        result.Issues.Should().Contain(i => i.RuleId == "spec-valid-json");
        result.Summary.IsPublishReady.Should().BeFalse();
    }

    [Fact]
    public void Runner_NonJsonFormat_ReturnsInfoOnly()
    {
        var result = ContractLintRunner.Run("<wsdl/>", "wsdl", FixedNow);
        result.Issues.Should().ContainSingle(i => i.RuleId == "lint-format" && i.Severity == ValidationSeverity.Info);
        result.Summary.ErrorCount.Should().Be(0);
    }

    // ── ValidateContractSpectral ───────────────────────────────────────────

    [Fact]
    public async Task Validate_Existing_ReturnsIssuesAndSummary()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(MakeContract(GoodSpec));
        var handler = new ValidateContractSpectral.Handler(repo, CreateClock());

        var result = await handler.Handle(new ValidateContractSpectral.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.ValidatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Validate_Unknown_ReturnsNotFound()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns((ContractVersion?)null);
        var handler = new ValidateContractSpectral.Handler(repo, CreateClock());

        var result = await handler.Handle(new ValidateContractSpectral.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContractVersion.NotFound");
    }

    // ── GetContractValidationSummary ───────────────────────────────────────

    [Fact]
    public async Task Summary_Existing_ReturnsSummary()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(MakeContract(GoodSpec));
        var handler = new GetContractValidationSummary.Handler(repo, CreateClock());

        var result = await handler.Handle(new GetContractValidationSummary.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublishReady.Should().BeTrue();
    }

    [Fact]
    public async Task Summary_Unknown_ReturnsNotFound()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns((ContractVersion?)null);
        var handler = new GetContractValidationSummary.Handler(repo, CreateClock());

        var result = await handler.Handle(new GetContractValidationSummary.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Validators_EmptyId_Fail()
    {
        new ValidateContractSpectral.Validator().Validate(new ValidateContractSpectral.Query(Guid.Empty)).IsValid.Should().BeFalse();
        new GetContractValidationSummary.Validator().Validate(new GetContractValidationSummary.Query(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
