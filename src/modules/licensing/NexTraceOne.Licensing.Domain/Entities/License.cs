using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma licença ativa da plataforma.
/// Controla ativação em hardware, capabilities e quotas de consumo.
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
        IEnumerable<UsageQuota>? usageQuotas = null)
    {
        if (maxActivations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxActivations), "Max activations must be greater than zero.");
        }

        var license = new License
        {
            Id = LicenseId.New(),
            LicenseKey = Guard.Against.NullOrWhiteSpace(licenseKey),
            CustomerName = Guard.Against.NullOrWhiteSpace(customerName),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            MaxActivations = maxActivations,
            IsActive = true
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
        return activation;
    }

    /// <summary>Verifica se a licença está utilizável para o hardware informado.</summary>
    public Result<Unit> VerifyAt(DateTimeOffset now, string hardwareFingerprint)
    {
        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        if (ExpiresAt <= now)
        {
            return LicensingErrors.LicenseExpired(ExpiresAt);
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
        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        if (ExpiresAt <= now)
        {
            return LicensingErrors.LicenseExpired(ExpiresAt);
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
        if (!IsActive)
        {
            return LicensingErrors.LicenseInactive();
        }

        if (ExpiresAt <= now)
        {
            return LicensingErrors.LicenseExpired(ExpiresAt);
        }

        var quota = _usageQuotas.SingleOrDefault(x => string.Equals(x.MetricCode, metricCode, StringComparison.OrdinalIgnoreCase));
        if (quota is null)
        {
            return LicensingErrors.QuotaNotFound(metricCode);
        }

        quota.Consume(quantity);

        if (quota.IsExceeded())
        {
            return LicensingErrors.QuotaExceeded(metricCode, quota.CurrentUsage, quota.Limit);
        }

        return quota;
    }

    /// <summary>Desativa a licença para impedir novos usos e ativações.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de License.</summary>
public sealed record LicenseId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseId From(Guid id) => new(id);
}
