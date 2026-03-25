# E17 — Gap Report da Validação Ponta a Ponta

> **Status:** CONCLUÍDO — Gaps documentados  
> **Data:** 2026-03-25  
> **Fase:** E17 — Validação Ponta a Ponta sobre a nova baseline PostgreSQL + ClickHouse  
> **Precedido por:** E16 — Implementação da Estrutura ClickHouse  
> **Sucedido por:** E18 — Estabilização Final  

---

## 1. O Que Funciona Plenamente

### Build e Infraestrutura

| Item | Estado |
|------|--------|
| Build da solução completa (0 erros) | ✅ PASS |
| 20 migrations PostgreSQL (E15 baseline) | ✅ PASS |
| 154 tabelas com prefixos correctos | ✅ PASS |
| Single database `nextraceone` (E14) | ✅ PASS |
| Auto-migrations wave-ordered (E14) | ✅ PASS |
| ClickHouse analytics schema (E16) | ✅ PASS |
| `IAnalyticsWriter` DI (NullWriter por defeito) | ✅ PASS |
| Docker Compose com ambas as DBs | ✅ PASS |

### Módulos Core (Testes Passam)

| Módulo | Testes | Estado |
|--------|--------|--------|
| Identity & Access | 290/290 | ✅ PASS |
| AI & Knowledge | 410/410 | ✅ PASS |
| Audit & Compliance | 113/113 | ✅ PASS |
| Service Catalog | 468/468 | ✅ PASS |
| Change Governance | 198/198 | ✅ PASS |
| Configuration | 251/251 | ✅ PASS |
| Governance | 163/163 | ✅ PASS |
| Notifications | 412/412 | ✅ PASS |
| Operational Intelligence | 323/323 | ✅ PASS |
| CLI | 44/44 | ✅ PASS |

### Segurança e Autorização

| Item | Estado |
|------|--------|
| JWT authentication | ✅ PASS |
| RBAC (7 roles, 70+ permissions) | ✅ PASS |
| Tenant isolation | ✅ PASS |
| Rate limiting (5 políticas) | ✅ PASS |
| CORS restritivo | ✅ PASS |
| Security headers | ✅ PASS |
| CSRF protection | ✅ PASS |
| Audit trail (AuditInterceptor) | ✅ PASS |

### Fluxos Integrados Funcionando

| Fluxo | Estado |
|-------|--------|
| Catalog + Contracts | ✅ WORKING |
| Environment + Change Governance | ✅ WORKING |
| Change Governance + Notifications | ✅ WORKING |
| Identity + Audit | ✅ WORKING |
| OI + Notifications | ✅ WORKING |

---

## 2. O Que Funciona com Gaps

### Módulos PASS_WITH_GAPS

| Módulo | Gap Principal |
|--------|--------------|
| Identity & Access | MFA enforcement, API keys, background expiration workers |
| Configuration | Frontend limitado (1 page test) |
| Service Catalog | BlastRadius sem Catalog queries reais, TransitionAssetLifecycle handler |
| Change Governance | Correlação com incidentes, domain events |
| Notifications | SMTP/Teams reais, background archival workers |
| Operational Intelligence | InMemoryIncidentStore, ClickHouse pipeline não activo |
| Audit & Compliance | EnvironmentId em AuditEvent, retention worker, export |
| Governance | Contém módulos temporários (OI-02/03) ainda não extraídos |

### Fluxos WORKING_WITH_GAPS

| Fluxo | Gap |
|-------|-----|
| Catalog + Change Governance | BlastRadius sem queries Catalog reais |
| AI + Audit | AiAgentExecution existe mas auditoria das execuções limitada |
| Integrations + Audit/Notifications/OI | Módulo não extraído, conectores sem webhook/retry |

---

## 3. O Que Está Parcial

### Módulos PARTIAL

| Módulo | Razão | Dependência |
|--------|-------|------------|
| AI & Knowledge (~65%) | LLM real stub (`GenerateStubResponse`), sem RAG real, sem tool calling, sem IDE extensions | LLM provider real |
| Integrations | Dentro de Governance (OI-02 pendente), sem webhook receiver, sem retry engine | OI-02 extracção |
| Product Analytics | Dentro de Governance (OI-03 pendente), sem ClickHouse pipeline activo | OI-03 extracção |
| Environment Management | Dentro de Identity (OI-04 pendente), env_ tables misturadas com iam_ | OI-04 extracção |

### Fluxo BROKEN

| Fluxo | Causa |
|-------|-------|
| Product Analytics → ClickHouse | `IAnalyticsWriter` não é chamado por nenhum handler; `Analytics:Enabled=false`; sem flush do buffer `pan_analytics_events` |

---

## 4. Blockers Reais Antes do Fechamento Técnico

| # | Blocker | Severidade | Estado |
|---|---------|-----------|--------|
| **B1** | LLM real — `GenerateStubResponse` em produção seria inaceitável | ALTA | ❌ Pendente |
| **B2** | `InMemoryIncidentStore` — incidentes perdidos no restart da aplicação | ALTA | ❌ Pendente |
| **B3** | Product Analytics → ClickHouse não activo | MÉDIA | ❌ Pendente |
| **B4** | `pan_analytics_events` buffer sem worker de purge | MÉDIA | ❌ Pendente |
| **B5** | OI-02/OI-03/OI-04 — módulos sem DbContext próprio | MÉDIA | ❌ Pendente |
| **B6** | E2E tests — falham por ausência de PostgreSQL no CI | ALTA | ❌ Pendente (infra) |

---

## 5. Itens Obrigatórios para o E18

### 5.1 Blockers de Startup/Runtime

| Item | Prioridade |
|------|-----------|
| Substituir `InMemoryIncidentStore` por `IncidentDbContext` real | P1 |
| Activar `IAnalyticsWriter` nos handlers prioritários (OI, Integrations) | P1 |
| Activar `Analytics:Enabled=true` em ambiente de desenvolvimento | P1 |
| Worker de purge para `pan_analytics_events` | P2 |
| Testes E2E com PostgreSQL real (via Testcontainers no CI) | P1 |

### 5.2 Extracções de Módulos (OI gaps)

| Item | Prioridade |
|------|-----------|
| OI-02: extrair `IntegrationsDbContext` do `GovernanceDbContext` | P2 |
| OI-03: extrair `ProductAnalyticsDbContext` do `GovernanceDbContext` | P2 |
| OI-04: extrair `EnvironmentManagement` do `IdentityDbContext` | P3 |

### 5.3 Capacidades Essenciais

| Item | Prioridade |
|------|-----------|
| LLM real (pelo menos um provider: OpenAI ou Ollama local) | P1 |
| SMTP real para Notifications | P2 |
| MFA enforcement background worker | P3 |
| EnvironmentId em AuditEvent | P2 |
| Retention enforcement worker (Audit) | P3 |

---

## 6. Itens que Podem Ficar para Evolução Posterior

| Item | Razão do Adiamento |
|------|------------------|
| RAG real (Retrieval Augmented Generation) | Requer vector store e corpus; alta complexidade |
| Tool calling em AI agents | Requer ferramentas de integração por domínio |
| IDE extensions reais (VS, VS Code) | Requer packaging e distribuição separada |
| SAML/OIDC provider real | Requer infra de IdP para testar |
| Blast radius com Catalog queries reais | Requer query graph + ciclos de teste |
| Webhook receiver para Integrations | Requer parceiros de integração para testar |
| Retry policy engine | Requer tuning por conector/cenário |
| Teams/Slack real para Notifications | Requer tokens e workspaces de teste |
| API Key entity (Identity) | Feature completa, não blocker de core |
| Background access expiration workers | Melhoria operacional, não blocker crítico |
| ClickHouse tuning avançado | Depende de volume real de dados |
| FinOps contextuais completos | Requer dados reais de custo |
| Compliance export | Feature de relatório, não blocker core |

---

## 7. Testes que Ainda Exigem Atenção

| Teste | Estado | Causa |
|-------|--------|-------|
| E2E tests (51 testes) | Falham em CI sem DB | Precisam de PostgreSQL via Testcontainers no CI |
| Security tests (2 flaky) | Pré-existente, flaky | Environment detection inconsistente entre runs |

---

## 8. Gaps de Configuração Residuais

| Gap | Estado |
|-----|--------|
| `appsettings.json` — passwords removidas ✅ | Resolvido no E17 |
| `appsettings.Development.json` — ConfigurationDatabase adicionado ✅ | Resolvido no E17 |
| `appsettings.json` — pool size corrigido para 10 ✅ | Resolvido no E17 |
| Secrets via environment variables em produção | ❌ Documentado, não implementado |
| `NEXTRACE_AUTO_MIGRATE=true` necessário para CI | ❌ Documentado |

---

## 9. Estado Final do Produto após E17

| Dimensão | Estado |
|----------|--------|
| **Build** | ✅ Limpo |
| **Testes unitários/integração** | ✅ 2628+ passando |
| **Startup blocker** | ✅ Corrigido |
| **Baseline PostgreSQL** | ✅ Sólida |
| **Estrutura ClickHouse** | ✅ Pronta |
| **Segurança base** | ✅ Sólida |
| **Autenticação/Autorização** | ✅ Funcional |
| **Módulos core (fluxos domain)** | ✅ Funcionais com gaps documentados |
| **ClickHouse ingestão activa** | ❌ E18 |
| **LLM real** | ❌ E18 |
| **InMemoryIncidentStore** | ❌ E18 |
| **E2E full com DB real** | ❌ E18 |
| **Production readiness** | ❌ E18+ |

**Classificação final: MOSTLY_READY_WITH_STABILIZATION_NEEDED**

O produto tem fundações sólidas e pode ser evoluído de forma controlada. O E18 deve focar nos P1/P2 blockers acima antes do fechamento técnico.
