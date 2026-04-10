using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateContractTemplate.CreateContractTemplate;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteContractTemplate.DeleteContractTemplate;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListContractTemplates.ListContractTemplates;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateContractTemplate, DeleteContractTemplate e ListContractTemplates —
/// gestão de templates de contrato por tenant.
/// </summary>
public sealed class ContractTemplateTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static ICurrentUser CreateAuthenticatedUser(string id = "user-123")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns("Test User");
        user.Email.Returns($"{id}@test.com");
        return user;
    }

    private static ICurrentUser CreateAnonymousUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    private static ICurrentTenant CreateTenant(string id = "00000000-0000-0000-0000-000000000001")
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.Parse(id));
        return tenant;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── CreateContractTemplate ────────────────────────────────────────────────

    [Fact]
    public async Task CreateContractTemplate_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("REST Basic", "REST", "{\"openapi\":\"3.0\"}", "Basic REST template"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("REST Basic");
        result.Value.ContractType.Should().Be("REST");
        result.Value.Description.Should().Be("Basic REST template");
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<ContractTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractTemplate_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Template", "REST", "{}", "desc"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteContractTemplate ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteContractTemplate_Should_Delete_When_Found()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var template = ContractTemplate.Create(
            "00000000-0000-0000-0000-000000000001", "REST Basic", "REST", "{}", "desc", "user-123", false, FixedNow);
        repo.GetByIdAsync(Arg.Any<ContractTemplateId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(template.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(template.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<ContractTemplateId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContractTemplate_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.GetByIdAsync(Arg.Any<ContractTemplateId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractTemplate?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteContractTemplate_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ListContractTemplates ─────────────────────────────────────────────────

    [Fact]
    public async Task ListContractTemplates_Should_Return_Templates()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var templates = new List<ContractTemplate>
        {
            ContractTemplate.Create("00000000-0000-0000-0000-000000000001", "REST Basic", "REST", "{}", "desc A", "user-123", false, FixedNow),
            ContractTemplate.Create("00000000-0000-0000-0000-000000000001", "SOAP Basic", "SOAP", "{}", "desc B", "user-123", false, FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(templates);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListContractTemplates_Should_Filter_By_ContractType()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var templates = new List<ContractTemplate>
        {
            ContractTemplate.Create("00000000-0000-0000-0000-000000000001", "SOAP Standard", "SOAP", "{}", "desc", "user-123", false, FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), "SOAP", Arg.Any<CancellationToken>())
            .Returns(templates);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query("SOAP"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ContractType.Should().Be("SOAP");
        await repo.Received(1).ListByTenantAsync(Arg.Any<string>(), "SOAP", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListContractTemplates_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IContractTemplateRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }
}
