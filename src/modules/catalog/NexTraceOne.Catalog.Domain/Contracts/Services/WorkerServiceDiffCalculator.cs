using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo de diff semântico entre especificações
/// de Background Service Contracts (workers, jobs, schedulers).
/// Compara metadados estruturais em vez de paths HTTP, PortTypes SOAP ou canais AsyncAPI.
///
/// Breaking changes em background services:
/// - Mudança de serviceName (renaming do processo)
/// - Mudança de triggerType
/// - Mudança de scheduleExpression
/// - Remoção de inputs previamente declarados
/// - Remoção de outputs previamente declarados
/// - Mudança de allowsConcurrency de true para false (pode afectar orchestration)
///
/// Additive changes:
/// - Adição de novos inputs/outputs
/// - Adição de novos side effects declarados
/// - Adição de timeoutExpression onde não existia
///
/// Non-breaking:
/// - Mudança de categoria
/// - Alteração de side effects sem remoção
/// - Atualização de timeoutExpression
/// </summary>
public static class WorkerServiceDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico entre duas especificações de Background Service Contract.
    /// Detecta mudanças estruturais — trigger, schedule, inputs, outputs, side effects — e
    /// classifica o nível geral (Breaking, Additive ou NonBreaking).
    /// </summary>
    /// <param name="baseSpecContent">Conteúdo JSON da spec base (versão anterior).</param>
    /// <param name="targetSpecContent">Conteúdo JSON da spec alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(string baseSpecContent, string targetSpecContent)
    {
        var baseSpec = BackgroundServiceSpecParser.Parse(baseSpecContent);
        var targetSpec = BackgroundServiceSpecParser.Parse(targetSpecContent);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // ServiceName renaming — breaking: consumers/orchestration reference the service by name
        if (!string.IsNullOrWhiteSpace(baseSpec.ServiceName)
            && !string.IsNullOrWhiteSpace(targetSpec.ServiceName)
            && !string.Equals(baseSpec.ServiceName, targetSpec.ServiceName, StringComparison.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "ServiceNameChanged",
                "ServiceName",
                targetSpec.ServiceName,
                $"ServiceName changed from '{baseSpec.ServiceName}' to '{targetSpec.ServiceName}'.",
                true));
        }

        // TriggerType changed — breaking: scheduling/orchestration systems depend on this
        if (!string.Equals(baseSpec.TriggerType, targetSpec.TriggerType, StringComparison.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "TriggerTypeChanged",
                "TriggerType",
                targetSpec.TriggerType,
                $"TriggerType changed from '{baseSpec.TriggerType}' to '{targetSpec.TriggerType}'.",
                true));
        }

        // ScheduleExpression changed — breaking: scheduler depends on cron/interval expressions
        if (baseSpec.ScheduleExpression is not null || targetSpec.ScheduleExpression is not null)
        {
            var baseSchedule = baseSpec.ScheduleExpression ?? "(none)";
            var targetSchedule = targetSpec.ScheduleExpression ?? "(none)";
            if (!string.Equals(baseSchedule, targetSchedule, StringComparison.OrdinalIgnoreCase))
            {
                breaking.Add(new ChangeEntry(
                    "ScheduleExpressionChanged",
                    "ScheduleExpression",
                    targetSchedule,
                    $"ScheduleExpression changed from '{baseSchedule}' to '{targetSchedule}'.",
                    true));
            }
        }

        // AllowsConcurrency changed from true → false — breaking: orchestration may rely on concurrent execution
        if (baseSpec.AllowsConcurrency && !targetSpec.AllowsConcurrency)
        {
            breaking.Add(new ChangeEntry(
                "ConcurrencyDisabled",
                "AllowsConcurrency",
                "false",
                "AllowsConcurrency changed from true to false. Orchestration systems relying on concurrent execution may be affected.",
                true));
        }
        else if (!baseSpec.AllowsConcurrency && targetSpec.AllowsConcurrency)
        {
            // Enabling concurrency is non-breaking (additive capability)
            additive.Add(new ChangeEntry(
                "ConcurrencyEnabled",
                "AllowsConcurrency",
                "true",
                "AllowsConcurrency changed from false to true.",
                false));
        }

        // Inputs: removed → breaking; added → additive
        ComputeParameterDiff("Input", baseSpec.Inputs, targetSpec.Inputs, breaking, additive);

        // Outputs: removed → breaking; added → additive
        ComputeParameterDiff("Output", baseSpec.Outputs, targetSpec.Outputs, breaking, additive);

        // SideEffects: removed → non-breaking (internal); added → additive
        ComputeSideEffectsDiff(baseSpec.SideEffects, targetSpec.SideEffects, additive, nonBreaking);

        // TimeoutExpression: added → additive; changed → non-breaking; removed → breaking
        ComputeTimeoutDiff(baseSpec.TimeoutExpression, targetSpec.TimeoutExpression, breaking, additive, nonBreaking);

        // Category changed — non-breaking: classification only
        if (!string.IsNullOrWhiteSpace(baseSpec.Category)
            && !string.Equals(baseSpec.Category, targetSpec.Category, StringComparison.OrdinalIgnoreCase))
        {
            nonBreaking.Add(new ChangeEntry(
                "CategoryChanged",
                "Category",
                targetSpec.Category,
                $"Category changed from '{baseSpec.Category}' to '{targetSpec.Category}'.",
                false));
        }

        var changeLevel = breaking.Count > 0
            ? ChangeLevel.Breaking
            : additive.Count > 0
                ? ChangeLevel.Additive
                : ChangeLevel.NonBreaking;

        return new OpenApiDiffCalculator.DiffResult(
            breaking.AsReadOnly(),
            additive.AsReadOnly(),
            nonBreaking.AsReadOnly(),
            changeLevel);
    }

    /// <summary>
    /// Compara dois dicionários de parâmetros (inputs ou outputs).
    /// Remoção → breaking; adição → additive.
    /// </summary>
    private static void ComputeParameterDiff(
        string paramType,
        IReadOnlyDictionary<string, string> baseParams,
        IReadOnlyDictionary<string, string> targetParams,
        List<ChangeEntry> breaking,
        List<ChangeEntry> additive)
    {
        foreach (var key in baseParams.Keys.Where(k => !targetParams.ContainsKey(k)))
        {
            breaking.Add(new ChangeEntry(
                $"{paramType}Removed",
                paramType,
                key,
                $"{paramType} parameter '{key}' was removed.",
                true));
        }

        foreach (var key in targetParams.Keys.Where(k => !baseParams.ContainsKey(k)))
        {
            additive.Add(new ChangeEntry(
                $"{paramType}Added",
                paramType,
                key,
                $"{paramType} parameter '{key}' was added.",
                false));
        }
    }

    /// <summary>
    /// Compara listas de side effects.
    /// Adicionados → additive; removidos → non-breaking (internal declarations only).
    /// </summary>
    private static void ComputeSideEffectsDiff(
        IReadOnlyList<string> baseEffects,
        IReadOnlyList<string> targetEffects,
        List<ChangeEntry> additive,
        List<ChangeEntry> nonBreaking)
    {
        var baseSet = new HashSet<string>(baseEffects, StringComparer.OrdinalIgnoreCase);
        var targetSet = new HashSet<string>(targetEffects, StringComparer.OrdinalIgnoreCase);

        foreach (var effect in targetSet.Where(e => !baseSet.Contains(e)))
        {
            additive.Add(new ChangeEntry(
                "SideEffectAdded", "SideEffects", effect,
                $"Side effect '{effect}' was added.", false));
        }

        foreach (var effect in baseSet.Where(e => !targetSet.Contains(e)))
        {
            nonBreaking.Add(new ChangeEntry(
                "SideEffectRemoved", "SideEffects", effect,
                $"Side effect '{effect}' was removed from declaration.", false));
        }
    }

    /// <summary>
    /// Compara timeoutExpression entre versões.
    /// Adicionado → additive; removido → breaking; mudança → non-breaking.
    /// </summary>
    private static void ComputeTimeoutDiff(
        string? baseTimeout,
        string? targetTimeout,
        List<ChangeEntry> breaking,
        List<ChangeEntry> additive,
        List<ChangeEntry> nonBreaking)
    {
        if (baseTimeout is null && targetTimeout is not null)
        {
            additive.Add(new ChangeEntry(
                "TimeoutAdded", "TimeoutExpression", targetTimeout,
                $"TimeoutExpression '{targetTimeout}' was added.", false));
        }
        else if (baseTimeout is not null && targetTimeout is null)
        {
            breaking.Add(new ChangeEntry(
                "TimeoutRemoved", "TimeoutExpression", "(none)",
                $"TimeoutExpression '{baseTimeout}' was removed. Uncontrolled execution duration may affect orchestration.", true));
        }
        else if (baseTimeout is not null && targetTimeout is not null
                 && !string.Equals(baseTimeout, targetTimeout, StringComparison.OrdinalIgnoreCase))
        {
            nonBreaking.Add(new ChangeEntry(
                "TimeoutChanged", "TimeoutExpression", targetTimeout,
                $"TimeoutExpression changed from '{baseTimeout}' to '{targetTimeout}'.", false));
        }
    }
}
