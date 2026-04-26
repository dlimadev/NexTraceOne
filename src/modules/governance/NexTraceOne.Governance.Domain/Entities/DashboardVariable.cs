namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Tipo de variável de dashboard — determina como o valor é resolvido e apresentado.
/// </summary>
public enum DashboardVariableType
{
    /// <summary>Seleciona um serviço do catálogo (resolve contra Catalog module).</summary>
    Service = 0,

    /// <summary>Seleciona uma equipa (resolve contra Governance.Teams).</summary>
    Team = 1,

    /// <summary>Seleciona um ambiente (resolve contra EnvironmentContext).</summary>
    Environment = 2,

    /// <summary>Seleciona um intervalo de tempo (ex: 1h, 6h, 24h, 7d, 30d).</summary>
    TimeRange = 3,

    /// <summary>Texto livre sem resolução automática.</summary>
    Text = 4,

    /// <summary>Lista estática de valores (enum definido pelo utilizador).</summary>
    Enum = 5
}

/// <summary>
/// Fonte de resolução dos valores de uma variável de dashboard.
/// </summary>
public enum DashboardVariableSource
{
    /// <summary>Valores estáticos definidos no campo <see cref="DashboardVariable.StaticValues"/>.</summary>
    Static = 0,

    /// <summary>Valores resolvidos contra o módulo Catalog (serviços, contratos).</summary>
    Catalog = 1,

    /// <summary>Valores resolvidos contra o contexto de ambiente do tenant.</summary>
    Environment = 2,

    /// <summary>Valores resolvidos contra o módulo Governance (equipas, domínios).</summary>
    Governance = 3
}

/// <summary>
/// Variável de dashboard (token) — permite parametrizar widgets com contexto dinâmico.
/// Suporta substituição de placeholders como $service, $team, $env, $timeRange em queries e títulos.
/// Serializado como JSONB em CustomDashboard.VariablesJson.
/// </summary>
public sealed record DashboardVariable(
    string Key,
    string Label,
    DashboardVariableType Type,
    string? DefaultValue,
    DashboardVariableSource Source,
    IReadOnlyList<string>? StaticValues = null);
