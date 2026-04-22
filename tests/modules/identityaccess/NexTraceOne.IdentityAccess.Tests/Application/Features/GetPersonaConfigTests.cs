using NexTraceOne.BuildingBlocks.Application.Abstractions;

using GetPersonaConfigFeature = NexTraceOne.IdentityAccess.Application.Features.GetPersonaConfig.GetPersonaConfig;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários da feature GetPersonaConfig (Wave X.3 — Persona-Aware Adaptive Navigation).
/// Cobre derivação de persona por claim, retorno de quick actions e módulos priorizados por persona,
/// e cenário de utilizador não autenticado.
/// </summary>
public sealed class GetPersonaConfigTests
{
    private static ICurrentUser CreateUser(bool authenticated, string? persona = null)
    {
        var u = Substitute.For<ICurrentUser>();
        u.IsAuthenticated.Returns(authenticated);
        u.Persona.Returns(persona);
        return u;
    }

    [Fact]
    public async Task Handle_Returns_Error_When_User_Not_Authenticated()
    {
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(false));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("Engineer", "services")]
    [InlineData("TechLead", "services")]
    [InlineData("Architect", "services")]
    [InlineData("Product", "analytics")]
    [InlineData("Executive", "governance")]
    [InlineData("PlatformAdmin", "admin")]
    [InlineData("Auditor", "governance")]
    public async Task Handle_Returns_Correct_First_Module_For_Persona(string persona, string expectedFirstModule)
    {
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, persona));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Persona.Should().Be(persona);
        result.Value.PrioritizedModules.Should().NotBeEmpty();
        result.Value.PrioritizedModules[0].Should().Be(expectedFirstModule);
    }

    [Theory]
    [InlineData("Engineer", 5)]
    [InlineData("TechLead", 5)]
    [InlineData("Architect", 5)]
    [InlineData("Product", 5)]
    [InlineData("Executive", 5)]
    [InlineData("PlatformAdmin", 5)]
    [InlineData("Auditor", 5)]
    public async Task Handle_Returns_Five_QuickActions_For_Known_Persona(string persona, int expectedCount)
    {
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, persona));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.QuickActions.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task Handle_Returns_Engineer_Config_When_Persona_Claim_Is_Null()
    {
        // No persona claim → fallback to Engineer
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, null));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.Persona.Should().Be("Engineer");
        result.Value.PrioritizedModules[0].Should().Be("services");
    }

    [Fact]
    public async Task Handle_Returns_Default_Config_For_Unknown_Persona()
    {
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, "UnknownPersona"));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.QuickActions.Should().HaveCountGreaterThan(0);
        result.Value.PrioritizedModules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Handle_Executive_QuickActions_Include_Executive_Dashboard_Route()
    {
        var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, "Executive"));
        var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.QuickActions.Should()
            .Contain(qa => qa.To.Contains("/governance/executive/intelligence"));
    }

    [Fact]
    public async Task Handle_Each_QuickAction_Has_Non_Empty_Fields()
    {
        foreach (var persona in new[] { "Engineer", "TechLead", "Architect", "Product", "Executive", "PlatformAdmin", "Auditor" })
        {
            var sut = new GetPersonaConfigFeature.Handler(CreateUser(true, persona));
            var result = await sut.Handle(new GetPersonaConfigFeature.Query(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            foreach (var qa in result.Value.QuickActions)
            {
                qa.Id.Should().NotBeNullOrWhiteSpace();
                qa.LabelKey.Should().NotBeNullOrWhiteSpace();
                qa.Icon.Should().NotBeNullOrWhiteSpace();
                qa.To.Should().StartWith("/");
            }
        }
    }
}
