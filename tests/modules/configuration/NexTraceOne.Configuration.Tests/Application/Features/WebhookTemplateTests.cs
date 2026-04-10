using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateWebhookTemplate.CreateWebhookTemplate;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteWebhookTemplate.DeleteWebhookTemplate;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListWebhookTemplates.ListWebhookTemplates;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleWebhookTemplate.ToggleWebhookTemplate;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateWebhookTemplate, DeleteWebhookTemplate, ListWebhookTemplates e ToggleWebhookTemplate —
/// gestão de templates de payload personalizados para webhooks do tenant.
/// </summary>
public sealed class WebhookTemplateTests
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

    // ── CreateWebhookTemplate ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateWebhookTemplate_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentTenant, currentUser, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Deploy Notification", "change.created", "{\"event\":\"{{event}}\"}", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Deploy Notification");
        result.Value.EventType.Should().Be("change.created");
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<WebhookTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWebhookTemplate_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentTenant, currentUser, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Template", "change.created", "{}", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteWebhookTemplate ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteWebhookTemplate_Should_Delete_When_Found()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();

        var template = WebhookTemplate.Create(
            "00000000-0000-0000-0000-000000000001", "My Template", "incident.opened", "{}", null, FixedNow);
        repo.GetByIdAsync(Arg.Any<WebhookTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(template.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplateId.Should().Be(template.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<WebhookTemplateId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteWebhookTemplate_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<WebhookTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((WebhookTemplate?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ListWebhookTemplates ──────────────────────────────────────────────────

    [Fact]
    public async Task ListWebhookTemplates_Should_Return_Templates()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var templates = new List<WebhookTemplate>
        {
            WebhookTemplate.Create("00000000-0000-0000-0000-000000000001", "Template A", "change.created", "{}", null, FixedNow),
            WebhookTemplate.Create("00000000-0000-0000-0000-000000000001", "Template B", "incident.opened", "{}", null, FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(templates);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListWebhookTemplates_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ToggleWebhookTemplate ─────────────────────────────────────────────────

    [Fact]
    public async Task ToggleWebhookTemplate_Should_Toggle_When_Found()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();

        var template = WebhookTemplate.Create(
            "00000000-0000-0000-0000-000000000001", "My Template", "change.created", "{}", null, FixedNow);
        repo.GetByIdAsync(Arg.Any<WebhookTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(template);

        var sut = new ToggleFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new ToggleFeature.Command(template.Id.Value, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        template.IsEnabled.Should().BeFalse();
        await repo.Received(1).UpdateAsync(Arg.Any<WebhookTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleWebhookTemplate_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IWebhookTemplateRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<WebhookTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((WebhookTemplate?)null);

        var sut = new ToggleFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new ToggleFeature.Command(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
