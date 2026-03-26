namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;

/// <summary>Status do ciclo de vida de uma instância de workflow.</summary>
public enum WorkflowStatus
{
    /// <summary>Rascunho — ainda não submetido para aprovação.</summary>
    Draft = 0,

    /// <summary>Pendente — aguardando início da revisão.</summary>
    Pending = 1,

    /// <summary>Em revisão — pelo menos um estágio em andamento.</summary>
    InReview = 2,

    /// <summary>Aprovado — todos os estágios concluídos com sucesso.</summary>
    Approved = 3,

    /// <summary>Rejeitado — pelo menos um estágio recusou a mudança.</summary>
    Rejected = 4,

    /// <summary>Cancelado — workflow interrompido antes da conclusão.</summary>
    Cancelled = 5
}

/// <summary>Status de um estágio individual dentro do workflow.</summary>
public enum StageStatus
{
    /// <summary>Pendente — aguardando início.</summary>
    Pending = 0,

    /// <summary>Em revisão — aprovadores estão avaliando.</summary>
    InReview = 1,

    /// <summary>Aprovado — estágio concluído com aprovação.</summary>
    Approved = 2,

    /// <summary>Rejeitado — estágio recusou a mudança.</summary>
    Rejected = 3,

    /// <summary>Ignorado — estágio pulado por regra de negócio.</summary>
    Skipped = 4
}

/// <summary>Ação de decisão tomada por um aprovador.</summary>
public enum ApprovalAction
{
    /// <summary>Aprovado — aprovador concordou com a mudança.</summary>
    Approved = 0,

    /// <summary>Rejeitado — aprovador recusou a mudança.</summary>
    Rejected = 1,

    /// <summary>Solicitou alterações — aprovador pediu correções antes de reavaliar.</summary>
    RequestedChanges = 2,

    /// <summary>Observação — comentário informativo sem aprovação ou rejeição.</summary>
    Observation = 3
}


/// <summary>
/// Valores válidos para o campo CiChecksResult do EvidencePack.
/// Representa o resultado consolidado dos checks de CI/CD.
/// </summary>
public static class CiChecksResults
{
    /// <summary>Todos os checks passaram.</summary>
    public const string Passed = "passed";

    /// <summary>Pelo menos um check falhou.</summary>
    public const string Failed = "failed";

    /// <summary>Alguns checks passaram, outros falharam.</summary>
    public const string Partial = "partial";

    /// <summary>Estado dos checks ainda não determinado.</summary>
    public const string Unknown = "unknown";

    /// <summary>Todos os valores válidos.</summary>
    public static readonly IReadOnlySet<string> ValidValues = new HashSet<string>
        { Passed, Failed, Partial, Unknown };
}
