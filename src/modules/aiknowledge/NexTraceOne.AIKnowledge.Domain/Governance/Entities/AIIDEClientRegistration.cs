using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Representa o registo de um cliente IDE autorizado a aceder ao assistente de IA do NexTraceOne.
/// Cada registo vincula um utilizador, tipo de cliente IDE e versão a um conjunto de capacidades governadas.
///
/// Invariantes:
/// - UserId é obrigatório e identifica o proprietário do registo.
/// - ClientType deve ser VsCode ou VisualStudio.
/// - O registo inicia ativo e pode ser revogado.
/// - ClientVersion é opcional e usado para telemetria.
/// </summary>
public sealed class AIIDEClientRegistration : AuditableEntity<AIIDEClientRegistrationId>
{
    private AIIDEClientRegistration() { }

    /// <summary>Identificador do utilizador que registou o cliente IDE.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Nome de exibição do utilizador.</summary>
    public string UserDisplayName { get; private set; } = string.Empty;

    /// <summary>Tipo de cliente IDE (VsCode ou VisualStudio).</summary>
    public AIClientType ClientType { get; private set; }

    /// <summary>Versão do cliente/extensão IDE (informativo).</summary>
    public string? ClientVersion { get; private set; }

    /// <summary>Identificador único da instalação do cliente (para correlação).</summary>
    public string? DeviceIdentifier { get; private set; }

    /// <summary>Data/hora UTC do último acesso registado.</summary>
    public DateTimeOffset? LastAccessAt { get; private set; }

    /// <summary>Indica se o registo está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Motivo da revogação, se aplicável.</summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Regista um novo cliente IDE com validações de invariantes.
    /// O registo inicia ativo e pronto para uso governado.
    /// </summary>
    public static AIIDEClientRegistration Register(
        string userId,
        string userDisplayName,
        AIClientType clientType,
        string? clientVersion,
        string? deviceIdentifier)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(userDisplayName);

        return new AIIDEClientRegistration
        {
            Id = AIIDEClientRegistrationId.New(),
            UserId = userId,
            UserDisplayName = userDisplayName,
            ClientType = clientType,
            ClientVersion = clientVersion,
            DeviceIdentifier = deviceIdentifier,
            IsActive = true
        };
    }

    /// <summary>
    /// Regista um novo acesso do cliente IDE, atualizando timestamp e versão.
    /// </summary>
    public Result<Unit> RecordAccess(DateTimeOffset accessAt, string? clientVersion)
    {
        LastAccessAt = accessAt;
        if (!string.IsNullOrWhiteSpace(clientVersion))
            ClientVersion = clientVersion;
        return Unit.Value;
    }

    /// <summary>
    /// Revoga o registo do cliente IDE, desativando o acesso.
    /// </summary>
    public Result<Unit> Revoke(string reason)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        IsActive = false;
        RevocationReason = reason;
        return Unit.Value;
    }

    /// <summary>
    /// Reativa o registo do cliente IDE.
    /// </summary>
    public Result<Unit> Reactivate()
    {
        IsActive = true;
        RevocationReason = null;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AIIDEClientRegistration.</summary>
public sealed record AIIDEClientRegistrationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIIDEClientRegistrationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIIDEClientRegistrationId From(Guid id) => new(id);
}
