using NexTraceOne.Catalog.Application.LegacyAssets.Features.DiffCopybookVersions;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Application.Validators;

/// <summary>
/// Testes do validator DiffCopybookVersions do sub-domínio Legacy Assets.
/// Cobre regras de validação para os campos da query.
/// </summary>
public sealed class DiffCopybookVersionsValidatorTests
{
    private readonly DiffCopybookVersions.Validator _validator = new();

    private static DiffCopybookVersions.Query CreateValidQuery() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Valid_Query_ShouldPass()
    {
        var result = _validator.Validate(CreateValidQuery());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCopybookId_ShouldFail()
    {
        var query = CreateValidQuery() with { CopybookId = Guid.Empty };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CopybookId");
    }

    [Fact]
    public void EmptyBaseVersionId_ShouldFail()
    {
        var query = CreateValidQuery() with { BaseVersionId = Guid.Empty };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BaseVersionId");
    }

    [Fact]
    public void EmptyTargetVersionId_ShouldFail()
    {
        var query = CreateValidQuery() with { TargetVersionId = Guid.Empty };
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetVersionId");
    }
}
