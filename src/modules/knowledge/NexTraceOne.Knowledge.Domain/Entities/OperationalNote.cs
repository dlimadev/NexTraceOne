using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para OperationalNote.
/// </summary>
public sealed record OperationalNoteId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa uma nota operacional persistida no Knowledge Hub.
/// Notas operacionais capturam observações, avisos, decisões ou contexto
/// efémero mas relevante para a operação de serviços e mudanças.
///
/// Diferente de um KnowledgeDocument (que é estável e versionado),
/// uma OperationalNote é tipicamente pontual e ligada a um contexto
/// operacional específico (serviço, incidente, mudança, etc.).
///
/// Owner: módulo Knowledge.
/// Pilar: Source of Truth &amp; Operational Knowledge.
/// </summary>
public sealed class OperationalNote : Entity<OperationalNoteId>
{
    /// <summary>Título curto da nota.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Conteúdo da nota operacional.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Severidade ou importância da nota.</summary>
    public NoteSeverity Severity { get; private set; }

    /// <summary>Identificador do autor (UserId).</summary>
    public Guid AuthorId { get; private init; }

    /// <summary>
    /// Identificador do objecto de contexto ao qual esta nota está associada.
    /// Pode ser um ServiceId, ReleaseId, IncidentId, etc.
    /// O tipo é determinado pelo ContextType.
    /// </summary>
    public Guid? ContextEntityId { get; private set; }

    /// <summary>
    /// Tipo do contexto associado (ex: "Service", "Release", "Incident").
    /// Permite polimorfismo leve sem dependência direta de outros módulos.
    /// </summary>
    public string? ContextType { get; private set; }

    /// <summary>Tags para classificação e pesquisa.</summary>
    public IReadOnlyList<string> Tags { get; private set; } = [];

    /// <summary>Indica se a nota está activa ou foi resolvida/fechada.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Data/hora UTC de resolução.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    private OperationalNote() { }

    /// <summary>Cria uma nova nota operacional.</summary>
    public static OperationalNote Create(
        string title,
        string content,
        NoteSeverity severity,
        Guid authorId,
        Guid? contextEntityId,
        string? contextType,
        IReadOnlyList<string>? tags,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 300, nameof(title));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.Default(authorId, nameof(authorId));

        return new OperationalNote
        {
            Id = new OperationalNoteId(Guid.NewGuid()),
            Title = title.Trim(),
            Content = content,
            Severity = severity,
            AuthorId = authorId,
            ContextEntityId = contextEntityId,
            ContextType = contextType?.Trim(),
            Tags = tags ?? [],
            IsResolved = false,
            CreatedAt = utcNow
        };
    }

    /// <summary>Atualiza o conteúdo da nota.</summary>
    public void UpdateContent(string title, string content, DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 300, nameof(title));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));

        Title = title.Trim();
        Content = content;
        UpdatedAt = utcNow;
    }

    /// <summary>Altera a severidade da nota.</summary>
    public void UpdateSeverity(NoteSeverity severity, DateTimeOffset utcNow)
    {
        Severity = severity;
        UpdatedAt = utcNow;
    }

    /// <summary>Marca a nota como resolvida.</summary>
    public void Resolve(DateTimeOffset utcNow)
    {
        IsResolved = true;
        ResolvedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Reabre a nota (remove estado de resolvido).</summary>
    public void Reopen(DateTimeOffset utcNow)
    {
        IsResolved = false;
        ResolvedAt = null;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza as tags da nota.</summary>
    public void UpdateTags(IReadOnlyList<string> tags, DateTimeOffset utcNow)
    {
        Tags = tags;
        UpdatedAt = utcNow;
    }
}
