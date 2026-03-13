using FluentAssertions;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Tests.Domain;

/// <summary>
/// Testes de domínio para o aggregate SavedSearch.
/// Valida criação, registo de utilização e atualização de query.
/// </summary>
public sealed class SavedSearchTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnSavedSearch_When_InputIsValid()
    {
        var userId = Guid.NewGuid();

        var search = SavedSearch.Create(
            userId,
            "APIs de Pagamento",
            "payment gateway",
            """{"tags":["payments","pci"]}""",
            Now);

        search.UserId.Should().Be(userId);
        search.Name.Should().Be("APIs de Pagamento");
        search.SearchQuery.Should().Be("payment gateway");
        search.Filters.Should().Contain("payments");
        search.CreatedAt.Should().Be(Now);
        search.LastUsedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_AcceptNullFilters()
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "Todas as APIs",
            "api",
            filters: null,
            Now);

        search.Filters.Should().BeNull();
    }

    [Fact]
    public void MarkUsed_Should_UpdateLastUsedAt()
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs REST",
            "rest openapi",
            null,
            Now);

        var usedAt = new DateTimeOffset(2025, 07, 01, 14, 30, 0, TimeSpan.Zero);
        search.MarkUsed(usedAt);

        search.LastUsedAt.Should().Be(usedAt);
    }

    [Fact]
    public void UpdateQuery_Should_ReturnSuccess_When_ValidData()
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs REST",
            "rest openapi",
            null,
            Now);

        var result = search.UpdateQuery(
            "APIs GraphQL",
            "graphql schema",
            """{"tags":["graphql"]}""");

        result.IsSuccess.Should().BeTrue();
        search.Name.Should().Be("APIs GraphQL");
        search.SearchQuery.Should().Be("graphql schema");
        search.Filters.Should().Contain("graphql");
    }

    [Fact]
    public void UpdateQuery_Should_AcceptNullFilters()
    {
        var search = SavedSearch.Create(
            Guid.NewGuid(),
            "APIs REST",
            "rest openapi",
            """{"tags":["rest"]}""",
            Now);

        var result = search.UpdateQuery("Nova pesquisa", "grpc protobuf", filters: null);

        result.IsSuccess.Should().BeTrue();
        search.Filters.Should().BeNull();
    }
}
