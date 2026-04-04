using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity;

/// <summary>
/// Feature: ValidateContractIntegrity — valida se uma especificação de contrato pode ser parseada com sucesso.
/// Suporta múltiplos protocolos: OpenAPI, Swagger, AsyncAPI, WSDL, Protobuf e GraphQL.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ValidateContractIntegrity
{
    /// <summary>Query de validação de integridade de versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de validação de integridade de contrato.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler multi-protocolo que verifica se a especificação da versão é válida e retorna seus metadados.
    /// Delega o parsing ao domain service correspondente ao protocolo da versão:
    /// OpenAPI, Swagger (paths/endpoints), AsyncAPI (channels/operations), WSDL (portTypes/operations),
    /// Protobuf (messages/rpcs), GraphQL (types/root fields).
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            try
            {
                return version.Protocol switch
                {
                    ContractProtocol.OpenApi => ValidateOpenApi(version),
                    ContractProtocol.Swagger => ValidateSwagger(version),
                    ContractProtocol.AsyncApi => ValidateAsyncApi(version),
                    ContractProtocol.Wsdl => ValidateWsdl(version),
                    ContractProtocol.Protobuf => ValidateProtobuf(version),
                    ContractProtocol.GraphQl => ValidateGraphQl(version),
                    _ => new Response(false, 0, 0, null, $"Unsupported protocol: {version.Protocol}.")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "ValidateContractIntegrity: Failed to validate contract version {0} — {1}: {2}",
                    request.ContractVersionId, ex.GetType().Name, ex.Message);
                return new Response(false, 0, 0, null, "The specification content could not be parsed.");
            }
        }

        /// <summary>Valida especificação OpenAPI 3.x via OpenApiSchema.Parse.</summary>
        private static Response ValidateOpenApi(ContractVersion version)
        {
            var schema = OpenApiSchema.Parse(version.SpecContent, version.Format);
            return new Response(true, schema.PathCount, schema.EndpointCount, schema.Version, null);
        }

        /// <summary>Valida especificação Swagger 2.0 via SwaggerSpecParser.</summary>
        private static Response ValidateSwagger(ContractVersion version)
        {
            var pathsAndMethods = SwaggerSpecParser.ExtractPathsAndMethods(version.SpecContent);
            var pathCount = pathsAndMethods.Count;
            var endpointCount = pathsAndMethods.Values.Sum(m => m.Count);

            string? schemaVersion = null;
            try
            {
                using var doc = JsonDocument.Parse(version.SpecContent);
                if (doc.RootElement.TryGetProperty("swagger", out var ver))
                    schemaVersion = ver.GetString();
            }
            catch (JsonException) { /* Versão não disponível — segue sem ela */ }

            return new Response(true, pathCount, endpointCount, schemaVersion, null);
        }

        /// <summary>Valida especificação AsyncAPI via AsyncApiSpecParser.</summary>
        private static Response ValidateAsyncApi(ContractVersion version)
        {
            var channelsAndOps = AsyncApiSpecParser.ExtractChannelsAndOperations(version.SpecContent);
            var channelCount = channelsAndOps.Count;
            var operationCount = channelsAndOps.Values.Sum(o => o.Count);

            string? schemaVersion = null;
            try
            {
                using var doc = JsonDocument.Parse(version.SpecContent);
                if (doc.RootElement.TryGetProperty("asyncapi", out var ver))
                    schemaVersion = ver.GetString();
            }
            catch (JsonException) { /* Versão não disponível — segue sem ela */ }

            return new Response(true, channelCount, operationCount, schemaVersion, null);
        }

        /// <summary>Valida especificação WSDL via WsdlSpecParser.</summary>
        private static Response ValidateWsdl(ContractVersion version)
        {
            var portTypesAndOps = WsdlSpecParser.ExtractOperations(version.SpecContent);
            var portTypeCount = portTypesAndOps.Count;
            var operationCount = portTypesAndOps.Values.Sum(o => o.Count);

            return new Response(true, portTypeCount, operationCount, null, null);
        }

        /// <summary>
        /// Valida especificação Protocol Buffers (.proto) via análise textual.
        /// Conta blocos 'message' (PathCount) e blocos 'service' (EndpointCount).
        /// Detecta versão de sintaxe (proto2/proto3).
        /// </summary>
        private static Response ValidateProtobuf(ContractVersion version)
        {
            var content = version.SpecContent;
            if (string.IsNullOrWhiteSpace(content))
                return new Response(false, 0, 0, null, "Empty Protobuf specification.");

            var messageCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*message\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var serviceCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*service\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var rpcCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*rpc\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            string? syntaxVersion = null;
            var syntaxMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"syntax\s*=\s*""(proto[23])""");
            if (syntaxMatch.Success)
                syntaxVersion = syntaxMatch.Groups[1].Value;

            if (messageCount == 0 && serviceCount == 0)
                return new Response(false, 0, 0, syntaxVersion, "No message or service definitions found in Protobuf spec.");

            return new Response(true, messageCount, serviceCount > 0 ? rpcCount : 0, syntaxVersion, null);
        }

        /// <summary>
        /// Valida especificação GraphQL SDL via análise textual.
        /// Conta definições de tipo (PathCount) e campos de Query/Mutation/Subscription (EndpointCount).
        /// </summary>
        private static Response ValidateGraphQl(ContractVersion version)
        {
            var content = version.SpecContent;
            if (string.IsNullOrWhiteSpace(content))
                return new Response(false, 0, 0, null, "Empty GraphQL specification.");

            var typeCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*type\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var inputCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*input\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var enumCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*enum\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var interfaceCount = System.Text.RegularExpressions.Regex.Matches(
                content, @"^\s*interface\s+\w+", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            var totalDefinitions = typeCount + inputCount + enumCount + interfaceCount;

            // Count fields in Query, Mutation, and Subscription root types
            var rootFieldCount = 0;
            var rootTypePattern = new System.Text.RegularExpressions.Regex(
                @"type\s+(?:Query|Mutation|Subscription)\s*\{([^}]*)\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (System.Text.RegularExpressions.Match match in rootTypePattern.Matches(content))
            {
                var body = match.Groups[1].Value;
                rootFieldCount += System.Text.RegularExpressions.Regex.Matches(
                    body, @"^\s*\w+\s*[\(:]", System.Text.RegularExpressions.RegexOptions.Multiline).Count;
            }

            if (totalDefinitions == 0)
                return new Response(false, 0, 0, null, "No type definitions found in GraphQL SDL.");

            return new Response(true, totalDefinitions, rootFieldCount, null, null);
        }
    }

    /// <summary>Resposta da validação de integridade de versão de contrato.</summary>
    public sealed record Response(
        bool IsValid,
        int PathCount,
        int EndpointCount,
        string? SchemaVersion,
        string? ValidationError);
}

