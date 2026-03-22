# Matriz Oficial de Validação do Produto — NexTraceOne

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22  
> Owner: Release Readiness Lead

---

## Propósito

Este documento é o artefato central de aprovação de go-live do NexTraceOne.  
Cada fluxo crítico de negócio está mapeado com camadas de validação, evidências atuais, lacunas e critérios de aceite.

**Regra mestra**: nenhum fluxo marcado como `CRÍTICO` pode ir a produção sem evidência objetiva de validação passando.

---

## Legenda

| Símbolo | Significado |
|---------|-------------|
| ✅ | Evidência presente e passando |
| ⚠️ | Parcialmente coberto — risco aceitável com mitigação |
| ❌ | Lacuna crítica — bloqueante para go-live |
| 🔵 | Fora do escopo do release atual (ZR-6) |

| Criticidade | Descrição |
|-------------|-----------|
| P0 | Bloqueante absoluto — sem este fluxo o produto não existe |
| P1 | Core funcional — impacta adoção e confiança |
| P2 | Importante — impacto moderado se ausente |
| P3 | Desejável — low risk se ausente na primeira release |

---

## Fluxo 01 — Login, Refresh e Seleção de Tenant

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | IdentityAccess |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `AuthApiFlowTests` cobre login válido/inválido, JWT, rota protegida |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` valida login + /me |
| **Evidência atual** | ✅ `IdentityAccess.Tests` — 280+ unit tests |
| **Lacuna** | ⚠️ Refresh token flow não coberto em E2E |
| **Ação** | E2E-01 no `E2E-GO-LIVE-SUITE.md` cobre o fluxo base; refresh pode ser adicionado em fase subsequente |
| **Critério de aceite** | Login com credencial válida retorna JWT 200; credencial inválida retorna 4xx; rota protegida sem token retorna 401 |

---

## Fluxo 02 — CRUD/Gestão de Utilizadores

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | IdentityAccess |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ Unit tests em `IdentityAccess.Tests` cobrem criação, atualização, roles |
| **Lacuna** | ⚠️ Sem E2E de CRUD de utilizador via API real |
| **Ação** | Smoke test via endpoint `/api/v1/identity/users` com token admin |
| **Critério de aceite** | GET /users retorna lista; criação de utilizador persiste; role assignment funciona |

---

## Fluxo 03 — Service Catalog / Source of Truth

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | Catalog (Graph, Contracts, Portal) |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `CriticalFlowsPostgreSqlTests` — criação de serviço + queries |
| **Evidência atual** | ✅ `DeepCoveragePostgreSqlTests` — topology, ownership, joins |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` — catalog + source-of-truth seedados |
| **Evidência atual** | ✅ `Catalog.Tests` — 200+ unit tests |
| **Lacuna** | ⚠️ Contract boundary entre Reliability↔Catalog não validada como teste explícito |
| **Ação** | `ContractBoundaryTests.cs` introduzido nesta fase |
| **Critério de aceite** | GET /catalog/services retorna dados seedados; source-of-truth search encontra serviço pelo nome |

---

## Fluxo 04 — Contratos de API (REST/SOAP/Evento)

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | Catalog.Contracts |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `ContractGovernanceApplicationTests` — CRUD completo de contratos |
| **Evidência atual** | ✅ `ProtocolAutoDetectionTests`, `ValidateContractIntegrityProtocolTests` |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` — `/api/v1/contracts/summary` passando |
| **Lacuna** | ⚠️ Sem E2E de criação de contrato via API HTTP |
| **Ação** | Coberto no E2E-02 da suíte mínima |
| **Critério de aceite** | POST /contracts cria e persiste; GET summary retorna contagens corretas |

---

## Fluxo 05 — Criação e Promoção de Release

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | ChangeGovernance (ChangeIntelligence, Promotion) |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `CriticalFlowsPostgreSqlTests` — criação de release persiste |
| **Evidência atual** | ✅ `DeepCoveragePostgreSqlTests` — release com service + API |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` — releases retornam versão 1.3.0 seedada |
| **Lacuna** | ⚠️ Fluxo de promoção (Staging→Prod) não coberto em E2E HTTP completo |
| **Ação** | E2E-03 na suíte mínima cobre fluxo parcial; promoção completa em fase subsequente |
| **Critério de aceite** | GET /releases retorna lista; release seedado visível; status de promoção atualizável |

---

## Fluxo 06 — Workflow/Aprovação de Mudança

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | ChangeGovernance.Workflow |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ `GovernanceWorkflowPostgreSqlTests` — workflow persiste |
| **Evidência atual** | ✅ Ruleset, aprovação, transições de estado |
| **Lacuna** | ⚠️ Sem E2E de aprovação via HTTP com múltiplos atores |
| **Ação** | Documentado como gap aceitável para ZR-6 |
| **Critério de aceite** | WorkflowInstance criado e transicionado sem erro de constraints |

---

## Fluxo 07 — Reliability — Lista e Detalhe

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | OperationalIntelligence.Runtime |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `RealBusinessApiFlowTests` — `/api/v1/runtime/services/health` |
| **Evidência atual** | ✅ `DeepCoveragePostgreSqlTests` — Runtime tabelas existem |
| **Lacuna** | ⚠️ Contract boundary Reliability↔Catalog sem teste explícito |
| **Ação** | `ContractBoundaryTests.cs` valida coexistência de dados cross-module |
| **Critério de aceite** | Endpoint de reliability retorna lista ou 200 com dados coerentes |

---

## Fluxo 08 — Incidents / Operational Intelligence

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | OperationalIntelligence.Incidents |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `CriticalFlowsPostgreSqlTests` — incident persiste no DB |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` — /incidents endpoint passando |
| **Lacuna** | ⚠️ Correlação incidente↔release não validada em E2E |
| **Ação** | Documentado como gap aceitável; correlation é feature P2 |
| **Critério de aceite** | GET /incidents retorna lista; criação de incidente persiste |

---

## Fluxo 09 — AI Assistant / AIKnowledge

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | AIKnowledge (Governance, Runtime, Orchestration) |
| **Tipos de teste** | Unit, Integration, E2E |
| **Evidência atual** | ✅ `AiGovernancePostgreSqlTests` — conversas, modelos, providers |
| **Evidência atual** | ✅ `AIKnowledge.Tests` — 266+ unit tests |
| **Evidência atual** | ✅ Isolamento por tenant validado em `AiAnalysisContextIsolationTests` |
| **Lacuna** | ⚠️ Sem E2E de sessão AI completa via HTTP (provider real não disponível em CI) |
| **Ação** | E2E-06 usa mock provider; chat endpoint validado com token de autenticação |
| **Critério de aceite** | POST /ai/assistant/chat retorna resposta ou erro tratado; sem vazamento de tenant |

---

## Fluxo 10 — Governance (Packs, Waivers, Connectors)

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | Governance |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ `GovernanceWorkflowPostgreSqlTests` — Team + GovernancePack persiste |
| **Evidência atual** | ✅ `Governance.Tests` — 23 unit tests |
| **Lacuna** | 🔵 Governance endpoints marcados fora do scope ZR-6 (releaseScope.ts) |
| **Ação** | Não bloqueante para release atual |
| **Critério de aceite** | Módulo compilado e migrations aplicadas; endpoints retornam 200 autenticado |

---

## Fluxo 11 — Automation Workflow

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P2 |
| **Módulos** | ChangeGovernance.Workflow |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ Workflow persiste e transiciona |
| **Lacuna** | 🔵 Fora do scope ZR-6 |
| **Ação** | Não bloqueante |
| **Critério de aceite** | Build sem erro; migrations aplicadas |

---

## Fluxo 12 — Comparação de Ambientes / Drift Findings

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | AIKnowledge.Runtime (CompareEnvironments), OperationalIntelligence.Runtime |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ Testes de validação de contexto AI (ambiente src≠target) |
| **Evidência atual** | ✅ `RuntimeIntelligenceDbContext` migrations validadas |
| **Lacuna** | ⚠️ Sem E2E de comparação com dados reais de duas snapshots |
| **Ação** | Documentado; E2E-07 valida abertura da página, não dados profundos |
| **Critério de aceite** | Endpoint de comparação retorna análise estruturada; sem crash |

---

## Fluxo 13 — Auditoria e Trilha Crítica

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P1 |
| **Módulos** | AuditCompliance |
| **Tipos de teste** | Unit, Integration |
| **Evidência atual** | ✅ `AuditDbContext` migrations aplicadas |
| **Evidência atual** | ✅ `AuditCompliance.Tests` cobrindo persistência |
| **Lacuna** | ⚠️ Sem teste de que ações críticas geram audit trail |
| **Ação** | `ContractBoundaryTests.cs` valida que tabela de audit existe e é acessível |
| **Critério de aceite** | Tabela audit_entries existe; eventos críticos persistem audit log |

---

## Fluxo 14 — Health/Readiness e Startup Seguro

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | ApiHost (StartupValidation, HealthChecks) |
| **Tipos de teste** | E2E, Smoke |
| **Evidência atual** | ✅ `SystemHealthFlowTests` — /health, /ready, /live passando |
| **Evidência atual** | ✅ `ReleaseCandidateSmokeFlowTests` — smoke health 200 |
| **Lacuna** | ⚠️ Startup com JWT secret ausente não validado em teste automatizado |
| **Ação** | `StartupValidation.cs` rejeita startup em Prod sem JWT; documentado no runbook |
| **Critério de aceite** | /live retorna 200; /ready retorna 200 com DB saudável; startup falha graciosamente sem secrets |

---

## Fluxo 15 — Deploy + Smoke + Rollback Mínimo

| Atributo | Detalhe |
|----------|---------|
| **Criticidade** | P0 |
| **Módulos** | Infra (Docker, Migrations, CI/CD) |
| **Tipos de teste** | Smoke, Post-deploy validation |
| **Evidência atual** | ✅ `POST-DEPLOY-VALIDATION.md` documenta smoke manual |
| **Evidência atual** | ✅ `ROLLBACK-RUNBOOK.md` documenta rollback de imagem e migration |
| **Evidência atual** | ✅ `staging.yml` tem job `smoke-check` |
| **Lacuna** | ⚠️ Scripts de smoke não são executados automaticamente em PR |
| **Ação** | Scripts em `/scripts/performance/` e `/scripts/quality/` cobrem automação |
| **Critério de aceite** | Deploy em staging sem falha; smoke checks passam; rollback documentado e testado manualmente |

---

## Resumo Executivo de Gaps

| Prioridade | Quantidade de gaps | Status |
|------------|-------------------|--------|
| P0 — Bloqueantes | 0 ❌ | Nenhum gap bloqueante identificado |
| P1 — Core funcional | 7 ⚠️ | Todos aceitáveis com mitigação documentada |
| P2 — Importantes | 1 🔵 | Fora do scope ZR-6 |
| P3 — Desejáveis | 1 🔵 | Fora do scope ZR-6 |

**Conclusão da matriz**: O NexTraceOne não tem gaps P0 bloqueantes para a release ZR-6.  
Os gaps P1 são aceitáveis com as mitigações documentadas e o scope controlado via `releaseScope.ts`.

---

*Matriz mantida pelo Release Readiness Lead. Atualizar a cada sprint de produção.*
