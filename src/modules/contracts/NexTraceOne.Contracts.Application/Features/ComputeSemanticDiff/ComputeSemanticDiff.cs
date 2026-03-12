using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff;

/// <summary>
/// Feature: ComputeSemanticDiff — computa o diff semântico entre duas versões de contrato OpenAPI.
/// Detecta mudanças breaking, aditivas e non-breaking e sugere a próxima versão semântica.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeSemanticDiff
{
    /// <summary>Query de computação de diff semântico entre versões de contrato.</summary>
    public sealed record Query(Guid BaseVersionId, Guid TargetVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de diff semântico.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BaseVersionId).NotEmpty();
            RuleFor(x => x.TargetVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que computa o diff semântico e persiste o resultado na versão alvo.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseVersion = await repository.GetByIdAsync(ContractVersionId.From(request.BaseVersionId), cancellationToken);
            if (baseVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.BaseVersionId.ToString());

            var targetVersion = await repository.GetByIdAsync(ContractVersionId.From(request.TargetVersionId), cancellationToken);
            if (targetVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.TargetVersionId.ToString());

            var basePaths = ExtractPathsAndMethods(baseVersion.SpecContent);
            var targetPaths = ExtractPathsAndMethods(targetVersion.SpecContent);

            var breaking = new List<ChangeEntry>();
            var additive = new List<ChangeEntry>();
            var nonBreaking = new List<ChangeEntry>();

            // Caminhos removidos (breaking)
            foreach (var path in basePaths.Keys.Except(targetPaths.Keys, StringComparer.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry("PathRemoved", path, null, $"Path '{path}' was removed.", true));
            }

            // Caminhos adicionados (aditivo)
            foreach (var path in targetPaths.Keys.Except(basePaths.Keys, StringComparer.OrdinalIgnoreCase))
            {
                additive.Add(new ChangeEntry("PathAdded", path, null, $"Path '{path}' was added.", false));
            }

            // Caminhos comuns — compara métodos
            foreach (var path in basePaths.Keys.Intersect(targetPaths.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var baseMethods = basePaths[path];
                var targetMethods = targetPaths[path];

                foreach (var method in baseMethods.Except(targetMethods, StringComparer.OrdinalIgnoreCase))
                    breaking.Add(new ChangeEntry("MethodRemoved", path, method, $"Method '{method}' was removed from '{path}'.", true));

                foreach (var method in targetMethods.Except(baseMethods, StringComparer.OrdinalIgnoreCase))
                    additive.Add(new ChangeEntry("MethodAdded", path, method, $"Method '{method}' was added to '{path}'.", false));

                // Compara parâmetros nos métodos comuns
                foreach (var method in baseMethods.Intersect(targetMethods, StringComparer.OrdinalIgnoreCase))
                {
                    ComputeParameterDiff(
                        baseVersion.SpecContent,
                        targetVersion.SpecContent,
                        path, method,
                        breaking, additive, nonBreaking);
                }
            }

            var changeLevel = breaking.Count > 0
                ? ChangeLevel.Breaking
                : additive.Count > 0
                    ? ChangeLevel.Additive
                    : ChangeLevel.NonBreaking;

            var baseSemVer = SemanticVersion.Parse(baseVersion.SemVer);
            var suggestedSemVer = baseSemVer is null
                ? baseVersion.SemVer
                : changeLevel switch
                {
                    ChangeLevel.Breaking => baseSemVer.BumpMajor().ToString(),
                    ChangeLevel.Additive => baseSemVer.BumpMinor().ToString(),
                    _ => baseSemVer.BumpPatch().ToString()
                };

            var diff = ContractDiff.Create(
                targetVersion.Id,
                baseVersion.Id,
                targetVersion.Id,
                targetVersion.ApiAssetId,
                changeLevel,
                breaking.AsReadOnly(),
                nonBreaking.AsReadOnly(),
                additive.AsReadOnly(),
                suggestedSemVer,
                dateTimeProvider.UtcNow);

            targetVersion.AddDiff(diff);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                diff.Id.Value,
                request.BaseVersionId,
                request.TargetVersionId,
                changeLevel,
                suggestedSemVer,
                breaking.AsReadOnly(),
                nonBreaking.AsReadOnly(),
                additive.AsReadOnly());
        }

        private static Dictionary<string, HashSet<string>> ExtractPathsAndMethods(string specContent)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var doc = JsonDocument.Parse(specContent);
                if (doc.RootElement.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var method in path.Value.EnumerateObject())
                        {
                            var m = method.Name.ToUpperInvariant();
                            if (m is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "HEAD" or "OPTIONS")
                                methods.Add(m);
                        }
                        result[path.Name] = methods;
                    }
                }
            }
            catch { /* Spec inválida — retorna vazio */ }
            return result;
        }

        private static void ComputeParameterDiff(
            string baseSpec,
            string targetSpec,
            string path,
            string method,
            List<ChangeEntry> breaking,
            List<ChangeEntry> additive,
            List<ChangeEntry> nonBreaking)
        {
            try
            {
                var baseParams = ExtractParameters(baseSpec, path, method);
                var targetParams = ExtractParameters(targetSpec, path, method);

                foreach (var (name, _) in baseParams.Where(p => !targetParams.ContainsKey(p.Key)))
                    breaking.Add(new ChangeEntry("ParameterRemoved", path, method, $"Parameter '{name}' was removed from '{method} {path}'.", true));

                foreach (var (name, required) in targetParams.Where(p => !baseParams.ContainsKey(p.Key)))
                {
                    if (required)
                        breaking.Add(new ChangeEntry("ParameterRequired", path, method, $"Required parameter '{name}' was added to '{method} {path}'.", true));
                    else
                        additive.Add(new ChangeEntry("ParameterAdded", path, method, $"Optional parameter '{name}' was added to '{method} {path}'.", false));
                }
            }
            catch { /* Ignora erros de parse de parâmetros */ }
        }

        private static Dictionary<string, bool> ExtractParameters(string specContent, string path, string method)
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var doc = JsonDocument.Parse(specContent);
                if (doc.RootElement.TryGetProperty("paths", out var paths)
                    && paths.TryGetProperty(path, out var pathEl)
                    && pathEl.TryGetProperty(method.ToLowerInvariant(), out var methodEl)
                    && methodEl.TryGetProperty("parameters", out var parameters)
                    && parameters.ValueKind == JsonValueKind.Array)
                {
                    foreach (var param in parameters.EnumerateArray())
                    {
                        var name = param.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                        var required = param.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
                        if (!string.IsNullOrEmpty(name))
                            result[name] = required;
                    }
                }
            }
            catch { /* Ignora erros de parse */ }
            return result;
        }
    }

    /// <summary>Resposta do diff semântico entre versões de contrato.</summary>
    public sealed record Response(
        Guid DiffId,
        Guid BaseVersionId,
        Guid TargetVersionId,
        ChangeLevel ChangeLevel,
        string SuggestedSemVer,
        IReadOnlyList<ChangeEntry> BreakingChanges,
        IReadOnlyList<ChangeEntry> NonBreakingChanges,
        IReadOnlyList<ChangeEntry> AdditiveChanges);
}

