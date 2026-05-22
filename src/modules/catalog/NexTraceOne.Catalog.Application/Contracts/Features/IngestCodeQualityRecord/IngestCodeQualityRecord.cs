using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.IngestCodeQualityRecord;

/// <summary>
/// Feature: IngestCodeQualityRecord — ingere o resultado de uma análise de qualidade de código.
///
/// Recebe métricas de ferramentas como SonarQube (quality gate, cobertura, bugs, vulnerabilidades,
/// code smells, duplicação) e persiste o <c>CodeQualityRecord</c> para relatórios e alertas.
///
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public static class IngestCodeQualityRecord
{
    // ── Command ────────────────────────────────────────────────────────────
    public sealed record Command(
        string TenantId,
        string ServiceId,
        string ServiceName,
        string ProjectKey,
        string QualityGateStatus,
        double Coverage,
        int Bugs,
        int Vulnerabilities,
        int CodeSmells,
        double DuplicatedLinesDensity,
        string? Branch) : ICommand<Guid>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(400);
            RuleFor(x => x.ProjectKey).NotEmpty().MaximumLength(400);
            RuleFor(x => x.QualityGateStatus).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Coverage).InclusiveBetween(0.0, 100.0);
            RuleFor(x => x.Bugs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Vulnerabilities).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CodeSmells).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DuplicatedLinesDensity).InclusiveBetween(0.0, 100.0);
            RuleFor(x => x.Branch).MaximumLength(200).When(x => x.Branch is not null);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────
    internal sealed class Handler(
        ICodeQualityRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Guid>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ServiceId);
            Guard.Against.NullOrWhiteSpace(request.ProjectKey);

            var id = Guid.NewGuid();
            var record = new CodeQualityRecord(
                id,
                request.TenantId,
                request.ServiceId,
                request.ServiceName,
                request.ProjectKey,
                request.QualityGateStatus.ToUpperInvariant(),
                request.Coverage,
                request.Bugs,
                request.Vulnerabilities,
                request.CodeSmells,
                request.DuplicatedLinesDensity,
                request.Branch,
                clock.UtcNow);

            await repository.AddAsync(record, cancellationToken);

            return Result<Guid>.Success(id);
        }
    }
}
