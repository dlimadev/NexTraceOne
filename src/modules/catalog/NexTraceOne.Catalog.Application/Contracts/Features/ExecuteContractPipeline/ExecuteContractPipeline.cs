using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ExecuteContractPipeline;

/// <summary>
/// Feature: ExecuteContractPipeline — inicia a execução de um pipeline automatizado
/// de geração de código a partir de um contrato (API Asset).
/// Regista a execução com estado Running e persiste para rastreabilidade.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ExecuteContractPipeline
{
    /// <summary>Comando para iniciar a execução de um pipeline de geração de código.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ContractName,
        string ContractVersion,
        string RequestedStages,
        string TargetLanguage,
        string? TargetFramework,
        int TotalStages,
        string InitiatedByUserId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de execução de pipeline.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ContractName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ContractVersion).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RequestedStages).NotEmpty();
            RuleFor(x => x.TargetLanguage).NotEmpty().MaximumLength(50);
            RuleFor(x => x.TargetFramework).MaximumLength(100).When(x => x.TargetFramework is not null);
            RuleFor(x => x.TotalStages).GreaterThan(0);
            RuleFor(x => x.InitiatedByUserId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma nova execução de pipeline de geração de código.
    /// </summary>
    public sealed class Handler(
        IPipelineExecutionRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var execution = PipelineExecution.Create(
                apiAssetId: request.ApiAssetId,
                contractName: request.ContractName,
                contractVersion: request.ContractVersion,
                requestedStages: request.RequestedStages,
                targetLanguage: request.TargetLanguage,
                targetFramework: request.TargetFramework,
                totalStages: request.TotalStages,
                initiatedByUserId: request.InitiatedByUserId,
                startedAt: clock.UtcNow);

            await repository.AddAsync(execution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                execution.Id.Value,
                execution.ApiAssetId,
                execution.ContractName,
                execution.ContractVersion,
                execution.Status,
                execution.TotalStages,
                execution.StartedAt);
        }
    }

    /// <summary>Resposta da criação de execução de pipeline.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid ApiAssetId,
        string ContractName,
        string ContractVersion,
        PipelineExecutionStatus Status,
        int TotalStages,
        DateTimeOffset StartedAt);
}
