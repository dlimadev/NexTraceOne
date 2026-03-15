using FluentAssertions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Tests.SourceOfTruth.Domain.Entities;

/// <summary>
/// Testes da entidade LinkedReference — referência vinculada no Source of Truth.
/// </summary>
public sealed class LinkedReferenceTests
{
    [Fact]
    public void Create_Should_SetAllProperties()
    {
        var assetId = Guid.NewGuid();
        var ref1 = LinkedReference.Create(
            assetId, LinkedAssetType.Service, LinkedReferenceType.Documentation,
            "API Docs", "Official API documentation", "https://docs.example.com");

        ref1.AssetId.Should().Be(assetId);
        ref1.AssetType.Should().Be(LinkedAssetType.Service);
        ref1.ReferenceType.Should().Be(LinkedReferenceType.Documentation);
        ref1.Title.Should().Be("API Docs");
        ref1.Description.Should().Be("Official API documentation");
        ref1.Url.Should().Be("https://docs.example.com");
        ref1.IsActive.Should().BeTrue();
        ref1.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithMinimalParams_Should_SetDefaults()
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Contract, LinkedReferenceType.Runbook, "Emergency Runbook");

        ref1.Description.Should().BeEmpty();
        ref1.Url.Should().BeNull();
        ref1.Content.Should().BeNull();
        ref1.Metadata.Should().BeNull();
        ref1.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyTitle_Should_Throw()
    {
        var act = () => LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Service, LinkedReferenceType.Documentation, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDefaultAssetId_Should_Throw()
    {
        var act = () => LinkedReference.Create(
            Guid.Empty, LinkedAssetType.Service, LinkedReferenceType.Documentation, "Title");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Should_ChangeProperties()
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Service, LinkedReferenceType.Documentation, "Old Title");

        ref1.Update("New Title", "New desc", "https://new.url", "inline content", "{\"key\":\"value\"}");

        ref1.Title.Should().Be("New Title");
        ref1.Description.Should().Be("New desc");
        ref1.Url.Should().Be("https://new.url");
        ref1.Content.Should().Be("inline content");
        ref1.Metadata.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse()
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Service, LinkedReferenceType.Runbook, "Runbook");

        ref1.Deactivate();
        ref1.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrue()
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Service, LinkedReferenceType.Runbook, "Runbook");
        ref1.Deactivate();

        ref1.Activate();
        ref1.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(LinkedReferenceType.Documentation)]
    [InlineData(LinkedReferenceType.Runbook)]
    [InlineData(LinkedReferenceType.OperationalNote)]
    [InlineData(LinkedReferenceType.ExternalLink)]
    [InlineData(LinkedReferenceType.Changelog)]
    [InlineData(LinkedReferenceType.EventTopic)]
    [InlineData(LinkedReferenceType.RelatedApi)]
    [InlineData(LinkedReferenceType.RelatedIncident)]
    public void Create_ShouldSupport_AllReferenceTypes(LinkedReferenceType refType)
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), LinkedAssetType.Service, refType, $"Test {refType}");

        ref1.ReferenceType.Should().Be(refType);
    }

    [Theory]
    [InlineData(LinkedAssetType.Service)]
    [InlineData(LinkedAssetType.Contract)]
    public void Create_ShouldSupport_AllAssetTypes(LinkedAssetType assetType)
    {
        var ref1 = LinkedReference.Create(
            Guid.NewGuid(), assetType, LinkedReferenceType.Documentation, "Test");

        ref1.AssetType.Should().Be(assetType);
    }
}
