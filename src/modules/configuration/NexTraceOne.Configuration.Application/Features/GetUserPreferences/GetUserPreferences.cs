using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.GetUserPreferences;

/// <summary>
/// Feature: GetUserPreferences — obtém as preferências do utilizador autenticado.
/// Retorna configurações do scope User para o utilizador corrente, cobrindo
/// personalização de sidebar, home/dashboard widgets, tema, etc.
/// </summary>
public static class GetUserPreferences
{
    /// <summary>Query para obter as preferências do utilizador corrente.</summary>
    public sealed record Query(string? Prefix) : IQuery<Response>;

    /// <summary>Handler que busca todas as configurações User do utilizador autenticado.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IConfigurationEntryRepository entryRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated to get preferences.");

            var userId = currentUser.Id;
            var prefix = request.Prefix ?? "platform.";

            // Get all user-scope entries for this user
            var entries = await entryRepository.GetAllByScopeAsync(
                ConfigurationScope.User, userId, cancellationToken);

            var preferences = entries
                .Where(e => e.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(e => new PreferenceItem(e.Key, e.Value, e.UpdatedAt))
                .OrderBy(e => e.Key)
                .ToList();

            // Also resolve effective values for known platform preferences
            var sidebarCustomization = await configService.ResolveEffectiveValueAsync(
                "platform.sidebar.user_customization.enabled", ConfigurationScope.Tenant, null, cancellationToken);

            var maxPinnedItems = await configService.ResolveEffectiveValueAsync(
                "platform.sidebar.pinned_items.max", ConfigurationScope.Tenant, null, cancellationToken);

            var maxWidgets = await configService.ResolveEffectiveValueAsync(
                "platform.home.max_widgets", ConfigurationScope.Tenant, null, cancellationToken);

            return new Response(
                UserId: userId,
                Preferences: preferences,
                SidebarCustomizationEnabled: sidebarCustomization?.EffectiveValue == "true",
                MaxPinnedItems: int.TryParse(maxPinnedItems?.EffectiveValue, out var mp) ? mp : 10,
                MaxWidgets: int.TryParse(maxWidgets?.EffectiveValue, out var mw) ? mw : 12,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Item de preferência do utilizador.</summary>
    public sealed record PreferenceItem(string Key, string Value, DateTimeOffset? UpdatedAt);

    /// <summary>Resposta com as preferências do utilizador e limites da plataforma.</summary>
    public sealed record Response(
        string UserId,
        List<PreferenceItem> Preferences,
        bool SidebarCustomizationEnabled,
        int MaxPinnedItems,
        int MaxWidgets,
        DateTimeOffset EvaluatedAt);
}
