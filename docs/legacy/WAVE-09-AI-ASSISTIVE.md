# Onda 9 — IA Assistiva para Legacy

> **Duração estimada:** 3-4 semanas
> **Dependências:** Ondas 6, 7 e 8
> **Risco:** Médio — qualidade depende do modelo LLM e dados disponíveis
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Dotar o AI Assistant do NexTraceOne de contexto legacy/mainframe, permitindo investigação assistida de problemas, análise de impacto e explicação de blast radius em linguagem natural.

---

## Entregáveis

- [ ] AI Tool: `ListLegacyAssets` — consulta ativos mainframe
- [ ] AI Tool: `AnalyzeCopybookImpact` — impacto de mudança em copybook
- [ ] AI Tool: `InvestigateBatchFailure` — investigação de falha batch
- [ ] AI Tool: `GetMqQueueHealth` — saúde de filas MQ
- [ ] AI Tool: `ExplainLegacyBlastRadius` — explicação de blast radius legacy
- [ ] AI Tool: `SuggestMitigationForLegacyIncident` — sugestão de mitigação
- [ ] Extensão do AI context com dados legacy
- [ ] Quick actions no AI Assistant para contexto legacy
- [ ] i18n para prompts e respostas de IA legacy

---

## Impacto Backend

### Novos AI Tools

Cada tool segue o padrão existente em `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Tools/`:

#### 1. `ListLegacyAssetsTool`

```csharp
public sealed class ListLegacyAssetsTool : IToolDefinition
{
    public string Name => "list_legacy_assets";
    public string Description => "List mainframe/legacy assets from the NexTraceOne catalog";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("asset_type", "Filter by type: MainframeSystem, CobolProgram, Copybook, CicsTransaction, BatchJob, MqQueueManager", "string"),
        new ToolParameterDefinition("system_name", "Filter by mainframe system name", "string"),
        new ToolParameterDefinition("team", "Filter by team name", "string"),
        new ToolParameterDefinition("criticality", "Filter by criticality: Critical, High, Medium, Low", "string"),
        new ToolParameterDefinition("limit", "Max results (default: 20)", "integer"),
    };
}
```

#### 2. `AnalyzeCopybookImpactTool`

```csharp
public sealed class AnalyzeCopybookImpactTool : IToolDefinition
{
    public string Name => "analyze_copybook_impact";
    public string Description => "Analyze the impact of a copybook change — which programs, transactions, and services are affected";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("copybook_name", "Name of the copybook", "string", Required: true),
        new ToolParameterDefinition("include_transitive", "Include transitive impact (default: true)", "boolean"),
    };
}
```

#### 3. `InvestigateBatchFailureTool`

```csharp
public sealed class InvestigateBatchFailureTool : IToolDefinition
{
    public string Name => "investigate_batch_failure";
    public string Description => "Investigate a batch job failure — get execution details, return codes, recent changes, and related incidents";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("job_name", "Batch job name", "string", Required: true),
        new ToolParameterDefinition("execution_id", "Specific execution ID (optional — latest if omitted)", "string"),
        new ToolParameterDefinition("include_recent_changes", "Include recent changes to the job and related assets (default: true)", "boolean"),
    };
}
```

#### 4. `GetMqQueueHealthTool`

```csharp
public sealed class GetMqQueueHealthTool : IToolDefinition
{
    public string Name => "get_mq_queue_health";
    public string Description => "Get MQ queue health — depth, throughput, DLQ status, anomalies";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("queue_name", "Queue name (or queue manager name)", "string", Required: true),
        new ToolParameterDefinition("queue_manager", "Queue manager name", "string"),
        new ToolParameterDefinition("include_history", "Include depth history (default: false)", "boolean"),
    };
}
```

#### 5. `ExplainLegacyBlastRadiusTool`

```csharp
public sealed class ExplainLegacyBlastRadiusTool : IToolDefinition
{
    public string Name => "explain_legacy_blast_radius";
    public string Description => "Explain the blast radius of a change to a legacy asset in natural language — suitable for CAB presentations";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("asset_name", "Name of the changed asset", "string", Required: true),
        new ToolParameterDefinition("change_type", "Type of change", "string"),
        new ToolParameterDefinition("audience", "Target audience: technical, cab, executive (default: technical)", "string"),
    };
}
```

#### 6. `SuggestMitigationForLegacyIncidentTool`

```csharp
public sealed class SuggestMitigationForLegacyIncidentTool : IToolDefinition
{
    public string Name => "suggest_mitigation_legacy_incident";
    public string Description => "Suggest mitigation steps for a legacy/mainframe incident based on context, history, and runbooks";
    
    public IReadOnlyList<ToolParameterDefinition> Parameters => new[]
    {
        new ToolParameterDefinition("incident_description", "Description of the incident", "string", Required: true),
        new ToolParameterDefinition("asset_name", "Affected asset name", "string"),
        new ToolParameterDefinition("asset_type", "Type of affected asset", "string"),
    };
}
```

### Regras de IA Governada

Conforme princípios do produto:
- IA **nunca** executa mudanças automaticamente em core systems
- IA pode analisar, sugerir, resumir e classificar risco
- Todas as chamadas são auditadas
- Políticas de acesso por tenant, ambiente e persona
- Budget de tokens aplicável

---

## Impacto Frontend

### Extensão do AI Assistant

- Quick actions no painel do Assistant:
  - "🔍 Analyze copybook change impact"
  - "🔧 Investigate batch failure"
  - "📊 Check MQ queue health"
  - "💥 Explain blast radius for legacy change"
  - "🛡️ Suggest mitigation for incident"

- Contexto automático quando navegando em página legacy (e.g., em `/operations/batch/:jobId`, o assistant tem contexto do job)

### i18n

Novas chaves para:
- Nomes e descrições das tools
- Quick action labels
- Prompts e sugestões

---

## Testes

### Testes Unitários (~30)
- Tool execution: cada tool retorna dados formatados
- Parameter validation: parâmetros obrigatórios e opcionais
- Output formatting: resposta adequada para cada audience

### Testes de Integração (~10)
- Tool chamada com dados reais → resposta coerente
- Audit trail registado para cada chamada
- Context propagation com tenant e permissions

---

## Critérios de Aceite

1. ✅ AI pode responder "what programs are affected by copybook X change?"
2. ✅ AI pode investigar batch failure com contexto de timeline e dependências
3. ✅ AI pode explicar blast radius para CAB (linguagem não técnica)
4. ✅ AI pode sugerir mitigação para incidente legacy
5. ✅ Respostas respeitam i18n e audience level
6. ✅ Todas as chamadas auditadas
7. ✅ Quick actions disponíveis no Assistant panel

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| LLM pode alucinar sobre mainframe | Alta | Tools fornecem dados reais. LLM apenas formata e explica |
| Contexto legacy pode ser grande | Média | Limitar dados por query. Summarization |
| Budget de tokens com muitas tools | Baixa | Tools retornam dados compactos. Audit de custo |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W9-S01 | Implementar `ListLegacyAssetsTool` | P1 |
| W9-S02 | Implementar `AnalyzeCopybookImpactTool` | P1 |
| W9-S03 | Implementar `InvestigateBatchFailureTool` | P1 |
| W9-S04 | Implementar `GetMqQueueHealthTool` | P1 |
| W9-S05 | Implementar `ExplainLegacyBlastRadiusTool` | P2 |
| W9-S06 | Implementar `SuggestMitigationForLegacyIncidentTool` | P2 |
| W9-S07 | Registar tools no AI runtime | P1 |
| W9-S08 | Criar quick actions no frontend | P2 |
| W9-S09 | Implementar context propagation para páginas legacy | P2 |
| W9-S10 | i18n para tools e quick actions | P2 |
| W9-S11 | Testes unitários (~30) | P1 |
| W9-S12 | Testes de integração (~10) | P2 |
