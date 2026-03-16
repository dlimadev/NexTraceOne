namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Tipo de ownership que uma equipa exerce sobre um domínio de negócio.
/// Define o nível de responsabilidade e autoridade da equipa no domínio.
/// </summary>
public enum OwnershipType
{
    /// <summary>Ownership principal — a equipa é a responsável primária pelo domínio.</summary>
    Primary = 0,

    /// <summary>Ownership partilhado — a equipa partilha responsabilidade com outras equipas.</summary>
    Shared = 1,

    /// <summary>Ownership delegado — a equipa recebeu responsabilidade delegada temporária ou parcial.</summary>
    Delegated = 2
}
