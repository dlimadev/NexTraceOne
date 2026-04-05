using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Detalhe específico de um serviço do tipo Framework / SDK.
/// Contém metadata sobre package, linguagem, versão e consumidores.
/// </summary>
public sealed class FrameworkAssetDetail : Entity<FrameworkAssetDetailId>
{
    private FrameworkAssetDetail() { }

    /// <summary>Referência ao serviço proprietário.</summary>
    public ServiceAssetId ServiceAssetId { get; private set; } = null!;

    // ── Identidade do Framework ──────────────────────────────────────

    /// <summary>Nome do pacote (ex: "NexTrace.Auth.SDK").</summary>
    public string PackageName { get; private set; } = string.Empty;

    /// <summary>Linguagem de programação (ex: "C#", "TypeScript", "Java").</summary>
    public string Language { get; private set; } = string.Empty;

    /// <summary>Gestor de pacotes (ex: "NuGet", "npm", "Maven").</summary>
    public string PackageManager { get; private set; } = string.Empty;

    /// <summary>URL do registry de artefactos.</summary>
    public string ArtifactRegistryUrl { get; private set; } = string.Empty;

    // ── Versão e Compatibilidade ─────────────────────────────────────

    /// <summary>Versão mais recente publicada.</summary>
    public string LatestVersion { get; private set; } = string.Empty;

    /// <summary>Versão mínima suportada.</summary>
    public string MinSupportedVersion { get; private set; } = string.Empty;

    /// <summary>Plataforma alvo (ex: ".NET 10", "Node 22").</summary>
    public string TargetPlatform { get; private set; } = string.Empty;

    // ── Metadata ─────────────────────────────────────────────────────

    /// <summary>Tipo de licença (ex: "Internal", "MIT").</summary>
    public string LicenseType { get; private set; } = string.Empty;

    /// <summary>URL do pipeline de build.</summary>
    public string BuildPipelineUrl { get; private set; } = string.Empty;

    /// <summary>URL do changelog.</summary>
    public string ChangelogUrl { get; private set; } = string.Empty;

    // ── Relações ─────────────────────────────────────────────────────

    /// <summary>Número de serviços consumidores conhecidos.</summary>
    public int KnownConsumerCount { get; private set; }

    // ── Factory method ───────────────────────────────────────────────

    /// <summary>Cria um novo detalhe de Framework.</summary>
    public static FrameworkAssetDetail Create(
        ServiceAssetId serviceAssetId,
        string packageName,
        string language,
        string packageManager,
        string? artifactRegistryUrl = null,
        string? latestVersion = null,
        string? minSupportedVersion = null,
        string? targetPlatform = null,
        string? licenseType = null,
        string? buildPipelineUrl = null,
        string? changelogUrl = null)
    {
        Guard.Against.Null(serviceAssetId);
        Guard.Against.NullOrWhiteSpace(packageName);
        Guard.Against.NullOrWhiteSpace(language);
        Guard.Against.NullOrWhiteSpace(packageManager);

        return new FrameworkAssetDetail
        {
            Id = FrameworkAssetDetailId.New(),
            ServiceAssetId = serviceAssetId,
            PackageName = packageName,
            Language = language,
            PackageManager = packageManager,
            ArtifactRegistryUrl = artifactRegistryUrl ?? string.Empty,
            LatestVersion = latestVersion ?? string.Empty,
            MinSupportedVersion = minSupportedVersion ?? string.Empty,
            TargetPlatform = targetPlatform ?? string.Empty,
            LicenseType = licenseType ?? string.Empty,
            BuildPipelineUrl = buildPipelineUrl ?? string.Empty,
            ChangelogUrl = changelogUrl ?? string.Empty,
            KnownConsumerCount = 0
        };
    }

    // ── Mutações controladas ─────────────────────────────────────────

    /// <summary>Atualiza os campos de metadata do framework.</summary>
    public void Update(
        string packageName,
        string language,
        string packageManager,
        string? artifactRegistryUrl,
        string? latestVersion,
        string? minSupportedVersion,
        string? targetPlatform,
        string? licenseType,
        string? buildPipelineUrl,
        string? changelogUrl)
    {
        PackageName = Guard.Against.NullOrWhiteSpace(packageName);
        Language = Guard.Against.NullOrWhiteSpace(language);
        PackageManager = Guard.Against.NullOrWhiteSpace(packageManager);
        ArtifactRegistryUrl = artifactRegistryUrl ?? string.Empty;
        LatestVersion = latestVersion ?? string.Empty;
        MinSupportedVersion = minSupportedVersion ?? string.Empty;
        TargetPlatform = targetPlatform ?? string.Empty;
        LicenseType = licenseType ?? string.Empty;
        BuildPipelineUrl = buildPipelineUrl ?? string.Empty;
        ChangelogUrl = changelogUrl ?? string.Empty;
    }

    /// <summary>Publica nova versão do framework.</summary>
    public void PublishVersion(string version)
    {
        Guard.Against.NullOrWhiteSpace(version);
        LatestVersion = version;
    }

    /// <summary>Atualiza contagem de consumidores.</summary>
    public void SetConsumerCount(int count)
    {
        KnownConsumerCount = count >= 0 ? count : 0;
    }
}

/// <summary>Identificador fortemente tipado de FrameworkAssetDetail.</summary>
public sealed record FrameworkAssetDetailId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static FrameworkAssetDetailId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static FrameworkAssetDetailId From(Guid id) => new(id);
}
