using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.ListPackVersions;

/// <summary>
/// Feature: ListPackVersions — histórico de versões de um governance pack.
/// Retorna versões publicadas com regras, enforcement mode e metadados de criação.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class ListPackVersions
{
    /// <summary>Query para listar versões de um governance pack pelo seu identificador.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna o histórico de versões do governance pack.</summary>
    /// <summary>Valida os parâmetros da query de listagem de versões de pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var versions = new List<PackVersionDto>
            {
                new("ver-003", "2.1.0", 18, EnforcementMode.Blocking,
                    "Added contract changelog enforcement rule and tightened schema validation",
                    "architect@nextraceone.io",
                    DateTimeOffset.UtcNow.AddDays(-10),
                    DateTimeOffset.UtcNow.AddDays(-9)),
                new("ver-002", "2.0.0", 15, EnforcementMode.Required,
                    "Major revision: introduced blocking mode for production environments",
                    "techlead@nextraceone.io",
                    DateTimeOffset.UtcNow.AddDays(-45),
                    DateTimeOffset.UtcNow.AddDays(-44)),
                new("ver-001", "1.0.0", 10, EnforcementMode.Advisory,
                    "Initial release with baseline contract governance rules",
                    "admin@nextraceone.io",
                    DateTimeOffset.UtcNow.AddDays(-120),
                    DateTimeOffset.UtcNow.AddDays(-119))
            };

            var response = new Response(Versions: versions);
            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de versões do governance pack.</summary>
    public sealed record Response(IReadOnlyList<PackVersionDto> Versions);

    /// <summary>DTO de uma versão de governance pack.</summary>
    public sealed record PackVersionDto(
        string VersionId,
        string Version,
        int RuleCount,
        EnforcementMode DefaultEnforcementMode,
        string ChangeDescription,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? PublishedAt);
}
