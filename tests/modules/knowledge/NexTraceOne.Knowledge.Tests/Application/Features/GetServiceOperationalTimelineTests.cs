using System.Linq;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.GetServiceOperationalTimeline;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes unitários para GetServiceOperationalTimeline (OPS-01).
/// Valida paginação, filtros, mapeamento e comportamento com repositório vazio.
/// </summary>
public sealed class GetServiceOperationalTimelineTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly IOperationalNoteRepository _noteRepo = Substitute.For<IOperationalNoteRepository>();

    private GetServiceOperationalTimeline.Handler CreateHandler() =>
        new(_noteRepo);

    private static OperationalNote MakeNote(
        string title,
        NoteSeverity severity,
        Guid? serviceId,
        DateTimeOffset? createdAt = null)
    {
        var note = OperationalNote.Create(
            title,
            "Content for " + title,
            severity,
            OperationalNoteType.Observation,
            "Manual",
            Guid.NewGuid(),
            serviceId,
            serviceId.HasValue ? "Service" : null,
            [],
            createdAt ?? FixedNow);
        return note;
    }

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidServiceId_ReturnsMatchingNotes()
    {
        var serviceId = Guid.NewGuid();
        var note = MakeNote("Deploy warning", NoteSeverity.Warning, serviceId);

        _noteRepo.ListAsync(null, "Service", serviceId, null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(([note], 1));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be(serviceId);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Title.Should().Be("Deploy warning");
        result.Value.Items[0].Severity.Should().Be("Warning");
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyTimeline()
    {
        var serviceId = Guid.NewGuid();

        _noteRepo.ListAsync(null, "Service", serviceId, null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<OperationalNote>)[], 0));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSeverityFilter_PassesSeverityToRepository()
    {
        var serviceId = Guid.NewGuid();
        var criticalNote = MakeNote("Critical issue", NoteSeverity.Critical, serviceId);

        _noteRepo.ListAsync(NoteSeverity.Critical, "Service", serviceId, null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(([criticalNote], 1));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId, Severity: NoteSeverity.Critical),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Severity.Should().Be("Critical");

        await _noteRepo.Received(1).ListAsync(
            NoteSeverity.Critical, "Service", serviceId, null, 1, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IsResolvedFilter_PassesFilterToRepository()
    {
        var serviceId = Guid.NewGuid();

        _noteRepo.ListAsync(null, "Service", serviceId, false, 1, 25, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<OperationalNote>)[], 0));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId, IsResolved: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _noteRepo.Received(1).ListAsync(
            null, "Service", serviceId, false, 1, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Pagination_CalculatesTotalPagesCorrectly()
    {
        var serviceId = Guid.NewGuid();
        var notes = Enumerable.Range(0, 10)
            .Select(i => MakeNote($"Note {i}", NoteSeverity.Info, serviceId))
            .ToList();

        _noteRepo.ListAsync(null, "Service", serviceId, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<OperationalNote>)notes, 23));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId, Page: 1, PageSize: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(23);
        result.Value.TotalPages.Should().Be(3); // ceil(23/10) = 3
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_AlwaysPassesServiceContextType_ToRepository()
    {
        var serviceId = Guid.NewGuid();

        _noteRepo.ListAsync(Arg.Any<NoteSeverity?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<OperationalNote>)[], 0));

        await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId),
            CancellationToken.None);

        await _noteRepo.Received(1).ListAsync(
            Arg.Any<NoteSeverity?>(),
            "Service",
            serviceId,
            Arg.Any<bool?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields_Correctly()
    {
        var serviceId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.Zero);

        var note = OperationalNote.Create(
            "Maintenance window",
            "Scheduled maintenance for DB upgrade",
            NoteSeverity.Warning,
            OperationalNoteType.Decision,
            "Manual",
            authorId,
            serviceId,
            "Service",
            ["db", "infra"],
            now);

        _noteRepo.ListAsync(null, "Service", serviceId, null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(([note], 1));

        var result = await CreateHandler().Handle(
            new GetServiceOperationalTimeline.Query(serviceId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value.Items[0];
        entry.Title.Should().Be("Maintenance window");
        entry.Content.Should().Contain("DB upgrade");
        entry.Severity.Should().Be("Warning");
        entry.NoteType.Should().Be("Decision");
        entry.Origin.Should().Be("Manual");
        entry.AuthorId.Should().Be(authorId);
        entry.Tags.Should().Contain("db").And.Contain("infra");
        entry.IsResolved.Should().BeFalse();
        entry.OccurredAt.Should().Be(now);
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyServiceId_ReturnsError()
    {
        var validator = new GetServiceOperationalTimeline.Validator();
        var result = validator.Validate(new GetServiceOperationalTimeline.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ServiceId");
    }

    [Fact]
    public void Validator_InvalidPage_ReturnsError()
    {
        var validator = new GetServiceOperationalTimeline.Validator();
        var result = validator.Validate(new GetServiceOperationalTimeline.Query(Guid.NewGuid(), Page: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validator_PageSizeTooLarge_ReturnsError()
    {
        var validator = new GetServiceOperationalTimeline.Validator();
        var result = validator.Validate(new GetServiceOperationalTimeline.Query(Guid.NewGuid(), PageSize: 101));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validator_ValidQuery_PassesValidation()
    {
        var validator = new GetServiceOperationalTimeline.Validator();
        var result = validator.Validate(new GetServiceOperationalTimeline.Query(
            Guid.NewGuid(), NoteSeverity.Warning, false, 2, 50));
        result.IsValid.Should().BeTrue();
    }
}
