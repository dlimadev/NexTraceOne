using Microsoft.EntityFrameworkCore;

using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para OperationalNote.
/// </summary>
internal sealed class OperationalNoteRepository(KnowledgeDbContext context) : IOperationalNoteRepository
{
    public async Task<OperationalNote?> GetByIdAsync(OperationalNoteId id, CancellationToken cancellationToken = default)
        => await context.OperationalNotes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task AddAsync(OperationalNote note, CancellationToken cancellationToken = default)
        => await context.OperationalNotes.AddAsync(note, cancellationToken);

    public void Update(OperationalNote note)
        => context.OperationalNotes.Update(note);

    public async Task<IReadOnlyList<OperationalNote>> SearchAsync(string searchTerm, int maxResults, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        return await context.OperationalNotes
            .Select(n => new
            {
                Note = n,
                SearchVector = EF.Functions.ToTsVector(
                    "simple",
                    (n.Title ?? string.Empty) + " " +
                    (n.Content ?? string.Empty) + " " +
                    (n.ContextType ?? string.Empty))
            })
            .Where(x => x.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", term)))
            .OrderByDescending(x => x.SearchVector.Rank(EF.Functions.PlainToTsQuery("simple", term)))
            .Select(x => x.Note)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OperationalNote> Items, int TotalCount)> ListAsync(
        NoteSeverity? severity,
        string? contextType,
        Guid? contextEntityId,
        bool? isResolved,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.OperationalNotes.AsQueryable();

        if (severity.HasValue)
            query = query.Where(n => n.Severity == severity.Value);

        if (!string.IsNullOrWhiteSpace(contextType))
            query = query.Where(n => n.ContextType == contextType);

        if (contextEntityId.HasValue)
            query = query.Where(n => n.ContextEntityId == contextEntityId.Value);

        if (isResolved.HasValue)
            query = query.Where(n => n.IsResolved == isResolved.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
