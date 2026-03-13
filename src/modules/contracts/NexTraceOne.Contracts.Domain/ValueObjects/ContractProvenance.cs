using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value Object que captura a proveniência (lineage) de um contrato.
/// Registra a origem completa do artefato: de onde veio, qual parser foi usado,
/// versão do padrão, quem importou/criou, e se o conteúdo foi gerado por IA.
/// Essencial para auditoria regulatória e rastreabilidade enterprise.
/// </summary>
public sealed class ContractProvenance : ValueObject
{
    private ContractProvenance() { }

    /// <summary>Origem do contrato: "upload", "url", "ai-generated", "migration".</summary>
    public string Origin { get; private set; } = string.Empty;

    /// <summary>Formato original do arquivo importado (ex: "openapi-3.1-json", "wsdl-1.1-xml").</summary>
    public string OriginalFormat { get; private set; } = string.Empty;

    /// <summary>Parser/importador utilizado para processar o contrato.</summary>
    public string ParserUsed { get; private set; } = string.Empty;

    /// <summary>Versão do padrão da especificação (ex: "3.1.0", "2.0", "1.1").</summary>
    public string StandardVersion { get; private set; } = string.Empty;

    /// <summary>Usuário que criou/importou o contrato.</summary>
    public string ImportedBy { get; private set; } = string.Empty;

    /// <summary>Indica se o conteúdo foi gerado ou assistido por IA.</summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>Identificador do modelo de IA utilizado, se aplicável.</summary>
    public string? AiModelVersion { get; private set; }

    /// <summary>
    /// Cria uma proveniência para importação manual ou via URL.
    /// </summary>
    public static ContractProvenance ForImport(
        string origin,
        string originalFormat,
        string parserUsed,
        string standardVersion,
        string importedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(parserUsed);
        ArgumentException.ThrowIfNullOrWhiteSpace(importedBy);

        return new ContractProvenance
        {
            Origin = origin,
            OriginalFormat = originalFormat,
            ParserUsed = parserUsed,
            StandardVersion = standardVersion ?? string.Empty,
            ImportedBy = importedBy,
            IsAiGenerated = false
        };
    }

    /// <summary>
    /// Cria uma proveniência para conteúdo gerado ou assistido por IA local.
    /// </summary>
    public static ContractProvenance ForAiGeneration(
        string parserUsed,
        string standardVersion,
        string importedBy,
        string aiModelVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parserUsed);
        ArgumentException.ThrowIfNullOrWhiteSpace(importedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(aiModelVersion);

        return new ContractProvenance
        {
            Origin = "ai-generated",
            OriginalFormat = "ai-draft",
            ParserUsed = parserUsed,
            StandardVersion = standardVersion ?? string.Empty,
            ImportedBy = importedBy,
            IsAiGenerated = true,
            AiModelVersion = aiModelVersion
        };
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Origin;
        yield return OriginalFormat;
        yield return ParserUsed;
        yield return StandardVersion;
        yield return ImportedBy;
        yield return IsAiGenerated;
        yield return AiModelVersion;
    }
}
