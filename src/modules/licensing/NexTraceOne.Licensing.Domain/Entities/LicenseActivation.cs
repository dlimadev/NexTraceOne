using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Registro histórico de ativação de uma licença em um hardware autorizado.
/// </summary>
public sealed class LicenseActivation : Entity<LicenseActivationId>
{
    private LicenseActivation() { }

    /// <summary>Fingerprint do hardware que recebeu a ativação.</summary>
    public string HardwareFingerprint { get; private set; } = string.Empty;

    /// <summary>Origem responsável pela ativação.</summary>
    public string ActivatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da ativação.</summary>
    public DateTimeOffset ActivatedAt { get; private set; }

    /// <summary>Indica se a ativação continua válida.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Cria um novo registro de ativação de licença.</summary>
    public static LicenseActivation Create(string hardwareFingerprint, string activatedBy, DateTimeOffset activatedAt)
        => new()
        {
            Id = LicenseActivationId.New(),
            HardwareFingerprint = Guard.Against.NullOrWhiteSpace(hardwareFingerprint),
            ActivatedBy = Guard.Against.NullOrWhiteSpace(activatedBy),
            ActivatedAt = activatedAt,
            IsActive = true
        };

    /// <summary>Revoga uma ativação previamente emitida.</summary>
    public void Revoke() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de LicenseActivation.</summary>
public sealed record LicenseActivationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseActivationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseActivationId From(Guid id) => new(id);
}
