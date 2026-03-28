# Tests, Pipelines & Quality — Gaps, Erros e Pendências

## 1. Estado resumido
20 test projects, ~370 test files, CI pipeline com 5 jobs (validate, build-backend, test-unit, test-integration, build-frontend, test-frontend). Security pipeline com 4 jobs. E2E pipeline com PR trigger. Infraestrutura de testes sólida mas com gaps de cobertura.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos

### 3.1 Integrations Module — 3 Test Files
- **Severidade:** HIGH
- **Classificação:** TEST_GAP
- **Descrição:** `NexTraceOne.Integrations.Tests` tem apenas 3 test files para 42 .cs de implementação. Módulo com processing de payloads real e parsing semântico com cobertura mínima.
- **Impacto:** Alterações em `ProcessIngestionPayload` ou connector logic não são validadas por testes.
- **Evidência:** `tests/modules/integrations/NexTraceOne.Integrations.Tests/` — 3 test files

### 3.2 Product Analytics — 1 Test File
- **Severidade:** HIGH
- **Classificação:** TEST_GAP
- **Descrição:** `NexTraceOne.ProductAnalytics.Tests` tem apenas 1 test file para 26 .cs de implementação.
- **Impacto:** 7 features sem cobertura de teste.
- **Evidência:** `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/` — 1 test file

## 4. Gaps médios

### 4.1 Knowledge Module — 6 Test Files para módulo mínimo
- **Severidade:** MEDIUM
- **Classificação:** PARTIAL
- **Descrição:** 6 test files para 27 .cs. Ratio razoável mas módulo precisa crescer.
- **Evidência:** `tests/modules/knowledge/NexTraceOne.Knowledge.Tests/`

### 4.2 E2E Tests não cobrem Notifications E2E
- **Severidade:** MEDIUM
- **Classificação:** TEST_GAP
- **Descrição:** E2E specs (8 + 5 real-env) cobrem catalog, changes, AI mas não validam fluxo de notificações.
- **Evidência:** `src/frontend/e2e/`

### 4.3 E2E Tests usam fixtures estáticas para Incidents e AI
- **Severidade:** MEDIUM
- **Classificação:** TEST_GAP
- **Descrição:** E2E specs de incidents e AI podem usar fixtures estáticas em vez de validar fluxo real contra backend.
- **Evidência:** `src/frontend/e2e/`

### 4.4 CI Pipeline sem Migration Validation
- **Severidade:** MEDIUM
- **Classificação:** PIPELINE_GAP
- **Descrição:** CI pipeline (`ci.yml`) não valida migrations (ex: `dotnet ef migrations has-pending-model-changes`). Mudanças no modelo podem chegar a main sem migration correspondente.
- **Impacto:** Risco de deploy com schema desactualizado.
- **Evidência:** `.github/workflows/ci.yml` — nenhum step de migration validation

### 4.5 CI Pipeline sem Smoke Check
- **Severidade:** MEDIUM
- **Classificação:** PIPELINE_GAP
- **Descrição:** Script `scripts/deploy/smoke-check.sh` existe mas não é executado no CI. Staging e production pipelines podem não ter smoke check automatizado.
- **Evidência:** `scripts/deploy/smoke-check.sh`, `.github/workflows/ci.yml`

## 5. Itens mock / stub / placeholder
Nenhum nos testes (mocks em testes são legítimos).

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7-12. N/A

## 13. Ações corretivas obrigatórias
1. **HIGH:** Expandir testes para Integrations module (ProcessIngestionPayload, connector logic)
2. **HIGH:** Expandir testes para Product Analytics module
3. **MEDIUM:** Adicionar migration validation step ao CI
4. **MEDIUM:** Adicionar E2E spec para notifications
5. **MEDIUM:** Actualizar E2E specs de incidents e AI para usar backend real (não fixtures estáticas)
6. **MEDIUM:** Integrar smoke-check.sh nos workflows de staging/production

---

## Referência: Test Coverage por Módulo

| Módulo | Test Files | Implementation .cs | Ratio |
|---|---|---|---|
| AIKnowledge | 54 | 287 | 1:5.3 |
| Catalog | 74 | 317 | 1:4.3 |
| Notifications | 49 | 124 | 1:2.5 |
| IdentityAccess | 48 | 185 | 1:3.9 |
| OperationalIntelligence | 33 | 275 | 1:8.3 |
| ChangeGovernance | 17 | 246 | 1:14.5 |
| Configuration | 16 | 67 | 1:4.2 |
| Governance | 13 | 144 | 1:11.1 |
| AuditCompliance | 11 | 56 | 1:5.1 |
| Knowledge | 6 | 27 | 1:4.5 |
| **Integrations** | **3** | **42** | **1:14.0** |
| **ProductAnalytics** | **1** | **26** | **1:26.0** |

## Referência: Pipeline Coverage

| Pipeline | Jobs | Trigger |
|---|---|---|
| `ci.yml` | validate, build-backend, test-unit, test-integration, build-frontend, test-frontend | push main/develop + PR |
| `e2e.yml` | E2E Playwright | PR + nightly |
| `security.yml` | dependency-scan, frontend-audit, codeql, docker-scan | push main + PR + weekly |
| `production.yml` | deployment | manual/tag |
| `staging.yml` | deployment | manual/tag |
