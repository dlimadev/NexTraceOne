using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Validators;

/// <summary>
/// Testes do validator RegisterMainframeSystem do sub-domínio Legacy Assets.
/// Cobre validação de campos obrigatórios e limites de comprimento.
/// </summary>
public sealed class RegisterMainframeSystemValidatorTests
{
    private readonly RegisterMainframeSystem.Validator _validator = new();

    private static RegisterMainframeSystem.Command CreateValidCommand() =>
        new("PRD-SYS-01", "Banking", "Platform-Team", "SYSPLEX1", "LPAR01", "CICSPRD1");

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var result = _validator.Validate(CreateValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_ShouldFail()
    {
        var command = CreateValidCommand() with { Name = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void NameTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 201) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void EmptyDomain_ShouldFail()
    {
        var command = CreateValidCommand() with { Domain = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Domain");
    }

    [Fact]
    public void EmptyTeamName_ShouldFail()
    {
        var command = CreateValidCommand() with { TeamName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TeamName");
    }

    [Fact]
    public void EmptySysplexName_ShouldFail()
    {
        var command = CreateValidCommand() with { SysplexName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SysplexName");
    }

    [Fact]
    public void EmptyLparName_ShouldFail()
    {
        var command = CreateValidCommand() with { LparName = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LparName");
    }
}
