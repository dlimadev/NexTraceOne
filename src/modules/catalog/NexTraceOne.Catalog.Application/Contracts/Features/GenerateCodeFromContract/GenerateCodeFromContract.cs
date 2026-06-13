using System.Text.RegularExpressions;

using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Generation;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateCodeFromContract;

/// <summary>
/// Feature: GenerateCodeFromContract — gerador determinístico de código a partir de um
/// contrato OpenAPI (YAML ou JSON), no padrão de arquitetura pré-estabelecido (Clean
/// Architecture .NET). Equivalente ao fluxo "openapi-generator": contrato → DTOs +
/// endpoints, sem depender de IA.
///
/// Fluxo:
///   1. Faz parse do contrato OpenAPI para o modelo neutro (IOpenApiContractParser).
///   2. Gera os ficheiros .NET (DotNetCleanArchitectureCodeGenerator).
///   3. Devolve a lista de ficheiros prontos a escrever (IDE/CLI/portal).
///
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// Wave AQ.4 — Contract-first deterministic code generation (Developer Acceleration).
/// </summary>
public static class GenerateCodeFromContract
{
    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>Gera código .NET a partir do conteúdo de um contrato OpenAPI.</summary>
    public sealed record Query(
        string SpecContent,
        string ServiceName,
        string? RootNamespace = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly Regex ServiceNamePattern =
            new(@"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$", RegexOptions.Compiled);

        public Validator()
        {
            RuleFor(x => x.SpecContent).NotEmpty().MaximumLength(2_000_000);
            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(64)
                .Must(n => ServiceNamePattern.IsMatch(n))
                .WithMessage("ServiceName must be lowercase kebab-case (e.g. 'payment-api').");
            RuleFor(x => x.RootNamespace).MaximumLength(200).When(x => x.RootNamespace is not null);
        }
    }

    // ── Response ──────────────────────────────────────────────────────────

    /// <summary>Resultado da geração: ficheiros e um resumo do que foi produzido.</summary>
    public sealed record Response(
        string ServiceName,
        string Title,
        int SchemaCount,
        int OperationCount,
        IReadOnlyList<GeneratedCodeFile> Files);

    // ── Handler ────────────────────────────────────────────────────────────

    internal sealed class Handler(IOpenApiContractParser parser) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.SpecContent);
            Guard.Against.NullOrWhiteSpace(request.ServiceName);

            var parseResult = parser.Parse(request.SpecContent);
            if (parseResult.IsFailure)
                return Task.FromResult<Result<Response>>(parseResult.Error);

            var model = parseResult.Value;
            var files = DotNetCleanArchitectureCodeGenerator.Generate(
                model, new CodeGenerationOptions(request.ServiceName, request.RootNamespace));

            return Task.FromResult(Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Title: model.Title,
                SchemaCount: model.Schemas.Count,
                OperationCount: model.Operations.Count,
                Files: files)));
        }
    }
}
