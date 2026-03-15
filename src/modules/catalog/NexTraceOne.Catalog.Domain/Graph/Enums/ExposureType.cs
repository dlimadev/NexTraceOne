namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tipo de exposição do serviço — determina quem pode aceder ao serviço.
/// Relevante para governança, segurança e auditoria.
/// </summary>
public enum ExposureType
{
    /// <summary>Serviço interno — acessível apenas dentro da organização.</summary>
    Internal = 0,

    /// <summary>Serviço externo — exposto a consumidores externos.</summary>
    External = 1,

    /// <summary>Serviço partner — exposto a parceiros selecionados.</summary>
    Partner = 2
}
