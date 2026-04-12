using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Prompt de IA guardado por um utilizador.
/// Permite reutilização de prompts frequentes com contexto semântico definido.
///
/// Invariantes:
/// - Nome não pode exceder 100 caracteres.
/// - PromptText não pode estar vazio.
/// - ContextType normalizado para valores reconhecidos.
/// </summary>
public sealed class SavedPrompt : Entity<SavedPromptId>
{
    private const int MaxNameLength = 100;
    private const int MaxPromptTextLength = 4000;
    private static readonly string[] ValidContextTypes =
        ["general", "incident", "contract", "change", "service"];

    private SavedPrompt() { }

    /// <summary>Identificador do utilizador dono do prompt.</summary>
    public string UserId { get; private init; } = string.Empty;

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo do prompt guardado.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Texto completo do prompt.</summary>
    public string PromptText { get; private set; } = string.Empty;

    /// <summary>Contexto semântico do prompt: general, incident, contract, change, service.</summary>
    public string ContextType { get; private set; } = "general";

    /// <summary>Tags separadas por vírgula para organização.</summary>
    public string? TagsCsv { get; private set; }

    /// <summary>Indica se o prompt está partilhado com o tenant.</summary>
    public bool IsShared { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Cria um novo prompt guardado.</summary>
    public static SavedPrompt Create(
        string userId,
        string tenantId,
        string name,
        string promptText,
        string contextType,
        string? tagsCsv,
        bool isShared,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(promptText, nameof(promptText));
        Guard.Against.OutOfRange(promptText.Length, nameof(promptText), 1, MaxPromptTextLength);

        return new SavedPrompt
        {
            Id = new SavedPromptId(Guid.NewGuid()),
            UserId = userId.Trim(),
            TenantId = tenantId.Trim(),
            Name = name.Trim(),
            PromptText = promptText.Trim(),
            ContextType = NormalizeContextType(contextType),
            TagsCsv = string.IsNullOrWhiteSpace(tagsCsv) ? null : tagsCsv.Trim(),
            IsShared = isShared,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Altera o estado de partilha do prompt.</summary>
    public void SetShared(bool shared)
    {
        IsShared = shared;
    }

    private static string NormalizeContextType(string? contextType) =>
        ValidContextTypes.Contains(contextType?.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
            ? contextType!.Trim().ToLower()
            : "general";
}
