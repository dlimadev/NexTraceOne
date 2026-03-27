# P-01-02 — Substituir dados simulados nos handlers de Automation e Cost do OperationalIntelligence

## Modo de operação

Implementation

## Objetivo

Consolidar os handlers de Automation e Cost do módulo OperationalIntelligence, eliminando dados
simulados hardcoded (nomeadamente no GetAutomationAuditTrail) e garantindo que todos os handlers
utilizam exclusivamente queries reais aos repositórios de persistência.

## Problema atual

A análise da codebase revela que a maioria dos handlers de Cost e Automation já utiliza dados reais
via repositórios dedicados. Contudo, existe uma exceção confirmada:

- **GetAutomationAuditTrail** — contém explicitamente dados simulados: o comentário na linha 13 diz
  "LIMITATION: dados são simulados com entradas hardcoded" e o handler na linha 48 chama
  `GenerateSimulatedEntries(request)` em vez de consultar o repositório `IAutomationAuditRepository`.

Os handlers de **Cost** (16 handlers) já utilizam repositórios reais:
- `ICostRecordRepository`, `ICostSnapshotRepository`, `ICostAttributionRepository`,
  `ICostTrendRepository`, `IServiceCostProfileRepository`, `ICostImportBatchRepository`
- Handlers como ImportCostBatch, IngestCostSnapshot, GetCostRecordsByService/Domain/Team/Release,
  ComputeCostTrend, GetCostDelta, AlertCostAnomaly, etc. — todos com queries reais.

Os handlers de **Automation** (exceto GetAutomationAuditTrail) usam repositórios reais:
- `IAutomationWorkflowRepository`, `IAutomationValidationRepository`

## Escopo permitido

- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Automation/Features/` — handlers de Automation
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Cost/Features/` — handlers de Cost (auditoria)
- Repositórios correspondentes em Infrastructure/

## Escopo proibido

- Handlers de Reliability (tratados em P-01-01)
- Handlers de Incidents ou Runtime
- Outros módulos
- Ficheiros de migração

## Ficheiros principais candidatos a alteração

### Correção obrigatória
- `Automation/Features/GetAutomationAuditTrail/GetAutomationAuditTrail.cs` — substituir `GenerateSimulatedEntries` por query real ao `IAutomationAuditRepository`

### Auditoria e consolidação
- `Automation/Features/GetAutomationAction/GetAutomationAction.cs`
- `Automation/Features/GetAutomationValidation/GetAutomationValidation.cs`
- `Automation/Features/GetAutomationWorkflow/GetAutomationWorkflow.cs`
- `Automation/Features/ListAutomationActions/ListAutomationActions.cs`
- `Automation/Features/ListAutomationWorkflows/ListAutomationWorkflows.cs`
- `Cost/Features/GetCostReport/GetCostReport.cs`
- `Cost/Features/ComputeCostTrend/ComputeCostTrend.cs`
- `Cost/Features/AlertCostAnomaly/AlertCostAnomaly.cs`
- `Infrastructure/Persistence/Automation/AutomationRepositories.cs`

## Responsabilidades permitidas

- Substituir `GenerateSimulatedEntries` no GetAutomationAuditTrail por query real ao repositório
- Verificar que o `IAutomationAuditRepository` tem os métodos necessários (GetByWorkflowIdAsync)
- Remover comentários de "LIMITATION: dados simulados"
- Auditar handlers de Cost para confirmar ausência de dados hardcoded residuais

## Responsabilidades proibidas

- Alterar handlers de Reliability
- Alterar lógica de cálculo de custos ou tendências
- Introduzir novas entidades de domínio
- Alterar migrações

## Critérios de aceite

1. GetAutomationAuditTrail consulta dados reais do repositório
2. Método `GenerateSimulatedEntries` removido
3. Comentários de "dados simulados" removidos
4. Auditoria dos handlers de Cost confirma zero dados hardcoded
5. Módulo compila sem erros
6. Testes existentes passam

## Validações obrigatórias

- `dotnet build src/modules/operationalintelligence/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -rn "simulated\|Simulated\|hardcoded\|Hardcoded\|fake\|Fake" src/modules/operationalintelligence/Application/Automation/` — retorna zero
- `grep -rn "simulated\|Simulated\|hardcoded\|Hardcoded\|fake\|Fake" src/modules/operationalintelligence/Application/Cost/` — retorna zero

## Riscos e cuidados

- GetAutomationAuditTrail pode não ter dados reais na base — garantir que o handler retorna lista vazia em vez de falhar
- O AutomationRepositories já tem `GetByWorkflowIdAsync` para audit records — confirmar mapeamento
- A remoção de dados simulados pode expor a UI a estados vazios — verificar handling no frontend

## Dependências

- Idealmente P-00-05 (CancellationToken) já aplicado
- P-01-01 (Reliability) pode ser executado em paralelo

## Próximos prompts sugeridos

- P-01-03 (IsSimulated nos handlers de FinOps do Governance)
- P-01-04 (Handlers de ExternalAI do AIKnowledge)
