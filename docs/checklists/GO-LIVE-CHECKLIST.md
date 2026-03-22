# Checklist de Go-Live — NexTraceOne

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22  
> Release: ZR-6 (Fase 8)

---

## Instruções de Uso

Este checklist deve ser preenchido pelo Release Readiness Lead antes de cada deploy de produção.  
**Nenhum item P0 pode estar incompleto sem evidência de mitigação aprovada.**

| Status | Significado |
|--------|-------------|
| ✅ | Concluído com evidência |
| ⚠️ | Parcial — risco aceitável documentado |
| ❌ | Incompleto — bloqueante para go-live |
| 🔵 | N/A para esta release |

---

## Bloco 1 — Segurança

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 1.1 | JWT_SECRET configurado com ≥ 32 chars em produção | ✅ | `StartupValidation.cs` rejeita startup sem secret válido | Platform Admin | P0 — sistema não inicia |
| 1.2 | Connection strings de produção não estão em appsettings.json | ✅ | `appsettings.json` tem strings vazias; credentials via environment variables | Platform Admin | P0 — exposição de credenciais |
| 1.3 | Security scan (SAST) sem vulnerabilidades críticas | ✅ | `security.yml` workflow — Trivy + dependency scan | Tech Lead | P0 — vulnerabilidade explorável |
| 1.4 | Endpoints protegidos retornam 401 sem token | ✅ | `AuthApiFlowTests` + `SystemHealthFlowTests` | Engineer | P0 — acesso não autorizado |
| 1.5 | Demo artifacts removidos de produção | ✅ | `scripts/quality/check-no-demo-artifacts.sh` | Engineer | P1 — dados de demo em prod |

---

## Bloco 2 — Configuração e Infraestrutura

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 2.1 | Docker images buildadas e publicadas no registry | ✅ | `staging.yml` job `build-images` | DevOps | P0 — deploy impossível |
| 2.2 | docker-compose.yml testado e funcional em staging | ✅ | Deploy em staging executado | DevOps | P0 — deploy impossível |
| 2.3 | Variáveis de ambiente de produção configuradas | ⚠️ | Dependente do environment de prod no GitHub | Platform Admin | P0 — startup falhando |
| 2.4 | Backups de banco configurados em produção | ⚠️ | Dependente da infra de produção | Platform Admin | P1 — perda de dados em falha |
| 2.5 | Health checks (`/live`, `/ready`, `/health`) respondendo | ✅ | `SystemHealthFlowTests` + `ReleaseCandidateSmokeFlowTests` | Engineer | P0 — sistema não monitorável |

---

## Bloco 3 — Migrations e Banco de Dados

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 3.1 | Todas as migrations aplicadas em staging | ✅ | `CriticalFlowsPostgreSqlTests` valida contagem de migrations | Engineer | P0 — banco inconsistente |
| 3.2 | 4 databases consolidados criados (ADR-001) | ✅ | `PostgreSqlIntegrationFixture` — integração validada | Engineer | P0 — dados inacessíveis |
| 3.3 | Script `apply-migrations.sh` testado | ✅ | `scripts/db/apply-migrations.sh` | DevOps | P1 — migrations manuais não executáveis |
| 3.4 | Tabelas críticas existem após migrations | ✅ | `ExtendedDbContextsPostgreSqlTests` | Engineer | P0 — funcionalidade quebrada |
| 3.5 | Rollback de migration documentado | ✅ | `MIGRATION-FAILURE-RUNBOOK.md` | Tech Lead | P1 — sem plano de recuperação |

---

## Bloco 4 — Build e Pipeline

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 4.1 | Build sem erros (0 errors) | ✅ | CI `ci.yml` — última execução bem-sucedida | Engineer | P0 — impossível deployar |
| 4.2 | Unit tests passando (0 failures) | ✅ | CI `ci.yml` — IdentityAccess 280+, AIKnowledge 266+, OI 283+ | Engineer | P0 — lógica de negócio com bug |
| 4.3 | Integration tests passando | ✅ | `CriticalFlowsPostgreSqlTests`, `DeepCoveragePostgreSqlTests`, etc. | Engineer | P1 — integração quebrada |
| 4.4 | E2E mínimos passando | ✅ | `ReleaseCandidateSmokeFlowTests`, `AuthApiFlowTests` | Engineer | P0 — jornada crítica quebrada |
| 4.5 | Contract boundary tests passando | ✅ | `ContractBoundaryTests.cs` — Fase 8 | Engineer | P1 — fronteira cross-module quebrada |
| 4.6 | Frontend TypeScript compilando sem erros | ✅ | `npx tsc --noEmit` — 0 erros | Frontend Eng | P1 — UI quebrada |
| 4.7 | Frontend tests passando (unit) | ✅ | `vitest run` — 394/394 passando | Frontend Eng | P1 — componentes com bug |

---

## Bloco 5 — Funcionalidades Críticas

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 5.1 | Autenticação/login funcionando | ✅ | `AuthApiFlowTests` + E2E | Engineer | P0 — produto inutilizável |
| 5.2 | Service Catalog / Source of Truth | ✅ | `ReleaseCandidateSmokeFlowTests` + unit tests | Engineer | P0 — pillar central quebrado |
| 5.3 | Contratos de API (CRUD + summary) | ✅ | `ContractGovernanceApplicationTests` + E2E smoke | Engineer | P0 — governança de contratos quebrada |
| 5.4 | Releases / ChangeGovernance | ✅ | `CriticalFlowsPostgreSqlTests` + E2E smoke | Engineer | P0 — change intelligence quebrado |
| 5.5 | Incidents / Operational Intelligence | ✅ | Integration tests + E2E | Engineer | P1 — operações não rastreáveis |
| 5.6 | AI Assistant (fluxo básico) | ✅ | `AiGovernancePostgreSqlTests` + unit tests de isolamento | Engineer | P1 — AI hub quebrado |
| 5.7 | Governance básico | ✅ | `GovernanceWorkflowPostgreSqlTests` | Engineer | P1 — governance quebrado |
| 5.8 | Auditoria e trilha | ✅ | `AuditDbContext` migrations + integration tests | Engineer | P1 — compliance sem rastreabilidade |

---

## Bloco 6 — Observabilidade e Operação

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 6.1 | Logs estruturados configurados | ✅ | Serilog configurado no ApiHost | Engineer | P1 — diagnóstico difícil |
| 6.2 | Smoke checks pós-deploy documentados | ✅ | `POST-DEPLOY-VALIDATION.md` | DevOps | P1 — deploy sem validação |
| 6.3 | Runbook de deploy de produção pronto | ✅ | `PRODUCTION-DEPLOY-RUNBOOK.md` | Tech Lead | P1 — operação sem guia |
| 6.4 | Runbook de rollback pronto | ✅ | `ROLLBACK-RUNBOOK.md` | Tech Lead | P1 — sem plano de rollback |
| 6.5 | Playbook de incidentes pronto | ✅ | `INCIDENT-RESPONSE-PLAYBOOK.md` | Tech Lead | P1 — resposta a incidente improvável |
| 6.6 | Runbook de AI provider pronto | ✅ | `AI-PROVIDER-DEGRADATION-RUNBOOK.md` | Tech Lead | P1 — degradação AI sem plano |
| 6.7 | Runbook de migration pronto | ✅ | `MIGRATION-FAILURE-RUNBOOK.md` | Tech Lead | P1 — falha de migration sem plano |

---

## Bloco 7 — Scope e Features

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 7.1 | releaseScope.ts define exclusões corretas (ZR-6) | ✅ | 13 prefixos de rota excluídos; routes mantidos validados | Frontend Eng | P1 — features incompletas expostas |
| 7.2 | Features fora de scope não acessíveis via UI | ✅ | releaseScope guard em rotas | Frontend Eng | P2 — confusão de utilizador |
| 7.3 | Pendências conhecidas documentadas | ✅ | `PHASE-8-GO-LIVE-READINESS-REPORT.md` | Tech Lead | P2 — surpresas pós-go-live |

---

## Bloco 8 — Performance Mínima

| # | Item | Status | Evidência | Owner | Risco se incompleto |
|---|------|--------|-----------|-------|---------------------|
| 8.1 | `/live` responde em < 100ms | ✅ | Medido em ambiente de dev (< 15ms) | Engineer | P0 — sistema não responsivo |
| 8.2 | Login responde em < 1s | ✅ | Medido em ambiente de dev (< 350ms p95) | Engineer | P1 — UX inaceitável |
| 8.3 | Endpoints catalog respondem em < 800ms | ✅ | Medido em ambiente de dev (< 280ms p95) | Engineer | P1 — UX degradada |
| 8.4 | Baseline de performance documentada | ✅ | `PERFORMANCE-AND-RESILIENCE-BASELINE.md` | Engineer | P2 — sem referência para comparação |

---

## Resumo do Checklist

| Bloco | Total | ✅ | ⚠️ | ❌ |
|-------|-------|-----|-----|-----|
| 1 — Segurança | 5 | 5 | 0 | 0 |
| 2 — Infra/Config | 5 | 3 | 2 | 0 |
| 3 — Migrations/DB | 5 | 5 | 0 | 0 |
| 4 — Build/Pipeline | 7 | 7 | 0 | 0 |
| 5 — Funcionalidades | 8 | 8 | 0 | 0 |
| 6 — Operação | 7 | 7 | 0 | 0 |
| 7 — Scope | 3 | 3 | 0 | 0 |
| 8 — Performance | 4 | 4 | 0 | 0 |
| **Total** | **44** | **42** | **2** | **0** |

---

## Itens Pendentes / Riscos Aceites

### 2.3 — Variáveis de ambiente de produção

**Status**: ⚠️ Dependente de configuração do ambiente de produção no GitHub  
**Risco**: Sistema não inicia se JWT_SECRET ou connection strings não configurados  
**Mitigação**: `StartupValidation.cs` detecta e falha rapidamente com mensagem clara; não há silent failure

### 2.4 — Backups de banco em produção

**Status**: ⚠️ Dependente da infraestrutura de produção  
**Risco**: Sem backup, falha de DB pode causar perda de dados  
**Mitigação**: Configurar pgBackup ou equivalente antes do go-live

---

## Decisão Final

Com base neste checklist:

- **0 itens ❌** (bloqueantes)
- **2 itens ⚠️** (dependentes de infraestrutura de produção com mitigações documentadas)
- **42 itens ✅** (concluídos com evidência)

**Recomendação**: Aprovado para Staging Ampliado. Aprovação para Produção condicionada à resolução dos itens 2.3 e 2.4.

---

*Checklist mantido pelo Release Readiness Lead. Preencher antes de cada deploy de produção.*
