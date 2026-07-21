using System.Text.Json;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.PromoteSchemaToCanonicalEntity;

/// <summary>
/// Feature: PromoteSchemaToCanonicalEntity — promove um schema nomeado de uma versão de contrato
/// a entidade canónica reutilizável. Extrai o schema do <c>SpecContent</c> do contrato-fonte
/// (<c>components.schemas.&lt;nome&gt;</c> em OpenAPI ou <c>definitions.&lt;nome&gt;</c> em JSON Schema)
/// e cria uma <see cref="CanonicalEntity"/> em estado Draft com esse conteúdo.
/// </summary>
public static class PromoteSchemaToCanonicalEntity
{
    // ── Command ────────────────────────────────────────────────────────────
    /// <summary>Corpo HTTP do POST (o Owner vem do utilizador autenticado).</summary>
    public sealed record PromoteBody(
        Guid SourceContractVersionId,
        string SchemaName,
        string Name,
        string Domain,
        string Category);

    /// <summary>Comando de promoção de schema a entidade canónica.</summary>
    public sealed record Command(
        Guid SourceContractVersionId,
        string SchemaName,
        string Name,
        string Domain,
        string Category,
        string Owner) : ICommand<Response>;

    /// <summary>Validador do comando <see cref="Command"/>.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SourceContractVersionId).NotEmpty();
            RuleFor(x => x.SchemaName).NotEmpty().MaximumLength(400);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(400);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Owner).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Resposta com o identificador da entidade canónica criada.</summary>
    public sealed record Response(string Id);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler do comando <see cref="Command"/>.</summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        ICanonicalEntityRepository canonicalEntityRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var contract = await contractVersionRepository.GetByIdAsync(
                new ContractVersionId(request.SourceContractVersionId), cancellationToken);
            if (contract is null)
                return Error.NotFound("ContractVersion.NotFound",
                    $"Source contract version {request.SourceContractVersionId} not found.");

            var schemaContent = ExtractSchema(contract.SpecContent, request.SchemaName);
            if (schemaContent is null)
                return Error.Business("CanonicalEntity.SchemaNotFound",
                    $"Schema '{request.SchemaName}' was not found in the source contract spec.");

            var entity = CanonicalEntity.Create(
                request.Name,
                description: $"Promoted from contract version {request.SourceContractVersionId} (schema '{request.SchemaName}').",
                request.Domain,
                request.Category,
                request.Owner,
                schemaContent);

            canonicalEntityRepository.Add(entity);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(entity.Id.Value.ToString()));
        }

        /// <summary>
        /// Extrai o schema nomeado do spec bruto. Suporta OpenAPI (<c>components.schemas</c>)
        /// e JSON Schema (<c>definitions</c>). Devolve <c>null</c> se não existir ou o spec não
        /// for JSON válido.
        /// </summary>
        private static string? ExtractSchema(string specContent, string schemaName)
        {
            if (string.IsNullOrWhiteSpace(specContent))
                return null;
            try
            {
                using var doc = JsonDocument.Parse(specContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("components", out var components)
                    && components.TryGetProperty("schemas", out var schemas)
                    && schemas.TryGetProperty(schemaName, out var openApiSchema))
                    return openApiSchema.GetRawText();

                if (root.TryGetProperty("definitions", out var definitions)
                    && definitions.TryGetProperty(schemaName, out var jsonSchema))
                    return jsonSchema.GetRawText();

                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
