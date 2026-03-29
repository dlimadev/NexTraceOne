using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade Copybook do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, atualização de layout e detalhes.
/// </summary>
public sealed class CopybookTests
{
    private static MainframeSystemId CreateSystemId() => MainframeSystemId.New();
    private static CopybookLayout CreateLayout() => CopybookLayout.Create(10, 80, "FB");

    private static Copybook CreateCopybook() =>
        Copybook.Create("CPY-CUSTOMER", CreateSystemId(), CreateLayout());

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var systemId = CreateSystemId();
        var layout = CreateLayout();

        var copybook = Copybook.Create("CPY-CUSTOMER", systemId, layout);

        copybook.Name.Should().Be("CPY-CUSTOMER");
        copybook.SystemId.Should().Be(systemId);
        copybook.Layout.Should().Be(layout);
        copybook.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => Copybook.Create(null!, CreateSystemId(), CreateLayout());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullSystemId_ShouldThrow()
    {
        var act = () => Copybook.Create("CPY-CUSTOMER", null!, CreateLayout());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullLayout_ShouldThrow()
    {
        var act = () => Copybook.Create("CPY-CUSTOMER", CreateSystemId(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var copybook = CreateCopybook();

        copybook.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateLayout_ShouldUpdateLayout()
    {
        var copybook = CreateCopybook();
        var newLayout = CopybookLayout.Create(20, 160, "VB");

        copybook.UpdateLayout(newLayout);

        copybook.Layout.Should().Be(newLayout);
        copybook.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateLayout_WithNull_ShouldThrow()
    {
        var copybook = CreateCopybook();

        var act = () => copybook.UpdateLayout(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateDetails_ShouldSetUpdatedAt()
    {
        var copybook = CreateCopybook();
        copybook.UpdatedAt.Should().BeNull();

        copybook.UpdateDetails("Display", "Desc", "v2", "SRC.LIB",
            "01 CUSTOMER-REC.", Criticality.High, LifecycleStatus.Active);

        copybook.UpdatedAt.Should().NotBeNull();
        copybook.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
