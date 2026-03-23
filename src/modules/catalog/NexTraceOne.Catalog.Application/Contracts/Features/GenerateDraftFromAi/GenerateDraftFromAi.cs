using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;

/// <summary>
/// Feature: GenerateDraftFromAi — gera um draft de contrato assistido por IA real.
/// Integra com IAiDraftGenerator para gerar conteúdo real via provider de IA governado.
/// Fallback para template estático quando IA não está disponível.
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
        Guid? ServiceId = null) : ICommand<Response>;

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
        }
    }

    /// <summary>
    /// Handler que gera um draft de contrato assistido por IA real.
    /// Usa IAiDraftGenerator quando disponível, com fallback para template estático.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IContractsUnitOfWork unitOfWork,
        IAiDraftGenerator? aiGenerator = null) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            string content;
            string format;

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

            return new Response(persistedDraft.Id.Value, content);
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
        string GeneratedContent);
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
