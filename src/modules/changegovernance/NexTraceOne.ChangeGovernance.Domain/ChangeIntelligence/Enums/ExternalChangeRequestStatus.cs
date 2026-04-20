namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>Estado do ciclo de vida de um pedido de mudança externo (ServiceNow, Jira, etc.).</summary>
public enum ExternalChangeRequestStatus
{
    /// <summary>Pedido recebido mas ainda não processado.</summary>
    Pending = 0,

    /// <summary>Pedido ingerido com sucesso no NexTraceOne.</summary>
    Ingested = 1,

    /// <summary>Pedido vinculado a uma Release interna.</summary>
    Linked = 2,

    /// <summary>Pedido rejeitado — sistema não reconhecido ou dados inválidos.</summary>
    Rejected = 3
}
