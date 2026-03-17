using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Define as capacidades habilitadas para um tipo de cliente IDE e persona.
/// Controla quais comandos, modelos e contextos estão disponíveis para cada combinação
/// de tipo de cliente IDE e perfil funcional.
///
/// Invariantes:
/// - ClientType deve ser VsCode ou VisualStudio.
/// - AllowedCommands é lista separada por vírgula de AIIDECommandType.
/// - AllowedContextScopes define quais domínios o IDE pode consultar.
/// - A política inicia ativa.
/// </summary>
public sealed class AIIDECapabilityPolicy : AuditableEntity<AIIDECapabilityPolicyId>
{
    private AIIDECapabilityPolicy() { }

    /// <summary>Tipo de cliente IDE ao qual a política se aplica.</summary>
    public AIClientType ClientType { get; private set; }

    /// <summary>Persona à qual a política se aplica (null = todas as personas).</summary>
    public string? Persona { get; private set; }

    /// <summary>Comandos permitidos separados por vírgula (ex: "Chat,ServiceLookup,ContractLookup").</summary>
    public string AllowedCommands { get; private set; } = string.Empty;

    /// <summary>Escopos de contexto permitidos separados por vírgula (ex: "services,contracts,incidents").</summary>
    public string AllowedContextScopes { get; private set; } = string.Empty;

    /// <summary>IDs de modelos permitidos separados por vírgula (vazio = sem restrição).</summary>
    public string AllowedModelIds { get; private set; } = string.Empty;

    /// <summary>Indica se geração de contratos está permitida.</summary>
    public bool AllowContractGeneration { get; private set; }

    /// <summary>Indica se troubleshooting de incidentes está permitido.</summary>
    public bool AllowIncidentTroubleshooting { get; private set; }

    /// <summary>Indica se uso de IA externa está permitido.</summary>
    public bool AllowExternalAI { get; private set; }

    /// <summary>Limite máximo de tokens por requisição (0 = sem limite específico).</summary>
    public int MaxTokensPerRequest { get; private set; }

    /// <summary>Indica se a política está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria uma nova política de capacidade IDE com validações de invariantes.
    /// </summary>
    public static AIIDECapabilityPolicy Create(
        AIClientType clientType,
        string? persona,
        string allowedCommands,
        string allowedContextScopes,
        bool allowContractGeneration,
        bool allowIncidentTroubleshooting,
        bool allowExternalAI,
        int maxTokensPerRequest)
    {
        return new AIIDECapabilityPolicy
        {
            Id = AIIDECapabilityPolicyId.New(),
            ClientType = clientType,
            Persona = persona,
            AllowedCommands = allowedCommands ?? string.Empty,
            AllowedContextScopes = allowedContextScopes ?? string.Empty,
            AllowedModelIds = string.Empty,
            AllowContractGeneration = allowContractGeneration,
            AllowIncidentTroubleshooting = allowIncidentTroubleshooting,
            AllowExternalAI = allowExternalAI,
            MaxTokensPerRequest = maxTokensPerRequest,
            IsActive = true
        };
    }

    /// <summary>
    /// Atualiza os parâmetros da política de capacidade IDE.
    /// </summary>
    public Result<Unit> Update(
        string allowedCommands,
        string allowedContextScopes,
        bool allowContractGeneration,
        bool allowIncidentTroubleshooting,
        bool allowExternalAI,
        int maxTokensPerRequest)
    {
        AllowedCommands = allowedCommands ?? string.Empty;
        AllowedContextScopes = allowedContextScopes ?? string.Empty;
        AllowContractGeneration = allowContractGeneration;
        AllowIncidentTroubleshooting = allowIncidentTroubleshooting;
        AllowExternalAI = allowExternalAI;
        MaxTokensPerRequest = maxTokensPerRequest;
        return Unit.Value;
    }

    /// <summary>
    /// Define os modelos permitidos para esta política IDE.
    /// </summary>
    public Result<Unit> SetAllowedModels(string modelIds)
    {
        AllowedModelIds = modelIds ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Desativa a política de capacidade IDE.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>
    /// Reativa a política de capacidade IDE.
    /// </summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AIIDECapabilityPolicy.</summary>
public sealed record AIIDECapabilityPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIIDECapabilityPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIIDECapabilityPolicyId From(Guid id) => new(id);
}
