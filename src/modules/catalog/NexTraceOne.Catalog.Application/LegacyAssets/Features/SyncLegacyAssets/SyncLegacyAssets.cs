using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.SyncLegacyAssets;

/// <summary>
/// Feature: SyncLegacyAssets — importação bulk de ativos legacy via API de ingestão.
/// Suporta criação e atualização de múltiplos ativos num único pedido.
/// </summary>
public static class SyncLegacyAssets
{
    public sealed record Command(
        string Provider,
        string? CorrelationId,
        List<AssetItem> Assets) : ICommand<Response>;

    public sealed record AssetItem(
        string AssetType,
        string Name,
        string? DisplayName,
        string? Description,
        string? SystemName,
        string? TeamName,
        string? Domain,
        string? Criticality,
        Dictionary<string, string>? Metadata);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Assets).NotEmpty().Must(a => a.Count <= 500)
                .WithMessage("Maximum 500 assets per sync request.");
            RuleForEach(x => x.Assets).ChildRules(asset =>
            {
                asset.RuleFor(a => a.AssetType).NotEmpty().MaximumLength(50);
                asset.RuleFor(a => a.Name).NotEmpty().MaximumLength(200);
            });
        }
    }

    public sealed class Handler(
        IMainframeSystemRepository mainframeSystemRepository,
        ILegacyAssetsUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var created = 0;
            var skipped = 0;
            var errors = new List<string>();

            foreach (var asset in request.Assets)
            {
                try
                {
                    if (string.Equals(asset.AssetType, "MainframeSystem", StringComparison.OrdinalIgnoreCase))
                    {
                        var existing = await mainframeSystemRepository.GetByNameAsync(asset.Name, cancellationToken);
                        if (existing is not null)
                        {
                            skipped++;
                            continue;
                        }

                        var sysplexName = asset.Metadata?.GetValueOrDefault("SysplexName");
                        var lparName = asset.Metadata?.GetValueOrDefault("LparName");

                        if (string.IsNullOrWhiteSpace(sysplexName) || string.IsNullOrWhiteSpace(lparName))
                        {
                            errors.Add($"{asset.Name}: Missing required metadata fields SysplexName and/or LparName.");
                            continue;
                        }

                        var lpar = LparReference.Create(sysplexName, lparName);

                        var system = MainframeSystem.Create(
                            asset.Name,
                            asset.Domain ?? "Unknown",
                            asset.TeamName ?? "Unknown",
                            lpar);

                        mainframeSystemRepository.Add(system);
                        created++;
                    }
                    else
                    {
                        errors.Add($"{asset.Name}: Unsupported asset type '{asset.AssetType}'.");
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{asset.Name}: {ex.Message}");
                }
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(created, skipped, errors.Count, errors);
        }
    }

    public sealed record Response(
        int Created,
        int Skipped,
        int Errors,
        List<string> ErrorDetails);
}
