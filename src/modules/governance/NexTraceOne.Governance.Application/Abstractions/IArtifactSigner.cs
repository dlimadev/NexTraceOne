using NexTraceOne.Governance.Application.Features.SignArtifact;
using NexTraceOne.Governance.Application.Features.VerifyArtifact;
using NexTraceOne.Governance.Application.Features.GenerateSbom;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Interface para assinatura digital de artefatos usando Cosign.
/// Implementa signing, verification e revogação de assinaturas com integração ao transparency log (Rekor).
/// Parte do módulo de governança e compliance de segurança.
/// </summary>
public interface IArtifactSigner
{
    /// <summary>Assina um artefato digitalmente e retorna metadados da assinatura.</summary>
    Task<SignedArtifactResult> SignArtifactAsync(SigningRequest request);

    /// <summary>Verifica a validade da assinatura de um artefato.</summary>
    Task<VerificationResult> VerifyArtifactAsync(string artifactId);

    /// <summary>Revoga a assinatura de um artefato (via Rekor).</summary>
    Task<bool> RevokeSignatureAsync(string artifactId);
}

/// <summary>
/// Interface para geração de SBOM (Software Bill of Materials) em formato SPDX 2.3.
/// Essencial para compliance de segurança e auditoria de dependências.
/// </summary>
public interface ISbomGenerator
{
    /// <summary>Gera documento SBOM para um projeto ou artefato.</summary>
    Task<SbomDocument> GenerateSbomAsync(string projectPath);

    /// <summary>Exporta SBOM para JSON formatado.</summary>
    Task<string> ExportSbomToJsonAsync(SbomDocument sbom);

    /// <summary>Valida conformidade do SBOM com especificação SPDX.</summary>
    Task ValidateSbomComplianceAsync(SbomDocument sbom);
}

/// <summary>
/// Resultado de um artefato assinado digitalmente.
/// </summary>
public sealed record SignedArtifactResult(
    string ArtifactId,
    string ArtifactName,
    string ArtifactType,
    string Version,
    string Checksum,
    string Signature,
    DateTime SignedAt,
    string SignerIdentity,
    string CertificateSubject,
    bool IsValid,
    DateTime? ExpiryDate,
    Dictionary<string, string> Metadata,
    SbomDocument? SbomDocument,
    string? TransparencyLogEntry);

/// <summary>
/// Requisição de assinatura de artefato.
/// </summary>
public sealed record SigningRequest(
    string ArtifactPath,
    string ArtifactType,
    string Version,
    Dictionary<string, string> Metadata);

/// <summary>
/// Resultado da verificação de assinatura.
/// </summary>
public sealed record VerificationResult(
    bool IsValid,
    string ArtifactId,
    DateTime? VerifiedAt,
    string SignerIdentity,
    List<string> Errors,
    List<string> Warnings);

/// <summary>
/// Documento SBOM no formato SPDX 2.3.
/// </summary>
public sealed record SbomDocument(
    string SpdxVersion,
    string DataLicense,
    string DocumentNamespace,
    SbomPackage Package,
    List<SbomPackage> Dependencies,
    List<SbomRelationship> Relationships,
    DateTime Created,
    SbomCreator Creator,
    Dictionary<string, string> Metadata);

/// <summary>
/// Pacote no SBOM (dependência ou artefato principal).
/// </summary>
public sealed record SbomPackage(
    string SpdxId,
    string Name,
    string VersionInfo,
    string DownloadLocation,
    string FilesAnalyzed,
    string LicenseConcluded,
    string CopyrightText);

/// <summary>
/// Relacionamento entre pacotes no SBOM.
/// </summary>
public sealed record SbomRelationship(
    string SpdxElementId,
    string RelatedSpdxElement,
    string RelationshipType);

/// <summary>
/// Criador do SBOM (ferramenta/organização).
/// </summary>
public sealed record SbomCreator(
    string Tool,
    string Organization);
