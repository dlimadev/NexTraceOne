using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ParseSpecPreview;

/// <summary>
/// Feature: ParseSpecPreview — parseia conteúdo de especificação ad-hoc (sem contrato persistido)
/// e retorna o modelo canónico normalizado para alimentar o live preview do Contract Studio Editor.
/// Reutiliza CanonicalModelBuilder existente para suporte multi-protocolo.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ParseSpecPreview
{
    /// <summary>Query com o conteúdo da especificação e protocolo para parsing.</summary>
    public sealed record Query(
        string SpecContent,
        string Protocol) : IQuery<Response>;

    /// <summary>Valida a entrada da query de preview.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SpecContent).NotEmpty();
            RuleFor(x => x.Protocol).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que constrói o modelo canónico a partir do conteúdo raw.
    /// Não persiste nada — operação puramente de leitura para alimentar o preview.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<ContractProtocol>(request.Protocol, ignoreCase: true, out var protocol))
            {
                return Task.FromResult(Result<Response>.Success(new Response(
                    false,
                    "Unsupported protocol",
                    null)));
            }

            try
            {
                var canonical = CanonicalModelBuilder.Build(request.SpecContent, protocol);

                // Detect silent parse failures — CanonicalModelBuilder returns EmptyModel
                // with title "Unknown" and zero operations/schemas when parsing fails.
                if (canonical.Title == "Unknown" && canonical.OperationCount == 0 && canonical.SchemaCount == 0)
                {
                    return Task.FromResult(Result<Response>.Success(new Response(
                        false,
                        "Could not parse specification. Verify the content is valid for the selected protocol.",
                        null)));
                }

                var model = MapToPreviewModel(canonical);
                return Task.FromResult(Result<Response>.Success(new Response(true, null, model)));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<Response>.Success(new Response(
                    false,
                    ex.Message,
                    null)));
            }
        }

        private static PreviewModel MapToPreviewModel(ContractCanonicalModel canonical)
        {
            var operations = canonical.Operations
                .Select(o => new PreviewOperation(
                    o.OperationId,
                    o.Name,
                    o.Description,
                    o.Method,
                    o.Path,
                    o.IsDeprecated,
                    o.Tags?.ToList() ?? [],
                    o.InputParameters.Select(MapSchemaElement).ToList(),
                    o.OutputFields.Select(MapSchemaElement).ToList(),
                    o.RequestBody is not null
                        ? new PreviewRequestBody(
                            o.RequestBody.ContentType,
                            o.RequestBody.IsRequired,
                            o.RequestBody.Properties.Select(MapSchemaElement).ToList(),
                            o.RequestBody.SchemaRef)
                        : null,
                    o.Responses?.Select(r => new PreviewResponse(
                        r.StatusCode,
                        r.Description,
                        r.ContentType,
                        r.Properties.Select(MapSchemaElement).ToList(),
                        r.SchemaRef)).ToList()))
                .ToList();

            var schemas = canonical.GlobalSchemas
                .Select(MapSchemaElement)
                .ToList();

            return new PreviewModel(
                canonical.Protocol.ToString(),
                canonical.Title,
                canonical.SpecVersion,
                canonical.Description,
                canonical.Servers.ToList(),
                canonical.Tags.ToList(),
                canonical.SecuritySchemes.ToList(),
                operations,
                schemas,
                canonical.OperationCount,
                canonical.SchemaCount,
                canonical.HasSecurityDefinitions,
                canonical.HasExamples,
                canonical.HasDescriptions);
        }

        private static PreviewSchemaElement MapSchemaElement(ContractSchemaElement e)
        {
            var children = e.Children?
                .Select(MapSchemaElement)
                .ToList();

            return new PreviewSchemaElement(
                e.Name,
                e.DataType,
                e.IsRequired,
                e.Description,
                e.Format,
                e.DefaultValue,
                e.IsDeprecated,
                children);
        }
    }

    /// <summary>Resposta com o resultado do parsing para preview.</summary>
    public sealed record Response(
        bool IsValid,
        string? ErrorMessage,
        PreviewModel? Preview);

    /// <summary>Modelo de preview normalizado para consumo pelo frontend.</summary>
    public sealed record PreviewModel(
        string Protocol,
        string Title,
        string SpecVersion,
        string? Description,
        List<string> Servers,
        List<string> Tags,
        List<string> SecuritySchemes,
        List<PreviewOperation> Operations,
        List<PreviewSchemaElement> Schemas,
        int OperationCount,
        int SchemaCount,
        bool HasSecurityDefinitions,
        bool HasExamples,
        bool HasDescriptions);

    /// <summary>Operação normalizada para preview.</summary>
    public sealed record PreviewOperation(
        string OperationId,
        string Name,
        string? Description,
        string Method,
        string Path,
        bool IsDeprecated,
        List<string> Tags,
        List<PreviewSchemaElement> InputParameters,
        List<PreviewSchemaElement> OutputFields,
        PreviewRequestBody? RequestBody,
        List<PreviewResponse>? Responses);

    /// <summary>Request body normalizado para preview.</summary>
    public sealed record PreviewRequestBody(
        string ContentType,
        bool IsRequired,
        List<PreviewSchemaElement> Properties,
        string? SchemaRef);

    /// <summary>Resposta normalizada para preview.</summary>
    public sealed record PreviewResponse(
        string StatusCode,
        string? Description,
        string? ContentType,
        List<PreviewSchemaElement> Properties,
        string? SchemaRef);

    /// <summary>Elemento de schema para preview.</summary>
    public sealed record PreviewSchemaElement(
        string Name,
        string DataType,
        bool IsRequired,
        string? Description,
        string? Format,
        string? DefaultValue,
        bool IsDeprecated,
        List<PreviewSchemaElement>? Children);
}
