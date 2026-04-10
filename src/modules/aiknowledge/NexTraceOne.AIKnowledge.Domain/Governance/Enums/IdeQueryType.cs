namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de consulta realizada por um developer via extensão IDE (VS Code / Visual Studio).
/// </summary>
public enum IdeQueryType
{
    /// <summary>Sugestão de contrato REST, SOAP ou AsyncAPI.</summary>
    ContractSuggestion = 1,

    /// <summary>Alerta de breaking change num contrato.</summary>
    BreakingChangeAlert = 2,

    /// <summary>Consulta de ownership de serviço ou contrato.</summary>
    OwnershipLookup = 3,

    /// <summary>Geração de cenários de teste.</summary>
    TestGeneration = 4,

    /// <summary>Consulta genérica ao assistente de IA.</summary>
    GeneralQuery = 5,

    /// <summary>Geração de código assistida por IA.</summary>
    CodeGeneration = 6
}
