using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CopybookVersion do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes e factory method.
/// </summary>
public sealed class CopybookVersionTests
{
    private static CopybookId CreateCopybookId() => CopybookId.New();
    private const string ValidRawContent = "       01 CUSTOMER-REC.\n           05 CUST-NAME PIC X(30).";

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var copybookId = CreateCopybookId();

        var version = CopybookVersion.Create(copybookId, "v1.0", ValidRawContent, 2, 30, "FB");

        version.CopybookId.Should().Be(copybookId);
        version.VersionLabel.Should().Be("v1.0");
        version.RawContent.Should().Be(ValidRawContent);
        version.FieldCount.Should().Be(2);
        version.TotalLength.Should().Be(30);
        version.RecordFormat.Should().Be("FB");
        version.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var version = CopybookVersion.Create(CreateCopybookId(), "v1.0", ValidRawContent, 1, 10, null);

        version.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullRecordFormat_ShouldDefaultToEmpty()
    {
        var version = CopybookVersion.Create(CreateCopybookId(), "v1.0", ValidRawContent, 1, 10, null);

        version.RecordFormat.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldTrimVersionLabel()
    {
        var version = CopybookVersion.Create(CreateCopybookId(), "  v2.0  ", ValidRawContent, 1, 10, "FB");

        version.VersionLabel.Should().Be("v2.0");
    }

    [Fact]
    public void Create_WithNullCopybookId_ShouldThrow()
    {
        var act = () => CopybookVersion.Create(null!, "v1.0", ValidRawContent, 1, 10, "FB");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithEmptyVersionLabel_ShouldThrow()
    {
        var act = () => CopybookVersion.Create(CreateCopybookId(), "", ValidRawContent, 1, 10, "FB");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceVersionLabel_ShouldThrow()
    {
        var act = () => CopybookVersion.Create(CreateCopybookId(), "   ", ValidRawContent, 1, 10, "FB");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullRawContent_ShouldThrow()
    {
        var act = () => CopybookVersion.Create(CreateCopybookId(), "v1.0", null!, 1, 10, "FB");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyRawContent_ShouldThrow()
    {
        var act = () => CopybookVersion.Create(CreateCopybookId(), "v1.0", "", 1, 10, "FB");

        act.Should().Throw<ArgumentException>();
    }
}
