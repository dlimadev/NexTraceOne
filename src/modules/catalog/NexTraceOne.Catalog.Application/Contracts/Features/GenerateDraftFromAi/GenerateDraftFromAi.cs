using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Policies;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;

/// <summary>
/// Feature: GenerateDraftFromAi — gera um draft de contrato assistido por IA real.
/// Integra com IAiDraftGenerator para gerar conteúdo real via provider de IA governado.
/// Quando IA não está disponível, retorna template estrutural mínimo com AiGenerated=false.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateDraftFromAi
{
    /// <summary>Comando de geração de draft por IA.</summary>
    public sealed record Command(
        string Title,
        string Author,
        ContractType ContractType,
        ContractProtocol Protocol,
        string Prompt,
        Guid ServiceId = default) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de geração por IA.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractType).IsInEnum();
            RuleFor(x => x.Protocol).IsInEnum();
            RuleFor(x => x.Prompt).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.ServiceId).NotEqual(Guid.Empty).WithMessage("ServiceId is required.");
        }
    }

    /// <summary>
    /// Handler que gera um draft de contrato assistido por IA real.
    /// Usa IAiDraftGenerator quando disponível. Quando IA não está disponível,
    /// usa template estrutural mínimo e indica AiGenerated=false na resposta.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IServiceAssetRepository serviceAssetRepository,
        IContractsUnitOfWork unitOfWork,
        IAiDraftGenerator? aiGenerator = null) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Valida existência do serviço
            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId),
                cancellationToken);

            if (service is null)
                return ContractsErrors.CatalogLinkNotFound(request.ServiceId.ToString());

            // Valida que o tipo de serviço suporta contratos
            if (!ServiceContractPolicy.SupportsContracts(service.ServiceType))
                return ContractsErrors.ServiceTypeDoesNotSupportContracts(service.ServiceType.ToString());

            // Valida que o tipo de contrato é permitido para o tipo de serviço
            if (!ServiceContractPolicy.IsContractTypeAllowed(service.ServiceType, request.ContractType))
                return ContractsErrors.ContractTypeNotAllowedForServiceType(
                    request.ContractType.ToString(),
                    service.ServiceType.ToString());

            string content;
            string format;
            var aiGenerated = false;

            if (aiGenerator is not null)
            {
                var generated = await aiGenerator.GenerateAsync(
                    request.Protocol,
                    request.Title,
                    request.Prompt,
                    cancellationToken);

                if (generated is not null)
                {
                    content = generated.Value.Content;
                    format = generated.Value.Format;
                    aiGenerated = true;
                }
                else
                {
                    (content, format) = GenerateTemplate(request.Protocol, request.Title);
                }
            }
            else
            {
                (content, format) = GenerateTemplate(request.Protocol, request.Title);
            }

            var result = ContractDraft.CreateFromAi(
                request.Title,
                request.Author,
                request.ContractType,
                request.Protocol,
                request.Prompt,
                content,
                format,
                request.ServiceId);

            if (result.IsFailure)
                return result.Error;

            var draft = result.Value;
            repository.Add(draft);
            await unitOfWork.CommitAsync(cancellationToken);

            var persistedDraft = (await repository.ListAsync(
                    DraftStatus.Editing,
                    request.ServiceId,
                    request.Author,
                    1,
                    20,
                    cancellationToken))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault(item => item.Title == request.Title && item.Protocol == request.Protocol)
                ?? draft;

            return new Response(persistedDraft.Id.Value, content, aiGenerated);
        }

        /// <summary>
        /// Gera template estático como fallback quando IA não está disponível.
        /// </summary>
        private static (string Content, string Format) GenerateTemplate(
            ContractProtocol protocol,
            string title)
        {
            return protocol switch
            {
                ContractProtocol.AsyncApi => (
                    $$"""
                      asyncapi: '2.6.0'
                      info:
                        title: '{{title}}'
                        version: '1.0.0'
                      channels: {}
                      """,
                    "yaml"),

                ContractProtocol.Wsdl => (
                    $"""
                     <?xml version="1.0" encoding="UTF-8"?>
                     <definitions name="{title}"
                       xmlns="http://schemas.xmlsoap.org/wsdl/"
                       xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/">
                     </definitions>
                     """,
                    "xml"),

                _ => (
                    $$"""
                      openapi: '3.1.0'
                      info:
                        title: '{{title}}'
                        version: '1.0.0'
                      paths: {}
                      """,
                    "yaml")
            };
        }
    }

    /// <summary>Resposta da geração de draft por IA com preview do conteúdo gerado.</summary>
    public sealed record Response(
        Guid DraftId,
        string GeneratedContent,
        bool AiGenerated = false);
}

/// <summary>
/// Abstração para geração de draft de contrato por IA.
/// Implementada na camada de infraestrutura com integração ao provider real.
/// </summary>
public interface IAiDraftGenerator
{
    /// <summary>Gera conteúdo de contrato via IA baseado no protocolo, título e prompt.</summary>
    Task<(string Content, string Format)?> GenerateAsync(
        ContractProtocol protocol,
        string title,
        string prompt,
        CancellationToken cancellationToken = default);
}
