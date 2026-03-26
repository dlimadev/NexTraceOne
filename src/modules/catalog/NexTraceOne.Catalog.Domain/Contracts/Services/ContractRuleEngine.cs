using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Motor determinístico de regras de conformidade para contratos.
/// Aplica regras organizacionais (naming, versioning, error model, segurança, paginação,
/// headers obrigatórios) sobre o modelo canônico normalizado do contrato.
/// Retorna violações tipadas para cada regra não atendida, com severidade e sugestão de correção.
/// As regras são independentes de protocolo, operando sobre o ContractCanonicalModel.
/// </summary>
public static class ContractRuleEngine
{
    /// <summary>
    /// Avalia todas as regras determinísticas sobre o modelo canônico de um contrato.
    /// Retorna lista de violações encontradas, cada uma com severidade e sugestão de correção.
    /// </summary>
    /// <param name="contractVersionId">Identificador da versão de contrato sendo avaliada.</param>
    /// <param name="model">Modelo canônico normalizado do contrato.</param>
    /// <param name="semVer">Versão semântica para validação de naming/versioning.</param>
    /// <param name="protocol">Protocolo do contrato para regras específicas.</param>
    /// <returns>Lista de violações encontradas.</returns>
    public static IReadOnlyList<ContractRuleViolation> Evaluate(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        string semVer,
        ContractProtocol protocol)
    {
        var violations = new List<ContractRuleViolation>();
        var now = DateTimeOffset.UtcNow;

        // Regras específicas para WorkerService (background services)
        if (protocol == ContractProtocol.WorkerService)
        {
            EvaluateWorkerServiceRules(contractVersionId, model, semVer, violations, now);
            return violations.AsReadOnly();
        }

        // Regra 1: Todas as operações devem ter descrição
        EvaluateDescriptionRule(contractVersionId, model, violations, now);

        // Regra 2: Naming conventions — operações não devem usar caracteres especiais inadequados
        EvaluateNamingConventions(contractVersionId, model, violations, now);

        // Regra 3: Segurança — o contrato deve definir esquemas de autenticação
        // (N/A para WSDL e WorkerService — tratados por camadas separadas)
        if (protocol != ContractProtocol.Wsdl)
            EvaluateSecurityRule(contractVersionId, model, violations, now);

        // Regra 4: Versioning — a versão semântica deve ser válida e consistente
        EvaluateVersioningRule(contractVersionId, semVer, model, violations, now);

        // Regra 5: Exemplos — operações devem incluir exemplos para documentação
        EvaluateExamplesRule(contractVersionId, model, violations, now);

        // Regra 6: Schemas — campos obrigatórios devem ter tipo definido
        EvaluateSchemaCompleteness(contractVersionId, model, violations, now);

        // Regra 7: Operações deprecated devem ter alternativa documentada
        EvaluateDeprecationRule(contractVersionId, model, violations, now);

        return violations.AsReadOnly();
    }

    /// <summary>
    /// Avalia regras específicas para Background Service Contracts (WorkerService).
    /// Substitui as regras HTTP-centradas (segurança, exemplos de path) por regras
    /// orientadas a worker/job: trigger type, schedule expression, operação declarada.
    /// </summary>
    private static void EvaluateWorkerServiceRules(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        string semVer,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        // Regra W1: Deve existir pelo menos uma operação (o processo em background)
        if (model.OperationCount == 0)
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId, null,
                "WorkerOperationMissing", "Error",
                "Background service contract has no declared operations.",
                "/serviceName", now,
                "Declare the background service name and category in the contract."));
        }

        // Regra W2: TriggerType (mapeado em SpecVersion) deve ser válido
        if (string.IsNullOrWhiteSpace(model.SpecVersion))
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId, null,
                "WorkerTriggerTypeMissing", "Error",
                "Background service contract does not declare a TriggerType.",
                "/triggerType", now,
                "Set TriggerType to one of: Cron, Interval, EventTriggered, OnDemand, Continuous."));
        }

        // Regra W3: Cron/Interval triggers devem ter ScheduleExpression
        var isCronOrInterval = string.Equals(model.SpecVersion, "Cron", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(model.SpecVersion, "Interval", StringComparison.OrdinalIgnoreCase);
        if (isCronOrInterval && string.IsNullOrWhiteSpace(model.Description))
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId, null,
                "WorkerScheduleMissing", "Warning",
                $"Background service with TriggerType '{model.SpecVersion}' should declare a ScheduleExpression.",
                "/scheduleExpression", now,
                "Provide a cron expression or ISO 8601 interval (e.g. '0 * * * *' or 'PT5M')."));
        }

        // Regra W4: Versioning semântico deve ser válido
        EvaluateVersioningRule(contractVersionId, semVer, model, violations, now);
    }

    /// <summary>
    /// Verifica se todas as operações possuem descrição.
    /// Operações sem descrição prejudicam a documentação e onboarding de consumers.
    /// </summary>
    private static void EvaluateDescriptionRule(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        foreach (var op in model.Operations)
        {
            if (string.IsNullOrWhiteSpace(op.Description))
            {
                violations.Add(ContractRuleViolation.Create(
                    contractVersionId,
                    null,
                    "OperationDescription",
                    "Warning",
                    $"Operation '{op.OperationId}' has no description.",
                    op.Path,
                    now,
                    $"Add a description to operation '{op.OperationId}' for better documentation."));
            }
        }
    }

    /// <summary>
    /// Verifica naming conventions: operationIds devem usar camelCase ou PascalCase sem caracteres especiais.
    /// </summary>
    private static void EvaluateNamingConventions(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        foreach (var op in model.Operations)
        {
            if (op.Name.Contains(' ') || op.Name.Contains('\t'))
            {
                violations.Add(ContractRuleViolation.Create(
                    contractVersionId,
                    null,
                    "NamingConvention",
                    "Warning",
                    $"Operation name '{op.Name}' contains whitespace characters.",
                    op.Path,
                    now,
                    "Use camelCase or PascalCase without spaces for operation names."));
            }
        }
    }

    /// <summary>
    /// Verifica se o contrato define esquemas de segurança/autenticação.
    /// Contratos sem segurança definida representam risco de exposição.
    /// </summary>
    private static void EvaluateSecurityRule(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        if (!model.HasSecurityDefinitions)
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId,
                null,
                "SecurityDefinition",
                "Error",
                "Contract does not define any security schemes.",
                "/",
                now,
                "Add security definitions (OAuth2, API Key, Bearer, etc.) to the contract."));
        }
    }

    /// <summary>
    /// Verifica se a versão semântica está presente e consistente com a spec.
    /// </summary>
    private static void EvaluateVersioningRule(
        ContractVersionId contractVersionId,
        string semVer,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(model.SpecVersion) &&
            !string.IsNullOrWhiteSpace(semVer) &&
            !model.SpecVersion.Contains(semVer) &&
            !semVer.Contains(model.SpecVersion))
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId,
                null,
                "VersionConsistency",
                "Info",
                $"Contract spec version '{model.SpecVersion}' differs from registered semver '{semVer}'.",
                "/info/version",
                now,
                "Ensure the spec version matches the registered semantic version."));
        }
    }

    /// <summary>
    /// Verifica se operações incluem exemplos para documentação de API.
    /// </summary>
    private static void EvaluateExamplesRule(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        if (!model.HasExamples && model.OperationCount > 0)
        {
            violations.Add(ContractRuleViolation.Create(
                contractVersionId,
                null,
                "ExamplesCoverage",
                "Info",
                "Contract does not include examples in operations.",
                "/",
                now,
                "Add request/response examples to improve developer experience."));
        }
    }

    /// <summary>
    /// Verifica se schemas globais possuem campos com tipos definidos.
    /// </summary>
    private static void EvaluateSchemaCompleteness(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        foreach (var schema in model.GlobalSchemas)
        {
            if (string.IsNullOrWhiteSpace(schema.DataType))
            {
                violations.Add(ContractRuleViolation.Create(
                    contractVersionId,
                    null,
                    "SchemaCompleteness",
                    "Warning",
                    $"Schema element '{schema.Name}' has no data type defined.",
                    $"/components/schemas/{schema.Name}",
                    now,
                    "Define the data type for all schema elements."));
            }
        }
    }

    /// <summary>
    /// Verifica se operações deprecated possuem descrição adequada.
    /// </summary>
    private static void EvaluateDeprecationRule(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        List<ContractRuleViolation> violations,
        DateTimeOffset now)
    {
        foreach (var op in model.Operations.Where(o => o.IsDeprecated))
        {
            if (string.IsNullOrWhiteSpace(op.Description) || !op.Description.Contains("deprecat", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(ContractRuleViolation.Create(
                    contractVersionId,
                    null,
                    "DeprecationDocumentation",
                    "Warning",
                    $"Deprecated operation '{op.OperationId}' lacks deprecation notice or migration guidance.",
                    op.Path,
                    now,
                    "Add deprecation notice with migration guidance to deprecated operations."));
            }
        }
    }
}
