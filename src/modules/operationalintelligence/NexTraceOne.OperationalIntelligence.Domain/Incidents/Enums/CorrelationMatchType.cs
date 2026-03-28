namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Tipo de correspondência utilizado pelo motor de correlação dinâmica incidente↔mudança.
/// Determina o critério pelo qual a correlação foi estabelecida.
/// </summary>
public enum CorrelationMatchType
{
    /// <summary>Correspondência exata de serviço — ServiceId ou ServiceName coincide.</summary>
    ExactServiceMatch = 0,

    /// <summary>Correspondência por dependência — serviço aparece como dependente ou consumidor.</summary>
    DependencyMatch = 1,

    /// <summary>Correspondência por proximidade temporal apenas — sem correspondência de serviço.</summary>
    TimeProximity = 2
}
