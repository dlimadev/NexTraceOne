namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Modelo de deployment que determina como a plataforma é entregue e operada.
/// Influencia o modo de ativação, validação, telemetria e operação comercial
/// sem duplicar a lógica central de licenciamento.
/// </summary>
public enum DeploymentModel
{
    /// <summary>Operação SaaS multi-tenant gerenciada pela NexTraceOne.</summary>
    SaaS = 0,

    /// <summary>Instalação self-hosted pelo cliente com conectividade opcional.</summary>
    SelfHosted = 1,

    /// <summary>Instalação on-premise em ambiente isolado (air-gapped).</summary>
    OnPremise = 2
}
