using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.DeveloperPortal.Domain.Entities;

/// <summary>
/// Aggregate Root que representa o registo de uma geração de código a partir de um contrato de API.
/// Mantém a trilha de auditoria completa de cada geração: quem solicitou, para qual API e versão,
/// em que linguagem, que tipo de artefacto foi gerado e se utilizou IA. Permite rastreabilidade
/// de artefactos gerados e métricas de utilização do gerador de código do portal.
/// </summary>
public sealed class CodeGenerationRecord : AggregateRoot<CodeGenerationRecordId>
{
    private CodeGenerationRecord() { }

    /// <summary>Identificador do ativo de API cujo contrato originou a geração.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome legível da API cujo contrato foi utilizado.</summary>
    public string ApiName { get; private set; } = string.Empty;

    /// <summary>Versão semver do contrato utilizado na geração.</summary>
    public string ContractVersion { get; private set; } = string.Empty;

    /// <summary>Identificador do utilizador que solicitou a geração.</summary>
    public Guid RequestedById { get; private set; }

    /// <summary>Linguagem de programação alvo (C#, Java, Python, TypeScript, Go).</summary>
    public string Language { get; private set; } = string.Empty;

    /// <summary>Tipo de artefacto gerado (SdkClient, IntegrationExample, ContractTest, DataModels).</summary>
    public string GenerationType { get; private set; } = string.Empty;

    /// <summary>Código-fonte gerado completo.</summary>
    public string GeneratedCode { get; private set; } = string.Empty;

    /// <summary>Indica se a geração utilizou inteligência artificial.</summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>Identificador opcional do template utilizado na geração baseada em templates.</summary>
    public string? TemplateId { get; private set; }

    /// <summary>Data/hora UTC em que o código foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>
    /// Cria o registo de uma geração de código a partir do contrato de uma API.
    /// Valida que todos os campos obrigatórios estão presentes e consistentes.
    /// </summary>
    public static CodeGenerationRecord Create(
        Guid apiAssetId,
        string apiName,
        string contractVersion,
        Guid requestedById,
        string language,
        string generationType,
        string generatedCode,
        bool isAiGenerated,
        string? templateId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(apiName);
        Guard.Against.NullOrWhiteSpace(contractVersion);
        Guard.Against.Default(requestedById);
        Guard.Against.NullOrWhiteSpace(language);
        Guard.Against.NullOrWhiteSpace(generationType);
        Guard.Against.NullOrWhiteSpace(generatedCode);

        return new CodeGenerationRecord
        {
            Id = CodeGenerationRecordId.New(),
            ApiAssetId = apiAssetId,
            ApiName = apiName,
            ContractVersion = contractVersion,
            RequestedById = requestedById,
            Language = language,
            GenerationType = generationType,
            GeneratedCode = generatedCode,
            IsAiGenerated = isAiGenerated,
            TemplateId = templateId,
            GeneratedAt = generatedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de CodeGenerationRecord.</summary>
public sealed record CodeGenerationRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CodeGenerationRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CodeGenerationRecordId From(Guid id) => new(id);
}
