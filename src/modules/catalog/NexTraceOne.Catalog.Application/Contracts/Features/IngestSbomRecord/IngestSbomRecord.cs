using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.IngestSbomRecord;

/// <summary>
/// Feature: IngestSbomRecord — ingere um SBOM para um serviço numa versão específica.
///
/// Recebe a lista de componentes (dependências directas e transitivas) e persiste
/// o <c>SbomRecord</c> para análise posterior de vulnerabilidades, licenciamento e proveniência.
///
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance (Catalog Contracts).
/// </summary>
public static class IngestSbomRecord
{
    // ── Component input ────────────────────────────────────────────────────
    public sealed record ComponentInput(
        string Name,
        string Version,
        string Registry,
        string License,
        int CveCount,
        string HighestCveSeverity);

    // ── Command ────────────────────────────────────────────────────────────
    public sealed record Command(
        string TenantId,
        string ServiceId,
        string ServiceName,
        string Version,
        IReadOnlyList<ComponentInput> Components) : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(400);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Components).NotNull();
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISbomRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ServiceId);
            Guard.Against.NullOrWhiteSpace(request.Version);

            var id = Guid.NewGuid();
            var components = request.Components
                .Select(c => new SbomComponent(c.Name, c.Version, c.Registry, c.License, c.CveCount, c.HighestCveSeverity))
                .ToList();

            var record = new SbomRecord(
                id, request.TenantId, request.ServiceId, request.ServiceName,
                request.Version, clock.UtcNow, components);

            await repository.AddAsync(record, cancellationToken);

            return Result<Guid>.Success(id);
        }
    }
}
