using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Domain.Enums;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma licença ativa da plataforma.
/// Controla ativação em hardware, capabilities, quotas de consumo,
/// tipo de licença (Standard/Trial/Enterprise), edição comercial e grace period.
///
/// Responsabilidades (coesas ao conceito de licença):
/// - Gestão do ciclo de vida (ativa/desativa/trial/conversão).
/// - Verificação de validade temporal e hardware binding.
/// - Controle de ativações (máximo permitido).
/// - Verificação de capabilities habilitadas.
/// - Rastreamento de quotas de uso com enforcement configurável.
/// - Suporte a trial estruturado com conversão para licença full.
/// - Cálculo de saúde da licença (License Health Score).
///
/// Decisão de design:
/// - Validação de estado (ativa + não expirada) centralizada no método privado
///   EnsureUsable() para eliminar duplicação entre CheckCapability, TrackUsage e VerifyAt.
/// - Aggregate Root mantém invariantes de todas as sub-entidades (activations, capabilities, quotas)
///   pois elas existem exclusivamente no contexto de uma licença.
/// - LicenseType e Edition adicionados para suportar múltiplos modelos comerciais
///   sem alterar o ciclo de vida fundamental do aggregate.
/// - GracePeriodDays define tolerância operacional após expiração.
/// </summary>
public sealed class License : AggregateRoot<LicenseId>
{
    private readonly List<LicenseCapability> _capabilities = [];
    private readonly List<LicenseActivation> _activations = [];
    private readonly List<UsageQuota> _usageQuotas = [];

    private License() { }

    /// <summary>Chave pública da licença.</summary>
    public string LicenseKey { get; private set; } = string.Empty;

    /// <summary>Nome do cliente ou tenant proprietário da licença.</summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da emissão da licença.</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>Data/hora UTC de expiração da licença.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Limite máximo de ativações permitidas.</summary>
    public int MaxActivations { get; private set; }

    /// <summary>Indica se a licença está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Tipo da licença (Trial, Standard, Enterprise).</summary>
    public LicenseType Type { get; private set; }

    /// <summary>Edição comercial (Community, Professional, Enterprise, Unlimited).</summary>
    public LicenseEdition Edition { get; private set; }

    /// <summary>
    /// Dias de tolerância após expiração.
    /// Durante o grace period, a licença opera em modo degradado (read-only)
    /// sem bloquear recursos já ativos.
    /// </summary>
    public int GracePeriodDays { get; private set; }

    /// <summary>Indica se o trial já foi convertido para licença full.</summary>
    public bool TrialConverted { get; private set; }

    /// <summary>Data/hora UTC da conversão do trial (null se não convertido).</summary>
    public DateTimeOffset? TrialConvertedAt { get; private set; }

    /// <summary>Número de extensões de trial já concedidas.</summary>
    public int TrialExtensionCount { get; private set; }

    /// <summary>Modelo de deployment: SaaS, SelfHosted ou OnPremise.</summary>
    public DeploymentModel DeploymentModel { get; private set; }

    /// <summary>Modo de ativação: Online, Offline ou Hybrid.</summary>
    public ActivationMode ActivationMode { get; private set; }

    /// <summary>Modelo comercial: Perpetual, Subscription, UsageBased, Trial ou Internal.</summary>
    public CommercialModel CommercialModel { get; private set; }

    /// <summary>Modo de medição de uso: RealTime, Periodic, Manual ou Disabled.</summary>
    public MeteringMode MeteringMode { get; private set; }

    /// <summary>Status operacional da licença para exibição e auditoria.</summary>
    public LicenseStatus Status { get; private set; }

    /// <summary>Vínculo atual do hardware autorizado.</summary>
    public HardwareBinding? HardwareBinding { get; private set; }

    /// <summary>Capabilities incluídas na licença.</summary>
    public IReadOnlyList<LicenseCapability> Capabilities => _capabilities.AsReadOnly();

    /// <summary>Histórico de ativações emitidas.</summary>
    public IReadOnlyList<LicenseActivation> Activations => _activations.AsReadOnly();

    /// <summary>Quotas de uso associadas à licença.</summary>
    public IReadOnlyList<UsageQuota> UsageQuotas => _usageQuotas.AsReadOnly();

    /// <summary>Cria uma nova licença com capabilities e quotas iniciais.</summary>
    public static License Create(
        string licenseKey,
        string customerName,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        int maxActivations,
        IEnumerable<LicenseCapability>? capabilities = null,
        IEnumerable<UsageQuota>? usageQuotas = null,
        LicenseType type = LicenseType.Standard,
        LicenseEdition edition = LicenseEdition.Professional,
        int gracePeriodDays = 0,
        DeploymentModel deploymentModel = DeploymentModel.SaaS,
        ActivationMode activationMode = ActivationMode.Online,
        CommercialModel commercialModel = CommercialModel.Subscription,
        MeteringMode meteringMode = MeteringMode.RealTime)
    {
        if (maxActivations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxActivations), "Max activations must be greater than zero.");
        }

        if (gracePeriodDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gracePeriodDays), "Grace period must be zero or positive.");
        }

        var license = new License
        {
            Id = LicenseId.New(),
            LicenseKey = Guard.Against.NullOrWhiteSpace(licenseKey),
            CustomerName = Guard.Against.NullOrWhiteSpace(customerName),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            MaxActivations = maxActivations,
            IsActive = true,
            Type = type,
            Edition = edition,
            GracePeriodDays = gracePeriodDays,
            DeploymentModel = deploymentModel,
            ActivationMode = activationMode,
            CommercialModel = commercialModel,
            MeteringMode = meteringMode,
            Status = LicenseStatus.PendingActivation
        };

        if (capabilities is not null)
        {
            license._capabilities.AddRange(capabilities);
        }

        if (usageQuotas is not null)
        {
            license._usageQuotas.AddRange(usageQuotas);
        }

        return license;
    }

    /// <summary>
    /// Cria uma licença de trial com limites padrão para avaliação.
    /// Duração padrão: 30 dias. Limites: 25 APIs, 2 ambientes, 5 usuários.
    /// Todas as capabilities ficam habilitadas para maximizar avaliação.
    /// </summary>
    public static License CreateTrial(
        string licenseKey,
        string customerName,
        DateTimeOffset issuedAt,
        int trialDays = 30,
        IEnumerable<LicenseCapability>? capabilities = null,
        IEnumerable<UsageQuota>? usageQuotas = null)
    {
        if (trialDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trialDays), "Trial days must be greater than zero.");
        }

        var expiresAt = issuedAt.AddDays(trialDays);

        var defaultQuotas = usageQuotas?.ToList() ?? [];
        if (defaultQuotas.Count == 0)
        {
            defaultQuotas.Add(UsageQuota.Create("api.count", 25, enforcementLevel: EnforcementLevel.Hard));
            defaultQuotas.Add(UsageQuota.Create("environment.count", 2, enforcementLevel: EnforcementLevel.Hard));
            defaultQuotas.Add(UsageQuota.Create("user.count", 5, enforcementLevel: EnforcementLevel.Hard));
        }

        return Create(
            licenseKey,
            customerName,
            issuedAt,
            expiresAt,
            maxActivations: 1,
            capabilities,
            defaultQuotas,
            type: LicenseType.Trial,
            edition: LicenseEdition.Professional,
            gracePeriodDays: 7);
    }

    /// <summary>
    /// Estende o trial por dias adicionais. Máximo de 1 extensão permitida.
    /// Extensão padrão: 15 dias. Requer que a licença seja do tipo Trial e esteja ativa.
    /// </summary>
    public Result<Unit> ExtendTrial(int additionalDays, DateTimeOffset now)
    {
        if (Type != LicenseType.Trial)
        {
            return LicensingErrors.NotTrialLicense();
        }

        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        if (TrialConverted)
        {
            return LicensingErrors.TrialAlreadyConverted();
        }

        if (TrialExtensionCount >= 1)
        {
            return LicensingErrors.TrialExtensionLimitReached();
        }

        if (additionalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalDays), "Additional days must be greater than zero.");
        }

        ExpiresAt = ExpiresAt.AddDays(additionalDays);
        TrialExtensionCount++;
        return Unit.Value;
    }

    /// <summary>
    /// Converte um trial para licença full preservando dados e ativações existentes.
    /// Atualiza tipo, edição, expiração, limites e grace period.
    /// </summary>
    public Result<Unit> ConvertTrial(
        LicenseEdition edition,
        DateTimeOffset newExpiresAt,
        int newMaxActivations,
        int gracePeriodDays,
        DateTimeOffset convertedAt)
    {
        if (Type != LicenseType.Trial)
        {
            return LicensingErrors.NotTrialLicense();
        }

        if (TrialConverted)
        {
            return LicensingErrors.TrialAlreadyConverted();
        }

        if (newMaxActivations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newMaxActivations), "Max activations must be greater than zero.");
        }

        Type = LicenseType.Standard;
        Edition = edition;
        ExpiresAt = newExpiresAt;
        MaxActivations = newMaxActivations;
        GracePeriodDays = gracePeriodDays;
        TrialConverted = true;
        TrialConvertedAt = convertedAt;
        return Unit.Value;
    }

    /// <summary>Ativa a licença para o hardware informado.</summary>
    public Result<LicenseActivation> Activate(string hardwareFingerprint, string activatedBy, DateTimeOffset activatedAt)
    {
        var verification = VerifyAt(activatedAt, hardwareFingerprint);
        if (verification.IsFailure && verification.Error.Code != "Licensing.Hardware.NotBound")
        {
            return verification.Error;
        }

        if (_activations.Count(activation => activation.IsActive) >= MaxActivations)
        {
            return LicensingErrors.ActivationLimitReached(MaxActivations);
        }

        if (HardwareBinding is null)
        {
            HardwareBinding = HardwareBinding.Create(hardwareFingerprint, activatedAt);
        }
        else if (!HardwareBinding.Matches(hardwareFingerprint))
        {
            return LicensingErrors.HardwareMismatch();
        }

        HardwareBinding.MarkValidated(activatedAt);

        var activation = LicenseActivation.Create(hardwareFingerprint, activatedBy, activatedAt);
        _activations.Add(activation);
        Status = LicenseStatus.Active;
        return activation;
    }

    /// <summary>Verifica se a licença está utilizável para o hardware informado.</summary>
    public Result<Unit> VerifyAt(DateTimeOffset now, string hardwareFingerprint)
    {
        var usableCheck = EnsureUsable(now);
        if (usableCheck.IsFailure)
        {
            return usableCheck.Error;
        }

        if (HardwareBinding is null)
        {
            return LicensingErrors.HardwareNotBound();
        }

        if (!HardwareBinding.Matches(hardwareFingerprint))
        {
            return LicensingErrors.HardwareMismatch();
        }

        HardwareBinding.MarkValidated(now);
        return Unit.Value;
    }

    /// <summary>Verifica se a licença possui a capability solicitada.</summary>
    public Result<LicenseCapability> CheckCapability(string capabilityCode, DateTimeOffset now)
    {
        var usableCheck = EnsureUsable(now);
        if (usableCheck.IsFailure)
        {
            return usableCheck.Error;
        }

        var capability = _capabilities.SingleOrDefault(x => string.Equals(x.Code, capabilityCode, StringComparison.OrdinalIgnoreCase));
        if (capability is null || !capability.IsEnabled)
        {
            return LicensingErrors.CapabilityNotLicensed(capabilityCode);
        }

        return capability;
    }

    /// <summary>Registra consumo em uma quota de uso da licença.</summary>
    public Result<UsageQuota> TrackUsage(string metricCode, long quantity, DateTimeOffset now)
    {
        var usableCheck = EnsureUsable(now);
        if (usableCheck.IsFailure)
        {
            return usableCheck.Error;
        }

        var quota = _usageQuotas.SingleOrDefault(x => string.Equals(x.MetricCode, metricCode, StringComparison.OrdinalIgnoreCase));
        if (quota is null)
        {
            return LicensingErrors.QuotaNotFound(metricCode);
        }

        quota.Consume(quantity, now);

        if (quota.ShouldBlock(now))
        {
            return LicensingErrors.QuotaExceeded(metricCode, quota.CurrentUsage, quota.Limit);
        }

        return quota;
    }

    /// <summary>Desativa a licença para impedir novos usos e ativações.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Status = LicenseStatus.Suspended;
    }

    /// <summary>
    /// Revoga a licença permanentemente. Operação irreversível.
    /// Usada por vendor ops para revogar licenças comprometidas ou canceladas.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
        Status = LicenseStatus.Revoked;
    }

    /// <summary>
    /// Rehost da licença: remove o hardware binding atual para permitir
    /// ativação em novo hardware. Usado em migrações de servidor.
    /// Preserva histórico de ativações para auditoria.
    /// </summary>
    public Result<Unit> Rehost()
    {
        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        HardwareBinding = null;
        Status = LicenseStatus.PendingActivation;
        return Unit.Value;
    }

    /// <summary>Indica se a licença é do tipo trial.</summary>
    public bool IsTrial => Type == LicenseType.Trial;

    /// <summary>Indica se a licença expirou considerando o instante informado.</summary>
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>
    /// Indica se a licença está em grace period.
    /// Licença expirada mas dentro do período de tolerância configurado.
    /// </summary>
    public bool IsInGracePeriod(DateTimeOffset now)
        => IsExpired(now) && GracePeriodDays > 0 && now <= ExpiresAt.AddDays(GracePeriodDays);

    /// <summary>Dias restantes até a expiração (negativo se já expirou).</summary>
    public int DaysUntilExpiration(DateTimeOffset now) => (int)(ExpiresAt - now).TotalDays;

    /// <summary>
    /// Calcula o License Health Score (0.0 a 1.0).
    /// Considera: expiração, consumo de quotas, status de ativação e conectividade.
    ///
    /// Score composto:
    /// - 40% tempo restante até expiração
    /// - 40% média de consumo das quotas (invertido: menos consumo = melhor)
    /// - 20% estado geral (ativo, hardware bound)
    /// </summary>
    public decimal CalculateHealthScore(DateTimeOffset now)
    {
        if (!IsActive) return 0.0m;

        // Componente temporal (40%): 1.0 se > 90 dias, 0.0 se expirado
        var daysLeft = DaysUntilExpiration(now);
        var timeScore = daysLeft switch
        {
            <= 0 => IsInGracePeriod(now) ? 0.1m : 0.0m,
            <= 7 => 0.2m,
            <= 30 => 0.5m,
            <= 90 => 0.8m,
            _ => 1.0m
        };

        // Componente de consumo (40%): média invertida do uso das quotas
        var consumptionScore = 1.0m;
        if (_usageQuotas.Count > 0)
        {
            var avgUsage = _usageQuotas.Average(q => q.UsagePercentage);
            consumptionScore = Math.Max(0, 1.0m - avgUsage);
        }

        // Componente de estado (20%): ativo + hardware vinculado
        var stateScore = 0.0m;
        if (IsActive) stateScore += 0.5m;
        if (HardwareBinding is not null) stateScore += 0.5m;

        return Math.Round(timeScore * 0.4m + consumptionScore * 0.4m + stateScore * 0.2m, 2);
    }

    /// <summary>
    /// Valida pré-condição comum: licença deve estar ativa e não expirada.
    /// Centraliza a verificação de estado para eliminar duplicação entre
    /// CheckCapability, TrackUsage e VerifyAt (DRY).
    /// Considera grace period: licença expirada em grace period ainda é usável.
    /// </summary>
    private Result<Unit> EnsureUsable(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        if (ExpiresAt <= now && !IsInGracePeriod(now))
        {
            return LicensingErrors.LicenseExpired(ExpiresAt);
        }

        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de License.</summary>
public sealed record LicenseId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseId From(Guid id) => new(id);
}
