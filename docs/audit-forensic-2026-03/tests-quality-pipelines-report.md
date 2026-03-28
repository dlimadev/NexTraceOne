# Relatório de Testes, Qualidade e Pipelines — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Testes são evidência de conclusão. Um produto enterprise deve ter cobertura dos fluxos críticos, pipelines que bloqueiam regressões, e qualidade verificável sem depender de avaliação subjetiva.

---

## Inventário de Testes

### Backend — Testes .NET

| Área | Ficheiros `.cs` | Projeto |
|---|---|---|
| Catalog tests | 30+ | `tests/modules/catalog/NexTraceOne.Catalog.Tests/` |
| ChangeGovernance tests | — | `tests/modules/changegovernance/` |
| IdentityAccess tests | — | `tests/modules/identityaccess/` |
| AuditCompliance tests | — | `tests/modules/auditcompliance/` |
| AIKnowledge tests | — | `tests/modules/aiknowledge/` |
| Governance tests | — | `tests/modules/governance/` |
| Configuration tests | — | `tests/modules/configuration/` |
| Notifications tests | — | `tests/modules/notifications/` |
| Integrations tests | — | `tests/modules/integrations/` |
| OperationalIntelligence tests | — | `tests/modules/operationalintelligence/` |
| Knowledge tests | — | `tests/modules/knowledge/` |
| ProductAnalytics tests | — | `tests/modules/productanalytics/` |
| BuildingBlocks tests | 5 projetos | `tests/building-blocks/` |
| Integration tests | — | `tests/platform/NexTraceOne.IntegrationTests/` |
| E2E tests (.NET) | — | `tests/platform/NexTraceOne.E2E.Tests/` |
| CLI tests | — | `tests/platform/NexTraceOne.CLI.Tests/` |
| **TOTAL ficheiros .cs** | **407** | — |

**Total confirmado de testes unitários backend:** ~1.447 (conforme `docs/ROADMAP.md`)

### Amostra de Testes do Catalog (Confirmados por Inspeção)

Os seguintes testes foram encontrados em `tests/modules/catalog/`:
- `ContractEvidencePackTests.cs`
- `ContractVersionLifecycleTests.cs`
- `ContractScorecardTests.cs`
- `ContractDraftTests.cs`
- `SoapDraftMetadataTests.cs`
- `ContractVersionTests.cs`
- `EventDraftMetadataTests.cs`
- `BackgroundServiceContractDetailTests.cs`
- `SoapContractDetailTests.cs`
- `ContractSignatureTests.cs`
- `SemanticVersionTests.cs`
- `AsyncApiSpecParserTests.cs`
- `SwaggerDiffCalculatorTests.cs`
- `OpenApiDiffCalculatorTests.cs`
- `WsdlDiffCalculatorTests.cs`
- `ContractRuleEngineTests.cs`

**Avaliação:** Testes de domain e domain services bem cobertos no módulo Catalog.

---

### Frontend — Testes Vitest

| Área | Ficheiros | Observação |
|---|---|---|
| `__tests__/components/` | 10+ | StatCard, Shell, Select, ProtectedRoute, AssistantPanel |
| `__tests__/hooks/` | 1+ | `usePermissions.test.tsx` |
| `__tests__/contexts/` | 2 | `AuthContext.test.tsx`, `EnvironmentContext.test.tsx` |
| `__tests__/pages/` | 2+ | `PromotionPage.test.tsx`, `UnauthorizedPage.test.tsx` |

**Total confirmado:** ~264 testes Vitest passando (conforme `docs/ROADMAP.md`)

### Frontend — E2E Playwright

**Specs mock (`src/frontend/e2e/`):**
- 8 specs confirmados
- `incidents.spec.ts` — usa mock fixtures, não correlação dinâmica real

**Specs real-environment (`src/frontend/e2e-real/`):**
- 5 ficheiros — configuração separada (`playwright.real.config.ts`)
- Não integrados no CI padrão

---

### Load Tests (k6)

**Localização:** `tests/load/`
**Ficheiros:** `package.json`, `config/`, `helpers/`, `scenarios/`
**Cenários:** 5 confirmados
**Thresholds:** não documentados nos ficheiros auditados

---

## CI/CD Pipelines — Análise Detalhada

### `.github/workflows/ci.yml` — CI Principal

**Jobs:**
1. `validate` — Quality Gate: `bash scripts/quality/check-no-demo-artifacts.sh`
2. `build-backend` — `dotnet restore + dotnet build`
3. `test-backend` — `dotnet test` (unit + integration)
4. `build-frontend` — `npm ci + tsc + vite build`
5. `test-frontend` — `vitest run`

**Triggers:** push para `main`, `develop`, `release/**`; PRs para `main`, `develop`

**Gap crítico:** E2E Playwright **não faz parte do CI principal**. `test-frontend` apenas corre Vitest — sem Playwright.

### `.github/workflows/e2e.yml` — E2E Separado

E2E pipeline existe mas não é obrigatório para merge em `main`.

### `.github/workflows/security.yml` — Security Scanning

Scanning de segurança automatizado — confirma foco em segurança do produto.

### `.github/workflows/production.yml` e `staging.yml`

- Aprovação manual para produção confirmada
- Deploy estruturado por ambiente

---

## Análise de Qualidade dos Testes

### Pontos Fortes

1. **Testes de domain bem cobertos** no módulo Catalog (30+ testes de entidades, value objects, services)
2. **Testes de auth** — `AuthContext.test.tsx`, `usePermissions.test.tsx`, `ProtectedRoute.test.tsx`
3. **Anti-demo guardrail** — `check-no-demo-artifacts.sh` bloqueia artifacts de demo no CI
4. **Coverage configurado** — `@vitest/coverage-v8` presente
5. **MSW presente** — `msw` para mocking de API em testes unitários

### Gaps Críticos

| Gap | Impacto | Evidência |
|---|---|---|
| E2E não bloqueia PRs | Regressões podem entrar em main | `ci.yml` sem Playwright |
| `incidents.spec.ts` valida mock data | Testa aparência, não comportamento real | `e2e/incidents.spec.ts` |
| Testes de integração não auditados | Coverage real desconhecida | `tests/platform/NexTraceOne.IntegrationTests/` |
| Testes para Governance mock | Testam handlers que retornam dados fabricados | `tests/modules/governance/` |
| 516 warnings CS8632 nullable | Risco de NullReferenceException em runtime | Build log |

---

## Anti-Demo Guardrail

**Script:** `scripts/quality/check-no-demo-artifacts.sh`

Este script verifica que artifacts de demonstração (mocks explícitos, IsSimulated, etc.) não entram em produção. É executado como **primeiro job** do CI — qualquer violação bloqueia o pipeline.

**Avaliação:** Positivo — mas pode estar a permitir os mocks actuais porque eles não violam os critérios de check. O script deve ser revisto para detectar `IsSimulated: true` em handlers não-governance.

---

## Scripts de Qualidade e Operação

| Script | Propósito | Estado |
|---|---|---|
| `scripts/quality/check-no-demo-artifacts.sh` | Anti-demo | ✅ Ativo |
| `scripts/db/apply-migrations.sh` | Migrações | ✅ Existe |
| `scripts/db/backup.sh` | Backup | ✅ Existe |
| `scripts/db/restore.sh` | Restore | ✅ Existe |
| `scripts/deploy/smoke-check.sh` | Health após deploy | ✅ Existe |
| `scripts/deploy/rollback.sh` | Rollback | ✅ Existe |
| `scripts/observability/verify-pipeline.sh` | Verificação telemetria | ✅ Existe |
| `scripts/performance/smoke-performance.sh` | Performance | ✅ Existe |

---

## Definição de "Pronto" por Módulo (Evidência de Testes)

| Módulo | Testes Unitários | E2E Cobertura | Status |
|---|---|---|---|
| Catalog | ✅ 30+ domain tests | ⚠️ Parcial | READY |
| ChangeGovernance | ✅ Existe | ⚠️ Parcial | READY |
| IdentityAccess | ✅ Existe | ✅ Auth flow testado | READY |
| AuditCompliance | ✅ Existe | ⚠️ | READY |
| Configuration | ✅ Existe | ⚠️ | READY |
| Notifications | ✅ Existe | ⚠️ | READY |
| OperationalIntelligence | ✅ Existe | ❌ incidents.spec usa mock | PARTIAL |
| AIKnowledge | ✅ Existe | ❌ sem E2E real | PARTIAL |
| Governance | ✅ Existe (testa mock) | ❌ | MOCK |

---

## Recomendações

1. **Crítico:** Adicionar Playwright E2E como gate obrigatório de merge para `main` no `ci.yml`
2. **Alta:** Reescrever `incidents.spec.ts` para testar correlação dinâmica real (após implementação)
3. **Alta:** Criar E2E real para AI Assistant após conectar LLM real
4. **Alta:** Revisar `check-no-demo-artifacts.sh` para detectar `IsSimulated: true` em handlers não-governance
5. **Média:** Auditar cobertura de `IntegrationTests` projeto — definir mínimo aceitável
6. **Média:** Documentar thresholds de load tests (`tests/load/`)
7. **Baixa:** Resolver 516 warnings CS8632 nullable

---

*Data: 28 de Março de 2026*
