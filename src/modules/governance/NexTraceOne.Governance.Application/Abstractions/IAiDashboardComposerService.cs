namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Abstracção de composição de dashboards por IA.
/// Implementada na camada de infraestrutura usando IChatCompletionProvider.
/// Quando o provider não está configurado, retorna null (fallback keyword-based no handler).
/// </summary>
public interface IAiDashboardComposerService
{
    /// <summary>
    /// Indica se o provider de IA está configurado e disponível.
    /// Quando false, o handler deve usar análise de keywords como fallback.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gera uma proposta de dashboard em linguagem natural.
    /// Retorna null se a geração falhar ou o provider não estiver configurado.
    /// </summary>
    Task<AiDashboardProposal?> ComposeAsync(
        AiDashboardCompositionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Request para composição de dashboard por IA.</summary>
public sealed record AiDashboardCompositionRequest(
    string Prompt,
    string Persona,
    string TenantId,
    string? TeamId,
    string? EnvironmentId,
    IReadOnlyList<string>? ServiceIds);

/// <summary>Proposta estruturada de dashboard gerada por IA.</summary>
public sealed record AiDashboardProposal(
    string Title,
    string Layout,
    IReadOnlyList<ProposedVariable> Variables,
    IReadOnlyList<ProposedWidget> Widgets,
    string ModelId,
    string ProviderId);

/// <summary>Variável proposta pelo LLM.</summary>
public sealed record ProposedVariable(string Key, string Label, string Type, string? DefaultValue);

/// <summary>Widget proposto pelo LLM.</summary>
public sealed record ProposedWidget(
    string WidgetType,
    string? Title,
    string? ServiceFilter,
    string? NqlQuery,
    int GridX,
    int GridY,
    int GridWidth,
    int GridHeight);
