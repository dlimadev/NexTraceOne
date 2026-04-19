using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetAiGovernanceDashboard;

/// <summary>
/// Feature: GetAiGovernanceDashboard — dashboard de governança de IA da plataforma.
/// Lê de IConfigurationResolutionService para chaves de configuração de governança de IA.
/// </summary>
public static class GetAiGovernanceDashboard
{
    /// <summary>Query sem parâmetros — retorna dashboard de governança de IA.</summary>
    public sealed record Query() : IQuery<AiGovernanceDashboard>;

    /// <summary>Comando para atualizar configuração de governança de IA.</summary>
    public sealed record UpdateAiGovernanceConfig(
        bool GroundingCheckEnabled,
        double HallucinationFlagThreshold,
        bool FeedbackEnabled,
        bool AuditPromptsEnabled,
        bool BlockExternalModelsOnSensitiveData,
        int MaxTokenBudgetPerUser) : ICommand<AiGovernanceDashboard>;

    /// <summary>Handler de leitura do dashboard de governança de IA.</summary>
    public sealed class Handler(IConfigurationResolutionService configService, IDateTimeProvider clock) : IQueryHandler<Query, AiGovernanceDashboard>
    {
        public async Task<Result<AiGovernanceDashboard>> Handle(Query request, CancellationToken cancellationToken)
        {
            var groundingDto = await configService.ResolveEffectiveValueAsync(
                "ai.governance.grounding_check.enabled", ConfigurationScope.System, null, cancellationToken);
            var feedbackDto = await configService.ResolveEffectiveValueAsync(
                "ai.governance.feedback.enabled", ConfigurationScope.System, null, cancellationToken);
            var auditDto = await configService.ResolveEffectiveValueAsync(
                "ai.governance.audit_prompts.enabled", ConfigurationScope.System, null, cancellationToken);

            var grounding = !bool.TryParse(groundingDto?.EffectiveValue, out var gv) || gv;
            var feedback = !bool.TryParse(feedbackDto?.EffectiveValue, out var fv) || fv;
            var audit = !bool.TryParse(auditDto?.EffectiveValue, out var av) || av;

            var config = new AiGovernanceConfig(
                GroundingCheckEnabled: grounding,
                HallucinationFlagThreshold: 0.85,
                FeedbackEnabled: feedback,
                AuditPromptsEnabled: audit,
                BlockExternalModelsOnSensitiveData: true,
                MaxTokenBudgetPerUser: 50000);

            var modelStats = new List<AiModelUsageDto>
            {
                new("internal-llm", 0, 0),
                new("gpt-4o", 0, 0)
            };

            var feedbackCounts = new AiFeedbackCounts(Positive: 0, Negative: 0, Total: 0);

            var dashboard = new AiGovernanceDashboard(
                Config: config,
                ModelStats: modelStats,
                FeedbackCounts: feedbackCounts,
                GeneratedAt: clock.UtcNow);

            return Result<AiGovernanceDashboard>.Success(dashboard);
        }
    }

    /// <summary>Handler de atualização de configuração de governança de IA.</summary>
    public sealed class UpdateHandler(IDateTimeProvider clock) : ICommandHandler<UpdateAiGovernanceConfig, AiGovernanceDashboard>
    {
        public Task<Result<AiGovernanceDashboard>> Handle(UpdateAiGovernanceConfig request, CancellationToken cancellationToken)
        {
            var config = new AiGovernanceConfig(
                GroundingCheckEnabled: request.GroundingCheckEnabled,
                HallucinationFlagThreshold: request.HallucinationFlagThreshold,
                FeedbackEnabled: request.FeedbackEnabled,
                AuditPromptsEnabled: request.AuditPromptsEnabled,
                BlockExternalModelsOnSensitiveData: request.BlockExternalModelsOnSensitiveData,
                MaxTokenBudgetPerUser: request.MaxTokenBudgetPerUser);

            var dashboard = new AiGovernanceDashboard(
                Config: config,
                ModelStats: [],
                FeedbackCounts: new AiFeedbackCounts(0, 0, 0),
                GeneratedAt: clock.UtcNow);

            return Task.FromResult(Result<AiGovernanceDashboard>.Success(dashboard));
        }
    }

    /// <summary>Dashboard de governança de IA.</summary>
    public sealed record AiGovernanceDashboard(
        AiGovernanceConfig Config,
        IReadOnlyList<AiModelUsageDto> ModelStats,
        AiFeedbackCounts FeedbackCounts,
        DateTimeOffset GeneratedAt);

    /// <summary>Configuração de governança de IA.</summary>
    public sealed record AiGovernanceConfig(
        bool GroundingCheckEnabled,
        double HallucinationFlagThreshold,
        bool FeedbackEnabled,
        bool AuditPromptsEnabled,
        bool BlockExternalModelsOnSensitiveData,
        int MaxTokenBudgetPerUser);

    /// <summary>Estatísticas de uso por modelo de IA.</summary>
    public sealed record AiModelUsageDto(string ModelId, long TotalRequests, long TotalTokens);

    /// <summary>Contagens de feedback de IA.</summary>
    public sealed record AiFeedbackCounts(int Positive, int Negative, int Total);
}
