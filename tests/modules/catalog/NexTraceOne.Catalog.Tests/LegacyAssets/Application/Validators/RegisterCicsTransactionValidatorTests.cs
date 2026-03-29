using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCicsTransaction;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Validators;

/// <summary>
/// Testes do validator RegisterCicsTransaction do sub-domínio Legacy Assets.
/// Cobre validação de campos obrigatórios e limite de tamanho do transactionId.
/// </summary>
public sealed class RegisterCicsTransactionValidatorTests
{
    private readonly RegisterCicsTransaction.Validator _validator = new();

    private static RegisterCicsTransaction.Command CreateValidCommand() =>
        new("TXN1", Guid.NewGuid(), "PROG01", "CICSPRD1", "5.6", 1490);

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var result = _validator.Validate(CreateValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTransactionId_ShouldFail()
    {
        var command = CreateValidCommand() with { TransactionId = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TransactionId");
    }

    [Fact]
    public void TransactionIdTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { TransactionId = "TOOLONG" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TransactionId");
    }

    [Fact]
    public void EmptySystemId_ShouldFail()
    {
        var command = CreateValidCommand() with { SystemId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SystemId");
    }

    [Fact]
    public void EmptyProgramName_ShouldFail()
    {
        var command = CreateValidCommand() with { ProgramName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProgramName");
    }

    [Fact]
    public void EmptyRegionName_ShouldFail()
    {
        var command = CreateValidCommand() with { RegionName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RegionName");
    }
}
