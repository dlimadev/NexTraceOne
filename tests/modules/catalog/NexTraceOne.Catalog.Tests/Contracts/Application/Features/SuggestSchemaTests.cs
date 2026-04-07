using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using SuggestSchemaFeature = NexTraceOne.Catalog.Application.Contracts.Features.SuggestSchemaFromCanonicalEntities.SuggestSchemaFromCanonicalEntities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes da feature SuggestSchemaFromCanonicalEntities — validação do motor
/// de sugestão de schemas baseado em entidades canónicas e correspondência de keywords.
/// </summary>
public sealed class SuggestSchemaTests
{
    private static CanonicalEntity CreateEntity(string name, string domain, string description = "")
        => CanonicalEntity.Create(
            name: name,
            description: description,
            domain: domain,
            category: "entity",
            owner: "team-a",
            schemaContent: """{"type":"object","properties":{"id":{"type":"string"}}}""");

    [Fact]
    public async Task Should_Return_Suggestions_When_Context_Matches()
    {
        var entity = CreateEntity("CustomerAddress", "customer", "Standard customer address model");

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();

        entityRepo.SearchAsync("customer", null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity> { entity }, 1));
        entityRepo.SearchAsync("address", null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity> { entity }, 1));

        var handler = new SuggestSchemaFeature.Handler(entityRepo);
        var result = await handler.Handle(
            new SuggestSchemaFeature.Query("customer address"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Suggestions.Should().NotBeEmpty();
        result.Value.Suggestions[0].EntityName.Should().Be("CustomerAddress");
        result.Value.Suggestions[0].RefPath.Should().Be("#/components/schemas/CustomerAddress");
        result.Value.Suggestions[0].RelevanceScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Return_Empty_Suggestions_When_No_Match()
    {
        var entityRepo = Substitute.For<ICanonicalEntityRepository>();

        entityRepo.SearchAsync("nonexistent", null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity>(), 0));
        entityRepo.SearchAsync("concept", null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity>(), 0));

        var handler = new SuggestSchemaFeature.Handler(entityRepo);
        var result = await handler.Handle(
            new SuggestSchemaFeature.Query("nonexistent concept"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Filter_By_Domain_When_Provided()
    {
        var paymentEntity = CreateEntity("PaymentMethod", "payments", "Payment method details");

        var entityRepo = Substitute.For<ICanonicalEntityRepository>();

        entityRepo.SearchAsync("payment", "payments", null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity> { paymentEntity }, 1));
        entityRepo.SearchAsync("method", "payments", null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<CanonicalEntity> { paymentEntity }, 1));

        var handler = new SuggestSchemaFeature.Handler(entityRepo);
        var result = await handler.Handle(
            new SuggestSchemaFeature.Query("payment method", Domain: "payments"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Suggestions.Should().NotBeEmpty();
        result.Value.Suggestions.Should().AllSatisfy(s => s.Domain.Should().Be("payments"));
    }
}
