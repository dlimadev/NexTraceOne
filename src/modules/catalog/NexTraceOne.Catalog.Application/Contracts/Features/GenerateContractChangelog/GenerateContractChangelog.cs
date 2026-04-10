using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateContractChangelog;

/// <summary>
/// Feature: GenerateContractChangelog — cria uma entrada de changelog para um contrato,
/// associando-a à versão de contrato e opcionalmente à verificação que a originou.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateContractChangelog
{
    /// <summary>Comando para gerar uma entrada de changelog de contrato.</summary>
    public sealed record Command(
        string ApiAssetId,
        string ServiceName,
        string? FromVersion,
        string ToVersion,
        Guid ContractVersionId,
        Guid? VerificationId,
        int Source,
        string Entries,
        string Summary,
        string? CommitSha) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de geração de changelog.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.ToVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.Source).IsInEnum();
            RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>
    /// Handler que cria uma entrada de changelog de contrato e persiste no repositório.
    /// </summary>
    public sealed class Handler(
        IContractChangelogRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var changelog = ContractChangelog.Create(
                tenantId: currentTenant.Id.ToString(),
                apiAssetId: request.ApiAssetId,
                serviceName: request.ServiceName,
                fromVersion: request.FromVersion,
                toVersion: request.ToVersion,
                contractVersionId: request.ContractVersionId,
                verificationId: request.VerificationId,
                source: (ChangelogSource)request.Source,
                entries: request.Entries,
                summary: request.Summary,
                markdownContent: null,
                jsonContent: null,
                commitSha: request.CommitSha,
                createdAt: now,
                createdBy: currentUser.Id);

            await repository.AddAsync(changelog, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(changelog.Id.Value, now);
        }
    }

    /// <summary>Resposta da geração de changelog de contrato.</summary>
    public sealed record Response(Guid ChangelogId, DateTimeOffset CreatedAt);
}
