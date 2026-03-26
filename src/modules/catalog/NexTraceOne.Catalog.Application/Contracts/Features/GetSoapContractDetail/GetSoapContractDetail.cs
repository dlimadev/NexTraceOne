using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSoapContractDetail;

/// <summary>
/// Feature: GetSoapContractDetail — consulta os metadados SOAP/WSDL específicos de uma versão de contrato.
/// Retorna informações extraídas do WSDL: serviço, namespace, versão SOAP, portType, binding, endpoint e operações.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetSoapContractDetail
{
    /// <summary>Query para obter os detalhes SOAP de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que consulta o SoapContractDetail associado a uma versão de contrato.
    /// Retorna erro quando a versão não tem detalhe SOAP associado.
    /// </summary>
    public sealed class Handler(
        ISoapContractDetailRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versionId = ContractVersionId.From(request.ContractVersionId);
            var detail = await repository.GetByContractVersionIdAsync(versionId, cancellationToken);

            if (detail is null)
                return ContractsErrors.SoapDetailNotFound(request.ContractVersionId.ToString());

            return new Response(
                SoapDetailId: detail.Id.Value,
                ContractVersionId: detail.ContractVersionId.Value,
                ServiceName: detail.ServiceName,
                TargetNamespace: detail.TargetNamespace,
                SoapVersion: detail.SoapVersion,
                EndpointUrl: detail.EndpointUrl,
                WsdlSourceUrl: detail.WsdlSourceUrl,
                PortTypeName: detail.PortTypeName,
                BindingName: detail.BindingName,
                ExtractedOperationsJson: detail.ExtractedOperationsJson);
        }
    }

    /// <summary>Resposta com os detalhes SOAP/WSDL extraídos da versão de contrato.</summary>
    public sealed record Response(
        Guid SoapDetailId,
        Guid ContractVersionId,
        string ServiceName,
        string TargetNamespace,
        string SoapVersion,
        string? EndpointUrl,
        string? WsdlSourceUrl,
        string? PortTypeName,
        string? BindingName,
        string ExtractedOperationsJson);
}
