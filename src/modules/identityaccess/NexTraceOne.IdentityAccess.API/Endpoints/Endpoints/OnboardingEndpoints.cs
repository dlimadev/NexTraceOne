using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.IdentityAccess.Application.Features.GetOnboardingStatus;
using NexTraceOne.IdentityAccess.Application.Features.UpdateOnboardingStep;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints Minimal API do wizard de onboarding.
/// SaaS-06: Onboarding Wizard.
///
/// Endpoints disponíveis:
/// - GET  /onboarding/status       → Estado actual do wizard
/// - POST /onboarding/steps/{step} → Marcar passo como concluído
/// </summary>
internal static class OnboardingEndpoints
{
    internal static void Map(IEndpointRouteBuilder group)
    {
        var g = group.MapGroup("/onboarding");

        g.MapGet("/status", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOnboardingStatus.Query(), ct);
            return result.ToHttpResult(localizer);
        })
        .RequireAuthorization();

        g.MapPost("/steps/{step}", async (
            string step,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateOnboardingStep.Command(step), ct);
            return result.ToHttpResult(localizer);
        })
        .RequireAuthorization();
    }
}
