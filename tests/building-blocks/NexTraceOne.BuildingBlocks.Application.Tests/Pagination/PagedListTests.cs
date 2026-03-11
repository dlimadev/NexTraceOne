using FluentAssertions;
using NexTraceOne.BuildingBlocks.Application.Pagination;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Pagination;

/// <summary>
/// Testes para o container de paginação PagedList{T}.
/// </summary>
public sealed class PagedListTests
{
    [Fact]
    public void Create_Should_SetMetadata_When_ItemsAndCountProvided()
    {
        var items = new[] { "a", "b", "c" };
        var paged = PagedList<string>.Create(items, total: 100, page: 2, size: 10);

        paged.Items.Should().BeEquivalentTo(items);
        paged.TotalCount.Should().Be(100);
        paged.Page.Should().Be(2);
        paged.PageSize.Should().Be(10);
        paged.TotalPages.Should().Be(10);
        paged.HasPrevious.Should().BeTrue();
        paged.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasPrevious_Should_BeFalse_When_OnFirstPage()
    {
        var paged = PagedList<int>.Create([1, 2], total: 50, page: 1, size: 10);

        paged.HasPrevious.Should().BeFalse();
        paged.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_Should_BeFalse_When_OnLastPage()
    {
        var paged = PagedList<int>.Create([1, 2], total: 5, page: 1, size: 10);

        paged.HasNext.Should().BeFalse();
    }

    [Fact]
    public void Empty_Should_ReturnEmptyResult_When_Called()
    {
        var paged = PagedList<string>.Empty();

        paged.IsEmpty.Should().BeTrue();
        paged.TotalCount.Should().Be(0);
        paged.TotalPages.Should().Be(0);
    }
}
