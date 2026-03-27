# Relatório de Testes, Qualidade e Pipelines — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Inventário de Testes

| Tipo | Quantidade | Estado |
|---|---|---|
| Testes unitários backend (.NET) | ~1.447 | Passando |
| Testes de integração backend | Presentes (Testcontainers PostgreSQL) | Verificar cobertura |
| Testes unitários frontend (Vitest) | ~264 | Passando |
| Testes E2E frontend (Playwright) | 8 specs | Cobertura parcial |
| Testes E2E real-environment | 5 arquivos (e2e-real/) | Configuração separada |
| Testes de carga (k6) | 5 cenários | Thresholds não documentados |
| Testes CLI | Projeto separado (`NexTraceOne.CLI.Tests`) | Presente |

---

## 2. Cobertura por Módulo — Backend

| Módulo | Testes Unitários | Integration Tests | Cobertura Real |
|---|---|---|---|
| Catalog | 53+ | Ausentes (DB real) | Alta para unitários |
| Change Governance | 195+ | Ausentes (DB real) | Alta para unitários |
| Identity Access | 10+ | Presentes (Testcontainers) | Boa |
| Audit Compliance | 5+ | Presentes | Boa |
| Operational Intelligence | 266+ | Presentes | Bons; correlação mock |
| AI Knowledge | 101+ | Parciais | Governance coberto |
| Governance | Presentes | Ausentes | Testes passam mas feature é mock |
| Notifications | 4+ | Ausentes | Baixa |
| Configuration | 5+ | Presentes | Boa |

**Gap crítico:** Testes de Governance passam mas o módulo retorna dados mock — os testes validam contratos de interface, não comportamento real.

---

## 3. Testes E2E Frontend (Playwright)

| Spec | O que Testa | Integração Real? |
|---|---|---|
| app.spec.ts | Auth, navegação, RBAC | Sim (auth mocked) |
| contracts.spec.ts | Lifecycle de contratos | Sim (fixtures estáveis) |
| service-catalog.spec.ts | Listagem e detalhe de serviços | Sim (fixtures) |
| incidents.spec.ts | Incidentes, correlação | Não — usa mock fixtures |
| change-confidence.spec.ts | Change advisory, blast radius | Sim (fixtures) |
| modules.spec.ts | Visibilidade de módulos | Sim |
| refresh-token.spec.ts | Fluxo de refresh token | Sim |
| governance-finops.spec.ts | Governance e FinOps | Sim (backend mock) |

**Gap:** incidents.spec.ts usa mock fixtures — não valida correlação dinâmica real.

**Evidência:** `src/frontend/e2e/*.spec.ts`

---

## 4. Qualidade dos Testes

### Pontos Fortes
- Testcontainers para PostgreSQL real em integration tests
- Fixtures estáveis para E2E (não dependem de dados variáveis)
- `mockAuthSession()` helper para RBAC testing
- k6 com múltiplos cenários de carga

### Problemas
- Governance module: testes passam mas feature é 100% mock — falsa confiança
- IncidentsPage: E2E usa mock data, não valida API real
- AiAssistantPage: sem teste real (100% mock)
- Correlação incident↔change: sem teste de engine real (inexistente)
- Cobertura de código: não rastreada no CI

---

## 5. CI/CD Pipeline — Estado

### ci.yml (Pull Requests e main)
**Status: BOM**

```
validate → check-no-demo-artifacts.sh
build-backend → .NET 10 build
test-backend-unit → 1.447 testes (filtrado: !IntegrationTests & !E2E)
test-backend-integration → PostgreSQL service real
build-frontend → TypeScript strict + ESLint + Vite build
test-frontend → 264 testes
```

**Gaps:**
- Sem gate de cobertura de código
- 516 warnings CS8632 não bloqueiam o build
- Sem E2E gate por PR (apenas nightly)

---

### staging.yml (Merge para main)
**Status: BOM**

```
Build 4 imagens Docker
Tag com SHA + "staging"
Push para GitHub Container Registry
Apply migrations (script bash)
Smoke checks (/live, /ready, frontend HTTP 200)
```

**Gap:** Smoke checks são opcionais (skip se URLs não configuradas). Sem validação que todos os 15+ DbContexts migraram.

---

### production.yml (Deploy manual)
**Status: EXCELENTE**

```
Approval gate manual (GitHub Environment)
Validação de image tag antes de deploy
Apply migrations
Re-tag e push de imagens staging → produção
Smoke checks pós-deploy
Rollback automático disponível
```

**Avaliação:** Controlo rigoroso correto para produção.

---

### e2e.yml (Nightly)
**Status: PARCIAL**

```
Schedule: 03:00 UTC diário
Build backend + docker-compose up (postgres, clickhouse, otel)
Playwright test suite
Upload HTML report (14 dias)
```

**Gap crítico:** E2E não bloqueia PRs — falhas chegam a main sem ser detectadas.

---

### security.yml
**Status: BOM**

```
NuGet dependency scanning (dotnet list --vulnerable)
npm audit (frontend)
CodeQL (C# e JavaScript/TypeScript)
Trivy (Docker scanning — apenas main branch)
SARIF upload para GitHub Security tab
```

**Gap:** Trivy só em main — PRs com imagens vulneráveis não são bloqueados.

---

## 6. Quality Gates — Resumo

| Gate | Estado | Cobertura |
|---|---|---|
| Anti-demo artifacts | ✅ Ativo por PR | `check-no-demo-artifacts.sh` |
| Build success | ✅ Obrigatório | Bloqueia merge |
| Unit tests pass | ✅ Obrigatório | 1.447 tests |
| Integration tests pass | ✅ Obrigatório | Com DB real |
| TypeScript strict | ✅ Obrigatório | tsconfig |
| ESLint | ✅ Obrigatório | Não bloqueador se apenas warnings |
| E2E tests | ❌ Apenas nightly | **Não bloqueia PRs** |
| Code coverage | ❌ Não rastreado | Sem threshold |
| Docker scanning (PRs) | ❌ Apenas main | Trivy limitado |
| Compilation warnings | ❌ Não bloqueador | 516 warnings CS8632 |

---

## 7. Scripts de Qualidade

| Script | Estado | Propósito |
|---|---|---|
| `check-no-demo-artifacts.sh` | READY (20K) | Previne artefatos demo em produção |
| `smoke-check.sh` | READY | Validação pós-deploy |
| `rollback.sh` | READY | Rollback de deployment |
| `smoke-performance.sh` | Verificar | Baseline de performance |
| `verify-pipeline.sh` | Verificar | Health de telemetria |

---

## 8. Recomendações

| Ação | Prioridade | Impacto |
|---|---|---|
| E2E tests como gate obrigatório no merge para main | Alta | Fluxos quebrados não chegam a produção |
| Adicionar Testcontainers para Catalog integration tests | Alta | 53 unit tests sem DB real |
| Adicionar coverage report com threshold mínimo (70%) | Média | Rastrear cobertura real |
| Corrigir/resolver 516 warnings CS8632 nullable | Média | Qualidade de código |
| E2E de incidents com API real (após engine de correlação) | Alta | Valida fluxo 3 |
| Trivy scanning em PRs (não só main) | Média | Segurança pré-merge |
| Documentar SLOs de performance por endpoint | Média | k6 com thresholds definidos |
