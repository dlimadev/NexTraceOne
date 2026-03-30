using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateFreezeWindowFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreateFreezeWindow.CreateFreezeWindow;
using CheckFreezeConflictFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CheckFreezeConflict.CheckFreezeConflict;
using ListFreezeWindowsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListFreezeWindows.ListFreezeWindows;
using UpdateFreezeWindowFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.UpdateFreezeWindow.UpdateFreezeWindow;
using DeactivateFreezeWindowFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.DeactivateFreezeWindow.DeactivateFreezeWindow;
using GetReleaseCalendarFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseCalendar.GetReleaseCalendar;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de janelas de freeze e calendário de releases.
/// Permite criar, listar, atualizar e desativar janelas de freeze,
/// verificar conflitos e obter o calendário agregado de releases e freezes.
/// </summary>
internal static class FreezeEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de freeze e calendário num grupo dedicado.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var freezeGroup = app.MapGroup("/api/v1/freeze-windows");

        // ── Criação de freeze window ────────────────────────────────

        freezeGroup.MapPost("/", async (
            CreateFreezeWindowFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── Listagem de freeze windows com filtros ──────────────────

        freezeGroup.MapGet("/", async (
            DateTimeOffset from,
            DateTimeOffset to,
            string? environment,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListFreezeWindowsFeature.Query(from, to, environment, isActive);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Atualização de freeze window ────────────────────────────

        freezeGroup.MapPut("/{freezeWindowId:guid}", async (
            Guid freezeWindowId,
            UpdateFreezeWindowFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { FreezeWindowId = freezeWindowId };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── Desativação de freeze window ────────────────────────────

        freezeGroup.MapPost("/{freezeWindowId:guid}/deactivate", async (
            Guid freezeWindowId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DeactivateFreezeWindowFeature.Command(freezeWindowId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── Verificação de conflito ─────────────────────────────────

        freezeGroup.MapGet("/check", async (
            [AsParameters] CheckFreezeConflictRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new CheckFreezeConflictFeature.Query(request.At, request.Environment);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Release Calendar ────────────────────────────────────────

        var calendarGroup = app.MapGroup("/api/v1/release-calendar");

        calendarGroup.MapGet("/", async (
            DateTimeOffset from,
            DateTimeOffset to,
            string? environment,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReleaseCalendarFeature.Query(from, to, environment);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}

/// <summary>Parâmetros de query string para verificação de conflito de freeze.</summary>
internal sealed record CheckFreezeConflictRequest(DateTimeOffset At, string? Environment);
