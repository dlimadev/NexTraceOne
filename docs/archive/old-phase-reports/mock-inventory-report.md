# Inventário de Mocks — NexTraceOne

> Prompt N16 — Parte 2 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este relatório inventaria todos os mocks relevantes encontrados no backend e frontend do NexTraceOne, excluindo mocks de testes unitários e E2E (que são aceitáveis e necessários).

**Total de mocks relevantes encontrados: 8**
- 🔴 REPLACE_WITH_REAL_IMPLEMENTATION: 3
- 🟠 REMOVE: 1
- 🟡 TEMPORARILY_ACCEPTABLE: 4

---

## 2. Mocks no Backend

### MOCK-B01 — InMemoryIncidentStore (Operational Intelligence)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/InMemoryIncidentStore.cs` |
| **Descrição** | Implementação in-memory do IIncidentStore com 6 incidentes fixos, 2 workflows e 3 runbooks hardcoded com GUIDs fixos |
| **Comentário no código** | "Implementação in-memory... Será substituído por persistência EF Core em fase futura" |
| **Impacto** | Módulo: Operational Intelligence. Dados de incidentes não são persistidos entre restarts |
| **Classificação** | 🔴 **REPLACE_WITH_REAL_IMPLEMENTATION** |
| **Nota** | IncidentSeedData.cs replica os mesmos dados para EF Core — a transição está parcialmente preparada |

### MOCK-B02 — GenerateSimulatedEntries (Automation Audit Trail)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Automation/Features/GetAutomationAuditTrail/GetAutomationAuditTrail.cs` |
| **Descrição** | Método `GenerateSimulatedEntries` retorna 8 entradas de audit trail hardcoded com GUIDs e timestamps fixos |
| **Comentário no código** | "nesta fase os dados são simulados até integração completa entre módulos" |
| **Impacto** | Módulo: Operational Intelligence. Audit trail de automação não reflete dados reais |
| **Classificação** | 🔴 **REPLACE_WITH_REAL_IMPLEMENTATION** |

### MOCK-B03 — AutomationActionCatalog (Static Catalog)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Automation/Features/AutomationActionCatalog.cs` |
| **Descrição** | Retorna 8 ações de automação hardcoded (RestartControlled, ReprocessControlled, etc.) com risk levels e preconditions fixos |
| **Impacto** | Módulo: Operational Intelligence. Catálogo de ações não é extensível nem configurável |
| **Classificação** | 🟡 **TEMPORARILY_ACCEPTABLE** — catálogos estáticos são aceitáveis numa primeira fase se documentados como tal |

### MOCK-B04 — IsSimulated Pattern (Governance FinOps)

| Campo | Valor |
|---|---|
| **Ficheiros** | `src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs`, `GetTeamFinOps.cs`, `GetDomainFinOps.cs`, `GetFinOpsSummary.cs`, `GetFinOpsTrends.cs`, `GetBenchmarking.cs` |
| **Descrição** | 6+ handlers de FinOps consomem dados reais de CostIntelligence mas retornam valores auxiliares hardcoded: `PreviousMonthCost=0m`, `CostTrend=Stable`, `Efficiency=0m`, `WasteSignals=Empty`, `Optimizations=Empty` |
| **Propriedade** | Todos incluem `IsSimulated` field para sinalizar ao frontend |
| **Impacto** | Módulo: Governance. Dashboards FinOps mostram métricas parcialmente reais, parcialmente hardcoded |
| **Classificação** | 🟡 **TEMPORARILY_ACCEPTABLE** — dados de custo reais existem; métricas derivadas são placeholder |

### MOCK-B05 — GetPlatformConfig Feature Flags Mock Fallback

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/governance/NexTraceOne.Governance.Application/Features/GetPlatformConfig/GetPlatformConfig.cs` |
| **Descrição** | Feature flags com fallback para mock quando configuração real não disponível |
| **Comentário no código** | "Feature flags reais a partir da configuração, com fallback para mock" |
| **Impacto** | Módulo: Governance. Feature flags podem não refletir configuração real |
| **Classificação** | 🟡 **TEMPORARILY_ACCEPTABLE** — padrão de fallback é aceitável se documentado |

---

## 3. Mocks no Frontend

### MOCK-F01 — Fake Assistant Response (AI Assistant)

| Campo | Valor |
|---|---|
| **Ficheiros** | `src/frontend/src/locales/en.json` (line "fake assistant response"), `src/frontend/src/locales/pt-BR.json` |
| **Descrição** | String i18n referenciando "fake assistant response" para feature flag do AI Assistant |
| **Impacto** | Módulo: AI & Knowledge. Interface sugere resposta de IA mas pode retornar resposta simulada |
| **Classificação** | 🟠 **REMOVE** — i18n string deve ser removida ou substituída por mensagem contextual real |

### MOCK-F02 — DemoBanner Component (Não utilizado)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/DemoBanner.tsx` |
| **Descrição** | Componente pronto para exibir banner de dados demonstrativos em páginas com `IsSimulated=true`, mas **não está integrado em nenhuma página** |
| **Impacto** | Nenhum impacto funcional — componente órfão |
| **Classificação** | 🟡 **TEMPORARILY_ACCEPTABLE** — manter para uso futuro quando IsSimulated estiver integrado |

---

## 4. Mocks de Testes (Aceitáveis — Fora do Escopo deste Inventário)

Os seguintes mocks são de testes e são aceitáveis:

- `src/frontend/e2e/helpers/auth.ts` — `mockAuthSession()` para E2E tests
- `src/frontend/src/__tests__/` — Todos os `vi.mocked()` e dados de teste
- Padrões de `vi.mock()` em testes unitários

**Classificação: OUT_OF_SCOPE**

---

## 5. Resumo por Módulo

| Módulo | Mocks Relevantes | Impacto |
|---|---|---|
| Operational Intelligence | MOCK-B01, MOCK-B02, MOCK-B03 | 🔴 Alto — InMemoryStore + dados simulados |
| Governance | MOCK-B04, MOCK-B05 | 🟡 Médio — FinOps parcialmente hardcoded |
| AI & Knowledge | MOCK-F01 | 🟠 Baixo — string i18n |
| Outros módulos | Nenhum mock relevante | ✅ Limpo |

---

## 6. Backlog de Ações

| ID | Ação | Prioridade | Estimativa |
|---|---|---|---|
| MOCK-B01 | Substituir InMemoryIncidentStore por IncidentDbContext real | P1_CRITICAL | 8h |
| MOCK-B02 | Substituir GenerateSimulatedEntries por dados reais do audit trail | P1_CRITICAL | 4h |
| MOCK-B03 | Documentar AutomationActionCatalog como catálogo de referência (não mock) | P3_MEDIUM | 1h |
| MOCK-B04 | Implementar cálculos reais de waste/efficiency no FinOps | P2_HIGH | 16h |
| MOCK-B05 | Garantir que feature flags consomem Configuration module | P2_HIGH | 4h |
| MOCK-F01 | Remover "fake assistant response" das strings i18n | P2_HIGH | 0.5h |
