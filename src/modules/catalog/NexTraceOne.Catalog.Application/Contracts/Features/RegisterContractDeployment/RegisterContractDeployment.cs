using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RegisterContractDeployment;

/// <summary>
/// Feature: RegisterContractDeployment — regista um evento de deployment de uma versão de contrato.
/// Liga a versão ao ambiente de destino para rastreabilidade de mudanças e Change Intelligence.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterContractDeployment
{
    /// <summary>Comando para registar um deployment de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string Environment,
        string DeployedBy,
        string SourceSystem,
        ContractDeploymentStatus Status,
        DateTimeOffset? DeployedAt,
        string? Notes) : ICommand<Response>;

    /// <summary>Valida os parâmetros do registo de deployment.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DeployedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
        }
    }

    /// <summary>
    /// Handler que regista o deployment de uma versão de contrato.
    /// Valida a existência da versão e cria o registo de rastreabilidade.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IContractDeploymentRepository deploymentRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await contractVersionRepository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var deployment = ContractDeployment.Create(
                contractVersionId: version.Id,
                apiAssetId: version.ApiAssetId,
                environment: request.Environment,
                semVer: version.SemVer,
                status: request.Status,
                deployedAt: request.DeployedAt ?? clock.UtcNow,
                deployedBy: request.DeployedBy,
                sourceSystem: request.SourceSystem,
                notes: request.Notes);

            deploymentRepository.Add(deployment);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                deployment.Id.Value,
                version.Id.Value,
                version.ApiAssetId,
                deployment.Environment,
                deployment.SemVer,
                deployment.Status.ToString(),
                deployment.DeployedAt,
                deployment.DeployedBy,
                deployment.SourceSystem,
                deployment.Notes);
        }
    }

    /// <summary>Resposta do registo de deployment de contrato.</summary>
    public sealed record Response(
        Guid DeploymentId,
        Guid ContractVersionId,
        Guid ApiAssetId,
        string Environment,
        string SemVer,
        string Status,
        DateTimeOffset DeployedAt,
        string DeployedBy,
        string SourceSystem,
        string? Notes);
}
