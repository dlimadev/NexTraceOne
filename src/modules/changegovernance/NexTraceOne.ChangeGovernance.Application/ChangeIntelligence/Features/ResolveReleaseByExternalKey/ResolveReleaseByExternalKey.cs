using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ResolveReleaseByExternalKey;

/// <summary>
/// Feature: ResolveReleaseByExternalKey — retorna o identificador interno de uma Release
/// a partir da chave natural do sistema de origem externo.
///
/// Permite que consumidores externos (Jenkins, GitHub Actions, Azure DevOps, Argo Rollouts)
/// obtenham o GUID interno do NexTraceOne a partir do seu próprio identificador sem precisarem
/// de armazenar ou conhecer IDs internos. Implementa o padrão "Natural Key Routing".
///
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ResolveReleaseByExternalKey
{
    /// <summary>
    /// Query que resolve o identificador interno de uma release a partir da chave natural externa.
    /// </summary>
    public sealed record Query(
        string ExternalReleaseId,
        string ExternalSystem) : IQuery<Response>;

    /// <summary>Valida a entrada da query de resolução por chave natural.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ExternalReleaseId).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ExternalSystem).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que resolve a release pela chave natural do sistema de origem.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByExternalKeyAsync(
                request.ExternalReleaseId, request.ExternalSystem, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(
                    $"{request.ExternalSystem}/{request.ExternalReleaseId}");

            return new Response(
                ReleaseId: release.Id.Value,
                ExternalReleaseId: release.ExternalReleaseId!,
                ExternalSystem: release.ExternalSystem!,
                ServiceName: release.ServiceName,
                Version: release.Version,
                Environment: release.Environment,
                Status: release.Status.ToString(),
                CreatedAt: release.CreatedAt);
        }
    }

    /// <summary>
    /// Resposta com o identificador interno da release resolvida pela chave natural externa.
    /// O campo <see cref="ReleaseId"/> pode ser usado em chamadas subsequentes nas rotas
    /// que aceitam GUID interno, evitando nova resolução em cada request.
    /// </summary>
    public sealed record Response(
        Guid ReleaseId,
        string ExternalReleaseId,
        string ExternalSystem,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        DateTimeOffset CreatedAt);
}
