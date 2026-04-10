using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateTaxonomyCategory.CreateTaxonomyCategory;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListTaxonomyCategories.ListTaxonomyCategories;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateTaxonomyCategory e ListTaxonomyCategories —
/// gestão de categorias de taxonomia definidas pelo admin do tenant.
/// </summary>
public sealed class TaxonomyCategoryTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── CreateTaxonomyCategory ───────────────────────────────────────────────

    [Fact]
    public async Task CreateTaxonomyCategory_Should_Create_Successfully()
    {
        var repo = Substitute.For<ITaxonomyRepository>();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("tenant-001", "Business Domain", "Domain classification", true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Business Domain");
        await repo.Received(1).AddCategoryAsync(Arg.Any<TaxonomyCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTaxonomyCategory_Should_Return_Correct_Properties()
    {
        var repo = Substitute.For<ITaxonomyRepository>();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("tenant-001", "Tier", "Service tier level", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().NotBeEmpty();
        result.Value.Name.Should().Be("Tier");
        result.Value.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTaxonomyCategory_With_Optional_Fields()
    {
        var repo = Substitute.For<ITaxonomyRepository>();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("tenant-001", "Data Classification", string.Empty, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Data Classification");
        result.Value.IsRequired.Should().BeFalse();
        await repo.Received(1).AddCategoryAsync(
            Arg.Is<TaxonomyCategory>(c => c.Description == string.Empty),
            Arg.Any<CancellationToken>());
    }

    // ── ListTaxonomyCategories ───────────────────────────────────────────────

    [Fact]
    public async Task ListTaxonomyCategories_Should_Return_Categories()
    {
        var repo = Substitute.For<ITaxonomyRepository>();

        var categories = new List<TaxonomyCategory>
        {
            TaxonomyCategory.Create("tenant-001", "Business Domain", "Domain classification", true, FixedNow),
            TaxonomyCategory.Create("tenant-001", "Tier", "Service tier", false, FixedNow),
        };
        repo.ListCategoriesAsync("tenant-001", Arg.Any<CancellationToken>())
            .Returns(categories);

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(
            new ListFeature.Query("tenant-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Name.Should().Be("Business Domain");
        result.Value.Items[1].Name.Should().Be("Tier");
    }

    [Fact]
    public async Task ListTaxonomyCategories_Should_Return_Empty_When_None()
    {
        var repo = Substitute.For<ITaxonomyRepository>();

        repo.ListCategoriesAsync("tenant-001", Arg.Any<CancellationToken>())
            .Returns(new List<TaxonomyCategory>());

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(
            new ListFeature.Query("tenant-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
