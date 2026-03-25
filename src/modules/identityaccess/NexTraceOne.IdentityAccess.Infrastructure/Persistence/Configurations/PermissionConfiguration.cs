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
            // Identity
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C001")), "identity:users:read", "Read users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C002")), "identity:users:write", "Create and update users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C003")), "identity:roles:assign", "Assign roles to users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C004")), "identity:sessions:revoke", "Revoke user sessions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C005")), "identity:roles:read", "View available roles", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C006")), "identity:sessions:read", "View active sessions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C007")), "identity:permissions:read", "View available permissions", "Identity"),

            // Engineering Graph
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C010")), "catalog:assets:read", "View service and API assets", "Catalog"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C011")), "catalog:assets:write", "Create and update assets", "Catalog"),

            // Contracts
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C020")), "contracts:read", "View contract versions and diffs", "Contracts"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C021")), "contracts:write", "Create and update contracts", "Contracts"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C022")), "contracts:import", "Import contract files", "Contracts"),

            // Change Intelligence
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C030")), "change-intelligence:releases:read", "View releases", "ChangeIntelligence"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C031")), "change-intelligence:releases:write", "Create and manage releases", "ChangeIntelligence"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C032")), "change-intelligence:blast-radius:read", "View blast radius reports", "ChangeIntelligence"),

            // Operations
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C034")), "operations:incidents:read", "View operational incidents", "Operations"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C035")), "operations:incidents:write", "Create and manage operational incidents", "Operations"),

            // Workflow
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C040")), "workflow:read", "View workflow instances and templates", "Workflow"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C041")), "workflow:write", "Create and configure workflows", "Workflow"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C042")), "workflow:approve", "Approve or reject workflow stages", "Workflow"),

            // Promotion
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C050")), "promotion:read", "View promotion requests and gates", "Promotion"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C051")), "promotion:write", "Create promotion requests", "Promotion"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C052")), "promotion:promote", "Execute environment promotions", "Promotion"),

            // Ruleset Governance
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C060")), "ruleset-governance:read", "View rulesets and bindings", "RulesetGovernance"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C061")), "ruleset-governance:write", "Manage rulesets and bindings", "RulesetGovernance"),

            // Audit
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C070")), "audit:read", "View audit trail", "Audit"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C071")), "audit:export", "Export audit data", "Audit"),

            // Licensing
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C080")), "licensing:read", "View license information", "Licensing"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C081")), "licensing:write", "Manage licenses", "Licensing"),

            // Platform
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C090")), "platform:settings:read", "View platform settings", "Platform"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C091")), "platform:settings:write", "Manage platform settings", "Platform"));
    }
}
