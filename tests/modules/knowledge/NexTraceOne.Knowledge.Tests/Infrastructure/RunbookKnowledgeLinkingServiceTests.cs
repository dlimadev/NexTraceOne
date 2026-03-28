using Microsoft.EntityFrameworkCore;
using System.Linq;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;
using NexTraceOne.Knowledge.Infrastructure.Search;

namespace NexTraceOne.Knowledge.Tests.Infrastructure;

public sealed class RunbookKnowledgeLinkingServiceTests
{
    [Fact]
    public async Task LinkRunbookToServiceAsync_ShouldCreateNoteAndRelation_WhenServiceIdIsValidGuid()
    {
        var dbName = $"runbook-link-{Guid.NewGuid()}";
        var clock = new FixedClock(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero));
        await using var context = CreateContext(dbName, clock);
        var noteRepository = new OperationalNoteRepository(context);
        var relationRepository = new KnowledgeRelationRepository(context);
        var sut = new RunbookKnowledgeLinkingService(noteRepository, relationRepository, context, clock);
        var serviceId = Guid.NewGuid();

        await sut.LinkRunbookToServiceAsync(
            runbookId: Guid.NewGuid(),
            runbookTitle: "Database timeout runbook",
            runbookDescription: "Steps to mitigate timeout.",
            linkedServiceId: serviceId.ToString(),
            maintainedBy: Guid.NewGuid().ToString(),
            cancellationToken: CancellationToken.None);

        context.OperationalNotes.Should().ContainSingle(x => x.ContextType == "Runbook");
        context.KnowledgeRelations.Should().ContainSingle(x => x.TargetType == RelationType.Service && x.TargetEntityId == serviceId && x.Context == "Runbook");
    }

    [Fact]
    public async Task LinkRunbookToServiceAsync_ShouldBeIdempotent_WhenRelationAlreadyExistsForRunbookAndService()
    {
        var runbookId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var dbName = $"runbook-link-{Guid.NewGuid()}";
        var clock = new FixedClock(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero));
        await using var context = CreateContext(dbName, clock);
        var noteRepository = new OperationalNoteRepository(context);
        var relationRepository = new KnowledgeRelationRepository(context);
        var sut = new RunbookKnowledgeLinkingService(noteRepository, relationRepository, context, clock);

        await sut.LinkRunbookToServiceAsync(
            runbookId: runbookId,
            runbookTitle: "Database timeout runbook",
            runbookDescription: "Steps to mitigate timeout.",
            linkedServiceId: serviceId.ToString(),
            maintainedBy: Guid.NewGuid().ToString(),
            cancellationToken: CancellationToken.None);

        var notesCountAfterFirstRun = context.OperationalNotes.Count();
        var relationsCountAfterFirstRun = context.KnowledgeRelations.Count();

        await sut.LinkRunbookToServiceAsync(
            runbookId: runbookId,
            runbookTitle: "Database timeout runbook",
            runbookDescription: "Steps to mitigate timeout.",
            linkedServiceId: serviceId.ToString(),
            maintainedBy: Guid.NewGuid().ToString(),
            cancellationToken: CancellationToken.None);

        context.OperationalNotes.Count().Should().Be(notesCountAfterFirstRun);
        context.KnowledgeRelations.Count().Should().Be(relationsCountAfterFirstRun);
    }

    [Fact]
    public async Task LinkRunbookToServiceAsync_ShouldNoOp_WhenLinkedServiceIsNotGuid()
    {
        var dbName = $"runbook-link-{Guid.NewGuid()}";
        var clock = new FixedClock(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero));
        await using var context = CreateContext(dbName, clock);
        var noteRepository = new OperationalNoteRepository(context);
        var relationRepository = new KnowledgeRelationRepository(context);
        var sut = new RunbookKnowledgeLinkingService(noteRepository, relationRepository, context, clock);

        await sut.LinkRunbookToServiceAsync(
            runbookId: Guid.NewGuid(),
            runbookTitle: "Non-guid service runbook",
            runbookDescription: "Desc",
            linkedServiceId: "payment-service",
            maintainedBy: Guid.NewGuid().ToString(),
            cancellationToken: CancellationToken.None);

        context.OperationalNotes.Should().BeEmpty();
        context.KnowledgeRelations.Should().BeEmpty();
    }

    private static KnowledgeDbContext CreateContext(string dbName, IDateTimeProvider clock)
    {
        var options = new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new KnowledgeDbContext(options, new TestCurrentTenant(), new TestCurrentUser(), clock);
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string Slug { get; } = "knowledge-tests";
        public string Name { get; } = "Knowledge Tests Tenant";
        public bool IsActive { get; } = true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id { get; } = "33333333-3333-3333-3333-333333333333";
        public string Name { get; } = "Knowledge Tests User";
        public string Email { get; } = "knowledge-tests@nextraceone.local";
        public bool IsAuthenticated { get; } = true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
        public DateOnly UtcToday { get; } = DateOnly.FromDateTime(utcNow.UtcDateTime);
    }
}
