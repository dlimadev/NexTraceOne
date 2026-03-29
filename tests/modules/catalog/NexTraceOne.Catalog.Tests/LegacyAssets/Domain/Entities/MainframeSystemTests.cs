using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate MainframeSystem do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, atualização de detalhes, ownership e LPAR.
/// </summary>
public sealed class MainframeSystemTests
{
    private static LparReference CreateLpar() =>
        LparReference.Create("SYSPLEX1", "LPAR01", "CICSPRD1");

    private static MainframeSystem CreateSystem() =>
        MainframeSystem.Create("PRD-SYS-01", "Banking", "Platform-Team", CreateLpar());

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var lpar = CreateLpar();

        var system = MainframeSystem.Create("PRD-SYS-01", "Banking", "Platform-Team", lpar);

        system.Name.Should().Be("PRD-SYS-01");
        system.Domain.Should().Be("Banking");
        system.TeamName.Should().Be("Platform-Team");
        system.Lpar.Should().Be(lpar);
        system.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => MainframeSystem.Create(null!, "Banking", "Platform-Team", CreateLpar());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => MainframeSystem.Create("  ", "Banking", "Platform-Team", CreateLpar());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDomain_ShouldThrow()
    {
        var act = () => MainframeSystem.Create("PRD-SYS-01", null!, "Platform-Team", CreateLpar());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTeamName_ShouldThrow()
    {
        var act = () => MainframeSystem.Create("PRD-SYS-01", "Banking", null!, CreateLpar());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullLpar_ShouldThrow()
    {
        var act = () => MainframeSystem.Create("PRD-SYS-01", "Banking", "Platform-Team", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var system = CreateSystem();

        system.Criticality.Should().Be(Criticality.Medium);
        system.LifecycleStatus.Should().Be(LifecycleStatus.Active);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;

        var system = CreateSystem();

        system.CreatedAt.Should().BeOnOrAfter(before);
        system.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var system = MainframeSystem.Create("  PRD-SYS-01  ", "Banking", "Platform-Team", CreateLpar());

        system.Name.Should().Be("PRD-SYS-01");
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        var system = CreateSystem();

        system.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateAllProperties()
    {
        var system = CreateSystem();

        system.UpdateDetails("Display Name", "Description", Criticality.High,
            LifecycleStatus.Deprecating, "z/OS 2.5", "4000 MIPS");

        system.DisplayName.Should().Be("Display Name");
        system.Description.Should().Be("Description");
        system.Criticality.Should().Be(Criticality.High);
        system.LifecycleStatus.Should().Be(LifecycleStatus.Deprecating);
        system.OperatingSystem.Should().Be("z/OS 2.5");
        system.MipsCapacity.Should().Be("4000 MIPS");
    }

    [Fact]
    public void UpdateDetails_ShouldSetUpdatedAt()
    {
        var system = CreateSystem();
        system.UpdatedAt.Should().BeNull();

        system.UpdateDetails("Display", "Desc", Criticality.High,
            LifecycleStatus.Active, "z/OS", "4000");

        system.UpdatedAt.Should().NotBeNull();
        system.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateOwnership_ShouldUpdateTeamAndOwners()
    {
        var system = CreateSystem();

        system.UpdateOwnership("New-Team", "tech-owner@co.com", "biz-owner@co.com");

        system.TeamName.Should().Be("New-Team");
        system.TechnicalOwner.Should().Be("tech-owner@co.com");
        system.BusinessOwner.Should().Be("biz-owner@co.com");
        system.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateOwnership_WithNullTeamName_ShouldThrow()
    {
        var system = CreateSystem();

        var act = () => system.UpdateOwnership(null!, "tech", "biz");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateLpar_ShouldUpdateLpar()
    {
        var system = CreateSystem();
        var newLpar = LparReference.Create("SYSPLEX2", "LPAR02");

        system.UpdateLpar(newLpar);

        system.Lpar.Should().Be(newLpar);
        system.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateLpar_WithNullLpar_ShouldThrow()
    {
        var system = CreateSystem();

        var act = () => system.UpdateLpar(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
