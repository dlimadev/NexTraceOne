namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tipos semânticos de nós no grafo de engenharia.
/// Cada tipo representa uma categoria distinta de ativo técnico ou organizacional,
/// permitindo visualizações hierárquicas, filtros por persona e overlays contextuais.
/// </summary>
public enum NodeType
{
    /// <summary>Domínio de negócio — agrupamento de alto nível para executivos e gestores.</summary>
    Domain = 0,

    /// <summary>Equipe ou squad responsável por um conjunto de serviços.</summary>
    Team = 1,

    /// <summary>Serviço técnico (microservice, monolito, etc.).</summary>
    Service = 2,

    /// <summary>API publicada por um serviço — nó central do grafo.</summary>
    Api = 3,

    /// <summary>Versão específica de uma API — vinculada a contrato e release.</summary>
    ApiVersion = 4,

    /// <summary>Endpoint individual dentro de uma API.</summary>
    Endpoint = 5,

    /// <summary>Ambiente de execução (Production, Staging, Development).</summary>
    Environment = 6,

    // ── Novos tipos de nós para core systems / mainframe ──

    /// <summary>Sistema mainframe — LPAR, sysplex ou região como nó de infraestrutura.</summary>
    MainframeSystem = 7,

    /// <summary>Batch Job — job de execução batch no grafo de dependências.</summary>
    BatchJob = 8,

    /// <summary>Transação CICS — transação online mainframe como nó do grafo.</summary>
    CicsTransaction = 9,

    /// <summary>Fila MQ — queue IBM MQ como nó do grafo de mensageria.</summary>
    MqQueue = 10,

    /// <summary>Copybook — definição de layout de dados como nó do grafo.</summary>
    Copybook = 11
}
