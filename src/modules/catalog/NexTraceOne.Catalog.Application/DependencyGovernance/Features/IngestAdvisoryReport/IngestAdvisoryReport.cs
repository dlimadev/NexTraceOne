using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.IngestAdvisoryReport;

/// <summary>
/// Feature: IngestAdvisoryReport — ingesta um lote de advisories de vulnerabilidade
/// para um serviço a partir de fontes externas (GHSA, NVD, custom).
/// Idempotente por (ServiceId, AdvisoryId): actualiza se já existe, cria se não.
///
/// Wave C.1 — Supply-chain security.
/// </summary>
public static class IngestAdvisoryReport
{
    public sealed record AdvisoryInput(
        string AdvisoryId,
        VulnerabilitySeverity Severity,
        decimal CvssScore,
        string Title,
        string Source,
        DateTimeOffset PublishedAt,
        string? Description = null,
        string? PackageName = null,
        string? AffectedVersionRange = null,
        string? FixedInVersion = null);

    public sealed record Command(
        Guid ServiceId,
        IReadOnlyList<AdvisoryInput> Advisories) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.Advisories).NotEmpty();
            RuleForEach(x => x.Advisories).ChildRules(a =>
            {
                a.RuleFor(x => x.AdvisoryId).NotEmpty().MaximumLength(200);
                a.RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
                a.RuleFor(x => x.Source).NotEmpty().MaximumLength(100);
                a.RuleFor(x => x.CvssScore).InclusiveBetween(0m, 10m);
            });
        }
    }

    public sealed class Handler(
        IVulnerabilityAdvisoryRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var created = 0;
            var updated = 0;

            foreach (var advisory in request.Advisories)
            {
                var existing = await repository.FindByServiceAndAdvisoryAsync(
                    request.ServiceId, advisory.AdvisoryId, cancellationToken);

                if (existing is null)
                {
                    var record = VulnerabilityAdvisoryRecord.Create(
                        serviceId: request.ServiceId,
                        advisoryId: advisory.AdvisoryId,
                        severity: advisory.Severity,
                        cvssScore: advisory.CvssScore,
                        title: advisory.Title,
                        source: advisory.Source,
                        publishedAt: advisory.PublishedAt,
                        ingestedAt: now,
                        description: advisory.Description,
                        packageName: advisory.PackageName,
                        affectedVersionRange: advisory.AffectedVersionRange,
                        fixedInVersion: advisory.FixedInVersion);
                    await repository.AddAsync(record, cancellationToken);
                    created++;
                }
                else if (advisory.FixedInVersion is not null && existing.FixedInVersion is null)
                {
                    existing.UpdateFixedInVersion(advisory.FixedInVersion);
                    await repository.UpdateAsync(existing, cancellationToken);
                    updated++;
                }
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(request.ServiceId, created, updated, now));
        }
    }

    public sealed record Response(Guid ServiceId, int Created, int Updated, DateTimeOffset IngestedAt);
}
