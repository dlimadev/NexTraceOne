using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.VerifyProviderCompatibility;

/// <summary>
/// Feature: VerifyProviderCompatibility — verifica se a versão publicada de um contrato
/// satisfaz todas as expectativas registadas pelos consumidores (Consumer-Driven Contract Testing).
/// Para cada consumidor, verifica se os endpoints e campos esperados existem no spec publicado.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class VerifyProviderCompatibility
{
    /// <summary>Query de verificação de compatibilidade provider/consumer.</summary>
    public sealed record Query(Guid ApiAssetId, Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que verifica se o spec da versão publicada satisfaz cada expectativa de consumidor.
    /// Tenta parsear o ExpectedSubsetJson para extrair paths/operações esperados
    /// e verificar se existem no spec canónico.
    /// </summary>
    public sealed class Handler(
        IConsumerExpectationRepository expectationRepository,
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await contractVersionRepository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var expectations = await expectationRepository.ListByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            var activeExpectations = expectations.Where(e => e.IsActive).ToList();

            if (activeExpectations.Count == 0)
            {
                return new Response(
                    request.ApiAssetId,
                    request.ContractVersionId,
                    version.SemVer,
                    true,
                    []);
            }

            // Constrói o modelo canónico do spec publicado
            var canonical = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol);
            var publishedPaths = canonical.Operations
                .Select(o => $"{o.Method.ToUpperInvariant()} {o.Path}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var results = new List<ConsumerVerificationResult>();

            foreach (var expectation in activeExpectations)
            {
                var violations = VerifyExpectation(expectation.ExpectedSubsetJson, publishedPaths, canonical);
                results.Add(new ConsumerVerificationResult(
                    expectation.ConsumerServiceName,
                    violations.Count == 0,
                    violations.AsReadOnly()));
            }

            var allSatisfied = results.All(r => r.Satisfied);
            return new Response(
                request.ApiAssetId,
                request.ContractVersionId,
                version.SemVer,
                allSatisfied,
                results.AsReadOnly());
        }

        private static List<string> VerifyExpectation(
            string expectedSubsetJson,
            HashSet<string> publishedPaths,
            NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractCanonicalModel canonical)
        {
            var violations = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(expectedSubsetJson);
                var root = doc.RootElement;

                // Verifica paths esperados
                if (root.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateArray())
                    {
                        var expected = path.GetString();
                        if (expected is null) continue;
                        if (!publishedPaths.Any(p => p.Contains(expected, StringComparison.OrdinalIgnoreCase)))
                            violations.Add($"Expected path '{expected}' not found in published spec.");
                    }
                }

                // Verifica operationIds esperados
                if (root.TryGetProperty("operationIds", out var opIds))
                {
                    var canonicalOpIds = canonical.Operations
                        .Select(o => o.OperationId)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var opId in opIds.EnumerateArray())
                    {
                        var id = opId.GetString();
                        if (id is null) continue;
                        if (!canonicalOpIds.Contains(id))
                            violations.Add($"Expected operationId '{id}' not found in published spec.");
                    }
                }

                // Verifica campos de resposta esperados
                if (root.TryGetProperty("responseFields", out var fields))
                {
                    var allFields = canonical.Operations
                        .SelectMany(o => o.OutputFields)
                        .Select(f => f.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var field in fields.EnumerateArray())
                    {
                        var fieldName = field.GetString();
                        if (fieldName is null) continue;
                        if (!allFields.Contains(fieldName))
                            violations.Add($"Expected response field '{fieldName}' not found in any operation output.");
                    }
                }
            }
            catch
            {
                violations.Add("Could not parse ExpectedSubsetJson — manual verification required.");
            }

            return violations;
        }
    }

    /// <summary>Resultado de verificação de compatibilidade para um consumidor.</summary>
    public sealed record ConsumerVerificationResult(
        string ConsumerServiceName,
        bool Satisfied,
        IReadOnlyList<string> Violations);

    /// <summary>Resposta da verificação de compatibilidade provider/consumer.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        Guid ContractVersionId,
        string SemVer,
        bool AllSatisfied,
        IReadOnlyList<ConsumerVerificationResult> Results);
}
