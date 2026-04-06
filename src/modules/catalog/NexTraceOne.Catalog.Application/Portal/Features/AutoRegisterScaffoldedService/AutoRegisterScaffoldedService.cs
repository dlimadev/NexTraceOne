using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

using RegisterFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset.RegisterServiceAsset;

namespace NexTraceOne.Catalog.Application.Portal.Features.AutoRegisterScaffoldedService;

/// <summary>
/// Feature: AutoRegisterScaffoldedService — registo automático no Service Catalog após scaffold.
///
/// Orquestra o fluxo pós-geração de código:
/// 1. Chama <see cref="RegisterFeature"/> para registar o serviço no catálogo
/// 2. Retorna o serviceAssetId criado para o caller poder encadear operações subsequentes
///    (ex: CreateContractVersion, ScanServiceDependencies, auditoria)
///
/// Utilizado no final do wizard de scaffold após o developer aceitar o código gerado.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class AutoRegisterScaffoldedService
{
    /// <summary>Comando para registo automático do serviço scaffoldado no catálogo.</summary>
    public sealed record Command(
        string ServiceName,
        string Domain,
        string TeamName,
        string? Description = null,
        string? ServiceType = null,
        string? Language = null,
        string? RepositoryUrl = null,
        string? DocumentationUrl = null,
        string? TechnicalOwner = null,
        string? BusinessOwner = null,
        string? ScaffoldId = null,
        string? TemplateSlug = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.RepositoryUrl).MaximumLength(500).When(x => x.RepositoryUrl is not null);
            RuleFor(x => x.DocumentationUrl).MaximumLength(500).When(x => x.DocumentationUrl is not null);
            RuleFor(x => x.TechnicalOwner).MaximumLength(200).When(x => x.TechnicalOwner is not null);
            RuleFor(x => x.BusinessOwner).MaximumLength(200).When(x => x.BusinessOwner is not null);
            RuleFor(x => x.TemplateSlug).MaximumLength(200).When(x => x.TemplateSlug is not null);
        }
    }

    /// <summary>
    /// Handler que regista automaticamente um serviço scaffoldado no catálogo.
    /// Delega a criação real ao handler de <see cref="RegisterFeature"/>.
    /// </summary>
    public sealed class Handler(ISender sender) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Constrói a descrição enriquecida com contexto de scaffold quando disponível
            var description = BuildDescription(request);

            var registerCommand = new RegisterFeature.Command(
                Name: request.ServiceName,
                Domain: request.Domain,
                TeamName: request.TeamName,
                Description: description,
                ServiceType: request.ServiceType,
                TechnicalOwner: request.TechnicalOwner,
                BusinessOwner: request.BusinessOwner,
                DocumentationUrl: request.DocumentationUrl,
                RepositoryUrl: request.RepositoryUrl);

            var registerResult = await sender.Send(registerCommand, cancellationToken);
            if (!registerResult.IsSuccess)
                return registerResult.Error;

            return Result<Response>.Success(new Response(
                ServiceAssetId: registerResult.Value.ServiceAssetId,
                ServiceName: registerResult.Value.Name,
                Domain: request.Domain,
                TeamName: request.TeamName,
                Language: request.Language,
                TemplateSlug: request.TemplateSlug,
                ScaffoldId: request.ScaffoldId,
                RegisteredAt: DateTimeOffset.UtcNow));
        }

        private static string? BuildDescription(Command request)
        {
            if (request.Description is not null)
                return request.Description;

            var parts = new List<string>();

            if (request.TemplateSlug is not null)
                parts.Add($"Generated from template '{request.TemplateSlug}'.");

            if (request.Language is not null)
                parts.Add($"Language: {request.Language}.");

            if (request.ScaffoldId is not null)
                parts.Add($"Scaffold reference: {request.ScaffoldId}.");

            return parts.Count > 0 ? string.Join(" ", parts) : null;
        }
    }

    /// <summary>Resultado do registo automático.</summary>
    public sealed record Response(
        Guid ServiceAssetId,
        string ServiceName,
        string Domain,
        string TeamName,
        string? Language,
        string? TemplateSlug,
        string? ScaffoldId,
        DateTimeOffset RegisteredAt);
}
