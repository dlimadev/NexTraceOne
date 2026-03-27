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
        var pattern = $"%{searchTerm}%";
        return await context.OperationalNotes
            .Where(n =>
                EF.Functions.ILike(n.Title, pattern) ||
                EF.Functions.ILike(n.Content, pattern))
            .OrderByDescending(n => n.UpdatedAt ?? n.CreatedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
