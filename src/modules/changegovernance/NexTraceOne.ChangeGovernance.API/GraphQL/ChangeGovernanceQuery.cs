using HotChocolate.Types;
using HotChocolate;
using MediatR;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary;

namespace NexTraceOne.ChangeGovernance.API.GraphQL;

/// <summary>
/// Tipo GraphQL que representa o sumário de mudanças de uma equipa/ambiente.
/// Expõe métricas agregadas de change intelligence para decisão de produção.
/// Persona: Tech Lead, Architect, Executive.
/// </summary>
public sealed class ChangesSummaryGraphType
{
    /// <summary>Total de mudanças no período analisado.</summary>
    public int TotalChanges { get; init; }

    /// <summary>Número de mudanças validadas com evidências suficientes.</summary>
    public int ValidatedChanges { get; init; }

    /// <summary>Número de mudanças que requerem atenção ou revisão.</summary>
    public int ChangesNeedingAttention { get; init; }

    /// <summary>Número de mudanças com regressões suspeitas detetadas.</summary>
    public int SuspectedRegressions { get; init; }

    /// <summary>Número de mudanças correlacionadas com incidentes.</summary>
    public int ChangesCorrelatedWithIncidents { get; init; }
}

/// <summary>
/// Extensão do tipo Query raiz para expor dados de Change Intelligence via GraphQL.
/// Usa [ExtendObjectType] para schema stitching sem acoplamento direto ao Catalog.API.
/// Persona: Tech Lead, Architect, Executive.
/// </summary>
[ExtendObjectType("Query")]
public sealed class ChangeGovernanceQuery
{
    /// <summary>
    /// Retorna sumário de mudanças de uma equipa ou ambiente num período.
    /// Inclui total de mudanças, validadas, com atenção, regressões e correlações com incidentes.
    /// </summary>
    /// <param name="mediator">Mediator injetado pelo HotChocolate.</param>
    /// <param name="teamName">Nome da equipa para filtrar (opcional).</param>
    /// <param name="environment">Ambiente a filtrar (Development/Pre-Production/Production, opcional).</param>
    /// <param name="daysBack">Número de dias retroativos a considerar (padrão: 30).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task<ChangesSummaryGraphType?> GetChangesSummaryAsync(
        [Service] IMediator mediator,
        string? teamName = null,
        string? environment = null,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = new GetChangesSummary.Query(
            TeamName: teamName,
            Environment: environment,
            From: now.AddDays(-daysBack),
            To: now);

        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return null;

        var r = result.Value;
        return new ChangesSummaryGraphType
        {
            TotalChanges = r.TotalChanges,
            ValidatedChanges = r.ValidatedChanges,
            ChangesNeedingAttention = r.ChangesNeedingAttention,
            SuspectedRegressions = r.SuspectedRegressions,
            ChangesCorrelatedWithIncidents = r.ChangesCorrelatedWithIncidents
        };
    }
}
