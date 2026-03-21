# PHASE-0 — Finalization Backlog

**Status:** ACTIVE  
**Generated:** 2026-03-21  
**Source:** `docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md`  
**Policy:** `docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md`

---

## Visão Geral

Este backlog reorganiza todo o trabalho de fechamento do NexTraceOne em três categorias:

| Categoria | Descrição | Itens |
|---|---|---|
| **A. Bloqueadores de produção** | Impedem qualquer deploy real | 4 |
| **B. Fechamento funcional core** | Impedem o produto de ser real | 22 |
| **C. Hardening de produção** | Completam robustez e confiabilidade | 10 |

---

## A. BLOQUEADORES DE PRODUÇÃO

> Estes itens **impedem qualquer deploy real**. Devem ser resolvidos antes de qualquer release para qualquer ambiente além de desenvolvimento local.

---

### A-01 — Habilitar IntegrityCheck em produção ✅ CONCLUÍDO (Phase 0)

| Campo | Valor |
|---|---|
| **ID Inventário** | D-001, D-044 |
| **Prioridade** | P0 — Bloqueador absoluto |
| **Área** | Segurança / Configuração |
| **Evidência** | `appsettings.json` linha 22: `"IntegrityCheck": false` — **CORRIGIDO para `true` em Phase 0** |
| **Objetivo** | Garantir que verificações de integridade do sistema estejam activas por default em produção |
| **Definição de pronto** | ✅ `appsettings.json` tem `IntegrityCheck: true`; `appsettings.Development.json` mantém `false`; diferença documentada |
| **Risco de não executar** | Sistema pode inicializar em estado inconsistente sem detecção |
| **Ordem recomendada** | ~~1 (imediato)~~ CONCLUÍDO |

---

### A-02 — Criar pipeline CI/CD mínimo

| Campo | Valor |
|---|---|
| **ID Inventário** | D-003 |
| **Prioridade** | P0 — Bloqueador absoluto |
| **Área** | Infraestrutura |
| **Evidência** | `.github/` contém apenas `copilot-instructions.md` — zero workflows de automação |
| **Objetivo** | Garantir que cada PR passe por build, testes e guardrail automáticos |
| **Definição de pronto** | Pipeline executa em cada PR: `dotnet build`, `dotnet test`, `npm run build`, `vitest run`, `check-no-demo-artifacts.sh` |
| **Risco de não executar** | Padrões proibidos podem ser introduzidos sem detecção; regressões não capturadas |
| **Ordem recomendada** | 2 (antes de qualquer outro deployment) |

---

### A-03 — Criar containerização mínima (Dockerfile + docker-compose)

| Campo | Valor |
|---|---|
| **ID Inventário** | D-004 |
| **Prioridade** | P0 — Bloqueador absoluto |
| **Área** | Infraestrutura |
| **Evidência** | Nenhum `Dockerfile` na raiz ou em `src/platform/`; `docker-compose.yml` de desenvolvimento ausente |
| **Objetivo** | Permitir build e deploy containerizado do ApiHost, BackgroundWorkers e Frontend |
| **Definição de pronto** | `Dockerfile` para ApiHost; `Dockerfile` para Frontend; `docker-compose.dev.yml` sobe stack completa localmente |
| **Risco de não executar** | Impossível deployar em qualquer ambiente gerido (cloud, Kubernetes, etc.) |
| **Ordem recomendada** | 3 |

---

### A-04 — Documentar e controlar processo de migração de banco

| Campo | Valor |
|---|---|
| **ID Inventário** | D-043 |
| **Prioridade** | P0 — Bloqueador absoluto |
| **Área** | Infraestrutura / Segurança |
| **Evidência** | `ApplyDatabaseMigrationsAsync` auto-executa em staging sem pipeline controlado; produção já bloqueia |
| **Objetivo** | Garantir que migrations em staging/QA sejam controladas via pipeline CI/CD, não auto-executadas |
| **Definição de pronto** | Runbook de migração documentado; script de migration runner; `NEXTRACE_AUTO_MIGRATE` documentado com casos de uso |
| **Risco de não executar** | Migration automática não controlada pode corromper dados em staging |
| **Ordem recomendada** | 4 |

---

## B. FECHAMENTO FUNCIONAL CORE

> Estes itens **impedem o produto de ser real**. Sem eles, o NexTraceOne continua sendo um protótipo navegável.

---

### B-01 — Implementar Reliability real (OperationalIntelligence)

| Campo | Valor |
|---|---|
| **ID Inventário** | D-005 a D-011 |
| **Prioridade** | P1 — Fechamento funcional crítico |
| **Área** | OperationalIntelligence / Backend + Frontend |
| **Evidência** | 7 handlers com `IsSimulated = true`; `GenerateSimulatedItems()` em `ListServiceReliability`; `mockServices` em `TeamReliabilityPage.tsx` |
| **Objetivo** | Conectar módulo de Reliability a dados reais ingeridos — SLAs, métricas, alertas |
| **Definição de pronto** | Handlers consultam `RuntimeIntelligenceDatabase`; frontend removido de mocks; `DemoBanner` ausente quando dados reais |
| **Risco de não executar** | Reliability é casca visual — engenheiros e tech leads não podem confiar nos dados |
| **Ordem recomendada** | 5 |
| **Dependências** | Ingestion pipeline com métricas de serviço; schema RuntimeIntelligence |

---

### B-02 — Implementar Governance de IA Externa (AIKnowledge ExternalAI)

| Campo | Valor |
|---|---|
| **ID Inventário** | D-024 a D-029, D-042 |
| **Prioridade** | P1 — Fechamento funcional crítico |
| **Área** | AIKnowledge / ExternalAI — Backend |
| **Evidência** | 6 handlers completamente vazios: `CaptureExternalAIResponse`, `ConfigureExternalAIPolicy`, `ApproveKnowledgeCapture`, `ReuseKnowledgeCapture`, `GetExternalAIUsage`, `ListKnowledgeCaptures` |
| **Objetivo** | Implementar governança real de IA externa — captura, aprovação, reutilização e auditoria |
| **Definição de pronto** | Todos os 6 handlers implementados com persistência real no ExternalAiDatabase; endpoints testados |
| **Risco de não executar** | Governança de IA é completamente não-funcional — pilar central do produto ausente |
| **Ordem recomendada** | 6 |
| **Dependências** | ExternalAiDatabase migrations; modelo de domínio para KnowledgeCapture |

---

### B-03 — Implementar Platform Operations com dados reais

| Campo | Valor |
|---|---|
| **ID Inventário** | D-032 |
| **Prioridade** | P1 — Fechamento funcional crítico |
| **Área** | Operations / Frontend + Backend |
| **Evidência** | `mockSubsystems`, `mockJobs`, `mockQueues`, `mockEvents` em `PlatformOperationsPage.tsx` — 4 arrays hardcoded |
| **Objetivo** | `PlatformOperationsPage` exibe estado real do sistema: health checks, jobs em execução, estado das filas |
| **Definição de pronto** | Endpoint de platform health implementado; frontend conectado; todos os mocks removidos |
| **Risco de não executar** | Operadores da plataforma veem estado fictício — decisões de operação baseadas em dados falsos |
| **Ordem recomendada** | 7 |
| **Dependências** | BackgroundWorkers health API; queue metrics |

---

### B-04 — Implementar ServiceReliabilityDetail com dados reais

| Campo | Valor |
|---|---|
| **ID Inventário** | D-031 |
| **Prioridade** | P1 — Fechamento funcional crítico |
| **Área** | Operations / Frontend |
| **Evidência** | `mockDetails: Record<string, {...}>` em `ServiceReliabilityDetailPage.tsx` — 8+ serviços hardcoded |
| **Objetivo** | Página de detalhe de serviço exibe dados reais de reliability |
| **Definição de pronto** | Página conectada a endpoint real; `mockDetails` removido |
| **Risco de não executar** | Detalhe de serviço sempre mostra dados fictícios |
| **Ordem recomendada** | 8 |
| **Dependências** | B-01 (D-006) |

---

### B-05 — Enriquecer Integration Connectors com campos reais

| Campo | Valor |
|---|---|
| **ID Inventário** | D-036, D-037, D-038 |
| **Prioridade** | P2 — Fechamento funcional importante |
| **Área** | Governance / Backend |
| **Evidência** | `Environment: "Production"` hardcoded; `AuthenticationMode: "OAuth2 App Token"` hardcoded; `AllowedTeams: ["platform-squad"]` hardcoded |
| **Objetivo** | Campos de connector provêm de entidade real do domínio |
| **Definição de pronto** | Campos adicionados à entidade; migration criada; handlers lêem de DB |
| **Risco de não executar** | Dados de connector não reflectem configuração real — governança de integração inoperacional |
| **Ordem recomendada** | 9 |
| **Dependências** | Governance domain model |

---

### B-06 — Implementar contagens reais em Governance Packs

| Campo | Valor |
|---|---|
| **ID Inventário** | D-039, D-040 |
| **Prioridade** | P2 — Fechamento funcional importante |
| **Área** | Governance / Backend |
| **Evidência** | `ScopeCount: 0, RuleCount: 0` hardcoded em `ListGovernancePacks` e `GetGovernancePack` |
| **Objetivo** | Governance Packs mostram contagem real de regras e scopes |
| **Definição de pronto** | Contagens calculadas via query; `GovernanceRuleBinding` implementado quando necessário |
| **Risco de não executar** | Packs parecem vazios — administradores não conseguem avaliar cobertura |
| **Ordem recomendada** | 10 |
| **Dependências** | GovernanceRuleBinding domain |

---

### B-07 — Implementar Automation Audit Trail real

| Campo | Valor |
|---|---|
| **ID Inventário** | D-012 |
| **Prioridade** | P2 — Fechamento funcional importante |
| **Área** | OperationalIntelligence / Backend |
| **Evidência** | `GenerateSimulatedEntries()` em `GetAutomationAuditTrail.cs` |
| **Objetivo** | Audit trail de automação usa persistência real |
| **Definição de pronto** | Entries persistidas em DB; handler lê de DB; `GenerateSimulated*` removido |
| **Risco de não executar** | Audit trail de automação é fictício — risco de compliance |
| **Ordem recomendada** | 11 |
| **Dependências** | AutomationDatabase schema para audit entries |

---

### B-08 — Implementar FinOps real (Governance/FinOps)

| Campo | Valor |
|---|---|
| **ID Inventário** | D-013 a D-023, D-041 |
| **Prioridade** | P1 — Fechamento funcional crítico |
| **Área** | Governance / FinOps — Backend |
| **Evidência** | 11 handlers com `IsSimulated = true`; nenhum schema de dados de custo real |
| **Objetivo** | Módulo FinOps conectado a fonte real de dados de custo cloud/infra |
| **Definição de pronto** | Schema de custo implementado; ingestion de billing ativo; handlers consultam dados reais |
| **Risco de não executar** | Decisões de custo baseadas em dados completamente fictícios — nenhum valor operacional |
| **Ordem recomendada** | 12 |
| **Dependências** | Integração com cloud billing API; CostIntelligenceDatabase schema |

---

### B-09 — Resolver nome de regra em GovernanceWaivers

| Campo | Valor |
|---|---|
| **ID Inventário** | D-047 |
| **Prioridade** | P3 — Hardening |
| **Área** | Governance / Backend |
| **Evidência** | `RuleName: w.RuleId ?? "(Entire Pack)"` — exibe ID em vez de nome |
| **Objetivo** | Waivers mostram nome descritivo da regra |
| **Definição de pronto** | Lookup de nome implementado via join ou repositório; exibe nome real |
| **Risco de não executar** | Interface de waivers exibe IDs crípticos |
| **Ordem recomendada** | 13 |
| **Dependências** | B-06 |

---

### B-10 — Conectar ProductAnalytics a dados reais

| Campo | Valor |
|---|---|
| **ID Inventário** | D-033, D-034, D-035 |
| **Prioridade** | P2 — Fechamento funcional importante |
| **Área** | Product Analytics / Frontend + Backend |
| **Evidência** | `mockPersonas`, `mockMilestones`, `mockJourneys` em 3 páginas |
| **Objetivo** | Product Analytics exibe dados reais de uso e jornada do utilizador |
| **Definição de pronto** | Pipeline de analytics colecta dados reais; endpoints implementados; frontend conectado |
| **Risco de não executar** | Product team toma decisões baseadas em dados completamente fictícios |
| **Ordem recomendada** | 14 |
| **Dependências** | Analytics pipeline; evento de tracking implementado |

---

## C. HARDENING DE PRODUÇÃO

> Itens que completam robustez, observabilidade e documentação operacional.

---

### C-01 — Runbooks de operação

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Documentação Operacional |
| **Objetivo** | Runbooks para cenários de falha comuns: DB down, migration falhou, health check failing, queue backup |
| **Definição de pronto** | Pelo menos 5 runbooks em `docs/runbooks/`; cada um com diagnóstico, mitigação e escalonamento |
| **Ordem recomendada** | 15 |

---

### C-02 — Observabilidade mínima de produção

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Observabilidade |
| **Objetivo** | Alertas para: API error rate > threshold, DB connection pool exhaustion, handler latency p99 |
| **Definição de pronto** | Alertas configurados no stack de observabilidade (OTel Collector já configurado); dashboards mínimos |
| **Ordem recomendada** | 16 |

---

### C-03 — Cobertura de testes para módulos críticos

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Qualidade |
| **Objetivo** | Cobertura de testes unitários e de integração para Reliability, FinOps e AI Governance após implementação real |
| **Definição de pronto** | Cobertura mínima de 80% em handlers implementados; testes de integração para endpoints críticos |
| **Ordem recomendada** | 17 |

---

### C-04 — Health check endpoints

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Infraestrutura |
| **Objetivo** | Endpoints `/health/live` e `/health/ready` com checks de DB, queues e serviços externos |
| **Definição de pronto** | Health checks respondem correctamente; integrados no docker-compose e CI |
| **Ordem recomendada** | 18 |

---

### C-05 — Documentação de deployment

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Documentação |
| **Objetivo** | Guia de deployment para staging e produção: variáveis de ambiente, segredos, migrations, health checks |
| **Definição de pronto** | `docs/deployment/DEPLOYMENT-GUIDE.md` completo e verificado |
| **Ordem recomendada** | 19 |

---

### C-06 — Validação de startup para configuração obrigatória

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Segurança / Configuração |
| **Objetivo** | Startup falha imediatamente se variáveis críticas ausentes: JWT Secret, DB connections, AI keys |
| **Definição de pronto** | `StartupValidation.cs` valida todas as configurações críticas; erro claro em startup se ausentes |
| **Ordem recomendada** | 20 |

---

### C-07 — Rate limiting e proteção de APIs

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Segurança |
| **Objetivo** | Rate limiting configurado e testado para endpoints públicos e de alta frequência |
| **Definição de pronto** | Rate limiter activo; configuração documentada; teste de carga básico passando |
| **Ordem recomendada** | 21 |

---

### C-08 — Políticas de CORS revisadas

| Campo | Valor |
|---|---|
| **Prioridade** | P3 — Hardening |
| **Área** | Segurança |
| **Objetivo** | CORS configurado com origins explícitos por ambiente — não wildcard em produção |
| **Definição de pronto** | CORS usa `AllowedOrigins` configurado por ambiente; wildcard apenas em desenvolvimento |
| **Ordem recomendada** | 22 |

---

### C-09 — Documentação de API (OpenAPI completo)

| Campo | Valor |
|---|---|
| **Prioridade** | P4 — Polimento |
| **Área** | Documentação |
| **Objetivo** | Todos os endpoints documentados com exemplos, descrições e erros tipados no OpenAPI spec |
| **Definição de pronto** | Swagger UI completo; todos os endpoints têm summary, description e examples |
| **Ordem recomendada** | 23 |

---

### C-10 — Testes E2E de fluxos críticos

| Campo | Valor |
|---|---|
| **Prioridade** | P4 — Polimento |
| **Área** | Qualidade |
| **Objetivo** | Testes E2E para fluxos críticos: login, criação de contrato, consulta de change, incident investigation |
| **Definição de pronto** | Pelo menos 5 testes E2E passando contra stack real |
| **Ordem recomendada** | 24 |

---

## Ordem de Execução Recomendada

```
Fase 0 (esta fase):
  ✅ A-02 (parcial) — Guardrail script criado (A-02 completo requer CI/CD em Fase 1)
  ✅ D-002 — ReactQueryDevtools guard corrigido

Fase 1 (próxima — Bloqueadores):
  → A-01 IntegrityCheck
  → A-02 CI/CD pipeline
  → A-03 Containerização
  → A-04 Processo de migração

Fase 2 (Fechamento funcional crítico):
  → B-01 Reliability real
  → B-02 AI Governance ExternalAI
  → B-03 Platform Operations real
  → B-04 ServiceReliabilityDetail real
  → B-05 Integration Connectors enrichment
  → B-06 Governance Packs counts
  → B-07 Automation Audit Trail

Fase 3 (Fechamento funcional importante):
  → B-08 FinOps real
  → B-09 GovernanceWaivers rule name
  → B-10 ProductAnalytics real

Fase 4 (Hardening):
  → C-01 a C-08

Fase 5 (Polimento):
  → C-09, C-10
```
