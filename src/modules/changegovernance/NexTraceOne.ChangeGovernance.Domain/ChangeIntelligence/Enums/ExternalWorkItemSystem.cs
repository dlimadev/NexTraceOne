namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Sistema externo de onde o work item (história/task) foi originado.
/// Suporta os principais sistemas de gestão de backlog do mercado.
/// </summary>
public enum ExternalWorkItemSystem
{
    /// <summary>Jira / Atlassian (ex: PROJ-1234).</summary>
    Jira = 0,

    /// <summary>Azure DevOps / Azure Boards (ex: AB#1234).</summary>
    AzureDevOps = 1,

    /// <summary>GitHub Issues (ex: #1234).</summary>
    GitHub = 2,

    /// <summary>Linear (ex: TEAM-1234).</summary>
    Linear = 3,

    /// <summary>GitLab Issues.</summary>
    GitLab = 4,

    /// <summary>ServiceNow (ex: RITM1234567).</summary>
    ServiceNow = 5,

    /// <summary>Sistema customizado / não listado.</summary>
    Custom = 99,
}
