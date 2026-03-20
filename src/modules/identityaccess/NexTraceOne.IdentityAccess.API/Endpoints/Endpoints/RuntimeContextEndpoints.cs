using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Contracts;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints para exposição do contexto de execução resolvido.
/// Permite que o frontend consulte o contexto ativo validado pelo backend.
/// </summary>
internal static class RuntimeContextEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de contexto de runtime no subgrupo <c>/context</c>.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        var contextGroup = group.MapGroup("/context")
            .WithTags("Runtime Context")
            .RequireAuthorization();

        contextGroup.MapGet("/runtime", GetRuntimeContext)
            .WithName("GetRuntimeContext")
            .WithSummary("Returns the resolved execution context for the current request.")
            .WithDescription(
                "Returns user, tenant, and environment context resolved by the backend. " +
                "The frontend should use this to configure contextual behavior without making security decisions.");
    }

    private static IResult GetRuntimeContext(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IEnvironmentContextAccessor environmentContextAccessor,
        IDateTimeProvider dateTimeProvider)
    {
        var userDto = new RuntimeUserDto(
            currentUser.Id,
            currentUser.Name,
            currentUser.Email,
            currentUser.IsAuthenticated);

        var tenantDto = new RuntimeTenantDto(
            currentTenant.Id,
            currentTenant.Slug,
            currentTenant.Name,
            currentTenant.IsActive);

        RuntimeEnvironmentDto? environmentDto = null;

        if (environmentContextAccessor.IsResolved)
        {
            var profile = environmentContextAccessor.Profile;
            environmentDto = new RuntimeEnvironmentDto(
                environmentContextAccessor.EnvironmentId.Value,
                profile,
                GetProfileDisplayName(profile),
                environmentContextAccessor.IsProductionLike,
                GetBadgeColor(profile),
                ShowProtectionWarning: environmentContextAccessor.IsProductionLike,
                AllowDestructiveActions: !environmentContextAccessor.IsProductionLike);
        }

        var isFullyResolved = currentUser.IsAuthenticated
            && currentTenant.Id != Guid.Empty
            && currentTenant.IsActive
            && environmentContextAccessor.IsResolved;

        var response = new RuntimeContextDto(
            userDto,
            tenantDto,
            environmentDto,
            isFullyResolved,
            dateTimeProvider.UtcNow);

        return Results.Ok(response);
    }

    private static string GetProfileDisplayName(EnvironmentProfile profile) => profile switch
    {
        EnvironmentProfile.Development => "Development",
        EnvironmentProfile.Validation => "Validation / QA",
        EnvironmentProfile.Staging => "Staging",
        EnvironmentProfile.Production => "Production",
        EnvironmentProfile.Sandbox => "Sandbox",
        EnvironmentProfile.DisasterRecovery => "Disaster Recovery",
        EnvironmentProfile.Training => "Training",
        EnvironmentProfile.UserAcceptanceTesting => "User Acceptance Testing",
        EnvironmentProfile.PerformanceTesting => "Performance Testing",
        _ => profile.ToString()
    };

    private static string GetBadgeColor(EnvironmentProfile profile) => profile switch
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
}
