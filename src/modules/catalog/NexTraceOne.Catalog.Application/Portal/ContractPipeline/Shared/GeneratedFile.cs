namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;

/// <summary>
/// Representa um ficheiro gerado pelo Contract-to-Code Pipeline.
/// Inclui nome, conteúdo, linguagem e descrição do propósito.
/// </summary>
public sealed record GeneratedFile(
    string FileName,
    string Content,
    string Language,
    string Description = "");
