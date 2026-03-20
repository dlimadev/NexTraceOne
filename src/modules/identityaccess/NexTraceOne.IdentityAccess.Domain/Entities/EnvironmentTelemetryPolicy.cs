using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa a política de telemetria para um ambiente específico de um tenant.
///
/// Define como a telemetria deve ser coletada, retida e analisada no contexto deste ambiente.
/// Ambientes de produção terão políticas mais conservadoras de retenção e anonimização,
/// enquanto ambientes de desenvolvimento podem ter maior verbosidade e menor retenção.
///
/// A IA utiliza estas políticas para determinar:
/// - Quais fontes de dados estão disponíveis para análise por ambiente
/// - Limites de retenção para análise histórica
/// - Se dados de telemetria podem ser usados para comparação cross-environment
///
/// Fase 2: migração AddEnvironmentTelemetryPolicies — sem mapeamento EF na Fase 1.
/// </summary>
public sealed class EnvironmentTelemetryPolicy : Entity<EnvironmentTelemetryPolicyId>
{
    private EnvironmentTelemetryPolicy() { }

    /// <summary>Tenant proprietário da política.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Ambiente ao qual a política de telemetria se aplica.</summary>
    public EnvironmentId EnvironmentId { get; private set; } = null!;

    /// <summary>
    /// Período de retenção de telemetria em dias.
    /// Ambientes de produção tipicamente retêm por mais tempo.
    /// </summary>
    public int RetentionDays { get; private set; }

    /// <summary>
    /// Nível de verbosidade da telemetria (ex.: Minimal, Standard, Verbose, Debug).
    /// </summary>
    public string VerbosityLevel { get; private set; } = TelemetryVerbosity.Standard;

    /// <summary>
    /// Indica se dados de telemetria deste ambiente podem ser usados pela IA
    /// para comparação com outros ambientes do mesmo tenant.
    /// </summary>
    public bool AllowCrossEnvironmentComparison { get; private set; }

    /// <summary>
    /// Indica se dados sensíveis devem ser anonimizados antes da análise pela IA.
    /// Obrigatório para ambientes de produção.
    /// </summary>
    public bool RequiresDataAnonymization { get; private set; }

    /// <summary>
    /// Indica se a telemetria deste ambiente serve como baseline de comparação
    /// para análise de regressão em ambientes não produtivos.
    /// Tipicamente verdadeiro para ambientes de produção.
    /// </summary>
    public bool IsBaselineSource { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de uma política de telemetria.
    /// Os defaults são inferidos pelo perfil do ambiente para facilitar configuração.
    /// </summary>
    public static EnvironmentTelemetryPolicy Create(
        TenantId tenantId,
        EnvironmentId environmentId,
        EnvironmentProfile profile,
        DateTimeOffset now,
        int? retentionDays = null,
        string? verbosityLevel = null,
        bool? allowCrossEnvironmentComparison = null,
        bool? requiresDataAnonymization = null,
        bool? isBaselineSource = null)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.Null(environmentId);

        var isProduction = profile is EnvironmentProfile.Production or EnvironmentProfile.DisasterRecovery;

        return new EnvironmentTelemetryPolicy
        {
            Id = EnvironmentTelemetryPolicyId.New(),
            TenantId = tenantId,
            EnvironmentId = environmentId,
            RetentionDays = retentionDays ?? (isProduction ? 90 : 30),
            VerbosityLevel = verbosityLevel ?? (profile == EnvironmentProfile.Development ? TelemetryVerbosity.Verbose : TelemetryVerbosity.Standard),
            AllowCrossEnvironmentComparison = allowCrossEnvironmentComparison ?? true,
            RequiresDataAnonymization = requiresDataAnonymization ?? isProduction,
            IsBaselineSource = isBaselineSource ?? isProduction,
            CreatedAt = now
        };
    }

    /// <summary>Constantes para nível de verbosidade da telemetria.</summary>
    public static class TelemetryVerbosity
    {
        /// <summary>Apenas métricas críticas. Mínimo overhead.</summary>
        public const string Minimal = "minimal";

        /// <summary>Métricas e traces padrão. Uso geral.</summary>
        public const string Standard = "standard";

        /// <summary>Métricas, traces e logs estruturados completos.</summary>
        public const string Verbose = "verbose";

        /// <summary>Máximo detalhe incluindo debug traces. Apenas desenvolvimento.</summary>
        public const string Debug = "debug";
    }
}

/// <summary>Identificador fortemente tipado de EnvironmentTelemetryPolicy.</summary>
public sealed record EnvironmentTelemetryPolicyId(Guid Value) : TypedIdBase(Value)
{
    public static EnvironmentTelemetryPolicyId New() => new(Guid.NewGuid());
    public static EnvironmentTelemetryPolicyId From(Guid id) => new(id);
}
