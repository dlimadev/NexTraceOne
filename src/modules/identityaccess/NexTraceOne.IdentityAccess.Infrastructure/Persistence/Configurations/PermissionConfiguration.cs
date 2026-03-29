using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Permission.
/// Seed data cobre permissões granulares de todos os módulos da plataforma.
/// Formato do código: "módulo:recurso:ação" para compatibilidade com i18n.
/// </summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("iam_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PermissionId.From(value));
        builder.Property(x => x.Code).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            // ── Identity (10) ───────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C001")), "identity:users:read", "Read users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C002")), "identity:users:write", "Create and update users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C003")), "identity:roles:assign", "Assign roles to users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C004")), "identity:sessions:revoke", "Revoke user sessions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C005")), "identity:roles:read", "View available roles", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C006")), "identity:sessions:read", "View active sessions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C007")), "identity:permissions:read", "View available permissions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C008")), "identity:jit-access:decide", "Approve or reject JIT access requests", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C009")), "identity:break-glass:decide", "Approve, revoke or audit break glass requests", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C00A")), "identity:delegations:manage", "Create and revoke delegations", "Identity"),

            // ── Catalog (2) ─────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C010")), "catalog:assets:read", "View service and API assets", "Catalog"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C011")), "catalog:assets:write", "Create and update assets", "Catalog"),

            // ── Contracts (3) ───────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C020")), "contracts:read", "View contract versions and diffs", "Contracts"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C021")), "contracts:write", "Create and update contracts", "Contracts"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C022")), "contracts:import", "Import contract files", "Contracts"),

            // ── Change Intelligence (2) ─────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C030")), "change-intelligence:read", "View change intelligence data", "ChangeIntelligence"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C031")), "change-intelligence:write", "Create and manage changes", "ChangeIntelligence"),

            // ── Operations (16) ─────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C034")), "operations:incidents:read", "View operational incidents", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C035")), "operations:incidents:write", "Create and manage operational incidents", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C036")), "operations:mitigation:read", "View mitigation actions", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C037")), "operations:mitigation:write", "Create and manage mitigation actions", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C038")), "operations:runbooks:read", "View runbooks", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C039")), "operations:runbooks:write", "Create and manage runbooks", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03A")), "operations:reliability:read", "View service reliability data", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03B")), "operations:reliability:write", "Manage service reliability targets", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03C")), "operations:runtime:read", "View runtime status and health", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03D")), "operations:runtime:write", "Manage runtime configuration", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03E")), "operations:cost:read", "View operational cost data", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C03F")), "operations:cost:write", "Manage operational cost allocations", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C043")), "operations:automation:read", "View automation rules and history", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C044")), "operations:automation:write", "Create and manage automation rules", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C045")), "operations:automation:execute", "Execute automation actions", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C046")), "operations:automation:approve", "Approve automation execution requests", "Operations"),

            // ── Workflow (3) ────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C040")), "workflow:instances:read", "View workflow instances", "Workflow"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C041")), "workflow:instances:write", "Create and manage workflow instances", "Workflow"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C042")), "workflow:templates:write", "Create and manage workflow templates", "Workflow"),

            // ── Promotion (4) ───────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C050")), "promotion:requests:read", "View promotion requests and gates", "Promotion"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C051")), "promotion:requests:write", "Create promotion requests", "Promotion"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C052")), "promotion:environments:write", "Execute environment promotions", "Promotion"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C053")), "promotion:gates:override", "Override promotion gates", "Promotion"),

            // ── Rulesets (3) ────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C060")), "rulesets:read", "View rulesets and bindings", "Rulesets"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C061")), "rulesets:write", "Manage rulesets and bindings", "Rulesets"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C062")), "rulesets:execute", "Execute rulesets", "Rulesets"),

            // ── Audit (5) ───────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C070")), "audit:trail:read", "View audit trail", "Audit"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C071")), "audit:reports:read", "View audit reports", "Audit"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C072")), "audit:compliance:read", "View compliance audit data", "Audit"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C073")), "audit:compliance:write", "Manage compliance audit records", "Audit"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C074")), "audit:events:write", "Write audit events", "Audit"),

            // ── Platform (3) ────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C090")), "platform:settings:read", "View platform settings", "Platform"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C091")), "platform:settings:write", "Manage platform settings", "Platform"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C092")), "platform:admin:read", "View platform administration data", "Platform"),

            // ── Developer Portal (2) ────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C100")), "developer-portal:read", "View developer portal content", "DeveloperPortal"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C101")), "developer-portal:write", "Manage developer portal content", "DeveloperPortal"),

            // ── Governance (17) ─────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C110")), "governance:admin:read", "View governance administration", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C111")), "governance:admin:write", "Manage governance administration", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C112")), "governance:compliance:read", "View compliance status", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C113")), "governance:controls:read", "View governance controls", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C114")), "governance:domains:read", "View governance domains", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C115")), "governance:domains:write", "Manage governance domains", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C116")), "governance:evidence:read", "View governance evidence", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C117")), "governance:finops:read", "View FinOps governance data", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C118")), "governance:packs:read", "View governance packs", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C119")), "governance:packs:write", "Manage governance packs", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11A")), "governance:policies:read", "View governance policies", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11B")), "governance:reports:read", "View governance reports", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11C")), "governance:risk:read", "View risk assessments", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11D")), "governance:teams:read", "View governance team assignments", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11E")), "governance:teams:write", "Manage governance team assignments", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C11F")), "governance:waivers:read", "View governance waivers", "Governance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C120")), "governance:waivers:write", "Manage governance waivers", "Governance"),

            // ── AI (8) ──────────────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C130")), "ai:assistant:read", "View AI assistant interactions", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C131")), "ai:assistant:write", "Use AI assistant", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C132")), "ai:governance:read", "View AI governance policies and usage", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C133")), "ai:governance:write", "Manage AI governance policies", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C134")), "ai:ide:read", "View AI IDE extension configuration", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C135")), "ai:ide:write", "Manage AI IDE extension configuration", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C136")), "ai:runtime:read", "View AI runtime status and models", "AI"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C137")), "ai:runtime:write", "Manage AI runtime and model registry", "AI"),

            // ── Integrations (2) ────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C140")), "integrations:read", "View integrations and connectors", "Integrations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C141")), "integrations:write", "Manage integrations and connectors", "Integrations"),

            // ── Notifications (7) ───────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C150")), "notifications:inbox:read", "View notification inbox", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C151")), "notifications:inbox:write", "Manage notification inbox", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C152")), "notifications:preferences:read", "View notification preferences", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C153")), "notifications:preferences:write", "Manage notification preferences", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C154")), "notifications:configuration:read", "View notification configuration", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C155")), "notifications:configuration:write", "Manage notification configuration", "Notifications"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C156")), "notifications:delivery:read", "View notification delivery status", "Notifications"),

            // ── Environment (5) ─────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C160")), "env:environments:read", "View environments", "Environment"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C161")), "env:environments:write", "Create and update environments", "Environment"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C162")), "env:environments:admin", "Administer environments", "Environment"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C163")), "env:access:read", "View environment access policies", "Environment"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C164")), "env:access:admin", "Administer environment access policies", "Environment"),

            // ── Configuration (2) ───────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C170")), "configuration:read", "View system configuration", "Configuration"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C171")), "configuration:write", "Manage system configuration", "Configuration"),

            // ── Analytics (2) ───────────────────────────────────────────────
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C180")), "analytics:read", "View analytics dashboards and data", "Analytics"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C181")), "analytics:write", "Manage analytics configuration", "Analytics"));
    }
}
