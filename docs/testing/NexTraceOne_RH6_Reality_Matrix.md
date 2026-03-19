# NexTraceOne — RH-6 Reality Matrix

> Source of truth operacional desta fase: revisão de prontidão, baseline estável e planos operacionais da Fase 10.
> Escopo desta matriz: classificar a realidade atual das suites e explicitar a evidência criada em RH-6.

## 1. Classificação das suites

| Suite | Caminho | Classificação | Observação |
|---|---|---|---|
| Backend unit | `tests/building-blocks/**`, `tests/modules/**` | `PARCIAL` | Protege regras e handlers, mas não substitui prova contra PostgreSQL real. |
| Frontend unit | `src/frontend/src/__tests__/**` | `PARCIAL` | Boa proteção de componentes/páginas, mas sem backend real. |
| Integration tests | `tests/platform/NexTraceOne.IntegrationTests/**` | `REAL` | Usa PostgreSQL real via Testcontainers, migrations reais, queries EF reais e reset controlado. |
| .NET E2E API | `tests/platform/NexTraceOne.E2E.Tests/**` | `REAL` | Usa `WebApplicationFactory` + PostgreSQL real + autenticação real; RH-6 removeu o placeholder remanescente. |
| Playwright legado | `src/frontend/e2e/**` | `MOCKADO DEMAIS` | Continua útil para UX rápida, mas depende de interceptação massiva e não serve como prova principal de produção. |
| Playwright real RH-6 | `src/frontend/e2e-real/**` | `REAL` | Sobe frontend + backend + PostgreSQL real e valida fluxos web com autenticação e dados seedados. |

## 2. Fluxos com prova real após RH-6

### Integration tests reais

- IdentityAccess:
  - persistência real de utilizador, papel e membership
  - joins reais por tenant
- Catalog / Source of Truth:
  - listagem/projeção real de serviços/APIs
  - ownership, topology e consumer relationships
- Contracts:
  - versões, artefactos, violações, diffs e drafts via PostgreSQL real
- ChangeGovernance:
  - releases, blast radius, rulesets, workflow e promotion
- OperationalIntelligence:
  - incidentes, mitigation workflows, runtime snapshots, cost snapshots
- AI:
  - `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext`
- Governance / Audit / Developer Portal:
  - persistência e queries reais para contextos antes frágeis

### .NET E2E API reais

- Auth real (`login`, `/me`, proteção de rota)
- Catalog real com dados seedados
- Contract Studio real (`create draft`, `get draft`, `update content`, `update metadata`, `submit-review`)
- Change Governance real (`list releases`, `intelligence`, `review/start`, workflow list)
- Incidents real (`list`, `detail`, `create`)
- AI Assistant real (`create conversation`, `list conversations`, `send message`, `list messages`, fallback explícito)

### Playwright real

- Auth web real
- Service Catalog + Source of Truth reais
- Contracts draft lifecycle real
- Change Governance real
- Incidents real
- AI Assistant real com backend/fallback explícito

## 3. Fluxos ainda frágeis ou parcialmente reais

| Fluxo | Estado | Motivo |
|---|---|---|
| Contract workspace enriquecido | `PARCIAL` | `studioMock.ts` ainda enriquece metadados secundários localmente. |
| Playwright legado | `MOCKADO DEMAIS` | Mantido para cobertura rápida de UI, mas não como evidência principal. |
| AI com provider externo/local real | `MÉDIA CONFIANÇA` | O backend é real; a resposta pode cair em fallback explícito quando não há provider operacional no ambiente. |
| Governance pages preview | `BAIXA CONFIANÇA` | Fora do escopo RH-6; continuam preview e não são evidência produtiva. |

## 4. Cobertura por DbContext

| DbContext | Migration | Seed dev | Integration test real | Nível de confiança |
|---|---|---:|---:|---|
| `IdentityDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `CatalogGraphDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `ContractsDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `DeveloperPortalDbContext` | Sim | Não | Sim | `MÉDIA CONFIANÇA` |
| `ChangeIntelligenceDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `WorkflowDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `PromotionDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `RulesetGovernanceDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `IncidentDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `RuntimeIntelligenceDbContext` | Sim | Não | Sim | `MÉDIA CONFIANÇA` |
| `CostIntelligenceDbContext` | Sim | Não | Sim | `MÉDIA CONFIANÇA` |
| `AiGovernanceDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `ExternalAiDbContext` | Sim | Não | Sim | `MÉDIA CONFIANÇA` |
| `AiOrchestrationDbContext` | Sim | Não | Sim | `MÉDIA CONFIANÇA` |
| `GovernanceDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |
| `AuditDbContext` | Sim | Sim | Sim | `ALTA CONFIANÇA` |

## 5. Massa de teste utilizada

- Utilizadores:
  - `admin@nextraceone.dev`
  - `techlead@nextraceone.dev`
  - `dev@nextraceone.dev`
  - `auditor@nextraceone.dev`
  - `e2e.admin@nextraceone.test`
- Tenants:
  - `NexTrace Corp`
  - `Acme Fintech`
  - `E2E Test Org`
- Serviços / APIs:
  - `Orders Service`, `Payments Service`, `Gateway Service`, etc.
- Contratos:
  - versões seedadas em `seed-catalog.sql`
  - drafts criados dinamicamente nos testes RH-6
- Changes / releases:
  - releases seedadas em `seed-changegovernance.sql`
- Incidentes:
  - incidentes seedados em `seed-incidents.sql`
  - incidentes criados dinamicamente nas suites reais
- AI:
  - providers/models/policies seedados em `seed-aiknowledge.sql`
  - conversas/mensagens criadas dinamicamente nos testes RH-6

## 6. Execução local/CI

### Integration tests reais

```bash
dotnet test tests/platform/NexTraceOne.IntegrationTests/NexTraceOne.IntegrationTests.csproj
```

### .NET E2E reais

```bash
dotnet test tests/platform/NexTraceOne.E2E.Tests/NexTraceOne.E2E.Tests.csproj
```

### Playwright real

```bash
cd src/frontend
npm install
npx playwright install chromium
npm run test:e2e:real
```

## 7. Blockers e limites remanescentes

- Docker continua requisito para RH-6 real (`Testcontainers` e Playwright real stack).
- O suite Playwright real prioriza `chromium` para estabilidade e tempo de execução.
- O fluxo AI é real, mas a confiança da resposta depende do provider disponível; quando indisponível, a evidência válida passa a ser o fallback explícito controlado.
- O catálogo/workspace de contratos ainda tem metadados secundários enriquecidos localmente no frontend; o núcleo do fluxo de draft já está real.
