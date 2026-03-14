namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Modo de ativação que define como a licença é validada e ativada.
/// SaaS tipicamente usa ativação automática; On-Premise pode requerer
/// ativação offline com arquivo de licença.
/// </summary>
public enum ActivationMode
{
    /// <summary>Ativação automática via API online (padrão para SaaS).</summary>
    Online = 0,

    /// <summary>Ativação via arquivo de licença sem necessidade de conectividade.</summary>
    Offline = 1,

    /// <summary>Suporta ambos os modos, priorizando online quando disponível.</summary>
    Hybrid = 2
}
