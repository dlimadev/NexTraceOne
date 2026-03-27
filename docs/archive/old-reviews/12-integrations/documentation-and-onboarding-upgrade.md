# Integrations — Documentation and Onboarding Upgrade

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Documentação actual: 0% → Plano definido

---

## 1. Estado actual da documentação

| Documento | Existe | Estado |
|-----------|--------|--------|
| `module-review.md` | ✅ | Básico — lista páginas e entities, identifica coupling com Governance |
| `module-consolidated-review.md` | ✅ | Consolidado — 45% maturity, identifica problemas |
| README do módulo | ❌ | Zero |
| Documentação de API | ❌ | Zero |
| Documentação de entidades | ❌ | Zero |
| Documentação de conectores | ❌ | Zero |
| Documentação de fluxos | ❌ | Zero |
| XML docs em classes | ⚠️ | Parcial — DbSets têm XML docs, entities não |
| Documentação de onboarding | ❌ | Zero |

**Score: 0% (descontando review docs que são artefactos de auditoria, não documentação do módulo)**

---

## 2. Revisão do `module-review.md`

| Aspecto | Estado | Nota |
|---------|--------|------|
| Listing de páginas | ✅ | 4 páginas listadas com rotas e permissões |
| Identificação do coupling | ✅ | Correctamente identifica backend em Governance |
| Actions recomendadas | ✅ | 5 acções listadas |
| Profundidade técnica | ❌ | Superficial — sem detalhe de entities, enums, flows |
| Código referenciado | ⚠️ | Refere `IntegrationHubEndpointModule` mas sem path completo |

---

## 3. Revisão do `module-consolidated-review.md`

| Aspecto | Estado | Nota |
|---------|--------|------|
| Maturity assessment | ✅ | 45% global, detalhado por dimensão |
| Coupling analysis | ✅ | Tabela clara de componentes em Governance |
| Frontend summary | ✅ | 4 páginas com rotas e status |
| Dependencies | ✅ | Governance, Configuration, Notifications |
| Action items | ✅ | 5 itens priorizados |
| Profundidade | ⚠️ | Boa para consolidação, mas sem detalhe de code-level |

---

## 4. Documentação ausente

| # | Documento | Prioridade | Impacto |
|---|----------|-----------|---------|
| D-01 | **README.md do módulo** | 🔴 P1_CRITICAL | Sem isto, ninguém sabe o que o módulo faz |
| D-02 | **API documentation** (endpoints, requests, responses) | 🔴 P1_CRITICAL | Sem isto, frontend e consumers não sabem como usar |
| D-03 | **Entity documentation** (IntegrationConnector, IngestionSource, IngestionExecution) | 🟡 P2_HIGH | Sem isto, domain model é opaco |
| D-04 | **Enum documentation** (6 enums com significado de cada valor) | 🟡 P2_HIGH | Ambiguidade nos estados |
| D-05 | **Flow documentation** (criar conector, retry, reprocess, webhook) | 🟡 P2_HIGH | Sem isto, fluxos não são claros |
| D-06 | **Connector guide** (como adicionar novo tipo de conector) | 🟡 P2_HIGH | Extensibilidade zero sem guia |
| D-07 | **Architecture decision record** (por que separar de Governance) | 🟢 P3_MEDIUM | Contexto de decisão |
| D-08 | **Troubleshooting guide** (erros comuns, logs, diagnóstico) | 🟢 P3_MEDIUM | Operacional |

---

## 5. Classes e fluxos que precisam de explicação

| Classe/Fluxo | Razão |
|-------------|-------|
| `IntegrationConnector.RecordSuccess()` / `RecordFailure()` | Lógica de health transitions não é óbvia |
| `IngestionSource.UpdateFreshnessStatus()` | Cálculo de freshness com multipliers (1x, 4x, 12x) não é documentado |
| `IntegrationConnector.UpdateFreshnessLag()` | Threshold de 240 min para degraded está hardcoded |
| `RetryConnector` command | Marca retry mas sem worker — confuso |
| `ReprocessExecution` command | Idem |
| `AllowedTeams` serialização | JSONB com serialization/deserialization custom |
| Relação entre ConnectorType, Provider e SourceType | Hierarquia não documentada |

---

## 6. XML docs necessárias

| Ficheiro | XML docs actuais | Estado |
|---------|-----------------|--------|
| `IntegrationConnector.cs` | Nenhumas | ❌ Precisam de class-level + method-level docs |
| `IngestionSource.cs` | Nenhumas | ❌ |
| `IngestionExecution.cs` | Nenhumas | ❌ |
| Enums (6 ficheiros) | Nenhumas | ❌ Cada valor precisa de descrição |
| `IntegrationHubEndpointModule.cs` | Nenhumas | ❌ Cada endpoint precisa de summary |
| CQRS handlers (7) | Nenhumas | ❌ |
| Repository interfaces (3) | Nenhumas | ❌ |
| GovernanceDbContext (DbSets) | ✅ Presentes | ✅ |

---

## 7. Notas de onboarding necessárias

| # | Tópico | Conteúdo mínimo |
|---|--------|----------------|
| O-01 | **O que é o módulo** | Papel, escopo, o que faz e não faz |
| O-02 | **Onde está o código** | Paths de backend (em Governance!), frontend, configs |
| O-03 | **Como correr localmente** | Seed data, migrations, API calls de teste |
| O-04 | **Como adicionar um conector** | Passo a passo com exemplo |
| O-05 | **Como testar a ingestão** | Criar execução, verificar health |
| O-06 | **Decisões arquitecturais** | Por que está em Governance, plano de extracção |
| O-07 | **Permissões e roles** | Quem pode fazer o quê |
| O-08 | **Troubleshooting** | Logs, erros comuns, como diagnosticar |

---

## 8. Documentação mínima do módulo (definição)

Para o módulo ser considerado com **documentação mínima aceitável**, precisa de:

| # | Documento | Prioridade | Esforço |
|---|----------|-----------|---------|
| 1 | **README.md** — Overview, setup, architecture | 🔴 P1_CRITICAL | 3h |
| 2 | **API.md** — Endpoints com examples | 🔴 P1_CRITICAL | 4h |
| 3 | **ENTITIES.md** — Domain model com diagramas | 🟡 P2_HIGH | 3h |
| 4 | **FLOWS.md** — Fluxos principais documentados | 🟡 P2_HIGH | 3h |
| 5 | **CONNECTORS.md** — Guia de conectores | 🟡 P2_HIGH | 2h |
| 6 | XML docs em todas as classes públicas | 🟡 P2_HIGH | 4h |
| 7 | **ONBOARDING.md** — Guia para novos devs | 🟢 P3_MEDIUM | 2h |

**Total mínimo: ~21h**

---

## 9. Preparação de documentação dos fluxos principais

### Fluxos a documentar

| # | Fluxo | Actors | Steps |
|---|-------|--------|-------|
| 1 | **Criar e configurar conector** | PlatformAdmin | POST connector → configure → activate → test |
| 2 | **Receber dados via webhook** | External system | POST webhook → validate → create execution → process → update metrics |
| 3 | **Polling de API externa** | Background worker | Read config → call API → create execution → process → update freshness |
| 4 | **Retry de conector falhado** | PlatformAdmin | Click retry → create new execution → process → update health |
| 5 | **Monitorizar health** | TechLead/Developer | View hub → check health badges → drill down → view executions |
| 6 | **Investigar data freshness** | TechLead | View freshness page → identify stale → check connector → retry |

---

## 10. Backlog de documentação

| # | Item | Prioridade | Tipo | Esforço |
|---|------|-----------|------|---------|
| D-01 | Criar README.md do módulo | 🔴 P1_CRITICAL | DOC | 3h |
| D-02 | Criar API.md com endpoints e examples | 🔴 P1_CRITICAL | DOC | 4h |
| D-03 | Criar ENTITIES.md com domain model | 🟡 P2_HIGH | DOC | 3h |
| D-04 | Criar FLOWS.md com fluxos principais | 🟡 P2_HIGH | DOC | 3h |
| D-05 | Criar CONNECTORS.md guia de conectores | 🟡 P2_HIGH | DOC | 2h |
| D-06 | Adicionar XML docs a todas as classes | 🟡 P2_HIGH | DOC | 4h |
| D-07 | Criar ONBOARDING.md | 🟢 P3_MEDIUM | DOC | 2h |
| D-08 | Documentar enum values com significado | 🟢 P3_MEDIUM | DOC | 1h |

**Total estimado: ~22h**
