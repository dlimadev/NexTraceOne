using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Copybook COBOL — definição de layout de dados partilhada entre programas.
/// Tratado como contrato de dados no catálogo legacy.
/// </summary>
public sealed class Copybook : Entity<CopybookId>
{
    private Copybook() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome do copybook (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do copybook.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do copybook e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual o copybook pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── Estrutura ─────────────────────────────────────────────────────

    /// <summary>Versão do copybook.</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Informação estrutural do layout (contagem de campos, tamanho, formato).</summary>
    public CopybookLayout Layout { get; private set; } = null!;

    /// <summary>Biblioteca de código fonte (PDS/PDSE).</summary>
    public string SourceLibrary { get; private set; } = string.Empty;

    /// <summary>Conteúdo raw do copybook.</summary>
    public string RawContent { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do copybook para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do copybook.</summary>
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

    /// <summary>Cria um novo copybook com os campos obrigatórios.</summary>
    public static Copybook Create(string name, MainframeSystemId systemId, CopybookLayout layout)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(systemId);
        Guard.Against.Null(layout);

        return new Copybook
        {
            Id = CopybookId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            SystemId = systemId,
            Layout = layout,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação do copybook.</summary>
    public void UpdateDetails(
        string displayName, string description, string version,
        string sourceLibrary, string rawContent,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Version = version ?? string.Empty;
        SourceLibrary = sourceLibrary ?? string.Empty;
        RawContent = rawContent ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza o layout estrutural do copybook.</summary>
    public void UpdateLayout(CopybookLayout layout)
    {
        Guard.Against.Null(layout);
        Layout = layout;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de Copybook.</summary>
public sealed record CopybookId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CopybookId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CopybookId From(Guid id) => new(id);
}
