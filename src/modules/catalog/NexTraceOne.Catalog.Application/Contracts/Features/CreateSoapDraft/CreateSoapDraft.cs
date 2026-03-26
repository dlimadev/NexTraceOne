using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft;

/// <summary>
/// Feature: CreateSoapDraft — cria um draft SOAP/WSDL no Contract Studio com metadados SOAP específicos.
/// Distingue-se do CreateDraft genérico: para além de criar o ContractDraft com ContractType=Soap e Protocol=Wsdl,
/// cria também um SoapDraftMetadata com os campos específicos de SOAP (serviço, namespace, binding, operações).
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class CreateSoapDraft
{
    /// <summary>
    /// Comando de criação de draft SOAP/WSDL com metadados específicos.
    /// </summary>
    public sealed record Command(
        string Title,
        string Author,
        string ServiceName,
        string TargetNamespace,
        string SoapVersion = "1.1",
        Guid? ServiceId = null,
        string? Description = null,
        string? EndpointUrl = null,
        string? PortTypeName = null,
        string? BindingName = null,
        string? OperationsJson = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de draft SOAP.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TargetNamespace).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.SoapVersion)
                .Must(v => v is "1.1" or "1.2")
                .WithMessage("SOAP version must be '1.1' or '1.2'.");
            RuleFor(x => x.EndpointUrl).MaximumLength(2000).When(x => x.EndpointUrl is not null);
        }
    }

    /// <summary>
    /// Handler que cria um draft SOAP/WSDL com metadados específicos:
    /// 1. Cria ContractDraft com ContractType=Soap e Protocol=Wsdl
    /// 2. Cria SoapDraftMetadata com os campos SOAP informados
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        ISoapDraftMetadataRepository soapMetadataRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Cria o ContractDraft com tipo e protocolo SOAP
            var draftResult = ContractDraft.Create(
                request.Title,
                request.Author,
                ContractType.Soap,
                ContractProtocol.Wsdl,
                request.ServiceId,
                request.Description);

            if (draftResult.IsFailure)
                return draftResult.Error;

            var draft = draftResult.Value;
            draftRepository.Add(draft);

            // 2. Cria SoapDraftMetadata com metadados SOAP específicos
            var metadata = SoapDraftMetadata.Create(
                draft.Id,
                request.ServiceName,
                request.TargetNamespace,
                request.SoapVersion,
                request.EndpointUrl,
                request.PortTypeName,
                request.BindingName,
                request.OperationsJson ?? "{}");

            soapMetadataRepository.Add(metadata);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                DraftId: draft.Id.Value,
                Title: draft.Title,
                Status: draft.Status.ToString(),
                ServiceName: request.ServiceName,
                TargetNamespace: request.TargetNamespace,
                SoapVersion: request.SoapVersion,
                CreatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da criação de draft SOAP, incluindo metadados SOAP inicializados.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        string ServiceName,
        string TargetNamespace,
        string SoapVersion,
        DateTimeOffset CreatedAt);
}
