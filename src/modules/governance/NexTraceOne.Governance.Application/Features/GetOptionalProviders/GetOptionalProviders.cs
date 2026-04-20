using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Application.Features.GetOptionalProviders;

/// <summary>
/// Feature: GetOptionalProviders — estado configured/not-configured de cada provider
/// opcional da plataforma. Permite que Platform Admins saibam imediatamente o porquê
/// de verem <c>simulatedNote</c> em dashboards (canary, backup, kafka, cloud billing, …).
///
/// Cada provider implementa <c>IsConfigured</c>; esta feature agrega esse sinal e devolve
/// metadata útil (config keys a preencher e link para documentação de setup).
///
/// Referência do plano: CFG-01 (SystemHealthPage) em <c>docs/HONEST-GAPS.md</c>.
/// </summary>
public static class GetOptionalProviders
{
    /// <summary>Query sem parâmetros — retorna estado de todos os providers opcionais conhecidos.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>
    /// Handler que inspecciona cada provider opcional registado na DI e devolve o seu estado.
    /// </summary>
    public sealed class Handler(
        ICanaryProvider canaryProvider,
        IBackupProvider backupProvider,
        IKafkaEventProducer kafkaProducer,
        ICloudBillingProvider cloudBillingProvider) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var providers = new List<OptionalProviderDto>
            {
                BuildDto(
                    name: OptionalProviderNames.Canary,
                    category: "operations",
                    isConfigured: canaryProvider.IsConfigured,
                    configKeyPrefix: "Canary",
                    docsPath: "docs/deployment/PRODUCTION-BOOTSTRAP.md#canary-provider",
                    description: "Canary rollouts dashboard (Argo Rollouts, Flagger, LaunchDarkly, …)."),

                BuildDto(
                    name: OptionalProviderNames.Backup,
                    category: "operations",
                    isConfigured: backupProvider.IsConfigured,
                    configKeyPrefix: "Backup",
                    docsPath: "docs/deployment/PRODUCTION-BOOTSTRAP.md#backup-provider",
                    description: "Database backup posture (pg_dump, pgBackRest, Barman, Velero, …)."),

                BuildDto(
                    name: OptionalProviderNames.Kafka,
                    category: "integrations",
                    isConfigured: kafkaProducer.IsConfigured,
                    configKeyPrefix: "Kafka",
                    docsPath: "docs/deployment/PRODUCTION-BOOTSTRAP.md#kafka",
                    description: "Kafka event producer. When not configured, events are discarded silently by NullKafkaEventProducer."),

                BuildDto(
                    name: OptionalProviderNames.CloudBilling,
                    category: "finops",
                    isConfigured: cloudBillingProvider.IsConfigured,
                    configKeyPrefix: "FinOps:Billing",
                    docsPath: "docs/deployment/PRODUCTION-BOOTSTRAP.md#cloud-billing",
                    description: "Cloud billing ingestion (AWS CUR, Azure Cost Management, GCP BigQuery Billing Export)."),
            };

            var configuredCount = providers.Count(p => p.Status == OptionalProviderStatus.Configured);
            var totalCount = providers.Count;

            var response = new Response(
                Providers: providers,
                ConfiguredCount: configuredCount,
                TotalCount: totalCount,
                CheckedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static OptionalProviderDto BuildDto(
            string name,
            string category,
            bool isConfigured,
            string configKeyPrefix,
            string docsPath,
            string description)
            => new(
                Name: name,
                Category: category,
                Status: isConfigured ? OptionalProviderStatus.Configured : OptionalProviderStatus.NotConfigured,
                ConfigKeyPrefix: configKeyPrefix,
                DocsPath: docsPath,
                Description: description);
    }

    /// <summary>Resposta agregada.</summary>
    public sealed record Response(
        IReadOnlyList<OptionalProviderDto> Providers,
        int ConfiguredCount,
        int TotalCount,
        DateTimeOffset CheckedAt);

    /// <summary>Detalhes de um provider opcional.</summary>
    public sealed record OptionalProviderDto(
        string Name,
        string Category,
        OptionalProviderStatus Status,
        string ConfigKeyPrefix,
        string DocsPath,
        string Description);

    /// <summary>Estado possível de um provider opcional.</summary>
    public enum OptionalProviderStatus
    {
        /// <summary>Provider não tem implementação real ativa (usa Null* fallback).</summary>
        NotConfigured = 0,

        /// <summary>Provider está configurado e operacional.</summary>
        Configured = 1,

        /// <summary>Não foi possível determinar o estado do provider.</summary>
        Unknown = 2,
    }
}
