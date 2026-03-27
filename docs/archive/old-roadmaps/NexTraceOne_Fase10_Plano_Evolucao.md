# NexTraceOne — Plano de Evolução — Fase 10

> **Baseline de referência:** `docs/acceptance/NexTraceOne_Baseline_Estavel.md`
> **Plano operacional:** `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`
> **Estado:** Fase 10 — Evolução do produto sobre baseline estável

---

## 1. Resumo executivo

A Fase 10 retoma a evolução estrutural do NexTraceOne sobre a baseline estável validada nas Fases 1–9. O aceite da Fase 8 confirmou **0 bugs P0/P1** e **4 observações P2** (mock data em módulos secundários). Esta evolução é incremental, orientada por trilhas independentes com recortes MVP claros.

---

## 2. Inventário técnico por trilha (estado real)

### 2.1 Trilha 1 — Contracts avançado

| Componente | Estado atual |
|-----------|-------------|
| `ContractVersion` entity | ✅ Completa — 15+ campos, lifecycle states, diffs, artifacts, violations |
| `ApiAsset` → `ServiceAsset` | ✅ `ServiceAsset` já tem `Domain`, `TeamName`, `TechnicalOwner`, `Criticality`, `ExposureType` |
| Contract Catalog API | ✅ `ListLatestPerApiAssetAsync` com filtros e paginação (real) |
| Frontend `mockEnrichment.ts` | ⚠️ Enriquece `ContractListItem` com domain/owner/team/technology via hash determinístico |
| Contract Studio (Draft) | ✅ `DraftStudioPage` funcional com editor real |
| Spectral realtime | ❌ Rota `/contracts/spectral` — página existe, marcada Preview, sem backend real |
| Canonical entities | ❌ Rota `/contracts/canonical` — página existe, marcada Preview, sem backend real |

**Gap crítico:** A API de listagem de contratos não faz join com `ApiAsset` → `ServiceAsset` para retornar domain/owner/team. O frontend compensa com mock enrichment.

### 2.2 Trilha 2 — Governance real

| Componente | Estado atual |
|-----------|-------------|
| Domain entities | ✅ 8 entities: Team, GovernanceDomain, GovernancePack, GovernancePackVersion, GovernanceWaiver, GovernanceRuleBinding, GovernanceRolloutRecord, TeamDomainLink, DelegatedAdministration |
| Domain enums | ✅ 40+ enums definidos |
| Application handlers | ✅ 60+ features VSA completas — **mas 100% retornam mock data in-memory** |
| API endpoints | ✅ 20 endpoint modules mapeados |
| Infrastructure | ❌ **NENHUM DbContext, NENHUM repository, NENHUMA persistência** — apenas `DependencyInjection.cs` vazio |
| Migrations | ❌ Nenhuma migration do módulo Governance |
| Frontend pages | ✅ 20+ páginas — todas marcadas Preview, todas usam API real (que retorna mock) |
| Frontend API client | ✅ `organizationGovernanceApi.ts` — chama endpoints reais |

**Gap crítico:** O módulo Governance é o mais incompleto ao nível de infraestrutura. Tem 100% da superfície (API, Application, Domain, Frontend) mas 0% de persistência real. Toda a evolução requer: DbContext → Configurations → Migrations → Seed → refactoring dos handlers para injetar repositories.

### 2.3 Trilha 3 — Integrations real

| Componente | Estado atual |
|-----------|-------------|
| Ingestion API | ✅ `NexTraceOne.Ingestion.Api` — 5 endpoints stub (`Results.Accepted`) para deployments, promotions, runtime signals, consumers, contracts sync |
| Backend handlers | ⚠️ No módulo Governance: `ListIntegrationConnectors`, `GetIntegrationConnector`, `RetryConnector`, `ListIngestionExecutions`, `GetIngestionHealth`, `GetIngestionFreshness`, `ListIngestionSources`, `ReprocessExecution` — todos retornam mock |
| Frontend pages | ✅ 4 páginas: `IntegrationHubPage`, `ConnectorDetailPage`, `IngestionExecutionsPage`, `IngestionFreshnessPage` — todas Preview |
| Persistência | ❌ Nenhuma — depende do Governance DbContext (inexistente) |

**Gap crítico:** A Ingestion API existe como serviço separado mas é totalmente stub. Os handlers de integração vivem dentro do módulo Governance sem persistência. Requer definição de bounded context próprio ou persistência via Governance.

### 2.4 Trilha 4 — Product Analytics real

| Componente | Estado atual |
|-----------|-------------|
| Backend handlers | ⚠️ No módulo Governance: `GetAnalyticsSummary`, `GetModuleAdoption`, `GetPersonaUsage`, `GetJourneys`, `GetValueMilestones`, `GetFrictionIndicators`, `RecordAnalyticsEvent` — todos retornam mock |
| API endpoint | ✅ `ProductAnalyticsEndpointModule.cs` |
| Frontend pages | ✅ 5 páginas: Overview, Adoption, Personas, Journeys, Value — todas Preview |
| Persistência | ❌ Nenhuma — depende do Governance DbContext |
| Event ingestion | ❌ Nenhum pipeline de captura de eventos real |

**Gap crítico:** Product Analytics requer um pipeline de eventos real (event capture → storage → aggregation) que não existe. É a trilha com maior esforço relativo.

### 2.5 Trilha 5 — AI Hub real

| Componente | Estado atual |
|-----------|-------------|
| AI Governance DbContext | ✅ `AiGovernanceDbContext` com 15+ entity configurations, migration `InitialAiGovernanceSchema` |
| Repositories | ✅ `AiGovernanceRepositories.cs`, `AiRuntimeRepositories.cs` |
| Domain entities | ✅ AiModel, AiProvider, AiAssistantConversation, AiMessage, AIAccessPolicy, AIBudget, AIRoutingStrategy, AIRoutingDecision, etc. |
| SendAssistantMessage handler | ✅ **Real** — usa repositories, cria conversations, persiste messages, routing strategy, usage audit |
| Runtime services | ✅ `AiProviderFactory`, `AiProviderHealthService`, `AiModelCatalogService`, `AiTokenQuotaService`, `AiSourceRegistryService` |
| Ollama provider | ✅ `OllamaProvider` + `OllamaHttpClient` — integração real com Ollama |
| Frontend pages | ✅ 7 páginas: Assistant, Audit, Policies, Routing, IDE, Models, TokenBudget |
| Frontend assistant | ⚠️ `AiAssistantPage` tem mock conversations para demonstração mas chama API real para send/create |

**Gap principal:** O assistente funciona com API real mas as conversations listadas no frontend são parcialmente mock. As páginas avançadas (Models, Policies, Routing, IDE) estão em Preview e precisam de integração frontend → backend que já existe.

---

## 3. Ordem recomendada de execução

### Critérios de priorização

1. **Impacto no Source of Truth** — fortalece a narrativa central do produto
2. **Reutilização de infraestrutura existente** — menor esforço técnico
3. **Desbloqueio de trilhas dependentes** — resolve pré-requisitos
4. **Resolução de P2 do aceite** — fecha observações da baseline

### Ordem proposta

```
┌─────────────────────────────────────────────────────────┐
│ ETAPA 1 — Contracts avançado (Trilha 1)                 │
│ Razão: resolve P2-001, máximo ROI, zero dependências    │
│ Esforço: Baixo–Médio                                    │
├─────────────────────────────────────────────────────────┤
│ ETAPA 2 — AI Hub real (Trilha 5)                        │
│ Razão: resolve P2-003, infraestrutura já existe (90%)   │
│ Esforço: Baixo                                          │
├─────────────────────────────────────────────────────────┤
│ ETAPA 3 — Governance real (Trilha 2)                    │
│ Razão: desbloqueia Trilhas 3 e 4, impacto transversal   │
│ Esforço: Alto                                           │
├─────────────────────────────────────────────────────────┤
│ ETAPA 4 — Integrations real (Trilha 3)                  │
│ Razão: depende de Governance DbContext (Etapa 3)        │
│ Esforço: Médio                                          │
├─────────────────────────────────────────────────────────┤
│ ETAPA 5 — Product Analytics real (Trilha 4)             │
│ Razão: depende de event pipeline + Governance (Etapa 3) │
│ Esforço: Alto                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Roadmap detalhado por trilha

---

### Trilha 1 — Contracts avançado

#### MVP (Etapa 1A — resolver P2-001)

**Objetivo:** Eliminar mock enrichment do catálogo de contratos.

| # | Tarefa | Componente | Esforço |
|---|--------|-----------|---------|
| 1.1 | Criar endpoint `GET /contracts/catalog` que faz join `ContractVersion` → `ApiAsset` → `ServiceAsset` e retorna domain, team, owner, criticality, exposure, serviceType | Catalog API + Application | Médio |
| 1.2 | Atualizar `ContractCatalogPage` para consumir o novo DTO com campos reais | Frontend | Baixo |
| 1.3 | Remover `mockEnrichment.ts` e `enrichCatalogItems()` | Frontend | Baixo |
| 1.4 | Atualizar seed data para garantir contratos com `ApiAsset` → `ServiceAsset` associados | Seed SQL | Baixo |

**Entregável:** Catálogo de contratos com todos os campos vindos do backend real.

#### Evolução (Etapa 1B)

| # | Tarefa | Esforço |
|---|--------|---------|
| 1.5 | Fortalecer source editor no DraftStudio — syntax highlighting, validation inline | Médio |
| 1.6 | Builder ↔ Source round-trip — editar no builder e ver no source, e vice-versa | Alto |
| 1.7 | Spectral realtime — integrar validação Spectral no editor com feedback imediato | Alto |
| 1.8 | Canonical entities — definição e gestão de shared schemas/DTOs | Alto |

**Dependências:** 1.5–1.8 são independentes entre si.

---

### Trilha 5 — AI Hub real

#### MVP (Etapa 2A — resolver P2-003)

**Objetivo:** Eliminar mock conversations do assistente.

| # | Tarefa | Componente | Esforço |
|---|--------|-----------|---------|
| 5.1 | Atualizar `AiAssistantPage` para usar `ListConversations` API real (já existe no backend) em vez de mock | Frontend | Baixo |
| 5.2 | Atualizar `ListMessages` para carregar histórico real de mensagens por conversa | Frontend | Baixo |
| 5.3 | Garantir seed data com conversas/mensagens de demonstração | Seed SQL | Baixo |

**Entregável:** Assistente de IA sem mock data visível.

#### Evolução (Etapa 2B)

| # | Tarefa | Esforço |
|---|--------|---------|
| 5.4 | Model Registry page — integrar com `ListModels`/`RegisterModel`/`UpdateModel` APIs reais | Baixo |
| 5.5 | AI Policies page — integrar com `ListPolicies`/`CreatePolicy`/`UpdatePolicy` APIs reais | Baixo |
| 5.6 | AI Routing page — integrar com `ListRoutingStrategies`/`GetRoutingDecision` APIs reais | Baixo |
| 5.7 | IDE Integrations page — integrar com `ListIdeClients`/`RegisterIdeClient`/`GetIdeCapabilities` APIs reais | Baixo |
| 5.8 | Token Budget page — integrar com `ListBudgets`/`UpdateBudget`/`GetTokenUsage` APIs reais | Baixo |
| 5.9 | AI Audit page — integrar com `ListAuditEntries` API real | Baixo |

**Dependências:** Backend AIKnowledge já tem 90%+ da infraestrutura. Esforço é principalmente frontend.

---

### Trilha 2 — Governance real

#### MVP (Etapa 3A — persistência core)

**Objetivo:** Criar infraestrutura de persistência para o módulo Governance.

| # | Tarefa | Componente | Esforço |
|---|--------|-----------|---------|
| 2.1 | Criar `GovernanceDbContext` com configurações para: Team, GovernanceDomain, GovernancePack, GovernancePackVersion, GovernanceWaiver, GovernanceRuleBinding, DelegatedAdministration, TeamDomainLink, GovernanceRolloutRecord | Infrastructure | Alto |
| 2.2 | Criar entity configurations (EF Core) para todas as entidades | Infrastructure | Médio |
| 2.3 | Gerar migration `InitialGovernanceSchema` | Infrastructure | Baixo |
| 2.4 | Definir interfaces de repository no Application (`ITeamRepository`, `IDomainRepository`, `IGovernancePackRepository`, `IGovernanceWaiverRepository`, etc.) | Application | Médio |
| 2.5 | Implementar repositories no Infrastructure | Infrastructure | Médio |
| 2.6 | Registar DbContext e repositories no `DependencyInjection.cs` | Infrastructure | Baixo |
| 2.7 | Registar migration no `Program.cs` (ApiHost) | Platform | Baixo |
| 2.8 | Criar seed SQL para governance (teams, domains, packs, waivers) | Seed | Médio |

**Entregável:** Infraestrutura de persistência completa para Governance.

#### MVP (Etapa 3B — handlers reais core)

**Objetivo:** Substituir mock data nos handlers mais importantes.

| # | Tarefa | Esforço |
|---|--------|---------|
| 2.9 | Refactorar `ListTeams`, `GetTeamDetail`, `CreateTeam`, `UpdateTeam` para usar repositories | Médio |
| 2.10 | Refactorar `ListDomains`, `GetDomainDetail`, `CreateDomain`, `UpdateDomain` para usar repositories | Médio |
| 2.11 | Refactorar `ListGovernancePacks`, `GetGovernancePack`, `CreateGovernancePack`, `UpdateGovernancePack` para usar repositories | Médio |
| 2.12 | Refactorar `ListGovernanceWaivers`, `CreateGovernanceWaiver`, `ApproveGovernanceWaiver`, `RejectGovernanceWaiver` para usar repositories | Médio |
| 2.13 | Refactorar `ListDelegatedAdministrations`, `CreateDelegatedAdministration` para usar repositories | Baixo |
| 2.14 | Remover Preview flag das rotas Teams, Domains, Delegated Admin no frontend | Baixo |

**Entregável:** Organization Governance (Teams, Domains, Packs, Waivers, Delegations) com persistência real.

#### Evolução (Etapa 3C — governance enterprise)

| # | Tarefa | Esforço |
|---|--------|---------|
| 2.15 | Refactorar Executive Overview — agregar dados reais cross-module | Alto |
| 2.16 | Refactorar Compliance, Risk, Controls, Evidence — persistência real | Alto |
| 2.17 | Refactorar FinOps — persistência por serviço, equipa, domínio | Alto |
| 2.18 | Refactorar Reports — geração real de relatórios | Alto |
| 2.19 | Refactorar Policies — persistência e enforcement real | Médio |
| 2.20 | Refactorar Maturity Scorecards, Benchmarking — dados reais | Alto |
| 2.21 | Remover Preview flags gradualmente à medida que cada sub-módulo for real | Baixo |

**Dependências:** 2.15–2.21 dependem de dados cross-module (Catalog, Contracts, ChangeGovernance, OperationalIntelligence).

---

### Trilha 3 — Integrations real

#### Pré-requisito: Governance DbContext (Etapa 3A)

#### MVP (Etapa 4A)

**Objetivo:** Pipeline de ingestão funcional end-to-end.

| # | Tarefa | Componente | Esforço |
|---|--------|-----------|---------|
| 3.1 | Definir entidades de Integração: `IntegrationConnector`, `IngestionExecution`, `IngestionSource` no módulo Governance (ou módulo próprio) | Domain | Médio |
| 3.2 | Adicionar configurações EF Core para entidades de integração | Infrastructure | Médio |
| 3.3 | Gerar migration incremental para tabelas de integração | Infrastructure | Baixo |
| 3.4 | Implementar `Ingestion.Api` endpoints reais — receber, validar, persistir eventos | Platform | Alto |
| 3.5 | Refactorar `ListIntegrationConnectors`, `GetIntegrationConnector`, `RetryConnector` para persistência real | Application | Médio |
| 3.6 | Refactorar `ListIngestionExecutions`, `ReprocessExecution` para persistência real | Application | Médio |
| 3.7 | Refactorar `GetIngestionHealth`, `GetIngestionFreshness`, `ListIngestionSources` para persistência real | Application | Médio |
| 3.8 | Remover Preview flags das páginas de Integrations no frontend | Frontend | Baixo |

**Entregável:** Integration Hub funcional com connectors, executions e freshness reais.

#### Evolução (Etapa 4B)

| # | Tarefa | Esforço |
|---|--------|---------|
| 3.9 | Implementar connectors reais: GitHub, GitLab, Azure DevOps | Alto |
| 3.10 | Implementar webhook receivers na Ingestion API | Médio |
| 3.11 | Implementar retry/reprocess automático | Médio |

---

### Trilha 4 — Product Analytics real

#### Pré-requisito: Governance DbContext (Etapa 3A)

#### MVP (Etapa 5A)

**Objetivo:** Captura e visualização de eventos de uso real do produto.

| # | Tarefa | Componente | Esforço |
|---|--------|-----------|---------|
| 4.1 | Definir entidade `AnalyticsEvent` com tipo, timestamp, userId, persona, module, action, metadata | Domain | Médio |
| 4.2 | Adicionar configuração EF Core + migration para `analytics_events` | Infrastructure | Baixo |
| 4.3 | Implementar `RecordAnalyticsEvent` handler real com persistência | Application | Baixo |
| 4.4 | Implementar middleware ou hook de tracking no frontend (page views, actions) | Frontend | Médio |
| 4.5 | Refactorar `GetAnalyticsSummary` para agregar eventos reais | Application | Médio |
| 4.6 | Refactorar `GetModuleAdoption` para calcular adopção real | Application | Médio |

**Entregável:** Product Analytics Overview com dados reais de uso.

#### Evolução (Etapa 5B)

| # | Tarefa | Esforço |
|---|--------|---------|
| 4.7 | Refactorar `GetPersonaUsage` para segmentação real por persona | Médio |
| 4.8 | Refactorar `GetJourneys` para funnel analysis real | Alto |
| 4.9 | Refactorar `GetValueMilestones` para value tracking real | Alto |
| 4.10 | Refactorar `GetFrictionIndicators` para detecção de atrito real | Alto |
| 4.11 | Remover Preview flags gradualmente | Baixo |

---

## 5. Dependências técnicas

```
Trilha 1 (Contracts)     →  nenhuma dependência
Trilha 5 (AI Hub)        →  nenhuma dependência
Trilha 2 (Governance)    →  nenhuma dependência
Trilha 3 (Integrations)  →  depende de Trilha 2 (Governance DbContext)
Trilha 4 (Analytics)     →  depende de Trilha 2 (Governance DbContext)
```

### Diagrama de dependências

```
[Trilha 1 - Contracts] ──────── independente
[Trilha 5 - AI Hub]    ──────── independente
[Trilha 2 - Governance] ─┬───── independente
                          ├──→ [Trilha 3 - Integrations]
                          └──→ [Trilha 4 - Analytics]
```

---

## 6. Riscos identificados

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|-------|--------------|---------|-----------|
| R1 | Governance DbContext complexo — 9 entidades + relationships | Alta | Alto | Criar em fases: core primeiro (Team, Domain, Pack), depois relações (Waivers, Bindings, Rollout) |
| R2 | Migration conflito com migrations existentes | Média | Médio | Nova database PostgreSQL separada para Governance (pattern existente: cada módulo tem sua database) |
| R3 | Refactoring de 60+ handlers simultâneo pode introduzir regressão | Alta | Alto | Refactorar por sub-domínio (Teams → Domains → Packs → Waivers), testar cada grupo |
| R4 | Contract enrichment join pode ter impacto de performance | Baixa | Médio | Usar eager loading seletivo ou view materializada |
| R5 | Product Analytics event volume pode crescer rápido | Média | Médio | Definir retenção e aggregation desde o início |
| R6 | Integrations depende de serviços externos (GitHub, GitLab) | Média | Médio | Começar com mock connectors testáveis, depois integrar com APIs reais |
| R7 | Frontend pages de Governance são muitas (20+) para integrar de uma vez | Alta | Alto | Integrar em grupos: Organization (Teams/Domains) → Packs → Enterprise |

---

## 7. Recorte de MVP por trilha

### Trilha 1 — Contracts MVP
- ✅ Catálogo de contratos com campos reais (sem mock enrichment)
- ✅ Join ContractVersion → ApiAsset → ServiceAsset para domain/team/owner
- ✅ Seed data atualizada

### Trilha 5 — AI Hub MVP
- ✅ Assistente sem mock conversations
- ✅ Listagem real de conversations/messages
- ✅ Model Registry com dados reais

### Trilha 2 — Governance MVP
- ✅ GovernanceDbContext com entities core
- ✅ Teams CRUD real
- ✅ Domains CRUD real
- ✅ Governance Packs CRUD real
- ✅ Waivers CRUD real
- ✅ Delegated Admin real

### Trilha 3 — Integrations MVP
- ✅ Connectors persistidos
- ✅ Ingestion executions persistidas
- ✅ Health/freshness calculados a partir de dados reais

### Trilha 4 — Analytics MVP
- ✅ Captura de eventos de navegação
- ✅ Analytics summary real
- ✅ Module adoption real

---

## 8. Entregáveis por etapa

| Etapa | Trilha | Entregável | P2 resolvido |
|-------|--------|-----------|-------------|
| 1A | Contracts | Catálogo sem mock enrichment | P2-001 ✅ |
| 2A | AI Hub | Assistente sem mock conversations | P2-003 ✅ |
| 2B | AI Hub | Pages avançadas integradas com backend real | — |
| 3A | Governance | GovernanceDbContext + migrations + seed | — |
| 3B | Governance | Organization governance real (Teams, Domains, Packs, Waivers) | — |
| 3C | Governance | Enterprise governance real (Executive, Compliance, Risk, FinOps) | — |
| 4A | Integrations | Integration Hub funcional end-to-end | — |
| 5A | Analytics | Event capture + analytics summary real | — |

---

## 9. Estimativa de esforço

| Etapa | Esforço estimado | Risco |
|-------|-----------------|-------|
| 1A — Contracts MVP | 🟢 Baixo (1–2 sessões) | Baixo |
| 2A — AI Hub MVP | 🟢 Baixo (1 sessão) | Baixo |
| 2B — AI Hub avançado | 🟢 Baixo (2–3 sessões) | Baixo |
| 3A — Governance persistência | 🔴 Alto (3–5 sessões) | Alto |
| 3B — Governance handlers core | 🟡 Médio (3–4 sessões) | Médio |
| 3C — Governance enterprise | 🔴 Alto (5–8 sessões) | Alto |
| 4A — Integrations MVP | 🟡 Médio (3–4 sessões) | Médio |
| 4B — Integrations connectors | 🔴 Alto (4–6 sessões) | Alto |
| 5A — Analytics MVP | 🟡 Médio (2–3 sessões) | Médio |
| 5B — Analytics avançado | 🔴 Alto (4–6 sessões) | Alto |

---

## 10. Recomendação de execução imediata

**Começar pelas Etapas 1A + 2A em paralelo:**

1. **Etapa 1A** — Resolver P2-001: criar endpoint de catálogo com join, remover mock enrichment
2. **Etapa 2A** — Resolver P2-003: integrar listagem real de conversations no AiAssistantPage

Estas duas etapas:
- Fecham 50% dos P2 da baseline
- Não requerem nova infraestrutura
- Maximizam o ROI sobre a base existente
- São completamente independentes

Depois, seguir com **Etapa 3A** (Governance persistência) para desbloquear as trilhas 3 e 4.

---

## 11. Declaração de alinhamento

Este plano:

- ✅ Respeita a baseline estável (Fase 9)
- ✅ Não mistura estabilização com evolução
- ✅ Segue refatoração incremental orientada por produto
- ✅ Prioriza Source of Truth (Contracts, Governance)
- ✅ Respeita bounded contexts existentes
- ✅ Identifica riscos e pré-requisitos
- ✅ Define MVPs por trilha
- ✅ Mantém preview flags até integração real validada
