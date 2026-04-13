using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordTraceCorrelation;

/// <summary>
/// Feature: RecordTraceCorrelation — correlaciona automaticamente um trace OTel a uma Release.
/// Pipeline: trace identificado → Release lookup → ChangeEvent(trace_correlated) + mapping analítico Elasticsearch.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordTraceCorrelation
{
    /// <summary>
    /// Comando para registar a correlação entre um trace OTel e uma Release existente.
    /// Produz um registo de auditoria (ChangeEvent) e escreve no mapping analítico (Elasticsearch).
    /// </summary>
    public sealed record Command(
        Guid ReleaseId,
        string TraceId,
        string ServiceName,
        string Environment,
        Guid? ServiceId = null,
        Guid? EnvironmentId = null,
        string CorrelationSource = "deployment_event",
        DateTimeOffset? TraceStartedAt = null,
        DateTimeOffset? TraceEndedAt = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de correlação de trace.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.TraceId).NotEmpty().MaximumLength(128);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CorrelationSource).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que persiste a correlação trace → release no domínio e no mapping analítico.
    /// 1. Verifica que a Release existe.
    /// 2. Cria ChangeEvent(trace_correlated) no PostgreSQL para rastreabilidade.
    /// 3. Escreve TraceReleaseMappingRecord no Elasticsearch via ITraceCorrelationWriter (fire-and-forget).
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeEventRepository changeEventRepository,
        ITraceCorrelationWriter traceCorrelationWriter,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var now = dateTimeProvider.UtcNow;
            var mappingId = Guid.NewGuid();

            // Registo de auditoria em PostgreSQL — timeline da release
            var changeEvent = ChangeEvent.Create(
                releaseId,
                eventType: "trace_correlated",
                description: $"Trace '{request.TraceId}' correlated to release {release.ServiceName}@{release.Version} in {release.Environment} (source: {request.CorrelationSource})",
                source: request.TraceId,
                occurredAt: now);

            changeEventRepository.Add(changeEvent);
            await unitOfWork.CommitAsync(cancellationToken);

            // Registo analítico no Elasticsearch — fire-and-forget
            // SuppressWriteErrors = true garante que falhas no Elasticsearch não propagam
            await traceCorrelationWriter.WriteAsync(
                mappingId: mappingId,
                tenantId: release.TenantId,
                releaseId: request.ReleaseId,
                traceId: request.TraceId,
                serviceName: request.ServiceName,
                serviceId: request.ServiceId,
                environment: request.Environment,
                environmentId: request.EnvironmentId,
                correlationSource: request.CorrelationSource,
                traceStartedAt: request.TraceStartedAt,
                traceEndedAt: request.TraceEndedAt,
                correlatedAt: now,
                cancellationToken: cancellationToken);

            return new Response(
                CorrelationId: mappingId,
                ReleaseId: request.ReleaseId,
                TraceId: request.TraceId,
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                CorrelationSource: request.CorrelationSource,
                CorrelatedAt: now);
        }
    }

    /// <summary>Resposta da correlação trace → release.</summary>
    public sealed record Response(
        Guid CorrelationId,
        Guid ReleaseId,
        string TraceId,
        string ServiceName,
        string Environment,
        string CorrelationSource,
        DateTimeOffset CorrelatedAt);
}
