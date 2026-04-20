using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ImportExternalChangeRequest;

/// <summary>
/// Feature: ImportExternalChangeRequest — importa um pedido de mudança (CR) de sistemas externos.
/// Suporta ServiceNow, Jira, AzureDevOps e Generic como sistemas de origem.
/// A operação é idempotente: pedidos já ingeridos são devolvidos sem duplicação.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportExternalChangeRequest
{
    private static readonly string[] AllowedSystems = ["ServiceNow", "Jira", "AzureDevOps", "Generic"];

    /// <summary>Comando para importar um pedido de mudança externo.</summary>
    public sealed record Command(
        string ExternalSystem,
        string ExternalId,
        string Title,
        string? Description,
        string ChangeType,
        string RequestedBy,
        DateTimeOffset? ScheduledStart,
        DateTimeOffset? ScheduledEnd,
        Guid? ServiceId,
        string? Environment) : ICommand<Response>;

    /// <summary>Valida o comando de importação de CR externo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ExternalSystem)
                .NotEmpty()
                .Must(s => AllowedSystems.Contains(s))
                .WithMessage($"ExternalSystem must be one of: {string.Join(", ", AllowedSystems)}");

            RuleFor(x => x.ExternalId)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description is not null);

            RuleFor(x => x.ChangeType)
                .NotEmpty()
                .Must(t => new[] { "Normal", "Emergency", "Standard" }.Contains(t))
                .WithMessage("ChangeType must be Normal, Emergency, or Standard.");

            RuleFor(x => x.RequestedBy)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Environment)
                .MaximumLength(100)
                .When(x => x.Environment is not null);
        }
    }

    /// <summary>
    /// Handler que importa o pedido de mudança externo com idempotência.
    /// Verifica duplicação pela chave natural (ExternalSystem + ExternalId).
    /// Lê configuração de auto-link via IConfigurationResolutionService.
    /// </summary>
    public sealed class Handler(
        IExternalChangeRequestRepository repository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IConfigurationResolutionService configService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Verificação de idempotência pela chave natural
            var existing = await repository.GetByExternalIdAsync(
                request.ExternalSystem,
                request.ExternalId,
                cancellationToken);

            if (existing is not null)
            {
                return new Response(
                    existing.Id.Value,
                    existing.ExternalSystem,
                    existing.ExternalId,
                    existing.Title,
                    existing.Status,
                    existing.IngestedAt);
            }

            // Leitura da configuração de auto-link (não aplicada na ingestão inicial)
            await configService.ResolveEffectiveValueAsync(
                "integrations.externalChange.autoLinkEnabled",
                ConfigurationScope.System,
                null,
                cancellationToken);

            var now = dateTimeProvider.UtcNow;

            var changeRequest = ExternalChangeRequest.Create(
                request.ExternalSystem,
                request.ExternalId,
                request.Title,
                request.Description,
                request.ChangeType,
                request.RequestedBy,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.ServiceId,
                request.Environment,
                now);

            repository.Add(changeRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                changeRequest.Id.Value,
                changeRequest.ExternalSystem,
                changeRequest.ExternalId,
                changeRequest.Title,
                changeRequest.Status,
                changeRequest.IngestedAt);
        }
    }

    /// <summary>Resposta com os dados do pedido de mudança externo importado.</summary>
    public sealed record Response(
        Guid Id,
        string ExternalSystem,
        string ExternalId,
        string Title,
        ExternalChangeRequestStatus Status,
        DateTimeOffset IngestedAt);
}
