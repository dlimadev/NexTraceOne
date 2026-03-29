using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Programa COBOL — unidade de execução no mainframe.
/// Referencia o sistema mainframe pai e pode ter copybooks associados.
/// </summary>
public sealed class CobolProgram : Entity<CobolProgramId>
{
    private CobolProgram() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome do programa (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do programa.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do programa e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual o programa pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── Compilação ────────────────────────────────────────────────────

    /// <summary>Linguagem do programa (COBOL por defeito).</summary>
    public string Language { get; private set; } = "COBOL";

    /// <summary>Versão do compilador utilizada.</summary>
    public string CompilerVersion { get; private set; } = string.Empty;

    /// <summary>Data da última compilação.</summary>
    public DateTimeOffset? LastCompiled { get; private set; }

    /// <summary>Biblioteca de código fonte (PDS/PDSE).</summary>
    public string SourceLibrary { get; private set; } = string.Empty;

    /// <summary>Nome do módulo de carga (load module).</summary>
    public string LoadModule { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do programa para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do programa.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo programa COBOL com os campos obrigatórios.</summary>
    public static CobolProgram Create(string name, MainframeSystemId systemId)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(systemId);

        return new CobolProgram
        {
            Id = CobolProgramId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            SystemId = systemId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação do programa.</summary>
    public void UpdateDetails(
        string displayName, string description, string compilerVersion,
        DateTimeOffset? lastCompiled, string sourceLibrary, string loadModule,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        CompilerVersion = compilerVersion ?? string.Empty;
        LastCompiled = lastCompiled;
        SourceLibrary = sourceLibrary ?? string.Empty;
        LoadModule = loadModule ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de CobolProgram.</summary>
public sealed record CobolProgramId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CobolProgramId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CobolProgramId From(Guid id) => new(id);
}
