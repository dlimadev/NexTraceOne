using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.ConfigurationKeys;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease;

/// <summary>
/// Feature: IngestExternalRelease — recebe uma release criada por um sistema externo
/// (AzureDevOps, Jira, Jenkins, GitLab) e regista-a internamente no NexTraceOne.
///
/// Se a release já existir (mesmo ExternalReleaseId + ExternalSystem), retorna a existente.
/// Commits e work items fornecidos são associados automaticamente.
///
/// A release é criada em estado Pending aguardando promoção.
///
/// A ingestão é controlada pelo parâmetro <c>env.behavior.change.ingest.enabled</c>.
/// Quando o ambiente alvo está com ingestão desabilitada, o comando retorna erro de negócio.
///
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class IngestExternalRelease
{
    /// <summary>Referência de work item externo para associação.</summary>
    public sealed record ExternalWorkItemRef(string Id, string System);

    /// <summary>Comando de ingestão de release de sistema externo.</summary>
    public sealed record Command(
        string ExternalReleaseId,
        string ExternalSystem,
        string ServiceName,
        string Version,
        string TargetEnvironment,
        string? Description = null,
        IReadOnlyList<string>? CommitShas = null,
        IReadOnlyList<ExternalWorkItemRef>? WorkItems = null,
        bool TriggerPromotion = false,
        /// <summary>
        /// Identificador do ambiente alvo (opcional).
        /// Quando fornecido, a verificação de <c>env.behavior.change.ingest.enabled</c>
        /// é feita ao nível do ambiente específico.
        /// Quando nulo, resolve ao nível de sistema (padrão habilitado).
        /// </summary>
        Guid? EnvironmentId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ExternalReleaseId).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ExternalSystem).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria uma Release internamente a partir dos dados do sistema externo.
    /// É idempotente: re-ingestão do mesmo ExternalReleaseId retorna o existente.
    /// Gateado por <c>env.behavior.change.ingest.enabled</c>.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        ICurrentTenant currentTenant,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEnvironmentBehaviorService environmentBehaviorService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Gate: verificar se ingestão está habilitada para este ambiente ──
            var environmentIdStr = request.EnvironmentId?.ToString();
            var ingestEnabled = await environmentBehaviorService.IsEnabledAsync(
                EnvironmentBehaviorConfigKeys.ChangeIngestEnabled,
                environmentIdStr,
                cancellationToken);

            if (!ingestEnabled)
                return ChangeIntelligenceErrors.IngestDisabledForEnvironment(request.TargetEnvironment);

            // Idempotência: verifica se já existe release com a mesma chave natural externa.
            // Usa ExternalReleaseId + ExternalSystem como chave canónica — é o identificador
            // que o sistema de origem possui, sem necessitar do GUID interno do NexTraceOne.
            var existing = await releaseRepository.GetByExternalKeyAsync(
                request.ExternalReleaseId, request.ExternalSystem, cancellationToken);

            if (existing is not null)
                return new Response(existing.Id.Value, request.ExternalReleaseId, false, existing.Status.ToString());

            var tenantId = currentTenant.Id;
            var now = dateTimeProvider.UtcNow;

            var release = Release.Create(
                tenantId: tenantId,
                apiAssetId: Guid.Empty, // sem ativo de API conhecido no momento da ingestão externa
                serviceName: request.ServiceName,
                version: request.Version,
                environment: request.TargetEnvironment,
                pipelineSource: $"External:{request.ExternalSystem}",
                commitSha: request.CommitShas is { Count: > 0 } shas ? shas[0] : "external",
                createdAt: now,
                externalReleaseId: request.ExternalReleaseId,
                externalSystem: request.ExternalSystem);

            releaseRepository.Add(release);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, request.ExternalReleaseId, true, release.Status.ToString());
        }
    }

    /// <summary>Resposta da ingestão de release externa.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ExternalReleaseId,
        bool IsNew,
        string Status);
}
