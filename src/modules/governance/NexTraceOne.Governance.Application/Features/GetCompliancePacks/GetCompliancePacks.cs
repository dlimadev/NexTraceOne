using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetCompliancePacks;

/// <summary>
/// Feature: GetCompliancePacks — pacotes de conformidade disponíveis na plataforma.
/// Retorna pacotes SOC2TypeII e GDPR com lista de controles realistas.
/// Usa IConfigurationResolutionService para chave "governance.compliance.framework".
/// </summary>
public static class GetCompliancePacks
{
    /// <summary>Query sem parâmetros — retorna pacotes de conformidade disponíveis.</summary>
    public sealed record Query() : IQuery<CompliancePacksResponse>;

    /// <summary>Handler de leitura dos pacotes de conformidade.</summary>
    public sealed class Handler(IConfigurationResolutionService configService) : IQueryHandler<Query, CompliancePacksResponse>
    {
        public async Task<Result<CompliancePacksResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var frameworkDto = await configService.ResolveEffectiveValueAsync(
                "governance.compliance.framework", ConfigurationScope.System, null, cancellationToken);

            var activeFramework = frameworkDto?.EffectiveValue ?? "SOC2TypeII";

            var packs = new List<CompliancePackDto>
            {
                new(
                    PackId: "SOC2TypeII",
                    Name: "SOC 2 Type II",
                    Description: "Security, availability, and confidentiality controls for service organizations.",
                    Version: "2023.1",
                    Active: activeFramework.Contains("SOC2", StringComparison.OrdinalIgnoreCase),
                    Controls:
                    [
                        new("CC1.1", "Control Environment", "Demonstrates commitment to integrity and ethical values.", "Automated"),
                        new("CC2.1", "Communication & Information", "Management uses relevant and quality information.", "Manual"),
                        new("CC6.1", "Logical Access", "Access controls restrict unauthorized access.", "Automated"),
                        new("CC7.2", "System Operations", "Monitors system capacity and performance.", "Automated"),
                        new("CC8.1", "Change Management", "Changes to infrastructure and software are authorized.", "Automated"),
                        new("A1.1", "Availability", "System availability meets SLO commitments.", "Automated")
                    ]),
                new(
                    PackId: "GDPR",
                    Name: "GDPR",
                    Description: "General Data Protection Regulation compliance controls.",
                    Version: "2018.1",
                    Active: activeFramework.Contains("GDPR", StringComparison.OrdinalIgnoreCase),
                    Controls:
                    [
                        new("Art5", "Principles relating to processing", "Personal data processed lawfully, fairly and transparently.", "Manual"),
                        new("Art17", "Right to erasure", "Data subjects can request deletion of personal data.", "Automated"),
                        new("Art25", "Data protection by design", "Data protection integrated into processing activities.", "Automated"),
                        new("Art32", "Security of processing", "Appropriate technical and organisational measures.", "Automated"),
                        new("Art33", "Breach notification", "Supervisory authority notified of breaches within 72 hours.", "Manual")
                    ])
            };

            var response = new CompliancePacksResponse(
                Packs: packs,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<CompliancePacksResponse>.Success(response);
        }
    }

    /// <summary>Resposta com pacotes de conformidade disponíveis.</summary>
    public sealed record CompliancePacksResponse(
        IReadOnlyList<CompliancePackDto> Packs,
        DateTimeOffset GeneratedAt);

    /// <summary>Pacote de conformidade com lista de controles.</summary>
    public sealed record CompliancePackDto(
        string PackId,
        string Name,
        string Description,
        string Version,
        bool Active,
        IReadOnlyList<ComplianceControlDto> Controls);

    /// <summary>Controle individual dentro de um pacote de conformidade.</summary>
    public sealed record ComplianceControlDto(
        string ControlId,
        string Name,
        string Description,
        string EvidenceType);
}
