using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Value Object que representa o perfil de experiência UI para um ambiente.
/// Enviado ao frontend para que ele possa adaptar a interface ao contexto do ambiente ativo.
///
/// O backend é a fonte de verdade: o frontend não decide comportamento, apenas o materializa.
/// Este VO é o contrato entre backend e frontend para experiência contextual.
///
/// Comportamentos controlados por este perfil:
/// - Indicador visual do ambiente (badge, cor, ícone)
/// - Avisos especiais para ambientes críticos
/// - Habilitação de funcionalidades destrutivas (apenas em não-produção)
/// - Nível de detalhe de alertas exibidos
/// - Escopo padrão de filtros e visualizações
/// </summary>
public sealed class EnvironmentUiProfile : ValueObject
{
    /// <summary>Nome de exibição do ambiente.</summary>
    public string DisplayName { get; }

    /// <summary>Perfil operacional base.</summary>
    public EnvironmentProfile Profile { get; }

    /// <summary>Criticidade do ambiente.</summary>
    public EnvironmentCriticality Criticality { get; }

    /// <summary>
    /// Cor semântica sugerida para o badge do ambiente na UI (ex.: "green", "yellow", "red").
    /// O design system do frontend mapeia estas cores para tokens visuais.
    /// </summary>
    public string BadgeColor { get; }

    /// <summary>
    /// Indica se a UI deve exibir avisos de proteção (ex.: "Você está em PRODUÇÃO").
    /// </summary>
    public bool ShowProtectionWarning { get; }

    /// <summary>
    /// Indica se funcionalidades destrutivas (delete, reset, purge) devem ser habilitadas.
    /// Desabilitadas em ambientes de alta criticidade ou produção-like.
    /// </summary>
    public bool AllowDestructiveActions { get; }

    /// <summary>
    /// Indica se a IA pode ser usada livremente neste contexto de UI.
    /// Em produção, a IA pode ter restrições adicionais de escopo.
    /// </summary>
    public bool AiAssistanceAvailable { get; }

    private EnvironmentUiProfile(
        string displayName,
        EnvironmentProfile profile,
        EnvironmentCriticality criticality,
        string badgeColor,
        bool showProtectionWarning,
        bool allowDestructiveActions,
        bool aiAssistanceAvailable)
    {
        DisplayName = displayName;
        Profile = profile;
        Criticality = criticality;
        BadgeColor = badgeColor;
        ShowProtectionWarning = showProtectionWarning;
        AllowDestructiveActions = allowDestructiveActions;
        AiAssistanceAvailable = aiAssistanceAvailable;
    }

    /// <summary>
    /// Cria o perfil de UI a partir do contexto operacional resolvido.
    /// Centraliza no backend a lógica de comportamento visual do ambiente.
    /// </summary>
    public static EnvironmentUiProfile From(TenantEnvironmentContext context, string displayName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var badgeColor = context.Profile switch
        {
            EnvironmentProfile.Production => "red",
            EnvironmentProfile.DisasterRecovery => "red",
            EnvironmentProfile.Staging => "orange",
            EnvironmentProfile.UserAcceptanceTesting => "orange",
            EnvironmentProfile.Validation => "yellow",
            EnvironmentProfile.PerformanceTesting => "yellow",
            EnvironmentProfile.Sandbox => "blue",
            EnvironmentProfile.Training => "blue",
            _ => "green"
        };

        return new EnvironmentUiProfile(
            displayName: displayName,
            profile: context.Profile,
            criticality: context.Criticality,
            badgeColor: badgeColor,
            showProtectionWarning: context.IsProductionLike,
            allowDestructiveActions: !context.IsProductionLike,
            aiAssistanceAvailable: context.IsActive);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Profile;
        yield return Criticality;
        yield return BadgeColor;
        yield return ShowProtectionWarning;
        yield return AllowDestructiveActions;
        yield return AiAssistanceAvailable;
    }
}
