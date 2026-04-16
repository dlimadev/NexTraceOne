using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Configuration.Domain.Enums;

using GetAuditHistoryFeature = NexTraceOne.Configuration.Application.Features.GetAuditHistory.GetAuditHistory;
using GetAuditHistoryByPrefixFeature = NexTraceOne.Configuration.Application.Features.GetAuditHistoryByPrefix.GetAuditHistoryByPrefix;
using GetDefinitionsFeature = NexTraceOne.Configuration.Application.Features.GetDefinitions.GetDefinitions;
using GetEffectiveFeatureFlagFeature = NexTraceOne.Configuration.Application.Features.GetEffectiveFeatureFlag.GetEffectiveFeatureFlag;
using GetEffectiveSettingsFeature = NexTraceOne.Configuration.Application.Features.GetEffectiveSettings.GetEffectiveSettings;
using GetEntriesFeature = NexTraceOne.Configuration.Application.Features.GetEntries.GetEntries;
using GetFeatureFlagsFeature = NexTraceOne.Configuration.Application.Features.GetFeatureFlags.GetFeatureFlags;
using RemoveFeatureFlagOverrideFeature = NexTraceOne.Configuration.Application.Features.RemoveFeatureFlagOverride.RemoveFeatureFlagOverride;
using RemoveOverrideFeature = NexTraceOne.Configuration.Application.Features.RemoveOverride.RemoveOverride;
using SetConfigurationValueFeature = NexTraceOne.Configuration.Application.Features.SetConfigurationValue.SetConfigurationValue;
using SetFeatureFlagOverrideFeature = NexTraceOne.Configuration.Application.Features.SetFeatureFlagOverride.SetFeatureFlagOverride;
using ToggleConfigurationFeature = NexTraceOne.Configuration.Application.Features.ToggleConfiguration.ToggleConfiguration;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// Request body para o endpoint PUT /api/v1/configuration/{key}.
/// </summary>
public sealed record SetConfigurationValueRequest(
    string Scope,
    string? ScopeReferenceId,
    string Value,
    string? ChangeReason);

/// <summary>
/// Request body para o endpoint POST /api/v1/configuration/{key}/toggle.
/// </summary>
public sealed record ToggleConfigurationRequest(
    string Scope,
    string? ScopeReferenceId,
    bool Activate,
    string? ChangeReason);

/// <summary>
/// Request body para o endpoint PUT /api/v1/configuration/flags/{key}/override.
/// </summary>
public sealed record SetFeatureFlagOverrideRequest(
    string Scope,
    string? ScopeReferenceId,
    bool IsEnabled,
    string? ChangeReason);

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Configuration.
/// </summary>
public sealed class ConfigurationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configuration");

        // GET /api/v1/configuration/definitions
        group.MapGet("/definitions", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDefinitionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // GET /api/v1/configuration/entries?scope={scope}&scopeReferenceId={id}&keyPrefix={prefix}
        group.MapGet("/entries", async (
            string scope,
            string? scopeReferenceId,
            string? keyPrefix,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new GetEntriesFeature.Query(parsedScope, scopeReferenceId, keyPrefix),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // GET /api/v1/configuration/effective?key={key}&scope={scope}&scopeReferenceId={id}
        group.MapGet("/effective", async (
            string? key,
            string scope,
            string? scopeReferenceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new GetEffectiveSettingsFeature.Query(key, parsedScope, scopeReferenceId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // PUT /api/v1/configuration/{key}
        group.MapPut("/{key}", async (
            string key,
            SetConfigurationValueRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(request.Scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{request.Scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new SetConfigurationValueFeature.Command(
                    key,
                    parsedScope,
                    request.ScopeReferenceId,
                    request.Value,
                    request.ChangeReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:write");

        // DELETE /api/v1/configuration/{key}/override?scope={scope}&scopeReferenceId={id}&changeReason={reason}
        group.MapDelete("/{key}/override", async (
            string key,
            string scope,
            string? scopeReferenceId,
            string? changeReason,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new RemoveOverrideFeature.Command(key, parsedScope, scopeReferenceId, changeReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:write");

        // POST /api/v1/configuration/{key}/toggle
        group.MapPost("/{key}/toggle", async (
            string key,
            ToggleConfigurationRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(request.Scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{request.Scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new ToggleConfigurationFeature.Command(
                    key,
                    parsedScope,
                    request.ScopeReferenceId,
                    request.Activate,
                    request.ChangeReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:write");

        // GET /api/v1/configuration/{key}/audit?limit={limit}
        group.MapGet("/{key}/audit", async (
            string key,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAuditHistoryFeature.Query(key, limit ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // GET /api/v1/configuration/audit-history?keyPrefix={prefix}&limit={limit}
        group.MapGet("/audit-history", async (
            string? keyPrefix,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAuditHistoryByPrefixFeature.Query(keyPrefix ?? string.Empty, limit ?? 100),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // ── Feature Flags ──────────────────────────────────────────────────

        // GET /api/v1/configuration/flags
        group.MapGet("/flags", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetFeatureFlagsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // GET /api/v1/configuration/flags/effective?key={key}&scope={scope}&scopeReferenceId={id}
        group.MapGet("/flags/effective", async (
            string? key,
            string scope,
            string? scopeReferenceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new GetEffectiveFeatureFlagFeature.Query(key, parsedScope, scopeReferenceId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:read");

        // PUT /api/v1/configuration/flags/{key}/override
        group.MapPut("/flags/{key}/override", async (
            string key,
            SetFeatureFlagOverrideRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(request.Scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{request.Scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new SetFeatureFlagOverrideFeature.Command(
                    key,
                    parsedScope,
                    request.ScopeReferenceId,
                    request.IsEnabled,
                    request.ChangeReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:write");

        // DELETE /api/v1/configuration/flags/{key}/override?scope={scope}&scopeReferenceId={id}
        group.MapDelete("/flags/{key}/override", async (
            string key,
            string scope,
            string? scopeReferenceId,
            string? changeReason,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ConfigurationScope>(scope, ignoreCase: true, out var parsedScope))
                return Results.Problem(
                    title: "Validation failed",
                    detail: $"Invalid scope value '{scope}'.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await sender.Send(
                new RemoveFeatureFlagOverrideFeature.Command(key, parsedScope, scopeReferenceId, changeReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:write");
    }
}
