using Microsoft.EntityFrameworkCore;

using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

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
}
