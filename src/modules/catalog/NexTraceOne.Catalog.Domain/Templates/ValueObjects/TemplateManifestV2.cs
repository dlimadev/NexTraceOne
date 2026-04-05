using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Templates.ValueObjects;

/// <summary>
/// Manifesto de arquitetura V2 para templates de serviço.
/// Define padrão arquitetural, stack tecnológica, estrutura de pastas, dependências obrigatórias
/// e requisitos de qualidade que devem ser respeitados nos serviços criados a partir do template.
/// Serializado como JSON no campo ArchitecturePatternJson da entidade ServiceTemplate.
/// </summary>
public sealed record TemplateManifestV2
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Version { get; init; } = "2.0";
    public ManifestArchitecture Architecture { get; init; } = new();
    public ManifestStack Stack { get; init; } = new();
    public IReadOnlyList<ManifestFolder> Folders { get; init; } = Array.Empty<ManifestFolder>();
    public IReadOnlyList<ManifestRequiredDependency> RequiredDependencies { get; init; } = Array.Empty<ManifestRequiredDependency>();
    public ManifestQualityGates QualityGates { get; init; } = new();

    /// <summary>Tenta fazer parse de um JSON de manifesto V2. Retorna null em caso de falha.</summary>
    public static TemplateManifestV2? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var manifest = JsonSerializer.Deserialize<TemplateManifestV2>(json, JsonOptions);
            return manifest is not null && Validate(manifest) ? manifest : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Faz parse de um JSON de manifesto V2. Lança exceção em caso de falha.</summary>
    public static TemplateManifestV2 Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Manifest JSON cannot be null or empty.", nameof(json));

        TemplateManifestV2 manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<TemplateManifestV2>(json, JsonOptions)
                ?? throw new InvalidOperationException("Deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid TemplateManifestV2 JSON: {ex.Message}", ex);
        }

        var errors = GetValidationErrors(manifest);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"TemplateManifestV2 validation failed: {string.Join("; ", errors)}");

        return manifest;
    }

    /// <summary>Verifica se um JSON representa um manifesto V2 válido.</summary>
    public static bool IsValid(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            var manifest = JsonSerializer.Deserialize<TemplateManifestV2>(json, JsonOptions);
            return manifest is not null && Validate(manifest);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Valida o manifesto e lança exceção com todos os erros encontrados.</summary>
    public static void ValidateOrThrow(TemplateManifestV2 manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var errors = GetValidationErrors(manifest);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"TemplateManifestV2 is invalid: {string.Join("; ", errors)}");
    }

    private static bool Validate(TemplateManifestV2 m) => GetValidationErrors(m).Count == 0;

    private static List<string> GetValidationErrors(TemplateManifestV2 m)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(m.Version))
            errors.Add("Version is required.");

        if (m.Architecture is null)
            errors.Add("Architecture is required.");
        else if (string.IsNullOrWhiteSpace(m.Architecture.Pattern))
            errors.Add("Architecture.Pattern is required.");

        if (m.Stack is null)
            errors.Add("Stack is required.");

        if (m.QualityGates is not null)
        {
            if (m.QualityGates.TestCoverageMinimum is < 0 or > 100)
                errors.Add("QualityGates.TestCoverageMinimum must be between 0 and 100.");
        }

        return errors;
    }

    /// <summary>Serializa o manifesto para JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}

/// <summary>Padrão arquitetural do template.</summary>
public sealed record ManifestArchitecture
{
    public string Pattern { get; init; } = string.Empty;
    public string Style { get; init; } = string.Empty;
    public IReadOnlyList<string> Layers { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
}

/// <summary>Stack tecnológica do template.</summary>
public sealed record ManifestStack
{
    public string Runtime { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Framework { get; init; } = string.Empty;
    public IReadOnlyList<string> AdditionalFrameworks { get; init; } = Array.Empty<string>();
}

/// <summary>Pasta/estrutura de diretório incluída no template.</summary>
public sealed record ManifestFolder(
    string Path,
    string Purpose,
    bool IsRequired = true);

/// <summary>Dependência obrigatória que os serviços criados com este template devem incluir.</summary>
public sealed record ManifestRequiredDependency(
    string Name,
    string MinVersion,
    string Ecosystem,
    string Reason);

/// <summary>Gates de qualidade que os serviços criados com este template devem respeitar.</summary>
public sealed record ManifestQualityGates
{
    public int TestCoverageMinimum { get; init; } = 70;
    public bool RequireUnitTests { get; init; } = true;
    public bool RequireIntegrationTests { get; init; } = false;
    public bool RequireOpenApiSpec { get; init; } = true;
    public IReadOnlyList<string> RequiredLinters { get; init; } = Array.Empty<string>();
}
