using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiCapabilityMaturityReport;

/// <summary>
/// Wave BD.3 — GetAiCapabilityMaturityReport
/// Avalia a maturidade de adopção das capacidades de IA pelo tenant: agents activos,
/// skills registadas, memória organizacional e feedback loops. Classifica o tenant
/// num nível de maturidade: Innovating / Scaling / Adopting / Exploring / Initiating.
/// Permite ao Platform Admin e AI Lead acompanhar a evolução organizacional da IA.
/// </summary>
public static class GetAiCapabilityMaturityReport
{
    public sealed record Query(
        Guid TenantId,
        int LookbackDays = 90,
        int PioneerThresholdPct = 20,
        int MinTeamExecutions = 10) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
            RuleFor(x => x.PioneerThresholdPct).InclusiveBetween(1, 100);
            RuleFor(x => x.MinTeamExecutions).InclusiveBetween(1, 1000);
        }
    }

    public sealed class Handler(
        IAiAgentPerformanceMetricRepository metricRepo,
        IAiSkillRepository skillRepo,
        IOrganizationalMemoryRepository memoryRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var metricsTask = metricRepo.ListByTenantAsync(request.TenantId, ct);
            var skillsTask = skillRepo.ListAsync(null, null, request.TenantId, ct);
            var memoryNodesTask = memoryRepo.ListByTypeAsync("decision", request.TenantId, ct);

            await Task.WhenAll(metricsTask, skillsTask, memoryNodesTask);

            var metrics = metricsTask.Result;
            var skills = skillsTask.Result;
            var decisionNodes = memoryNodesTask.Result;

            // Agent dimension
            int totalAgents = metrics.Count;
            int activeAgents = metrics.Count(m => m.TotalExecutions >= request.MinTeamExecutions);
            long totalExecutions = metrics.Sum(m => m.TotalExecutions);
            long totalWithFeedback = metrics.Sum(m => m.ExecutionsWithFeedback);
            int agentsWithRl = metrics.Count(m => m.RlCyclesCompleted > 0);

            // Skill dimension
            int totalSkills = skills.Count;
            int publishedSkills = skills.Count(s => s.Status == SkillStatus.Active);

            // Memory dimension
            int memoryNodes = decisionNodes.Count;

            // Feedback loop quality
            double feedbackLoopScore = totalExecutions > 0
                ? Math.Round((double)totalWithFeedback / totalExecutions, 3)
                : 0.0;

            // Maturity score (0-100)
            double agentScore = Math.Min(activeAgents / 5.0, 1.0) * 30;
            double skillScore = Math.Min(publishedSkills / 10.0, 1.0) * 25;
            double memoryScore = Math.Min(memoryNodes / 20.0, 1.0) * 25;
            double feedbackScore = feedbackLoopScore * 20;

            double maturityScore = Math.Round(agentScore + skillScore + memoryScore + feedbackScore, 1);

            string maturityLevel = ClassifyMaturityLevel(maturityScore);

            double rlAdoptionPct = totalAgents > 0
                ? Math.Round((double)agentsWithRl / totalAgents * 100, 1)
                : 0.0;

            bool hasPioneerAdoption = rlAdoptionPct >= request.PioneerThresholdPct;

            var dimensions = new List<MaturityDimension>
            {
                new("AgentDeployment", activeAgents, totalAgents,
                    Math.Round(agentScore / 30 * 100, 1), "Active agents with sufficient executions"),
                new("SkillLibrary", publishedSkills, totalSkills,
                    Math.Round(skillScore / 25 * 100, 1), "Published skills available to teams"),
                new("OrganizationalMemory", memoryNodes, memoryNodes,
                    Math.Round(memoryScore / 25 * 100, 1), "Decision nodes in organizational memory"),
                new("FeedbackLoops", (int)(totalWithFeedback), (int)(totalExecutions),
                    Math.Round(feedbackLoopScore * 100, 1), "Executions with feedback captured"),
            };

            return new Response(
                TenantId: request.TenantId,
                MaturityScore: maturityScore,
                MaturityLevel: maturityLevel,
                TotalActiveAgents: activeAgents,
                TotalAgents: totalAgents,
                TotalPublishedSkills: publishedSkills,
                TotalSkills: totalSkills,
                OrganizationalMemoryNodes: memoryNodes,
                FeedbackLoopScore: feedbackLoopScore,
                RlAdoptionPct: rlAdoptionPct,
                HasPioneerAdoption: hasPioneerAdoption,
                MaturityDimensions: dimensions.AsReadOnly(),
                LookbackDays: request.LookbackDays);
        }

        private static string ClassifyMaturityLevel(double score) => score switch
        {
            >= 80 => "Innovating",
            >= 60 => "Scaling",
            >= 40 => "Adopting",
            >= 20 => "Exploring",
            _ => "Initiating"
        };
    }

    public sealed record MaturityDimension(
        string DimensionName,
        int ActualValue,
        int MaxObserved,
        double ScorePct,
        string Description);

    public sealed record Response(
        Guid TenantId,
        double MaturityScore,
        string MaturityLevel,
        int TotalActiveAgents,
        int TotalAgents,
        int TotalPublishedSkills,
        int TotalSkills,
        int OrganizationalMemoryNodes,
        double FeedbackLoopScore,
        double RlAdoptionPct,
        bool HasPioneerAdoption,
        IReadOnlyList<MaturityDimension> MaturityDimensions,
        int LookbackDays);
}
