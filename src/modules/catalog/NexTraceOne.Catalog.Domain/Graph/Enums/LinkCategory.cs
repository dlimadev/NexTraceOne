namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Categoria de um link associado a um serviço ou contrato.
/// Permite classificação semântica e renderização com ícone adequado na UI.
/// </summary>
public enum LinkCategory
{
    Repository,
    Documentation,
    CiCd,
    Monitoring,
    Wiki,
    SwaggerUi,
    ApiPortal,
    Backstage,
    Adr,
    Runbook,
    Changelog,
    Dashboard,
    Other
}
