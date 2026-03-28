using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.CreateKnowledgeDocument;
using NexTraceOne.Knowledge.Application.Features.CreateKnowledgeRelation;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeByRelationTarget;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Knowledge.Tests.Infrastructure;

public sealed class KnowledgePersistenceAndSearchIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("nextrace_knowledge_it")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private DbContextOptions<KnowledgeDbContext>? _dbOptions;
    private readonly ICurrentTenant _tenant = new TestCurrentTenant();
    private readonly ICurrentUser _user = new TestCurrentUser();
    private readonly IDateTimeProvider _clock = new TestDateTimeProvider(new DateTimeOffset(2026, 3, 28, 17, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dbOptions = new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task CreateDocument_ThenFtsSearch_ShouldReturnCreatedDocument()
    {
        // Arrange
        await using (var context = CreateContext())
        {
            var documentRepository = new KnowledgeDocumentRepository(context);
            var createHandler = new CreateKnowledgeDocument.Handler(documentRepository, _clock);

            var createResult = await createHandler.Handle(
                new CreateKnowledgeDocument.Command(
                    Title: "Production Incident Runbook",
                    Content: "Runbook steps for database connection timeout and recovery in production.",
                    Summary: "Incident mitigation playbook",
                    Category: DocumentCategory.Runbook,
                    Tags: ["incident", "database", "timeout"],
                    AuthorId: Guid.Parse(_user.Id)),
                CancellationToken.None);

            createResult.IsSuccess.Should().BeTrue();
            await context.CommitAsync();
        }

        // Act
        await using (var verificationContext = CreateContext())
        {
            var repository = new KnowledgeDocumentRepository(verificationContext);
            var results = await repository.SearchAsync("database timeout", 10, CancellationToken.None);

            // Assert
            results.Should().ContainSingle(d => d.Title == "Production Incident Runbook");
        }
    }

    [Fact]
    public async Task CreateRelation_ThenQueryByTarget_ShouldReturnLinkedRelation()
    {
        var incidentId = Guid.NewGuid();
        Guid documentId;

        // Arrange document + relation
        await using (var context = CreateContext())
        {
            var documentRepository = new KnowledgeDocumentRepository(context);
            var noteRepository = new OperationalNoteRepository(context);
            var relationRepository = new KnowledgeRelationRepository(context);

            var createDocument = new CreateKnowledgeDocument.Handler(documentRepository, _clock);
            var created = await createDocument.Handle(
                new CreateKnowledgeDocument.Command(
                    Title: "Incident DB timeout guide",
                    Content: "Operational guidance for timeout handling.",
                    Summary: "Guide",
                    Category: DocumentCategory.Troubleshooting,
                    Tags: ["incident", "db"],
                    AuthorId: Guid.Parse(_user.Id)),
                CancellationToken.None);

            created.IsSuccess.Should().BeTrue();
            documentId = created.Value.DocumentId;

            await context.CommitAsync();

            var createRelation = new CreateKnowledgeRelation.Handler(documentRepository, noteRepository, relationRepository, _clock);
            var relationResult = await createRelation.Handle(
                new CreateKnowledgeRelation.Command(
                    SourceEntityId: documentId,
                    SourceEntityType: KnowledgeSourceEntityType.KnowledgeDocument,
                    TargetEntityId: incidentId,
                    TargetType: RelationType.Incident,
                    Description: "Linked as mitigation reference",
                    Context: "IncidentResponse",
                    CreatedById: Guid.Parse(_user.Id)),
                CancellationToken.None);

            relationResult.IsSuccess.Should().BeTrue();
            await context.CommitAsync();
        }

        // Act + Assert via query handler
        await using (var verificationContext = CreateContext())
        {
            var relationRepository = new KnowledgeRelationRepository(verificationContext);
            var documentRepository = new KnowledgeDocumentRepository(verificationContext);
            var noteRepository = new OperationalNoteRepository(verificationContext);
            var queryHandler = new GetKnowledgeByRelationTarget.Handler(relationRepository, documentRepository, noteRepository);

            var result = await queryHandler.Handle(
                new GetKnowledgeByRelationTarget.Query(RelationType.Incident, incidentId),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Documents.Should().ContainSingle(d => d.DocumentId == documentId);
            result.Value.Notes.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task MigrationSchema_ShouldContainKnowledgeTables()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)::int
            from information_schema.tables
            where table_schema = 'public'
              and table_name in ('knw_documents', 'knw_operational_notes', 'knw_relations');
            """;

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        count.Should().Be(3);
    }

    private KnowledgeDbContext CreateContext()
    {
        _dbOptions.Should().NotBeNull();
        return new KnowledgeDbContext(_dbOptions!, _tenant, _user, _clock);
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string Slug { get; } = "integration";
        public string Name { get; } = "Integration Tenant";
        public bool IsActive { get; } = true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id { get; } = "22222222-2222-2222-2222-222222222222";
        public string Name { get; } = "Integration User";
        public string Email { get; } = "integration@nextraceone.local";
        public bool IsAuthenticated { get; } = true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
        public DateOnly UtcToday { get; } = DateOnly.FromDateTime(utcNow.UtcDateTime);
    }
}
