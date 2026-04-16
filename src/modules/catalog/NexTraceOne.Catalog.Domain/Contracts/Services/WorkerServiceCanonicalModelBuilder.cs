using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Constrói o modelo canônico a partir de especificações de Background Service Contract.
/// </summary>
internal static class WorkerServiceCanonicalModelBuilder
{
    /// <summary>
    /// Constrói modelo canônico a partir de especificação de Background Service Contract.
    /// Mapeia os metadados estruturais (inputs, outputs, trigger) para o modelo canônico
    /// de forma que scorecard, rule engine e evidências possam operar sobre o tipo.
    /// </summary>
    internal static ContractCanonicalModel Build(string specContent)
    {
        try
        {
            var spec = BackgroundServiceSpecParser.Parse(specContent);

            // Mapeia inputs e outputs como "operações" canónicas para reutilizar o pipeline
            var operations = new List<ContractOperation>();

            // A operação canónica principal é o processo em background em si
            if (!string.IsNullOrWhiteSpace(spec.ServiceName))
            {
                var inputParams = spec.Inputs
                    .Select(kvp => new ContractSchemaElement(kvp.Key, kvp.Value, true))
                    .ToList();

                var description = spec.Category is { Length: > 0 }
                    ? $"{spec.TriggerType} background service in category '{spec.Category}'"
                    : $"{spec.TriggerType} background service";

                if (spec.ScheduleExpression is not null)
                    description += $". Schedule: {spec.ScheduleExpression}";

                operations.Add(new ContractOperation(
                    spec.ServiceName,
                    spec.ServiceName,
                    description,
                    spec.TriggerType,
                    spec.ServiceName,
                    inputParams,
                    [],
                    false,
                    string.IsNullOrWhiteSpace(spec.Category) ? [] : [spec.Category]));
            }

            var schemas = spec.Outputs
                .Select(kvp => new ContractSchemaElement(kvp.Key, kvp.Value, false))
                .ToList<ContractSchemaElement>();

            // Side effects são metadata — não há security schemes para worker services
            var hasDescriptions = operations.Any(o => !string.IsNullOrWhiteSpace(o.Description));
            var hasExamples = spec.Inputs.Count > 0 || spec.Outputs.Count > 0;

            return new ContractCanonicalModel(
                ContractProtocol.WorkerService,
                spec.ServiceName is { Length: > 0 } ? spec.ServiceName : "Unknown Worker",
                spec.TriggerType,
                spec.ScheduleExpression,
                operations.AsReadOnly(),
                schemas.AsReadOnly(),
                [], // Security schemes are not applicable to background services
                [], // No network servers for background services
                spec.Category is { Length: > 0 } ? [spec.Category] : [],
                operations.Count,
                schemas.Count,
                false, // HasSecurityDefinitions — not applicable for worker services
                hasExamples,
                hasDescriptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse WorkerService spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return CanonicalModelHelpers.EmptyModel(ContractProtocol.WorkerService);
        }
    }
}
