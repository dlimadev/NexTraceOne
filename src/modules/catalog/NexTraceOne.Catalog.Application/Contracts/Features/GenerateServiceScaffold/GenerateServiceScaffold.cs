using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Generation;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateServiceScaffold;

/// <summary>
/// Feature: GenerateServiceScaffold — cria o scaffold da aplicação a partir do NOME ÚNICO de um
/// serviço já registado na plataforma. Identifica o TIPO do serviço e o CONTRATO associado e gera:
///   - API REST com contrato OpenAPI → classes (DTOs + endpoints) via gerador determinístico;
///   - outros modelos (Kafka consumer/producer, SOAP, gRPC, worker, …) → esqueleto do modelo.
///
/// É o backend do comando CLI 'nex scaffold service &lt;nome&gt;'. Determinístico (sem IA).
/// Wave AQ.4 — Contract-first deterministic code generation.
/// </summary>
public static class GenerateServiceScaffold
{
    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>Gera o scaffold da aplicação para o serviço registado com este nome único.</summary>
    public sealed record Query(string ServiceName) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
        }
    }

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Resultado: estratégia usada, contrato detetado e ficheiros gerados.</summary>
    public sealed record Response(
        string ServiceName,
        string ServiceType,
        string GenerationStrategy,
        string? ContractProtocol,
        int FileCount,
        IReadOnlyList<GeneratedCodeFile> Files);

    // ── Handler ────────────────────────────────────────────────────────────

    internal sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        IOpenApiContractParser openApiParser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.ServiceName);

            var service = await serviceAssetRepository.GetByNameAsync(request.ServiceName, cancellationToken);
            if (service is null)
                return Error.NotFound("Service.NotFound", $"Service '{request.ServiceName}' was not found in the catalog.");

            var serviceType = service.ServiceType;

            // Resolver o contrato associado: ServiceAsset → ApiAsset → ContractVersion (mais recente).
            string? specContent = null;
            ContractProtocol? protocol = null;

            var apiAssets = await apiAssetRepository.ListByServiceIdAsync(service.Id, cancellationToken);
            foreach (var api in apiAssets)
            {
                var latest = await contractVersionRepository.GetLatestByApiAssetAsync(api.Id.Value, cancellationToken);
                if (latest is not null && !string.IsNullOrWhiteSpace(latest.SpecContent))
                {
                    specContent = latest.SpecContent;
                    protocol = latest.Protocol;
                    break;
                }
            }

            var (files, strategy) = Generate(request.ServiceName, serviceType, specContent, protocol);

            return new Response(
                ServiceName: request.ServiceName,
                ServiceType: serviceType.ToString(),
                GenerationStrategy: strategy,
                ContractProtocol: protocol?.ToString(),
                FileCount: files.Count,
                Files: files);
        }

        private (IReadOnlyList<GeneratedCodeFile> Files, string Strategy) Generate(
            string serviceName, ServiceType serviceType, string? specContent, ContractProtocol? protocol)
        {
            var isHttpApi = serviceType is ServiceType.RestApi or ServiceType.GraphqlApi or ServiceType.Gateway;
            var isOpenApi = protocol is ContractProtocol.OpenApi or ContractProtocol.Swagger;

            // API REST/HTTP com contrato OpenAPI → gerar classes a partir do contrato.
            if (isHttpApi && isOpenApi && specContent is not null)
            {
                var parsed = openApiParser.Parse(specContent);
                if (parsed.IsSuccess)
                {
                    var generated = DotNetCleanArchitectureCodeGenerator.Generate(
                        parsed.Value, new CodeGenerationOptions(serviceName));
                    return (generated, "openapi");
                }

                // Contrato presente mas inválido/não parseável → esqueleto REST como fallback seguro.
                return (ServiceScaffoldGenerator.Generate(serviceName, serviceType), "skeleton (contract not parseable)");
            }

            // Restantes modelos → esqueleto determinístico do modelo.
            return (ServiceScaffoldGenerator.Generate(serviceName, serviceType), "skeleton");
        }
    }
}
