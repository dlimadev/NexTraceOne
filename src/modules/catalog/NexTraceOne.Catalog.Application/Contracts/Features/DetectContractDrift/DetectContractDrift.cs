using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.DetectContractDrift;

/// <summary>
/// Feature: DetectContractDrift — detecta desvios entre o contrato publicado e os traces observados.
/// Compara os endpoints declarados na especificação com as operações realmente observadas nos traces,
/// identificando ghost endpoints (declarados mas nunca observados) e endpoints não declarados
/// (observados mas ausentes no contrato).
/// Estrutura VSA: Query + ObservedTraceOperation + Validator + Handler + Response em arquivo único.
/// </summary>
public static class DetectContractDrift
{
    /// <summary>Operação observada em traces de produção (método HTTP + caminho).</summary>
    public sealed record ObservedTraceOperation(string Method, string Path);

    /// <summary>Query para detecção de drift entre contrato e traces.</summary>
    public sealed record Query(
        Guid ApiAssetId,
        IReadOnlyList<ObservedTraceOperation> ObservedOperations) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detecção de drift.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ObservedOperations).NotNull();
        }
    }

    /// <summary>
    /// Handler que compara operações declaradas no contrato com traces observados.
    /// Utiliza parsing simples de string na especificação para extrair operações HTTP declaradas.
    /// Calcula DriftScore e classifica o status de desvio.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        private static readonly string[] HttpMethods = ["get", "post", "put", "delete", "patch", "head", "options"];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var latest = await repository.GetLatestByApiAssetAsync(request.ApiAssetId, cancellationToken);
            if (latest is null)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            var declared = ParseDeclaredOperations(latest.SpecContent);
            var observed = request.ObservedOperations
                .Select(o => NormalizeOperation(o.Method, o.Path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var declaredNormalized = declared
                .Select(d => NormalizeOperation(d.Method, d.Path))
                .ToList();

            var ghostEndpoints = declared
                .Where(d => !observed.Contains(NormalizeOperation(d.Method, d.Path)))
                .ToList()
                .AsReadOnly();

            var declaredSet = new HashSet<string>(declaredNormalized, StringComparer.OrdinalIgnoreCase);

            var undeclaredEndpoints = request.ObservedOperations
                .Where(o => !declaredSet.Contains(NormalizeOperation(o.Method, o.Path)))
                .ToList()
                .AsReadOnly();

            var driftScore = declared.Count == 0
                ? 0.0
                : Math.Round((double)(declared.Count - ghostEndpoints.Count) / declared.Count, 4);

            var status = driftScore switch
            {
                >= 1.0 => "Clean",
                > 0.8 => "Minor",
                > 0.5 => "Major",
                _ => "Critical"
            };

            return new Response(
                ApiAssetId: request.ApiAssetId,
                SemVer: latest.SemVer,
                DeclaredOperations: declared,
                ObservedOperations: request.ObservedOperations,
                GhostEndpoints: ghostEndpoints,
                UndeclaredEndpoints: undeclaredEndpoints,
                DriftScore: driftScore,
                Status: status);
        }

        /// <summary>
        /// Extrai operações declaradas da especificação usando parsing simples de string.
        /// Suporta verbos HTTP na mesma linha do path (JSON inline) e em linhas separadas.
        /// </summary>
        private static IReadOnlyList<DeclaredOperation> ParseDeclaredOperations(string specContent)
        {
            var results = new List<DeclaredOperation>();
            var lines = specContent.Split('\n');
            string? currentPath = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Detecta definição de path (ex.: "/api/users": ou "/api/users/:id":)
                if (line.StartsWith("\"/", StringComparison.Ordinal) && line.Contains("\":"))
                {
                    var end = line.IndexOf("\":", StringComparison.Ordinal);
                    if (end > 1)
                        currentPath = line.Substring(1, end - 1);
                }

                if (currentPath is null) continue;

                // Para linhas que definem o path, pesquisa verbos APÓS o separador do path.
                // Para outras linhas, pesquisa a partir do início (verbo em linha própria).
                var searchOffset = line.StartsWith("\"/", StringComparison.Ordinal)
                    ? (line.IndexOf("\":", StringComparison.Ordinal) + 2)
                    : 0;

                foreach (var method in HttpMethods)
                {
                    var pattern = $"\"{method}\":";
                    if (line.IndexOf(pattern, searchOffset, StringComparison.OrdinalIgnoreCase) >= 0)
                        results.Add(new DeclaredOperation(method.ToUpperInvariant(), currentPath));
                }
            }

            return results.AsReadOnly();
        }

        private static string NormalizeOperation(string method, string path)
            => $"{method.ToUpperInvariant()} {path.TrimEnd('/')}";
    }

    /// <summary>Operação declarada no contrato (método HTTP + caminho).</summary>
    public sealed record DeclaredOperation(string Method, string Path);

    /// <summary>Resposta da detecção de drift de contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string SemVer,
        IReadOnlyList<DeclaredOperation> DeclaredOperations,
        IReadOnlyList<ObservedTraceOperation> ObservedOperations,
        IReadOnlyList<DeclaredOperation> GhostEndpoints,
        IReadOnlyList<ObservedTraceOperation> UndeclaredEndpoints,
        double DriftScore,
        string Status);
}
