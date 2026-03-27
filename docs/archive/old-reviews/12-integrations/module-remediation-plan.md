# Integrations — Module Remediation Plan

> **Module:** Integrations (12)  
> **Table prefix (target):** `int_`  
> **Date:** 2026-03-25  
> **Status:** Plano final de remediação — pronto para execução  
> **Maturidade actual:** ~45%  
> **Maturidade target:** 75%+

---

## 1. Resumo executivo

O módulo Integrations é o hub de integrações externas do NexTraceOne. O frontend é funcional (4 páginas, i18n, permissões), mas o **backend está fisicamente dentro do módulo Governance**, criando um coupling arquitectural que impede a evolução independente do módulo.

### Principais lacunas
- Backend não é independente — entidades, endpoints, DbContext, migrations todos em Governance
- Tabelas usam prefixo `gov_` em vez de `int_`
- CRUD incompleto — conectores e fontes não podem ser criados/editados via API
- Zero domain events publicados — Notifications, Audit e OI não recebem eventos
- Zero acções auditadas
- Retry/reprocess apenas marcam queue — sem worker que processe
- Zero documentação dedicada
- Credenciais de conectores não geridas de forma segura

### Risco actual: 🟠 ALTO
- Sem CRUD, o módulo depende de seed data — não é operacional em produção
- Sem audit events, acções sensíveis não são rastreáveis
- Sem extracção de Governance, migrations não podem ser recriadas com `int_`

### Prioridade do módulo: ALTA para consolidação estrutural

---

## 2. Quick wins

| # | Título | Problema que resolve | Camada | Esforço | Prioridade |
|---|--------|---------------------|--------|---------|-----------|
| QW-01 | Adicionar Executions ao menu lateral | Sub-rota inacessível sem navegar internamente | Frontend | 30min | P3_MEDIUM |
| QW-02 | Adicionar Freshness ao menu lateral | Idem | Frontend | 30min | P3_MEDIUM |
| QW-03 | Retornar 202 Accepted em retry/reprocess | 200 OK sugere que acção foi executada, mas foi só queued | Backend | 1h | P3_MEDIUM |
| QW-04 | AuthenticationMode/PollingMode com label i18n | Strings raw "Not configured" no frontend | Frontend | 30min | P3_MEDIUM |
| QW-05 | AllowedTeams como chips formatados | Array JSON raw exibido | Frontend | 1h | P3_MEDIUM |
| QW-06 | CorrelationId com tooltip completo | Truncado, difícil de copiar | Frontend | 30min | P4_LOW |
| QW-07 | Empty states para sources e executions | Telas sem dados não informam o utilizador | Frontend | 1h | P3_MEDIUM |
| QW-08 | Keys i18n para erros de integração | Erros genéricos, sem contexto | Frontend | 1h | P3_MEDIUM |

**Total quick wins: ~6h**

---

## 3. Correções funcionais obrigatórias

### 3.1 Fluxos ponta a ponta

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-01 | **Criar conector via API** | POST `/connectors` com CreateConnector command + validação FluentValidation | Sem isto, conectores só existem via seed | P1_CRITICAL | Nenhuma |
| FF-02 | **Editar conector via API** | PUT `/connectors/{id}` com UpdateConnector command | Configuração imutável sem isto | P1_CRITICAL | FF-01 |
| FF-03 | **Eliminar conector (soft delete)** | DELETE `/connectors/{id}` com soft delete (IsDeleted + DeletedAt) | Conectores não podem ser limpos | P2_HIGH | FF-01 |
| FF-04 | **Activar/Desactivar conector** | POST `/connectors/{id}/activate` e `/disable` | Status não pode ser controlado via API | P2_HIGH | FF-01 |
| FF-05 | **CRUD de fontes de ingestão** | POST, PUT, DELETE `/ingestion/sources` | Fontes não podem ser geridas | P1_CRITICAL | FF-01 |
| FF-06 | **Formulário de criação de conector** | Modal/page no frontend com validação | CRUD backend inútil sem UI | P1_CRITICAL | FF-01 |
| FF-07 | **Formulário de edição de conector** | Form pre-populated + save | Idem | P1_CRITICAL | FF-02 |
| FF-08 | **Botões activate/disable/delete no detalhe** | Acções no ConnectorDetailPage | Acções não acessíveis | P2_HIGH | FF-03, FF-04 |

### 3.2 Retries e reprocessamento

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-09 | **Retry policy fields no domain** | Adicionar MaxRetryAttempts, RetryBackoffSeconds, TimeoutSeconds a IntegrationConnector | Retry sem limites | P1_CRITICAL | Nenhuma |
| FF-10 | **Retry worker (background job)** | Background service que processa retry queue com backoff exponencial | Retry marcado mas nunca executado | P1_CRITICAL | FF-09 |
| FF-11 | **Reprocess worker** | Background service que reprocessa execuções falhadas | Reprocess marcado mas nunca executado | P1_CRITICAL | Nenhuma |
| FF-12 | **Stuck execution detector** | Background job que detecta execuções Running > N minutos | Execuções orphaned | P2_HIGH | Nenhuma |

### 3.3 Integração com Notifications

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-13 | **Publicar ConnectorFailedEvent** | Quando RecordFailure() é chamado | Falhas de integração não notificam ninguém | P1_CRITICAL | Nenhuma |
| FF-14 | **Publicar ConnectorHealthChangedEvent** | Quando health transiciona (Healthy→Degraded, etc.) | Degradação silenciosa | P1_CRITICAL | Nenhuma |

### 3.4 Integração com Audit

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-15 | **Publicar audit events para todas as acções de escrita** | Create, Update, Delete, Retry, Reprocess, Activate, Disable | Zero rastreabilidade | P0_BLOCKER | Nenhuma |
| FF-16 | **Criar IntegrationDomainEvents.cs** | Ficheiro dedicado com todos os domain events do módulo | Eventos inexistentes | P1_CRITICAL | Nenhuma |

### 3.5 Webhooks

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-17 | **Endpoint de webhook** | POST `/webhooks/{connectorId}` com webhook secret validation | Sem recepção push de dados | P1_CRITICAL | FF-01 |

### 3.6 Segurança

| # | Título | Descrição | Impacto | Prioridade | Dependências |
|---|--------|-----------|---------|-----------|-------------|
| FF-18 | **Credential encryption** | Campo `credential_encrypted` com EncryptionInterceptor | API keys em plaintext | P1_CRITICAL | Extracção do módulo |
| FF-19 | **Permissão integrations:admin** | Nova permissão para delete e credential management | Acções destrutivas sem segregação | P1_CRITICAL | Nenhuma |
| FF-20 | **Validação URL de endpoint (anti-SSRF)** | Validar formato e bloquear IPs internos/localhost | Vulnerabilidade SSRF | P1_CRITICAL | Nenhuma |
| FF-21 | **Secret masking no frontend** | Não exibir credentials em API responses/UI | Exposição de secrets | P1_CRITICAL | FF-18 |

---

## 4. Ajustes estruturais

### EST-01: Extrair backend de Governance para módulo independente

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Mover entidades, endpoints, features, configs, repositories de `src/modules/governance/` para `src/modules/integrations/` |
| **Por que é estrutural** | Sem isto, IntegrationsDbContext não pode existir, prefixo `int_` não pode ser aplicado, migrations independentes impossíveis |
| **Impacto** | ~30 ficheiros a mover/recriar, GovernanceDbContext perde 3 DbSets |
| **Dependências** | Nenhuma — pode ser feito imediatamente |

### EST-02: Criar IntegrationsDbContext

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Novo DbContext com 3 DbSets, herdando NexTraceDbContextBase |
| **Por que é estrutural** | Isolamento de persistência, prefixo `int_`, migrations independentes |
| **Impacto** | Herda RLS, Audit, Encryption, Outbox automaticamente |
| **Dependências** | EST-01 |

### EST-03: Aplicar prefixo `int_` às tabelas

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Renomear `gov_integration_connectors` → `int_integration_connectors`, etc. |
| **Por que é estrutural** | Convenção obrigatória de naming do produto |
| **Impacto** | 3 tabelas renomeadas |
| **Dependências** | EST-01, EST-02 |

### EST-04: Adicionar RowVersion (xmin) a Connector e Source

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Adicionar `xmin` como RowVersion para concurrency control |
| **Por que é estrutural** | Sem isto, updates concorrentes podem perder dados |
| **Impacto** | 2 entidades alteradas, migrations |
| **Dependências** | EST-02 |

### EST-05: Adicionar soft delete a Connector e Source

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Campos `IsDeleted` + `DeletedAt`, global query filter |
| **Por que é estrutural** | Eliminação sem rastreabilidade |
| **Impacto** | 2 entidades, query filters |
| **Dependências** | EST-02 |

### EST-06: Tipar AuthenticationMode e PollingMode como enums

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | Substituir strings livres por enums tipados |
| **Por que é estrutural** | Validação impossível com strings livres |
| **Impacto** | 2 novos enums, migration, mapeamento |
| **Dependências** | EST-02 |

### EST-07: Substituir Environment string por EnvironmentId

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | `Environment` string → `EnvironmentId?` (Guid) referenciando Environment Management |
| **Por que é estrutural** | Referência formal vs string livre |
| **Impacto** | Schema change, queries, frontend select |
| **Dependências** | EST-02, Environment Management (02) extraction |

### EST-08: Definir separação PostgreSQL vs ClickHouse

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | PostgreSQL para CRUD transaccional, ClickHouse para execuções históricas e métricas |
| **Por que é estrutural** | Performance a longo prazo com volume crescente |
| **Impacto** | Schema ClickHouse, pipeline de replicação |
| **Dependências** | EST-02, ClickHouse infrastructure ready |

### EST-09: Adicionar CHECK constraints

| Aspecto | Detalhe |
|---------|---------|
| **Descrição** | CHECK constraints em enums, counters, retry limits |
| **Por que é estrutural** | Integridade de dados ao nível da DB |
| **Impacto** | ~10 constraints novas |
| **Dependências** | EST-02 |

---

## 5. Pré-condições para recriar migrations

Antes de apagar as migrations antigas de Governance e gerar nova baseline para Integrations:

| # | Pré-condição | Estado | Bloqueador |
|---|-------------|--------|-----------|
| 1 | **EST-01 completo** — Backend extraído de Governance | ❌ | SIM |
| 2 | **EST-02 completo** — IntegrationsDbContext criado | ❌ | SIM |
| 3 | **EST-03 completo** — Prefixo `int_` definido nas configs | ❌ | SIM |
| 4 | **EST-04 completo** — RowVersion (xmin) configurado | ❌ | SIM |
| 5 | **EST-05 completo** — Soft delete configurado | ❌ | SIM |
| 6 | **EST-06 completo** — Enums tipados configurados | ❌ | SIM |
| 7 | **EST-09 completo** — CHECK constraints definidos | ❌ | SIM |
| 8 | FF-09 completo — Retry policy fields adicionados ao domain | ❌ | SIM |
| 9 | FF-18 completo — Credential encrypted field adicionado | ❌ | SIM |
| 10 | Governance migrations limpas dos componentes de Integrations | ❌ | SIM |
| 11 | Seed data definida para Integrations | ❌ | Não bloqueador |

---

## 6. Critérios de aceite do módulo

### Backend
- [ ] Backend extraído para `src/modules/integrations/` independente
- [ ] `IntegrationsDbContext` funcional com 3 DbSets
- [ ] CRUD completo de conectores (POST, GET, PUT, DELETE)
- [ ] CRUD completo de fontes de ingestão
- [ ] Retry worker funcional com backoff
- [ ] Reprocess worker funcional
- [ ] Webhook endpoint funcional
- [ ] Todos os domain events publicados

### Frontend
- [ ] Formulários de criação/edição de conector
- [ ] Botões activate/disable/delete
- [ ] Sub-rotas no menu lateral (Executions, Freshness)
- [ ] i18n completo (0 strings hardcoded)

### Retries e Status
- [ ] Retry policy configurável por conector
- [ ] Retry worker com exponential backoff
- [ ] Stuck execution detector
- [ ] Cancel execution funcional

### Segurança
- [ ] Permissão `integrations:admin` criada e enforced
- [ ] Credentials encriptadas com EncryptionInterceptor
- [ ] Credentials não expostas em API responses
- [ ] Endpoint URL validado (anti-SSRF)
- [ ] Webhook secret validation

### Auditoria
- [ ] Todas as acções de escrita publicam audit events
- [ ] IntegrationDomainEvents.cs com eventos completos
- [ ] Forward de eventos para Audit & Compliance

### Persistência
- [ ] Tabelas com prefixo `int_`
- [ ] RowVersion (xmin) em Connector e Source
- [ ] Soft delete em Connector e Source
- [ ] CHECK constraints em enums e counters
- [ ] Unique constraint por tenant (Name)

### ClickHouse
- [ ] Schema ClickHouse definido para execuções (pode ser fase posterior)
- [ ] Pipeline de replicação desenhado

### Documentação
- [ ] README.md do módulo
- [ ] API.md com endpoints
- [ ] XML docs em classes públicas
- [ ] Fluxos principais documentados

---

## 7. Ordem recomendada de execução

1. **EST-01: Extrair backend de Governance** — Criar `src/modules/integrations/` com Domain, Application, Infrastructure, API, Contracts
2. **EST-02: Criar IntegrationsDbContext** — Com herança de NexTraceDbContextBase
3. **EST-03: Aplicar prefixo `int_`** — Nas 3 entity configurations
4. **FF-16: Criar IntegrationDomainEvents.cs** — Base para todas as integrações
5. **FF-15: Publicar audit events** — Em todos os métodos de transição
6. **FF-01 + FF-02: CRUD de conectores** — POST e PUT endpoints
7. **FF-05: CRUD de fontes** — POST, PUT, DELETE endpoints
8. **FF-09 + EST-04 + EST-05 + EST-06: Domain enrichment** — Retry policy, RowVersion, soft delete, enums
9. **FF-06 + FF-07: Frontend CRUD** — Formulários de criação/edição
10. **FF-10 + FF-11: Workers** — Retry e reprocess background services
11. **FF-13 + FF-14: Events para Notifications** — ConnectorFailed, HealthChanged
12. **FF-17: Webhook endpoint** — Recepção de dados push
13. **FF-18 + FF-19 + FF-20 + FF-21: Segurança** — Credentials, admin perm, SSRF, masking
14. **FF-12: Stuck execution detector** — Background job
15. **EST-09: CHECK constraints** — Integridade de dados
16. **QW-01 a QW-08: Quick wins** — Menu, i18n, empty states
17. **EST-08: ClickHouse schema** — Preparar pipeline analítico
18. **D-01 a D-08: Documentação** — README, API, fluxos
19. **FF-08: Botões no frontend** — Activate/disable/delete
20. **EST-07: EnvironmentId** — Quando Environment Management estiver extraído

---

## 8. Backlog priorizado

| # | Item | Camada | Prioridade | Tipo | Depende outro módulo? | Sprint | Esforço |
|---|------|--------|-----------|------|----------------------|--------|---------|
| EST-01 | Extrair backend de Governance | Backend | P0_BLOCKER | STRUCTURAL_FIX | NÃO | 1 | 16h |
| EST-02 | Criar IntegrationsDbContext | Backend | P0_BLOCKER | STRUCTURAL_FIX | NÃO | 1 | 4h |
| EST-03 | Aplicar prefixo `int_` | Persistência | P0_BLOCKER | STRUCTURAL_FIX | NÃO | 1 | 2h |
| FF-15 | Audit events para acções de escrita | Backend | P0_BLOCKER | FUNCTIONAL_FIX | NÃO | 2 | 4h |
| FF-16 | IntegrationDomainEvents.cs | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 2h |
| FF-01 | POST `/connectors` (CreateConnector) | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 4h |
| FF-02 | PUT `/connectors/{id}` (UpdateConnector) | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 4h |
| FF-05 | CRUD de fontes de ingestão | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 6h |
| FF-09 | Retry policy fields no domain | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 2h |
| FF-10 | Retry worker (background job) | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 8h |
| FF-11 | Reprocess worker | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 8h |
| FF-13 | ConnectorFailedEvent → Notifications | Backend | P1_CRITICAL | FUNCTIONAL_FIX | SIM (Notifications) | 3 | 2h |
| FF-14 | ConnectorHealthChangedEvent | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 2h |
| FF-17 | Webhook endpoint | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 4 | 8h |
| FF-18 | Credential encryption | Backend | P1_CRITICAL | STRUCTURAL_FIX | NÃO | 4 | 4h |
| FF-19 | Permissão integrations:admin | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 2h |
| FF-20 | Validação URL anti-SSRF | Backend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 2 | 2h |
| FF-06 | Formulário criação conector | Frontend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 8h |
| FF-07 | Formulário edição conector | Frontend | P1_CRITICAL | FUNCTIONAL_FIX | NÃO | 3 | 6h |
| D-01 | README.md do módulo | Docs | P1_CRITICAL | DOC | NÃO | 4 | 3h |
| D-02 | API.md com endpoints | Docs | P1_CRITICAL | DOC | NÃO | 4 | 4h |
| FF-03 | DELETE conector (soft delete) | Backend | P2_HIGH | FUNCTIONAL_FIX | NÃO | 4 | 3h |
| FF-04 | Activate/Disable endpoints | Backend | P2_HIGH | FUNCTIONAL_FIX | NÃO | 4 | 2h |
| FF-08 | Botões activate/disable/delete no frontend | Frontend | P2_HIGH | FUNCTIONAL_FIX | NÃO | 5 | 3h |
| FF-12 | Stuck execution detector | Backend | P2_HIGH | FUNCTIONAL_FIX | NÃO | 5 | 4h |
| FF-21 | Secret masking no frontend | Frontend | P2_HIGH | FUNCTIONAL_FIX | NÃO | 4 | 2h |
| EST-04 | RowVersion (xmin) | Persistência | P2_HIGH | STRUCTURAL_FIX | NÃO | 3 | 2h |
| EST-05 | Soft delete fields | Persistência | P2_HIGH | STRUCTURAL_FIX | NÃO | 3 | 2h |
| EST-06 | Tipar AuthMode/PollingMode como enums | Backend | P2_HIGH | STRUCTURAL_FIX | NÃO | 3 | 3h |
| EST-09 | CHECK constraints | Persistência | P2_HIGH | STRUCTURAL_FIX | NÃO | 5 | 2h |
| D-03 | ENTITIES.md domain model | Docs | P2_HIGH | DOC | NÃO | 5 | 3h |
| D-04 | FLOWS.md fluxos principais | Docs | P2_HIGH | DOC | NÃO | 5 | 3h |
| D-05 | CONNECTORS.md guia | Docs | P2_HIGH | DOC | NÃO | 5 | 2h |
| D-06 | XML docs em classes | Docs | P2_HIGH | DOC | NÃO | 5 | 4h |
| QW-01 | Executions no menu lateral | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 1 | 30min |
| QW-02 | Freshness no menu lateral | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 1 | 30min |
| QW-03 | 202 Accepted em retry/reprocess | Backend | P3_MEDIUM | QUICK_WIN | NÃO | 1 | 1h |
| QW-04 | AuthMode/PollingMode i18n labels | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 1 | 30min |
| QW-05 | AllowedTeams como chips | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 2 | 1h |
| QW-07 | Empty states sources/executions | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 2 | 1h |
| QW-08 | Keys i18n erros integração | Frontend | P3_MEDIUM | QUICK_WIN | NÃO | 2 | 1h |
| EST-07 | Substituir Environment → EnvironmentId | Backend | P3_MEDIUM | STRUCTURAL_FIX | SIM (Env Mgmt) | 6 | 3h |
| EST-08 | ClickHouse schema e pipeline | Infra | P3_MEDIUM | STRUCTURAL_FIX | SIM (ClickHouse) | 6 | 8h |
| D-07 | ONBOARDING.md | Docs | P3_MEDIUM | DOC | NÃO | 6 | 2h |
| D-08 | Enum value docs | Docs | P3_MEDIUM | DOC | NÃO | 5 | 1h |
| QW-06 | CorrelationId tooltip | Frontend | P4_LOW | QUICK_WIN | NÃO | 2 | 30min |

---

## 9. Estimativa de esforço total

| Categoria | Items | Horas |
|-----------|-------|-------|
| Quick wins | 8 | 6h |
| Correcções funcionais | 21 | ~90h |
| Ajustes estruturais | 9 | ~42h |
| Documentação | 8 | 22h |
| **Total** | **46** | **~160h** |

---

## 10. Sprints de execução

| Sprint | Foco | Items | Horas |
|--------|------|-------|-------|
| Sprint 1 | **Extracção e fundação** | EST-01, EST-02, EST-03, QW-01-04 | ~25h |
| Sprint 2 | **CRUD e eventos** | FF-01, FF-02, FF-05, FF-15, FF-16, FF-19, FF-20, QW-05-08 | ~28h |
| Sprint 3 | **Domain enrichment + workers** | FF-09, FF-10, FF-11, FF-13, FF-14, FF-06, FF-07, EST-04-06 | ~41h |
| Sprint 4 | **Webhooks + segurança + docs** | FF-17, FF-18, FF-21, FF-03, FF-04, D-01, D-02 | ~26h |
| Sprint 5 | **Frontend + constraints + docs** | FF-08, FF-12, EST-09, D-03-06, D-08 | ~22h |
| Sprint 6 | **Integração avançada** | EST-07, EST-08, D-07 | ~13h |
