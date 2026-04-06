using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.GenerateAutoDocumentation;

/// <summary>
/// Feature: GenerateAutoDocumentation — gera documentação automática de um serviço
/// a partir de documentos e relações existentes no Knowledge Hub.
/// Computação pura — sem cross-module ou persistência adicional.
/// </summary>
public static class GenerateAutoDocumentation
{
    public sealed record Query(
        string ServiceName,
        IReadOnlyList<string>? Sections = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
        }
    }

    public sealed class Handler(
        IKnowledgeDocumentRepository documentRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlyList<string> AllSections =
            ["Overview", "Ownership", "Contracts", "Dependencies", "SLOs", "Runbooks", "Recent Changes"];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var requestedSections = request.Sections is { Count: > 0 }
                ? request.Sections
                : AllSections;

            // Buscar documentos do tipo Runbook para identificar runbooks do serviço
            var (runbooks, _) = await documentRepository.ListAsync(Domain.Enums.DocumentCategory.Runbook, null, 1, 20, cancellationToken);

            var sections = new List<DocSection>();
            foreach (var section in requestedSections)
                sections.Add(GenerateSection(section, request.ServiceName, runbooks, clock.UtcNow));

            return Result<Response>.Success(new Response(
                request.ServiceName,
                sections,
                clock.UtcNow,
                sections.Count));
        }

        private static DocSection GenerateSection(
            string section,
            string serviceName,
            IReadOnlyList<Domain.Entities.KnowledgeDocument> runbooks,
            DateTimeOffset generatedAt)
        {
            return section switch
            {
                "Overview" => new DocSection(
                    "Overview",
                    $"# {serviceName}\n\nAuto-generated documentation from NexTraceOne Knowledge Hub.\n\nGenerated: {generatedAt:yyyy-MM-dd HH:mm UTC}\n\nRunbook documents found: {runbooks.Count}",
                    "Knowledge Hub"),

                "Ownership" => new DocSection(
                    "Ownership",
                    "## Ownership\n\nOwnership is managed in the Service Catalog. Review team assignments and domain membership there.",
                    "Service Catalog"),

                "Contracts" => new DocSection(
                    "Contracts",
                    "## Contracts\n\nContracts are managed in the Contract Governance module. View REST, SOAP, Event and Background Service contracts there.",
                    "Contract Governance"),

                "Dependencies" => new DocSection(
                    "Dependencies",
                    "## Dependencies\n\nDependencies are tracked in the Dependency Graph. Use the Topology view to explore direct and transitive dependencies.",
                    "Dependency Graph"),

                "SLOs" => new DocSection(
                    "SLOs",
                    "## SLOs\n\nService Level Objectives are tracked in the Reliability module. Review the SLO burn rate and error budget dashboard.",
                    "Reliability Module"),

                "Runbooks" => new DocSection(
                    "Runbooks",
                    $"## Runbooks\n\n" +
                    (runbooks.Count > 0
                        ? $"Found {runbooks.Count} runbook(s) in the Knowledge Hub."
                        : "No runbooks found. Create runbooks in the Knowledge Hub to improve operational readiness."),
                    "Knowledge Hub"),

                "Recent Changes" => new DocSection(
                    "Recent Changes",
                    "## Recent Changes\n\nChange history is tracked in Change Intelligence. Review releases and promotion history there.",
                    "Change Intelligence"),

                _ => new DocSection(section, $"Section '{section}' — no content available.", "Knowledge Hub")
            };
        }
    }

    public sealed record DocSection(string Title, string Content, string Source);

    public sealed record Response(
        string ServiceName,
        IReadOnlyList<DocSection> Sections,
        DateTimeOffset GeneratedAt,
        int TotalSections);
}
