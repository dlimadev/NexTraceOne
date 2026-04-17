using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetEnvironmentPolicies;

/// <summary>
/// Feature: GetEnvironmentPolicies — políticas de acesso por ambiente.
/// Retorna políticas padrão: ProductionAccess, StagingAccess, DevelopmentAccess.
/// </summary>
public static class GetEnvironmentPolicies
{
    /// <summary>Query sem parâmetros — retorna políticas de ambiente disponíveis.</summary>
    public sealed record Query() : IQuery<EnvironmentPoliciesResponse>;

    /// <summary>Comando para atualizar política de um ambiente.</summary>
    public sealed record UpdateEnvironmentPolicy(
        string PolicyId,
        IReadOnlyList<string> AllowedRoles,
        IReadOnlyList<string> RequireJitFor,
        string? JitApprovalRequiredFrom,
        string Description) : ICommand<EnvironmentPoliciesResponse>;

    private static readonly List<EnvironmentPolicyDto> DefaultPolicies =
    [
        new(
            PolicyId: "ProductionAccess",
            Environment: "Production",
            AllowedRoles: ["platform-admin", "tech-lead", "release-manager"],
            RequireJitFor: ["deploy", "config-change", "database-access"],
            JitApprovalRequiredFrom: "tech-lead",
            Description: "Controls access to the production environment. JIT required for sensitive operations.",
            UpdatedAt: DateTimeOffset.UtcNow),
        new(
            PolicyId: "StagingAccess",
            Environment: "Staging",
            AllowedRoles: ["platform-admin", "tech-lead", "engineer"],
            RequireJitFor: ["database-access"],
            JitApprovalRequiredFrom: null,
            Description: "Controls access to the staging environment.",
            UpdatedAt: DateTimeOffset.UtcNow),
        new(
            PolicyId: "DevelopmentAccess",
            Environment: "Development",
            AllowedRoles: ["platform-admin", "tech-lead", "engineer", "viewer"],
            RequireJitFor: [],
            JitApprovalRequiredFrom: null,
            Description: "Controls access to the development environment.",
            UpdatedAt: DateTimeOffset.UtcNow)
    ];

    /// <summary>Handler de leitura das políticas de ambiente.</summary>
    public sealed class Handler : IQueryHandler<Query, EnvironmentPoliciesResponse>
    {
        public Task<Result<EnvironmentPoliciesResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var availableEnvironments = new List<string> { "Production", "Staging", "Development" };

            var response = new EnvironmentPoliciesResponse(
                Policies: DefaultPolicies,
                AvailableEnvironments: availableEnvironments,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<EnvironmentPoliciesResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização de política de ambiente.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateEnvironmentPolicy, EnvironmentPoliciesResponse>
    {
        public Task<Result<EnvironmentPoliciesResponse>> Handle(UpdateEnvironmentPolicy request, CancellationToken cancellationToken)
        {
            var updated = DefaultPolicies
                .Select(p => p.PolicyId == request.PolicyId
                    ? p with
                    {
                        AllowedRoles = request.AllowedRoles,
                        RequireJitFor = request.RequireJitFor,
                        JitApprovalRequiredFrom = request.JitApprovalRequiredFrom,
                        Description = request.Description,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }
                    : p)
                .ToList();

            var response = new EnvironmentPoliciesResponse(
                Policies: updated,
                AvailableEnvironments: ["Production", "Staging", "Development"],
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<EnvironmentPoliciesResponse>.Success(response));
        }
    }

    /// <summary>Resposta com políticas de acesso por ambiente.</summary>
    public sealed record EnvironmentPoliciesResponse(
        IReadOnlyList<EnvironmentPolicyDto> Policies,
        IReadOnlyList<string> AvailableEnvironments,
        DateTimeOffset GeneratedAt);

    /// <summary>Política de acesso para um ambiente específico.</summary>
    public sealed record EnvironmentPolicyDto(
        string PolicyId,
        string Environment,
        IReadOnlyList<string> AllowedRoles,
        IReadOnlyList<string> RequireJitFor,
        string? JitApprovalRequiredFrom,
        string Description,
        DateTimeOffset UpdatedAt);
}
