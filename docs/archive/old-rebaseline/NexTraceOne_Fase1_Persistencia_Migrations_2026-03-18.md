# NexTraceOne — Fase 1 — Persistência e migrations pendentes (evidência)

> **Data:** 2026-03-18
> 
> **Source of truth:**
> - `docs/rebaseline/NexTraceOne_Fase0_Rebaseline_Arquitetural_2026-03-18.md`
> - `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md`

---

## Objetivo

Fechar gaps reais de persistência/migrations confirmados na Fase 0, com foco em:
- `OperationalIntelligence.Runtime`
- `OperationalIntelligence.Cost`
- `AIKnowledge.AiGovernance` (validar)
- `AIKnowledge.ExternalAI` (validar)
- `AIKnowledge.AiOrchestration` (validar)

---

## Resultado resumido

### Contextos fechados nesta fase

- `RuntimeIntelligenceDbContext` — **migration criada** + incluído no auto-migrate do host.
- `CostIntelligenceDbContext` — **migration criada** + incluído no auto-migrate do host.

### Contextos validados (sem ação)

- `AiGovernanceDbContext` — já possui migration `InitialAiGovernanceSchema`.

### Contextos ainda pendentes (por design/incompletude real)

- `ExternalAiDbContext` — existe como placeholder (sem `DbSet<T>` e infra DI ainda TODO); migrations **não criadas** nesta fase para evitar schema “vazio” sem uso.
- `AiOrchestrationDbContext` — idem.

---

## Evidências

### Host coerente com contextos ativos

O host (`NexTraceOne.ApiHost`) agora aplica migrations também para:
- `RuntimeIntelligenceDbContext`
- `CostIntelligenceDbContext`

em `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs`.

### Migrations geradas

- Runtime:
  - `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Runtime/Persistence/Migrations/*InitialRuntimeIntelligenceSchema*`
- Cost:
  - `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Cost/Persistence/Migrations/*InitialCostIntelligenceSchema*`

### Design-time factories adicionadas

- `RuntimeIntelligenceDbContextDesignTimeFactory`
- `CostIntelligenceDbContextDesignTimeFactory`

---

## Seeds

Nenhum seed SQL foi adicionado/alterado nesta fase. Para Runtime/Cost o critério mínimo é schema deployável (migrations), e o fluxo de ingestão gera dados.

---

## Riscos remanescentes

- ExternalAI e AiOrchestration seguem com persistência **não implementada** (infra DI TODO + ausência de entidades). Se esses módulos forem considerados “ativos para deploy”, será necessário:
  1) definir entidades e repositórios mínimos;
  2) registrar DbContexts em DI;
  3) gerar migrations;
  4) decidir estratégia de seed.

- Test suite geral ainda reporta falhas (fora do escopo desta fase).
