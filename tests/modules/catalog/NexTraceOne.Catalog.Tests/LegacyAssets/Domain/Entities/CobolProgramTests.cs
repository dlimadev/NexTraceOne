using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CobolProgram do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e atualização de detalhes.
/// </summary>
public sealed class CobolProgramTests
{
    private static MainframeSystemId CreateSystemId() => MainframeSystemId.New();

    private static CobolProgram CreateProgram() =>
        CobolProgram.Create("PAYROLL01", CreateSystemId());

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var systemId = CreateSystemId();

        var program = CobolProgram.Create("PAYROLL01", systemId);

        program.Name.Should().Be("PAYROLL01");
        program.SystemId.Should().Be(systemId);
        program.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => CobolProgram.Create(null!, CreateSystemId());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => CobolProgram.Create("  ", CreateSystemId());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullSystemId_ShouldThrow()
    {
        var act = () => CobolProgram.Create("PAYROLL01", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldSetDefaultLanguageToCOBOL()
    {
        var program = CreateProgram();

        program.Language.Should().Be("COBOL");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var program = CreateProgram();

        program.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var program = CobolProgram.Create("  PAYROLL01  ", CreateSystemId());

        program.Name.Should().Be("PAYROLL01");
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateAllProperties()
    {
        var program = CreateProgram();
        var compiledAt = DateTimeOffset.UtcNow.AddDays(-1);

        program.UpdateDetails("Display", "Batch payroll", "V6R1",
            compiledAt, "SRC.COBOL", "LOAD.MODULE",
            Criticality.Critical, LifecycleStatus.Active);

        program.DisplayName.Should().Be("Display");
        program.Description.Should().Be("Batch payroll");
        program.CompilerVersion.Should().Be("V6R1");
        program.LastCompiled.Should().Be(compiledAt);
        program.SourceLibrary.Should().Be("SRC.COBOL");
        program.LoadModule.Should().Be("LOAD.MODULE");
        program.Criticality.Should().Be(Criticality.Critical);
        program.LifecycleStatus.Should().Be(LifecycleStatus.Active);
        program.UpdatedAt.Should().NotBeNull();
    }
}
