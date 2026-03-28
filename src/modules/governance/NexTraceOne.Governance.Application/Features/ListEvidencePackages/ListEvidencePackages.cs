using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListEvidencePackages;

/// <summary>
/// Feature: ListEvidencePackages — lista pacotes de evidência disponíveis.
/// Pacotes agrupam evidências de aprovações, mudanças, contratos, IA e mitigação.
/// </summary>
public static class ListEvidencePackages
{
    /// <summary>Query para listar pacotes de evidência.</summary>
    public sealed record Query(
        string? Scope = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna pacotes de evidência.</summary>
    public sealed class Handler(
        IEvidencePackageRepository evidencePackageRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            EvidencePackageStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<EvidencePackageStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var packages = await evidencePackageRepository.ListAsync(
                scope: request.Scope,
                status: statusFilter,
                ct: cancellationToken);

            var dtos = packages
                .Select(p => new EvidencePackageDto(
                    PackageId: p.Id.Value.ToString(),
                    Name: p.Name,
                    Description: p.Description,
                    Scope: p.Scope,
                    Status: p.Status,
                    ItemCount: p.Items.Count,
                    IncludedTypes: p.Items.Select(i => i.Type.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    CreatedBy: p.CreatedBy,
                    CreatedAt: p.CreatedAt,
                    SealedAt: p.SealedAt))
                .ToList();

            var response = new Response(
                TotalPackages: dtos.Count,
                SealedCount: dtos.Count(p => p.Status == EvidencePackageStatus.Sealed),
                ExportedCount: dtos.Count(p => p.Status == EvidencePackageStatus.Exported),
                DraftCount: dtos.Count(p => p.Status == EvidencePackageStatus.Draft),
                Packages: dtos,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista de pacotes de evidência.</summary>
    public sealed record Response(
        int TotalPackages,
        int SealedCount,
        int ExportedCount,
        int DraftCount,
        IReadOnlyList<EvidencePackageDto> Packages,
        bool IsSimulated = false);

    /// <summary>DTO de pacote de evidência.</summary>
    public sealed record EvidencePackageDto(
        string PackageId,
        string Name,
        string Description,
        string Scope,
        EvidencePackageStatus Status,
        int ItemCount,
        string[] IncludedTypes,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? SealedAt);
}
