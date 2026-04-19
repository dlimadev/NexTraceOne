using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade AnalyticsEvent.
/// Eventos mínimos de Product Analytics.
/// </summary>
internal sealed class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.ToTable("pan_analytics_events", t =>
        {
            t.HasCheckConstraint("CK_pan_analytics_events_module",
                "\"Module\" IN ('Dashboard','ServiceCatalog','SourceOfTruth','ContractStudio','ChangeIntelligence','Incidents','Reliability','Runbooks','AiAssistant','Governance','ExecutiveViews','FinOps','IntegrationHub','DeveloperPortal','Admin','Automation','Search')");
            t.HasCheckConstraint("CK_pan_analytics_events_event_type",
                "\"EventType\" IN ('ModuleViewed','EntityViewed','SearchExecuted','SearchResultClicked','ZeroResultSearch','QuickActionTriggered','AssistantPromptSubmitted','AssistantResponseUsed','ContractDraftCreated','ContractPublished','ChangeViewed','IncidentInvestigated','MitigationWorkflowStarted','MitigationWorkflowCompleted','EvidencePackageExported','PolicyViewed','ExecutiveOverviewViewed','RunbookViewed','SourceOfTruthQueried','ReportGenerated','OnboardingStepCompleted','JourneyAbandoned','EmptyStateEncountered','ReliabilityDashboardViewed','AutomationWorkflowManaged')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AnalyticsEventId(value));

        builder.Property(x => x.TenantId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(200);

        builder.Property(x => x.Persona)
            .HasMaxLength(50);

        builder.Property(x => x.Module)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Feature)
            .HasMaxLength(200);

        builder.Property(x => x.EntityType)
            .HasMaxLength(100);

        builder.Property(x => x.Outcome)
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasMaxLength(200);

        builder.Property(x => x.DomainId)
            .HasMaxLength(200);

        builder.Property(x => x.SessionId)
            .HasMaxLength(200);

        builder.Property(x => x.ClientType)
            .HasMaxLength(50);

        builder.Property(x => x.MetadataJson)
            .HasColumnType("text");

        builder.Property(x => x.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.Module);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.Persona);
        builder.HasIndex(x => x.UserId);

        // ── Índice composto para consultas analíticas por tenant ──────────
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        builder.HasIndex(x => new { x.TenantId, x.Module, x.OccurredAt });

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.OccurredAt })
            .HasDatabaseName("IX_pan_analytics_events_TenantId_UserId_OccurredAt");

        builder.HasIndex(x => new { x.SessionId, x.OccurredAt })
            .HasDatabaseName("IX_pan_analytics_events_SessionId_OccurredAt");

        builder.HasIndex(x => new { x.Module, x.EventType })
            .HasDatabaseName("IX_pan_analytics_events_Module_EventType");
    }
}
