namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Tipo de alvo de um scan de segurança.</summary>
public enum ScanTarget
{
    /// <summary>Código gerado por scaffold ou IA.</summary>
    GeneratedCode = 0,

    /// <summary>Contrato OpenAPI/AsyncAPI/WSDL.</summary>
    Contract = 1,

    /// <summary>Template de serviço.</summary>
    Template = 2,

    /// <summary>Dependência de pacote.</summary>
    Dependency = 3
}
