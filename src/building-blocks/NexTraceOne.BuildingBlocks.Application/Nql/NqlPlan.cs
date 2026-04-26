namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>Operadores de comparação suportados em filtros NQL.</summary>
public enum NqlFilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEquals,
    LessThanOrEquals,
    Like
}

/// <summary>Filtro individual de uma cláusula WHERE em NQL.</summary>
public sealed record NqlFilter(
    string Field,
    NqlFilterOperator Operator,
    string Value);

/// <summary>Direção de ordenação NQL.</summary>
public enum NqlSortDirection { Asc, Desc }

/// <summary>Cláusula ORDER BY de um plano NQL.</summary>
public sealed record NqlOrderBy(string Field, NqlSortDirection Direction);

/// <summary>
/// Plano de execução de uma query NQL, produzido pelo <see cref="NqlParser"/>.
/// Imutável; criado apenas pelo parser.
/// </summary>
public sealed record NqlPlan(
    NqlEntity Entity,
    IReadOnlyList<NqlFilter> Filters,
    IReadOnlyList<string> GroupBy,
    NqlOrderBy? OrderBy,
    int Limit,
    string? RenderHint);
