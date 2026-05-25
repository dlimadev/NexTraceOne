using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ResolveDashboardVariables;

/// <summary>
/// Feature: ResolveDashboardVariables — resolve os valores possíveis para variáveis de dashboard
/// com base no tipo e fonte (Catalog, Governance, Environment, Static).
/// Permite ao frontend renderizar dropdowns dinâmicos estilo Grafana template variables.
/// </summary>
public static class ResolveDashboardVariables
{
    /// <summary>Query para resolver valores de variáveis de um dashboard.</summary>
    public sealed record Query(
        Guid DashboardId,
        string TenantId,
        string? EnvironmentId = null) : IQuery<Response>;

    /// <summary>Resposta com as variáveis e seus valores resolvidos.</summary>
    public sealed record Response(
        IReadOnlyList<ResolvedVariable> Variables);

    /// <summary>Variável resolvida com valores possíveis.</summary>
    public sealed record ResolvedVariable(
        string Key,
        string Label,
        string Type,
        string? DefaultValue,
        IReadOnlyList<string> Values,
        bool AllowMultiple);

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que resolve variáveis de dashboard.</summary>
    public sealed class Handler(
        ICustomDashboardRepository repository,
        IVariableValueResolver variableResolver) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound("CustomDashboard.NotFound", "Dashboard not found.");

            if (dashboard.TenantId != request.TenantId)
                return Error.Forbidden("CustomDashboard.Forbidden", "Access denied.");

            var resolved = new List<ResolvedVariable>();

            foreach (var variable in dashboard.Variables)
            {
                var values = await variableResolver.ResolveAsync(
                    variable.Type,
                    variable.Source,
                    variable.StaticValues,
                    request.TenantId,
                    request.EnvironmentId,
                    cancellationToken);

                resolved.Add(new ResolvedVariable(
                    Key: variable.Key,
                    Label: variable.Label,
                    Type: variable.Type.ToString(),
                    DefaultValue: variable.DefaultValue,
                    Values: values,
                    AllowMultiple: variable.Type is DashboardVariableType.Service
                                   or DashboardVariableType.Team
                                   or DashboardVariableType.Environment));
            }

            // Se o dashboard não tem variáveis definidas, retorna as variáveis padrão
            // (service, team, env) com valores resolvidos do tenant
            if (resolved.Count == 0)
            {
                var defaultVariables = await ResolveDefaultVariablesAsync(
                    request.TenantId, request.EnvironmentId, cancellationToken);
                resolved.AddRange(defaultVariables);
            }

            return Result<Response>.Success(new Response(resolved));
        }

        private async Task<IReadOnlyList<ResolvedVariable>> ResolveDefaultVariablesAsync(
            string tenantId,
            string? environmentId,
            CancellationToken cancellationToken)
        {
            var serviceValues = await variableResolver.ResolveAsync(
                DashboardVariableType.Service,
                DashboardVariableSource.Catalog,
                null,
                tenantId,
                environmentId,
                cancellationToken);

            var teamValues = await variableResolver.ResolveAsync(
                DashboardVariableType.Team,
                DashboardVariableSource.Governance,
                null,
                tenantId,
                environmentId,
                cancellationToken);

            var envValues = await variableResolver.ResolveAsync(
                DashboardVariableType.Environment,
                DashboardVariableSource.Environment,
                null,
                tenantId,
                environmentId,
                cancellationToken);

            return
            [
                new ResolvedVariable(
                    Key: "service",
                    Label: "Service",
                    Type: "Service",
                    DefaultValue: null,
                    Values: serviceValues,
                    AllowMultiple: true),
                new ResolvedVariable(
                    Key: "team",
                    Label: "Team",
                    Type: "Team",
                    DefaultValue: null,
                    Values: teamValues,
                    AllowMultiple: true),
                new ResolvedVariable(
                    Key: "env",
                    Label: "Environment",
                    Type: "Environment",
                    DefaultValue: environmentId,
                    Values: envValues,
                    AllowMultiple: false),
            ];
        }
    }
}
