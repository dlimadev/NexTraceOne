namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Valores válidos para o estado de aprovação de uma Release.
/// Centraliza as constantes de string utilizadas em comparações de ApprovalStatus
/// enquanto o campo permanece como string (compatível com o schema existente).
///
/// Nota evolutiva: o caminho ideal é migrar ApprovalStatus para um enum fortemente
/// tipado na entidade Release. Enquanto essa migration não ocorre, estas constantes
/// garantem que comparações não fiquem dispersas como magic strings.
/// </summary>
public static class ReleaseApprovalStatus
{
    /// <summary>Release aprovada para deploy.</summary>
    public const string Approved = "Approved";

    /// <summary>Release rejeitada — não pode avançar para deploy.</summary>
    public const string Rejected = "Rejected";

    /// <summary>Release aprovada com condições — deploy permitido apenas se as condições forem cumpridas.</summary>
    public const string ApprovedConditionally = "ApprovedConditionally";

    /// <summary>
    /// Todos os valores válidos, para uso em validações FluentValidation e guards.
    /// </summary>
    public static readonly IReadOnlyList<string> AllValues =
        [Approved, Rejected, ApprovedConditionally];
}
