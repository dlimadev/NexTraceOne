using NexTraceOne.Catalog.Application.LegacyAssets.Features.ImportCopybookLayout;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Validators;

/// <summary>
/// Testes do validator ImportCopybookLayout do sub-domínio Legacy Assets.
/// Cobre regras de validação para os campos do comando.
/// </summary>
public sealed class ImportCopybookLayoutValidatorTests
{
    private readonly ImportCopybookLayout.Validator _validator = new();

    private static ImportCopybookLayout.Command CreateValidCommand() =>
        new(Guid.NewGuid(), "       01 CUSTOMER-REC.\n           05 CUST-NAME PIC X(30).", "v1.0");

    [Fact]
    public void Valid_Command_ShouldPass()
    {
        var result = _validator.Validate(CreateValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCopybookId_ShouldFail()
    {
        var command = CreateValidCommand() with { CopybookId = Guid.Empty };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CopybookId");
    }

    [Fact]
    public void EmptyCopybookText_ShouldFail()
    {
        var command = CreateValidCommand() with { CopybookText = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CopybookText");
    }

    [Fact]
    public void EmptyVersionLabel_ShouldFail()
    {
        var command = CreateValidCommand() with { VersionLabel = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VersionLabel");
    }

    [Fact]
    public void VersionLabel_ExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { VersionLabel = new string('v', 101) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VersionLabel");
    }
}
