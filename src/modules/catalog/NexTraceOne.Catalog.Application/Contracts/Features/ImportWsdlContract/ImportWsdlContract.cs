using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ImportWsdlContract;

/// <summary>
/// Feature: ImportWsdlContract — importa e regista um contrato SOAP/WSDL com workflow real.
/// Distingue-se do ImportContract genérico: para além de criar a ContractVersion com Protocol=Wsdl,
/// extrai metadados SOAP específicos (portTypes, operações, binding, namespace, endpoint) via WsdlMetadataExtractor
/// e persiste-os num SoapContractDetail independente.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class ImportWsdlContract
{
    /// <summary>
    /// Comando de importação de contrato WSDL/SOAP com metadados específicos.
    /// </summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string WsdlContent,
        string ImportedFrom,
        string? EndpointUrl = null,
        string? WsdlSourceUrl = null,
        string? SoapVersion = null) : ICommand<Response>;

    /// <summary>
    /// Valida a entrada do comando de importação WSDL.
    /// O conteúdo deve ser XML WSDL válido (começa com &lt; e contém wsdl:definitions ou &lt;definitions).
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.WsdlContent).NotEmpty()
                .MaximumLength(5_242_880)
                .WithMessage("WSDL content exceeds maximum allowed size of 5MB.")
                .Must(IsWsdlContent)
                .WithMessage("Content must be a valid WSDL XML document (must start with '<' and contain WSDL definitions).");
            RuleFor(x => x.ImportedFrom).NotEmpty().MaximumLength(500);
            RuleFor(x => x.EndpointUrl).MaximumLength(2000).When(x => x.EndpointUrl is not null);
            RuleFor(x => x.WsdlSourceUrl).MaximumLength(2000).When(x => x.WsdlSourceUrl is not null);
            RuleFor(x => x.SoapVersion)
                .Must(v => v is null or "1.1" or "1.2")
                .WithMessage("SOAP version must be '1.1' or '1.2' when provided.");
        }

        private static bool IsWsdlContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
            var trimmed = content.TrimStart();
            return trimmed.StartsWith('<')
                && (trimmed.Contains("wsdl:definitions", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("<definitions", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("xmlns:wsdl", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Handler que importa um contrato WSDL/SOAP com workflow real:
    /// 1. Verifica unicidade da versão para o ativo de API
    /// 2. Cria a ContractVersion com Protocol=Wsdl
    /// 3. Extrai metadados SOAP via WsdlMetadataExtractor
    /// 4. Persiste o SoapContractDetail com os metadados extraídos
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        ISoapContractDetailRepository soapDetailRepository,
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

            // 2. Cria ContractVersion com Protocol=Wsdl
            var importResult = ContractVersion.Import(
                request.ApiAssetId,
                request.SemVer,
                request.WsdlContent,
                "xml",
                request.WsdlSourceUrl ?? request.ImportedFrom,
                ContractProtocol.Wsdl);

            if (importResult.IsFailure)
                return importResult.Error;

            var contractVersion = importResult.Value;
            contractVersionRepository.Add(contractVersion);

            // 3. Extrai metadados SOAP específicos do WSDL
            var metadata = WsdlMetadataExtractor.Extract(request.WsdlContent);

            // Override do endpoint se fornecido explicitamente
            var endpointUrl = request.EndpointUrl ?? metadata.EndpointUrl;

            // Override da versão SOAP se fornecida explicitamente
            var soapVersion = request.SoapVersion ?? metadata.SoapVersion;

            // 4. Cria SoapContractDetail com metadados extraídos
            var detailResult = SoapContractDetail.Create(
                contractVersion.Id,
                metadata.ServiceName,
                metadata.TargetNamespace,
                soapVersion,
                metadata.ExtractedOperationsJson,
                endpointUrl,
                request.WsdlSourceUrl,
                metadata.PortTypeName,
                metadata.BindingName);

            if (detailResult.IsFailure)
                return detailResult.Error;

            soapDetailRepository.Add(detailResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ContractVersionId: contractVersion.Id.Value,
                ApiAssetId: contractVersion.ApiAssetId,
                SemVer: contractVersion.SemVer,
                SoapVersion: soapVersion,
                ServiceName: metadata.ServiceName,
                TargetNamespace: metadata.TargetNamespace,
                PortTypeName: metadata.PortTypeName,
                BindingName: metadata.BindingName,
                EndpointUrl: endpointUrl,
                ExtractedOperationsJson: metadata.ExtractedOperationsJson,
                ImportedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da importação WSDL, incluindo metadados SOAP extraídos.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string SoapVersion,
        string ServiceName,
        string TargetNamespace,
        string? PortTypeName,
        string? BindingName,
        string? EndpointUrl,
        string ExtractedOperationsJson,
        DateTimeOffset ImportedAt);
}
