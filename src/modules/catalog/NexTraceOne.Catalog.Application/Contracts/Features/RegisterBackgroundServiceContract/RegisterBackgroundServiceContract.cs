using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RegisterBackgroundServiceContract;

/// <summary>
/// Feature: RegisterBackgroundServiceContract — regista um Background Service Contract com workflow real.
/// Distingue-se de outros tipos: para além de criar a ContractVersion com ContractType=BackgroundService,
/// persiste um BackgroundServiceContractDetail com metadados específicos do processo
/// (nome, categoria, trigger, schedule, inputs/outputs, side effects, concurrency).
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class RegisterBackgroundServiceContract
{
    /// <summary>
    /// Comando de registo de Background Service Contract com metadados específicos do processo.
    /// </summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string ServiceName,
        string Category,
        string TriggerType,
        string? ScheduleExpression = null,
        string? TimeoutExpression = null,
        bool AllowsConcurrency = false,
        string? InputsJson = null,
        string? OutputsJson = null,
        string? SideEffectsJson = null,
        string? SpecContent = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de Background Service Contract.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> ValidTriggerTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Cron", "Interval", "EventTriggered", "OnDemand", "Continuous"
        };

        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TriggerType)
                .NotEmpty()
                .MaximumLength(50)
                .Must(t => ValidTriggerTypes.Contains(t))
                .WithMessage("TriggerType must be one of: Cron, Interval, EventTriggered, OnDemand, Continuous.");
            RuleFor(x => x.ScheduleExpression).MaximumLength(200).When(x => x.ScheduleExpression is not null);
            RuleFor(x => x.TimeoutExpression).MaximumLength(50).When(x => x.TimeoutExpression is not null);
        }
    }

    /// <summary>
    /// Handler que regista um Background Service Contract:
    /// 1. Verifica unicidade da versão para o ativo de API
    /// 2. Cria ContractVersion com ContractType=BackgroundService
    /// 3. Persiste BackgroundServiceContractDetail com os metadados do processo
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IBackgroundServiceContractDetailRepository detailRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Verifica unicidade
            var existing = await contractVersionRepository.GetByApiAssetAndSemVerAsync(
                request.ApiAssetId, request.SemVer, cancellationToken);

            if (existing is not null)
                return ContractsErrors.AlreadyExists(request.SemVer, request.ApiAssetId.ToString());

            // 2. Cria ContractVersion com ContractType=BackgroundService
            // Usa protocolo genérico; background services não têm protocolo de wire próprio
            var importResult = ContractVersion.Import(
                request.ApiAssetId,
                request.SemVer,
                request.SpecContent ?? "{}",
                "json",
                "manual-registration",
                ContractProtocol.OpenApi);  // Protocolo genérico — background services não têm protocolo de wire

            if (importResult.IsFailure)
                return importResult.Error;

            var contractVersion = importResult.Value;
            contractVersionRepository.Add(contractVersion);

            // 3. Cria BackgroundServiceContractDetail com metadados específicos
            var detailResult = BackgroundServiceContractDetail.Create(
                contractVersion.Id,
                request.ServiceName,
                request.Category,
                request.TriggerType,
                request.InputsJson ?? "{}",
                request.OutputsJson ?? "{}",
                request.SideEffectsJson ?? "[]",
                request.ScheduleExpression,
                request.TimeoutExpression,
                request.AllowsConcurrency);

            if (detailResult.IsFailure)
                return detailResult.Error;

            detailRepository.Add(detailResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ContractVersionId: contractVersion.Id.Value,
                ApiAssetId: contractVersion.ApiAssetId,
                SemVer: contractVersion.SemVer,
                ServiceName: request.ServiceName,
                Category: request.Category,
                TriggerType: request.TriggerType,
                ScheduleExpression: request.ScheduleExpression,
                TimeoutExpression: request.TimeoutExpression,
                AllowsConcurrency: request.AllowsConcurrency,
                RegisteredAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta do registo de Background Service Contract.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string ServiceName,
        string Category,
        string TriggerType,
        string? ScheduleExpression,
        string? TimeoutExpression,
        bool AllowsConcurrency,
        DateTimeOffset RegisteredAt);
}
