namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de conector de fonte de dados externa.
/// Determina como o conteúdo é obtido e indexado no pipeline RAG.
/// </summary>
public enum ExternalDataSourceConnectorType
{
    /// <summary>API de busca web (ex: Brave Search, Serper, Google CSE).</summary>
    WebSearch,

    /// <summary>Repositório GitHub — indexa ficheiros via GitHub API.</summary>
    GitHub,

    /// <summary>Repositório GitLab — indexa ficheiros via GitLab API.</summary>
    GitLab,

    /// <summary>Directório local ou de rede — indexa ficheiros de texto/markdown/PDF.</summary>
    LocalDirectory,

    /// <summary>Endpoint HTTP/REST customizado — configuração genérica de API.</summary>
    CustomHttp,

    /// <summary>Atlassian Confluence — indexa páginas e espaços.</summary>
    Confluence,

    /// <summary>Notion workspace — indexa páginas e bases de dados.</summary>
    Notion,

    /// <summary>Azure DevOps — indexa repos e wikis.</summary>
    AzureDevOps,
}
