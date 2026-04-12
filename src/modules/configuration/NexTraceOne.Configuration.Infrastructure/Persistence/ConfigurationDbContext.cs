using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Configuration.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
///
/// Hierarquia de configuração suportada: Instance → Tenant → Environment → Module.
/// A dimensão Module é representada explicitamente pela entidade ConfigurationModule.
/// Feature flags persistidas: FeatureFlagDefinition (metadados) + FeatureFlagEntry (valores por âmbito).
/// </summary>
public sealed class ConfigurationDbContext(
    DbContextOptions<ConfigurationDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    // ── Foundation (Phase 1-2) ─────────────────────────────────────────

    /// <summary>Definições de configuração (metadados e schema).</summary>
    public DbSet<ConfigurationDefinition> Definitions => Set<ConfigurationDefinition>();

    /// <summary>Valores concretos de configuração por âmbito.</summary>
    public DbSet<ConfigurationEntry> Entries => Set<ConfigurationEntry>();

    /// <summary>Registos de auditoria de alterações em configurações.</summary>
    public DbSet<ConfigurationAuditEntry> AuditEntries => Set<ConfigurationAuditEntry>();

    /// <summary>
    /// Módulos/domínios funcionais da plataforma para agrupamento de configurações.
    /// Torna explícita a dimensão "módulo" na hierarquia Instance → Tenant → Environment → Module.
    /// </summary>
    public DbSet<ConfigurationModule> Modules => Set<ConfigurationModule>();

    /// <summary>
    /// Definições de feature flags da plataforma.
    /// Prepara o terreno para resolução de flags por âmbito (implementação em P3.2).
    /// </summary>
    public DbSet<FeatureFlagDefinition> FeatureFlagDefinitions => Set<FeatureFlagDefinition>();

    /// <summary>
    /// Substituições de valor de feature flags por âmbito.
    /// Permite sobrepor o valor padrão de uma flag para um tenant, environment ou outro âmbito específico.
    /// </summary>
    public DbSet<FeatureFlagEntry> FeatureFlagEntries => Set<FeatureFlagEntry>();

    /// <summary>Vistas guardadas pelo utilizador por contexto de lista.</summary>
    public DbSet<UserSavedView> UserSavedViews => Set<UserSavedView>();

    /// <summary>Favoritos de entidades da plataforma por utilizador.</summary>
    public DbSet<UserBookmark> UserBookmarks => Set<UserBookmark>();

    // ── Phase 3: Watch Lists & Alert Rules ─────────────────────────────

    /// <summary>Items de watch list do utilizador para serviços, contratos, mudanças, incidentes e runbooks.</summary>
    public DbSet<UserWatch> UserWatches => Set<UserWatch>();

    /// <summary>Regras de alerta personalizadas do utilizador.</summary>
    public DbSet<UserAlertRule> UserAlertRules => Set<UserAlertRule>();

    // ── Phase 4: Tags, Custom Fields & Taxonomies ──────────────────────

    /// <summary>Tags key:value associadas a entidades da plataforma.</summary>
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();

    /// <summary>Campos personalizados para serviços no catálogo.</summary>
    public DbSet<ServiceCustomField> ServiceCustomFields => Set<ServiceCustomField>();

    /// <summary>Categorias de taxonomia definidas pelo admin do tenant.</summary>
    public DbSet<TaxonomyCategory> TaxonomyCategories => Set<TaxonomyCategory>();

    /// <summary>Valores dentro de categorias de taxonomia.</summary>
    public DbSet<TaxonomyValue> TaxonomyValues => Set<TaxonomyValue>();

    // ── Phase 5: Automation, Checklists & Contract Templates ───────────

    /// <summary>Regras de automação If-Then para o tenant.</summary>
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();

    /// <summary>Checklists personalizadas para mudanças.</summary>
    public DbSet<ChangeChecklist> ChangeChecklists => Set<ChangeChecklist>();

    /// <summary>Templates de contrato personalizados por tenant.</summary>
    public DbSet<ContractTemplate> ContractTemplates => Set<ContractTemplate>();

    // ── Phase 6: Scheduled Reports ─────────────────────────────────────

    /// <summary>Relatórios programados por utilizador ou admin.</summary>
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();

    // ── Phase 7: Saved Prompts ─────────────────────────────────────────

    /// <summary>Prompts de IA guardados pelo utilizador.</summary>
    public DbSet<SavedPrompt> SavedPrompts => Set<SavedPrompt>();

    // ── Phase 8: Webhook Templates ─────────────────────────────────────

    /// <summary>Templates de payload personalizados para webhooks do tenant.</summary>
    public DbSet<WebhookTemplate> WebhookTemplates => Set<WebhookTemplate>();

    // ── Phase 9: Contract Compliance Policies ──────────────────────────

    /// <summary>Políticas de compliance contratual configuráveis por âmbito.</summary>
    public DbSet<ContractCompliancePolicy> ContractCompliancePolicies => Set<ContractCompliancePolicy>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ConfigurationDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Configuration.Infrastructure";

    /// <inheritdoc />
    protected override string OutboxTableName => "cfg_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
