using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetEvidencePackage;

/// <summary>
/// Feature: GetEvidencePackage — detalhe de um pacote de evidência.
/// </summary>
public static class GetEvidencePackage
{
    /// <summary>Query para obter detalhe de um pacote de evidência.</summary>
    public sealed record Query(string PackageId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe de um pacote de evidência.</summary>
    public sealed class Handler(
        IEvidencePackageRepository evidencePackageRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackageId, out var packageGuid))
                return Error.Validation("INVALID_EVIDENCE_PACKAGE_ID", "Package ID '{0}' is not a valid GUID.", request.PackageId);

            var package = await evidencePackageRepository.GetByIdAsync(new EvidencePackageId(packageGuid), cancellationToken);
            if (package is null)
                return Error.NotFound("EVIDENCE_PACKAGE_NOT_FOUND", "Evidence package '{0}' not found.", request.PackageId);

            var items = package.Items
                .OrderByDescending(i => i.RecordedAt)
                .Select(i => new EvidenceItemDto(
                    ItemId: i.Id.Value.ToString(),
                    Type: i.Type,
                    Title: i.Title,
                    Description: i.Description,
                    SourceModule: i.SourceModule,
                    ReferenceId: i.ReferenceId,
                    RecordedBy: i.RecordedBy,
                    RecordedAt: i.RecordedAt))
                .ToList();

            var detail = new EvidencePackageDetailDto(
                PackageId: package.Id.Value.ToString(),
                Name: package.Name,
                Description: package.Description,
                Scope: package.Scope,
                Status: package.Status,
                CreatedBy: package.CreatedBy,
                CreatedAt: package.CreatedAt,
                SealedAt: package.SealedAt,
                Items: items);

            return Result<Response>.Success(new Response(detail, IsSimulated: false));
        }
    }

    /// <summary>Resposta com detalhe do pacote de evidência.</summary>
    public sealed record Response(EvidencePackageDetailDto Package, bool IsSimulated = false);

    /// <summary>DTO de detalhe de pacote de evidência.</summary>
    public sealed record EvidencePackageDetailDto(
        string PackageId,
        string Name,
        string Description,
        string Scope,
        EvidencePackageStatus Status,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? SealedAt,
        IReadOnlyList<EvidenceItemDto> Items);

    /// <summary>DTO de item de evidência.</summary>
    public sealed record EvidenceItemDto(
        string ItemId,
        EvidenceType Type,
        string Title,
        string Description,
        string SourceModule,
        string ReferenceId,
        string RecordedBy,
        DateTimeOffset RecordedAt);
}
