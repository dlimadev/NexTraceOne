# Governance — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Governance** é o mais amplo do NexTraceOne, funcionando como "catch-all" para funcionalidades transversais de governança organizacional. Cobre 15 subdomínios distintos:

| Subdomínio | Descrição | Pertinência ao módulo |
|---|---|---|
| Executive | Overview executivo, drill-down, FinOps executivo | ✅ Core Governance |
| Compliance | Packs de compliance, checks, certificações | ✅ Core Governance |
| Risk | Centro de risco, heatmaps | ✅ Core Governance |
| FinOps | Custos por serviço, equipa, domínio | ✅ Core Governance |
| Policies | Catálogo de políticas, enterprise controls | ✅ Core Governance |
| Governance Packs | Packs de governança, scorecards de maturidade | ✅ Core Governance |
| Evidence | Pacotes de evidência | ✅ Core Governance |
| Waivers | Exceções de governança | ✅ Core Governance |
| Reports | Relatórios de governança | ✅ Core Governance |
| Teams & Domains | Gestão organizacional | ✅ Organization (candidato a extração) |
| Delegated Admin | Administração delegada | ✅ Organization |
| Onboarding | Fluxo de onboarding | ⚠️ Candidato a módulo separado |
| Integrations | Hub de integrações, conectores | ⚠️ **Deveria ser módulo separado** |
| Product Analytics | Adoção, personas, jornadas, valor | ⚠️ **Deveria ser módulo separado** |
| Platform Status | Status, jobs, queues | ⚠️ **Deveria ser módulo separado** |

**Prioridade:** P3 | **Maturidade global:** 64% | **Tipo de correção:** STRUCTURAL_REFACTOR | **Wave:** 3-4

**Caminhos:**
- Backend: `src/modules/governance/`
- Frontend: `src/frontend/src/features/governance/`
- Persistência: `GovernanceDbContext` → base `nextraceone_operations`

---

## 2. Estado Atual

### 2.1 Maturidade por Dimensão

| Dimensão | Estado | Nota |
|---|---|---|
| Backend | 🟢 85% | 18 endpoint modules, 58 entidades, domínio DDD coerente |
| Frontend | 🟢 80% | 25 páginas, todas com rotas ativas, classificação COMPLETE_APPARENT |
| Documentação | 🔴 35% | Sem README, sem documentação de subdomínios |
| Testes | 🟡 55% | Insuficiente para escopo tão amplo (18 endpoint modules) |

### 2.2 Inventário Técnico

| Métrica | Valor |
|---|---|
| Páginas frontend | 25 |
| Ficheiros frontend | 30 |
| Ficheiros API frontend | 4 (`organizationGovernance.ts`, `evidence.ts`, `executive.ts`, `finOps.ts`) |
| Endpoint modules backend | 18 |
| Features backend | 73 |
| Entidades de domínio | 58 |
| DbContexts | 1 (`GovernanceDbContext`) |
| Entity configurations | ~30 |
| Migrações | 3 (`InitialCreate`, `Phase5Enrichment`, `AddLastProcessedAt`) |
| Itens de menu | 7 directos + 2 navegação interna |

### 2.3 Entidades de Domínio Principais

| Entidade | Tipo | Finalidade |
|---|---|---|
| Team | AggregateRoot | Gestão de equipas |
| Domain (GovernanceDomain) | AggregateRoot | Gestão de domínios organizacionais |
| Policy | AggregateRoot | Catálogo de políticas |
| GovernancePack | Entity | Packs de governança e maturidade |
| Waiver (GovernanceWaiver) | Entity | Exceções de governança |
| ComplianceReport | Entity | Relatórios de compliance |
| RiskAssessment | Entity | Avaliações de risco |
| FinOpsEntry | Entity | Entradas FinOps |
| Control | Entity | Controlos enterprise |
| Evidence | Entity | Pacotes de evidência |
| IngestionSource | Entity | Fontes de ingestão (subdomínio Integrations) |
| IngestionExecution | Entity | Execuções de ingestão |
| IntegrationConnector | Entity | Conectores de integração |

### 2.4 Dependências do Módulo

| Depende de | Para quê |
|---|---|
| Identity & Access | Autenticação, permissões, tenancy, RLS |
| Catalog | Dados de serviços para FinOps, Risk, Reports |
| Change Governance | Dados de mudanças para correlação em Executive Views |

| Bloqueia | Porquê |
|---|---|
| Executive Views | Dashboards executivos dependem deste módulo |
| Reports cross-module | Relatórios de governança consomem dados consolidados |
| Integrations | Endpoints de integrações vivem dentro deste módulo |
| Product Analytics | Endpoints de analytics vivem dentro deste módulo |

---

## 3. Problemas Críticos e Bloqueadores

### 🔴 CR-5 — Módulo Governance como Catch-All (Causa Raiz Sistémica)

O módulo acumulou responsabilidades de múltiplos domínios, violando o princípio de bounded context. Com 25 páginas e 18 endpoint modules, é o maior módulo do sistema em termos de granularidade de endpoints.

**Responsabilidades que deveriam ser módulos separados:**

| Responsabilidade | Evidência | Impacto |
|---|---|---|
| Integration Hub | `IntegrationsEndpointModule`, entidades `IntegrationConnector`, `IngestionSource`, `IngestionExecution` | Acoplamento com domínio de integrações |
| Product Analytics | `ProductAnalyticsEndpointModule`, endpoint `/api/governance/analytics` | Mistura analytics com governance |
| Platform Status | `PlatformStatusEndpointModule`, endpoint `/api/governance/platform-status` | Mistura infra com governance |
| Onboarding | `OnboardingEndpointModule`, endpoint `/api/governance/onboarding` | Lógica de onboarding no domínio errado |

**Consequências:**
- Alterações num subdomínio arriscam afetar outros
- Difícil atribuir ownership clara por equipa
- Testes genéricos sem foco (55% para 18 endpoints é insuficiente)
- Evolução independente dos subdomínios é impossível

### 🟠 Fronteira Ambígua com Audit & Compliance

Governance contém subdomínios de compliance (CompliancePage, ComplianceEndpointModule, ComplianceChecksEndpointModule) que se sobrepõem parcialmente ao módulo `auditcompliance`. A fronteira entre ambos não está documentada.

### 🟠 FinOps Genérico e Não Contextualizado

O subdomínio FinOps (`FinOpsPage`, `ServiceFinOpsPage`, `TeamFinOpsPage`, `DomainFinOpsPage`) existe mas opera de forma genérica, sem contextualização profunda por serviço, equipa ou operação conforme a visão oficial do produto. O pilar "FinOps contextual" não está plenamente implementado.

---

## 4. Problemas por Camada

### 4.1 Frontend

| Severidade | Problema | Detalhe |
|---|---|---|
| 🟡 Médio | Permissão única `governance:read` para 25 páginas | Todas as rotas protegidas usam `governance:read` — falta granularidade (deveria haver `governance:finops:read`, `governance:risk:read`, etc.) |
| 🟡 Médio | Sub-rotas não promovidas ao menu | `Controls`, `Evidence`, `Maturity`, `Waivers`, `Benchmarking` acessíveis apenas via navegação interna (sub-rotas), não aparecem directamente no menu |
| 🟢 Baixo | 25 páginas sem documentação individual | Nenhuma das 25 páginas tem documentação dedicada |

**Rotas do módulo (25 — todas ✅ funcionais):**

| Rota | Página | Menu |
|---|---|---|
| `/governance/executive` | ExecutiveOverviewPage | ✅ governance |
| `/governance/executive/:area` | ExecutiveDrillDownPage | Navegação interna |
| `/governance/executive/finops` | ExecutiveFinOpsPage | Navegação interna |
| `/governance/reports` | ReportsPage | ✅ governance |
| `/governance/compliance` | CompliancePage | ✅ governance |
| `/governance/risk` | RiskCenterPage | ✅ governance |
| `/governance/risk/heatmap` | RiskHeatmapPage | Navegação interna |
| `/governance/finops` | FinOpsPage | ✅ governance |
| `/governance/finops/service/:id` | ServiceFinOpsPage | Navegação interna |
| `/governance/finops/team/:id` | TeamFinOpsPage | Navegação interna |
| `/governance/finops/domain/:id` | DomainFinOpsPage | Navegação interna |
| `/governance/policies` | PolicyCatalogPage | ✅ governance |
| `/governance/packs` | GovernancePacksOverviewPage | ✅ governance |
| `/governance/packs/:packId` | GovernancePackDetailPage | Navegação interna |
| `/governance/controls` | EnterpriseControlsPage | Via compliance |
| `/governance/evidence` | EvidencePackagesPage | Via compliance |
| `/governance/waivers` | WaiversPage | Via policies |
| `/governance/maturity` | MaturityScorecardsPage | Via reports |
| `/governance/benchmarking` | BenchmarkingPage | Via reports |
| `/governance/teams` | TeamsOverviewPage | ✅ organization |
| `/governance/teams/:teamId` | TeamDetailPage | Navegação interna |
| `/governance/domains` | DomainsOverviewPage | ✅ organization |
| `/governance/domains/:domainId` | DomainDetailPage | Navegação interna |
| `/governance/delegated-admin` | DelegatedAdminPage | Via admin |
| `/governance/configuration` | GovernanceConfigurationPage | Via admin |

### 4.2 Backend

| Severidade | Problema | Detalhe |
|---|---|---|
| 🟠 Alto | 18 endpoint modules num único módulo | Granularidade extrema de endpoints, mas todos acoplados ao mesmo `GovernanceDbContext` |
| 🟠 Alto | Mistura de permissões entre domínios | O módulo usa permissões de 3 namespaces diferentes: `governance:*`, `integrations:*`, `platform:admin:*` — evidência de responsabilidades misturadas |
| 🟡 Médio | `OnboardingEndpointModule` usa permissão `governance:teams:read` | Permissão semanticamente incorreta para operações de onboarding |
| 🟡 Médio | Endpoints com apenas GET (read-only) | 6 dos 18 endpoint modules têm apenas operação de leitura (Policies, FinOps, Risk, Compliance, Reports, Controls) — pode indicar lógica de escrita incompleta |

**Mapa de endpoints (18 módulos):**

| Endpoint Module | Rotas | Permissão | Rate Limit |
|---|---|---|---|
| TeamsEndpointModule | `/api/governance/teams` | `governance:teams:read/write` | Global |
| DomainsEndpointModule | `/api/governance/domains` | `governance:domains:read/write` | Global |
| PoliciesEndpointModule | `/api/governance/policies` | `governance:policies:read` | Global |
| PacksEndpointModule | `/api/governance/packs` | `governance:packs:read/write` | Global |
| WaiversEndpointModule | `/api/governance/waivers` | `governance:waivers:read/write` | Global |
| FinOpsEndpointModule | `/api/governance/finops` | `governance:finops:read` | data-intensive |
| RiskEndpointModule | `/api/governance/risk` | `governance:risk:read` | Global |
| ComplianceEndpointModule | `/api/governance/compliance` | `governance:compliance:read` | Global |
| ReportsEndpointModule | `/api/governance/reports` | `governance:reports:read` | data-intensive |
| ControlsEndpointModule | `/api/governance/controls` | `governance:controls:read` | Global |
| EvidenceEndpointModule | `/api/governance/evidence` | `governance:evidence:read` | Global |
| ExecutiveEndpointModule | `/api/governance/executive` | `governance:reports:read` | data-intensive |
| IntegrationsEndpointModule | `/api/governance/integrations` | `integrations:read/write` | Global |
| OnboardingEndpointModule | `/api/governance/onboarding` | `governance:teams:read/write` | Global |
| PlatformStatusEndpointModule | `/api/governance/platform-status` | `platform:admin:read` | Global |
| ProductAnalyticsEndpointModule | `/api/governance/analytics` | `governance:analytics:read/write` | data-intensive |
| ScopedContextEndpointModule | `/api/governance/scoped-context` | `governance:teams:read` | Global |
| DelegatedAdminEndpointModule | `/api/governance/delegated-admin` | `platform:admin:read` | auth-sensitive |

### 4.3 Database

| Severidade | Problema | Detalhe |
|---|---|---|
| 🟡 Médio | GovernanceDb partilha `nextraceone_operations` com 11 outros DbContexts | Base de dados mais carregada do sistema (12 DbContexts no total) — risco de contenção DDL e performance |
| 🟡 Médio | Apenas 3 migrações para 58 entidades | Rácio baixo — sugere que grande parte do schema foi criado na migração inicial, dificultando rastreio de evolução |
| 🟡 Médio | Sem RowVersion/ConcurrencyToken | Controlo de concorrência via `UpdatedAt` app-level — aceitável mas insuficiente sob carga elevada |
| 🟢 Baixo | Zero check constraints | Validação de domínio depende exclusivamente da camada aplicacional |
| 🟢 Baixo | Sem modelo dedicado para Risk Center | Risco parcialmente coberto pela entidade `RiskAssessment` mas sem modelo rico |

**Características de persistência (GovernanceDbContext):**

| Capacidade | Estado |
|---|---|
| Multi-tenant RLS | ✅ Activo |
| Audit interceptor | ✅ Activo |
| Soft deletes | ✅ Via AuditableEntity |
| Outbox pattern | ✅ Implementado |
| Encriptação de campos | ✅ Tokens de integração |
| Strongly-typed IDs | ✅ TypedIdBase |

**Seeds relevantes:** `seed-governance.sql` (420 linhas) — políticas e standards para desenvolvimento.

### 4.4 Segurança

| Severidade | Problema | Detalhe |
|---|---|---|
| 🟠 Alto | Frontend usa permissão genérica `governance:read` para 25 páginas | Backend tem permissões granulares (12+ permissões distintas) mas o frontend não as utiliza — qualquer utilizador com `governance:read` acede a tudo |
| 🟡 Médio | Executive Views sem validação de persona | As views executivas devem ser restritas às personas Executive e Tech Lead, mas actualmente dependem apenas de permissão genérica |
| 🟢 Baixo | DelegatedAdminEndpointModule usa `platform:admin:read` para POST | Operação de escrita protegida com permissão de leitura — potencial sobre-permissão |

### 4.5 IA e Agentes

Sem integração directa com o módulo AI Knowledge nesta fase. Não existem agentes configurados para operar sobre dados de governance, compliance ou risk.

**Recomendação futura:** agentes de IA para consultar dados de compliance, sugerir políticas e gerar relatórios de risco — mas fora do escopo imediato (Wave 4+).

### 4.6 Documentação

| Severidade | Problema | Detalhe |
|---|---|---|
| 🟠 Alto | Zero documentação dedicada ao módulo | Sem README, sem documentação de subdomínios, sem diagramas |
| 🟠 Alto | Fronteiras internas não documentadas | Os 15 subdomínios não têm documentação de responsabilidades e limites |
| 🟡 Médio | Entidades de domínio sem XML docs | 58 entidades sem documentação inline de invariantes e regras de negócio |
| 🟡 Médio | 25 páginas sem documentação individual | Nenhuma página tem descrição de propósito, persona-alvo ou dados consumidos |

---

## 5. Dependências

### 5.1 Dependências de Runtime

```
Identity & Access ─── (auth, permissions, tenancy, RLS)
       │
       └── Governance ──── 18 endpoint modules
              │
              ├── Catalog (dados de serviços para FinOps, Executive, Risk)
              │
              ├── Change Governance (dados de mudanças para Executive)
              │
              ├──► Integrations frontend (endpoints servidos por Governance)
              │
              └──► Product Analytics frontend (endpoints servidos por Governance)
```

### 5.2 Dependências de Infraestrutura

- **Base de dados:** `nextraceone_operations` (partilhada com 11 outros DbContexts)
- **Building blocks:** Core, Application, Infrastructure, Security
- **Outbox:** Eventos de domínio publicados via `OutboxProcessorJob`

### 5.3 Módulos que Dependem de Governance

| Módulo dependente | O que consome |
|---|---|
| Integrations (frontend) | Endpoints `/api/governance/integrations` |
| Product Analytics (frontend) | Endpoints `/api/governance/analytics` |
| Executive dashboards | Dados consolidados via `/api/governance/executive` |

---

## 6. Quick Wins

| # | Quick Win | Esforço | Impacto | Prioridade |
|---|---|---|---|---|
| QW-G1 | Criar README do módulo Governance com mapa de subdomínios | 4h | Documentação de 0% → ~15% | 🟠 P1 |
| QW-G2 | Promover `Controls`, `Evidence`, `Maturity`, `Waivers`, `Benchmarking` ao menu sidebar | 2h | Funcionalidades escondidas tornam-se visíveis | 🟡 P2 |
| QW-G3 | Alinhar permissões frontend com granularidade do backend (usar `governance:finops:read`, `governance:risk:read`, etc.) | 3-4h | Segurança granular em 25 páginas | 🟠 P2 |
| QW-G4 | Corrigir permissão de `DelegatedAdminEndpointModule` POST (usar `platform:admin:write` em vez de `read`) | 30min | Correcção de sobre-permissão | 🟡 P2 |
| QW-G5 | Corrigir permissão de `OnboardingEndpointModule` (criar `governance:onboarding:read/write`) | 30min | Semântica correcta | 🟡 P3 |
| QW-G6 | Documentar fronteiras internas dos 15 subdomínios | 3h | Base para futuro refactoring | 🟡 P2 |

**Esforço total Quick Wins:** ~13-14h (2 dias)

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Risco | Impacto | Prioridade |
|---|---|---|---|---|---|
| SR-G1 | Extrair IntegrationHub como módulo independente (`src/modules/integrations/`) | 2-3 semanas | Médio | Bounded context limpo, evolução independente | 🟠 P2 |
| SR-G2 | Extrair ProductAnalytics como módulo independente (`src/modules/productanalytics/`) | 2-3 semanas | Médio | Elimina poluição de domínio | 🟡 P3 |
| SR-G3 | Extrair PlatformStatus para módulo `platform` | 1 semana | Baixo | Responsabilidade correcta | 🟡 P3 |
| SR-G4 | Contextualizar FinOps por serviço/equipa/operação com dados reais | 2-3 semanas | Médio | Alinhamento com visão oficial do produto | 🟠 P2 |
| SR-G5 | Avaliar extração de Teams/Domains para módulo Organization separado | 2-3 semanas | Médio-Alto | Clareza de bounded contexts | 🟡 P3 |
| SR-G6 | Elevar cobertura de testes de 55% para ≥70% | 1-2 semanas | Baixo | Confiança em refactoring | 🟡 P3 |
| SR-G7 | Adicionar RowVersion em entidades críticas do GovernanceDb (Team, Domain, Policy, GovernancePack) | 1 semana | Médio | Integridade sob concorrência | 🟡 P3 |

**Dependências entre refactors:**
- SR-G1 (Integrations) e SR-G2 (Analytics) são independentes e podem executar em paralelo
- SR-G5 (Organization) deve ser avaliado após SR-G1 e SR-G2 — redução do módulo clarifica fronteiras restantes
- SR-G4 (FinOps) é independente dos restantes
- SR-G6 (Testes) deve acompanhar qualquer outro refactor

---

## 8. Critérios de Fecho

O módulo Governance considera-se estabilizado quando:

- [ ] **Subdomínios identificados e documentados** — os 15 subdomínios têm fronteiras claras e documentação
- [ ] **README do módulo criado** — com mapa de subdomínios, entidades, endpoints e rotas
- [ ] **Permissões frontend granulares** — cada sub-área protegida com permissão específica (não apenas `governance:read`)
- [ ] **FinOps contextualizado** — drill-down por serviço/equipa/domínio com dados reais (não genérico)
- [ ] **IntegrationHub extraído** — ou decisão documentada de manter com justificação
- [ ] **ProductAnalytics extraído** — ou decisão documentada de manter com justificação
- [ ] **Testes ≥70%** — cobertura adequada ao escopo de 18 endpoint modules
- [ ] **Maturidade global ≥75%** — subida de 64% para 75%+
- [ ] **Executive Views persona-aware** — conteúdo adaptado à persona do utilizador
- [ ] **Sem permissões semânticas incorrectas** — cada endpoint com permissão adequada

**Maturidade-alvo:** 64% → 75%+

---

## 9. Plano de Ação Priorizado

### Fase 1 — Quick Wins (Semana 1, ~2 dias)

| Ordem | Acção | Referência | Esforço |
|---|---|---|---|
| 1 | Criar README do módulo com mapa de subdomínios | QW-G1 | 4h |
| 2 | Documentar fronteiras internas | QW-G6 | 3h |
| 3 | Alinhar permissões frontend com backend | QW-G3 | 3-4h |
| 4 | Corrigir permissão DelegatedAdmin POST | QW-G4 | 30min |
| 5 | Promover sub-rotas ao menu | QW-G2 | 2h |

### Fase 2 — Validação de Dados (Semana 2-3, ~2 dias)

| Ordem | Acção | Esforço |
|---|---|---|
| 6 | Validar Executive Overview (dados reais vs mock) | 2h |
| 7 | Validar Compliance e Risk Center | 2h |
| 8 | Validar FinOps drill-downs (service, team, domain) | 2h |
| 9 | Validar Governance Packs e Evidence | 2h |
| 10 | Validar Teams e Domains CRUD | 1h |

### Fase 3 — Refactoring Estrutural (Semanas 3-6)

| Ordem | Acção | Referência | Esforço |
|---|---|---|---|
| 11 | Extrair IntegrationHub como módulo independente | SR-G1 | 2-3 semanas |
| 12 | Contextualizar FinOps com dados reais por serviço/equipa | SR-G4 | 2-3 semanas |
| 13 | Elevar cobertura de testes para ≥70% | SR-G6 | 1-2 semanas |

### Fase 4 — Consolidação (Semanas 6-8)

| Ordem | Acção | Referência | Esforço |
|---|---|---|---|
| 14 | Extrair ProductAnalytics como módulo independente | SR-G2 | 2-3 semanas |
| 15 | Avaliar extração de Teams/Domains (Organization) | SR-G5 | 2-3 semanas |
| 16 | Adicionar RowVersion em entidades críticas | SR-G7 | 1 semana |

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Relatórios envolvidos | Resolução |
|---|---|---|---|
| 1 | **Contagem de endpoint modules: 18 vs 19** | `module-review.md` refere "19 endpoint modules" enquanto `backend-module-inventory.md` e `backend-endpoints-report.md` listam 18 | A lista explícita em `backend-endpoints-report.md` contém exactamente 18 endpoints. O `module-review.md` conta `ComplianceChecksEndpointModule` como módulo separado (secção 4), mas este não aparece na lista detalhada de endpoints. **Adoptar 18 como valor correcto.** |
| 2 | **Classificação backend: "MADURO" vs 85%** | `backend-module-inventory.md` classifica governance como "MADURO" (igual a catalog/aiknowledge), mas `module-consolidation-report.md` atribui 85% ao backend | A classificação "MADURO" é binária e não reflecte a granularidade do problema (catch-all). **Adoptar 85% como referência mais precisa.** |
| 3 | **Entidades no GovernanceDbContext: 12 DbSets vs 58 entidades** | `database-structural-audit.md` reporta 12 DbSets no GovernanceDbContext, mas `backend-domain-report.md` reporta 58 entidades | Não é contradição — 58 entidades incluem value objects e entidades owned que não correspondem a DbSets directos. **Ambos os valores estão correctos em contextos diferentes.** |
| 4 | **Frontend classificação: COMPLETE_APPARENT vs 80%** | `frontend-module-inventory.md` classifica como COMPLETE_APPARENT, mas o módulo tem apenas 80% e permissões superficiais | COMPLETE_APPARENT significa que todas as rotas existem, mas não avalia profundidade. **A classificação 80% é mais precisa para planeamento.** |
| 5 | **Número de migrações: 3 vs "estável"** | `database-structural-audit.md` reporta 3 migrações, mas `backend-persistence-report.md` classifica como "✅ COERENTE" | 3 migrações para 58 entidades é baixo mas não é um problema se o schema foi bem desenhado desde o início. **Classificação coerente é mantida, mas com nota de que o rácio é baixo.** |
| 6 | **Prioridade de correcção: LOW vs STRUCTURAL_REFACTOR** | `frontend-module-inventory.md` marca prioridade como LOW, mas `module-consolidation-report.md` classifica como STRUCTURAL_REFACTOR | Perspectivas diferentes: o frontend está funcional (LOW), mas o módulo como um todo precisa de refactoring (STRUCTURAL_REFACTOR). **Adoptar STRUCTURAL_REFACTOR como classificação global.** |

---

*Relatório consolidado gerado como parte da revisão modular do NexTraceOne.*
*Fontes: module-review.md, modular-review-summary.md, module-consolidation-report.md, module-closure-plan.md, backend-endpoints-report.md, backend-domain-report.md, backend-persistence-report.md, backend-module-inventory.md, frontend-pages-and-routes-report.md, frontend-module-inventory.md, database-structural-audit.md, root-cause-consolidation-report.md, quick-wins-vs-structural-refactors.md, execution-waves-plan.md*
