using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.Configuration.Application.Abstractions;

using AddFeature = NexTraceOne.Configuration.Application.Features.AddEntityTag.AddEntityTag;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListEntityTags.ListEntityTags;
using RemoveFeature = NexTraceOne.Configuration.Application.Features.RemoveEntityTag.RemoveEntityTag;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de AddEntityTag, ListEntityTags e RemoveEntityTag —
/// gestão de tags associadas a entidades da plataforma.
/// </summary>
public sealed class EntityTagApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static EntityTag CreateExistingTag(
        string key = "environment",
        string value = "production",
        string tenantId = "tenant-001",
        string entityType = "service",
        string entityId = "svc-001")
    {
        return EntityTag.Create(tenantId, entityType, entityId, key, value, "user-001", FixedNow);
    }

    // ── AddEntityTag ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddEntityTag_Should_Create_New_Tag()
    {
        var repo = Substitute.For<IEntityTagRepository>();
        var clock = CreateClock();

        repo.ListByEntityAsync("tenant-001", "service", "svc-001", Arg.Any<CancellationToken>())
            .Returns(new List<EntityTag>());

        var sut = new AddFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new AddFeature.Command("tenant-001", "service", "svc-001", "environment", "production", "user-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("environment");
        result.Value.Value.Should().Be("production");
        await repo.Received(1).AddAsync(Arg.Any<EntityTag>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().UpdateAsync(Arg.Any<EntityTag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEntityTag_Should_Update_Existing_Tag()
    {
        var repo = Substitute.For<IEntityTagRepository>();
        var clock = CreateClock();

        var existing = CreateExistingTag();
        repo.ListByEntityAsync("tenant-001", "service", "svc-001", Arg.Any<CancellationToken>())
            .Returns(new List<EntityTag> { existing });

        var sut = new AddFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new AddFeature.Command("tenant-001", "service", "svc-001", "environment", "staging", "user-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("staging");
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddAsync(Arg.Any<EntityTag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEntityTag_Should_Return_Correct_Response()
    {
        var repo = Substitute.For<IEntityTagRepository>();
        var clock = CreateClock();

        repo.ListByEntityAsync("tenant-001", "service", "svc-001", Arg.Any<CancellationToken>())
            .Returns(new List<EntityTag>());

        var sut = new AddFeature.Handler(repo, clock);
        var result = await sut.Handle(
            new AddFeature.Command("tenant-001", "service", "svc-001", "tier", "critical", "user-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TagId.Should().NotBeEmpty();
        result.Value.Key.Should().Be("tier");
        result.Value.Value.Should().Be("critical");
    }

    // ── ListEntityTags ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListEntityTags_Should_Return_Tags()
    {
        var repo = Substitute.For<IEntityTagRepository>();

        var tags = new List<EntityTag>
        {
            CreateExistingTag("environment", "production"),
            CreateExistingTag("tier", "critical"),
        };
        repo.ListByEntityAsync("tenant-001", "service", "svc-001", Arg.Any<CancellationToken>())
            .Returns(tags);

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(
            new ListFeature.Query("tenant-001", "service", "svc-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListEntityTags_Should_Return_Empty_When_None()
    {
        var repo = Substitute.For<IEntityTagRepository>();

        repo.ListByEntityAsync("tenant-001", "service", "svc-001", Arg.Any<CancellationToken>())
            .Returns(new List<EntityTag>());

        var sut = new ListFeature.Handler(repo);
        var result = await sut.Handle(
            new ListFeature.Query("tenant-001", "service", "svc-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── RemoveEntityTag ──────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveEntityTag_Should_Remove_When_Found()
    {
        var repo = Substitute.For<IEntityTagRepository>();

        var existing = CreateExistingTag();
        repo.GetByIdAsync(Arg.Any<EntityTagId>(), "tenant-001", Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new RemoveFeature.Handler(repo);
        var result = await sut.Handle(
            new RemoveFeature.Command(existing.Id.Value, "tenant-001"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await repo.Received(1).DeleteAsync(Arg.Any<EntityTagId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveEntityTag_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IEntityTagRepository>();

        repo.GetByIdAsync(Arg.Any<EntityTagId>(), "tenant-001", Arg.Any<CancellationToken>())
            .Returns((EntityTag?)null);

        var sut = new RemoveFeature.Handler(repo);
        var result = await sut.Handle(
            new RemoveFeature.Command(Guid.NewGuid(), "tenant-001"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
