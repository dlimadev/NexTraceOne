using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GenerateSbom;

/// <summary>
/// Feature: GenerateSbom — gera Software Bill of Materials (SBOM) para um projeto ou artefato.
/// Produz documento SPDX 2.3 com lista de dependências, licenças e metadados de compliance.
/// Requisito fundamental para auditoria de segurança e conformidade (NTIA, Executive Order 14028).
/// </summary>
public static class GenerateSbom
{
    /// <summary>Comando para gerar SBOM de um projeto.</summary>
    public sealed record Command(
        string ProjectPath) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de geração de SBOM.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectPath).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Resposta com o documento SBOM em formato JSON.</summary>
    public sealed record Response(
        string SbomJson,
        int DependencyCount,
        DateTime GeneratedAt);

    /// <summary>Handler que executa a geração de SBOM via ISbomGenerator.</summary>
    public sealed class Handler(
        ISbomGenerator sbomGenerator,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var sbom = await sbomGenerator.GenerateSbomAsync(request.ProjectPath);
                
                await sbomGenerator.ValidateSbomComplianceAsync(sbom);
                
                var sbomJson = await sbomGenerator.ExportSbomToJsonAsync(sbom);

                return new Response(
                    SbomJson: sbomJson,
                    DependencyCount: sbom.Dependencies.Count,
                    GeneratedAt: clock.UtcNow.UtcDateTime);
            }
            catch (Exception ex)
            {
                return Error.Business("sbom.generation_failed", $"Falha ao gerar SBOM: {ex.Message}");
            }
        }
    }
}
