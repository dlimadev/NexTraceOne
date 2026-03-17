using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ImportContract;

/// <summary>
/// Feature: ImportContract — importa a primeira versão de um contrato multi-protocolo para um ativo de API.
/// Suporta OpenAPI, Swagger, WSDL/SOAP, AsyncAPI e formatos futuros (Protobuf, GraphQL).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportContract
{
    /// <summary>
    /// Comando de importação de versão de contrato.
    /// O campo Protocol é opcional e padrão OpenApi para compatibilidade retroativa.
    /// </summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string SpecContent,
        string Format,
        string ImportedFrom,
        ContractProtocol Protocol = ContractProtocol.OpenApi) : ICommand<Response>;

    /// <summary>
    /// Valida a entrada do comando de importação de contrato.
    /// Aceita formatos json, yaml e xml para suportar WSDL e outros formatos baseados em XML.
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SpecContent).NotEmpty()
                .MaximumLength(5_242_880)
                .WithMessage("Spec content exceeds maximum allowed size of 5MB.");
            RuleFor(x => x.Format).NotEmpty()
                .Must(f => f is "json" or "yaml" or "xml")
                .WithMessage("Format must be 'json', 'yaml' or 'xml'.");
            RuleFor(x => x.ImportedFrom).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Protocol).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que importa uma nova versão de contrato multi-protocolo.
    /// Delega a criação da entidade ao factory method ContractVersion.Import,
    /// que aplica todas as regras de domínio e invariantes.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await repository.GetByApiAssetAndSemVerAsync(request.ApiAssetId, request.SemVer, cancellationToken);
            if (existing is not null)
                return ContractsErrors.AlreadyExists(request.SemVer, request.ApiAssetId.ToString());

            // Auto-detecção de protocolo quando o valor padrão (OpenApi) é informado
            var protocol = DetectProtocol(request.SpecContent, request.Protocol);

            var result = ContractVersion.Import(
                request.ApiAssetId,
                request.SemVer,
                request.SpecContent,
                request.Format,
                request.ImportedFrom,
                protocol);

            if (result.IsFailure)
                return result.Error;

            var version = result.Value;
            repository.Add(version);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                version.ApiAssetId,
                version.SemVer,
                version.Format,
                version.Protocol,
                dateTimeProvider.UtcNow);
        }

        /// <summary>
        /// Detecta o protocolo do contrato a partir do conteúdo da especificação.
        /// Quando o protocolo informado é o padrão (OpenApi), tenta inferir automaticamente
        /// analisando chaves e estrutura do conteúdo (swagger, asyncapi, wsdl:definitions).
        /// Limita a busca aos primeiros 4KB para eficiência em specs grandes.
        /// Suporta detecção em JSON (chaves com aspas) e YAML (chaves sem aspas).
        /// </summary>
        private static ContractProtocol DetectProtocol(string specContent, ContractProtocol specified)
        {
            if (specified != ContractProtocol.OpenApi)
                return specified;

            var trimmed = specContent.TrimStart();

            // WSDL: conteúdo XML com definições WSDL — verificação no início é suficiente
            if (trimmed.StartsWith('<') && (trimmed.Contains("wsdl:definitions", StringComparison.OrdinalIgnoreCase)
                                            || trimmed.Contains("<definitions", StringComparison.OrdinalIgnoreCase)))
                return ContractProtocol.Wsdl;

            // Para JSON/YAML, chaves de identificação ficam no topo — limita busca a 4KB
            var header = specContent.Length > 4096 ? specContent[..4096] : specContent;

            // AsyncAPI: chave "asyncapi" em JSON ou YAML
            if (header.Contains("\"asyncapi\"", StringComparison.OrdinalIgnoreCase)
                || header.Contains("asyncapi:", StringComparison.OrdinalIgnoreCase))
                return ContractProtocol.AsyncApi;

            // Swagger 2.0: chave "swagger" em JSON ou YAML
            if (header.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
                || header.Contains("swagger:", StringComparison.OrdinalIgnoreCase))
                return ContractProtocol.Swagger;

            return specified;
        }
    }

    /// <summary>Resposta da importação de versão de contrato, incluindo protocolo identificado.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Format,
        ContractProtocol Protocol,
        DateTimeOffset CreatedAt);
}

