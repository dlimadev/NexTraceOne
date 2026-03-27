# P4.5 — Contract Versioning, Compatibility & Validation Report

**Data:** 2026-03-26  
**Fase:** P4.5 — Fecho enterprise de Versioning, Compatibility & Validation por tipo contratual  
**Estado:** COMPLETO

---

## 1. Estado anterior

Antes desta fase:

| Aspeto | Estado |
|--------|--------|
| `ContractProtocol` enum | 6 valores: OpenApi, Swagger, Wsdl, AsyncApi, Protobuf, GraphQl — sem WorkerService |
| `BackgroundServiceContract` protocol | Usava `ContractProtocol.OpenApi` como fallback genérico |
| `ContractDiffCalculator` | Roteava WorkerService para `EmptyResult()` (sem diff real) |
| `CanonicalModelBuilder` | Roteava WorkerService para `EmptyModel()` (sem modelo real) |
| `ContractRuleEngine` | Aplicava regra de SecurityDefinition a todos os protocolos, incluindo WorkerService e WSDL |
| `ContractScorecardCalculator` | Penalizava WorkerService por ausência de security schemes (N/A para workers) |
| Tests | 607 passando |

---

## 2. Ficheiros alterados

### Backend — domínio

| Ficheiro | Alteração |
|---------|-----------|
| `ContractProtocol.cs` | + `WorkerService = 6` |
| `CanonicalModelBuilder.cs` | + `BuildFromWorkerService()` + route WorkerService |
| `ContractDiffCalculator.cs` | + route `WorkerService → WorkerServiceDiffCalculator` |
| `ContractRuleEngine.cs` | + regras específicas para WorkerService (W1-W4); SecurityDefinition rule N/A para WSDL |
| `ContractScorecardCalculator.cs` | Protocol-aware: quality/completeness/compatibility/risk scores para WorkerService |

### Backend — novos ficheiros

| Ficheiro | Descrição |
|---------|-----------|
| `BackgroundServiceSpecParser.cs` | Parser de specs JSON de Background Service Contracts |
| `WorkerServiceDiffCalculator.cs` | Diff semântico para workers: trigger, schedule, inputs, outputs, side effects, concurrency |

### Backend — application

| Ficheiro | Alteração |
|---------|-----------|
| `RegisterBackgroundServiceContract.cs` | Protocol changed from `OpenApi` (fallback) → `WorkerService` (explícito) |

### Frontend

| Ficheiro | Alteração |
|---------|-----------|
| `types/index.ts` | `ContractProtocol` + `'WorkerService'` |
| `VersioningSection.tsx` | Protocol-aware diff header com hint por tipo; importação de `Info` e `ContractProtocol` |

### Testes

| Ficheiro | Testes |
|---------|--------|
| `BackgroundServiceSpecParserTests.cs` | 6 testes novos |
| `WorkerServiceDiffCalculatorTests.cs` | 14 testes novos |
| `CanonicalModelBuilderWorkerServiceTests.cs` | 10 testes novos (inclui routing test) |
| `ContractRuleEngineTests.cs` | +5 testes (WorkerService e WSDL security rule) |

---

## 3. Entidades e serviços introduzidos ou ajustados

### `ContractProtocol.WorkerService = 6`

Novo valor do enum que representa Background Service / Worker contracts. Substitui o fallback incorreto de `ContractProtocol.OpenApi` usado anteriormente.

### `BackgroundServiceSpecParser`

Parse de JSON estruturado com campos:
- `serviceName`, `category`, `triggerType`, `scheduleExpression`, `timeoutExpression`, `allowsConcurrency`
- `inputs: { name: type }`, `outputs: { name: type }`, `sideEffects: [string]`

Resiliente: retorna `EmptySpec()` para JSON malformado ou vazio, sem bloquear o pipeline.

### `WorkerServiceDiffCalculator`

Diff semântico por categorias:

| Mudança | Classificação |
|---------|--------------|
| ServiceName changed | Breaking |
| TriggerType changed | Breaking |
| ScheduleExpression changed | Breaking |
| Input removed | Breaking |
| Output removed | Breaking |
| Timeout removed | Breaking |
| AllowsConcurrency: true→false | Breaking |
| Input added | Additive |
| Output added | Additive |
| SideEffect added | Additive |
| Timeout added | Additive |
| AllowsConcurrency: false→true | Additive |
| Category changed | Non-breaking |
| Timeout changed | Non-breaking |
| SideEffect removed | Non-breaking |

### `CanonicalModelBuilder.BuildFromWorkerService()`

Mapeia a spec de Background Service para `ContractCanonicalModel`:
- `Title` = `serviceName`
- `SpecVersion` = `triggerType` (usado nas regras W2/W3)
- `Description` = `scheduleExpression`
- `Operations` = [operação principal com `InputParameters` mapeados de `inputs`]
- `GlobalSchemas` = outputs mapeados como schemas
- `SecuritySchemes` = `[]` (N/A)
- `HasSecurityDefinitions` = `false`
- `HasExamples` = `inputs.Count > 0 || outputs.Count > 0`

### `ContractRuleEngine` — WorkerService rules

| Regra | Severidade | Condição |
|-------|-----------|----------|
| `WorkerOperationMissing` | Error | Nenhuma operação declarada |
| `WorkerTriggerTypeMissing` | Error | SpecVersion vazio |
| `WorkerScheduleMissing` | Warning | TriggerType Cron/Interval sem ScheduleExpression |
| `VersionConsistency` | Info | (herdado) |

**WSDL**: SecurityDefinition rule também removida (WSDL usa WS-Security, não OAuth/API Key).

### `ContractScorecardCalculator` — Protocol-aware

| Dimensão | WorkerService | Outros protocolos |
|---------|--------------|------------------|
| Quality | Security → Trigger type declared | Security defined |
| Completeness | Schedule expression replaces servers | Servers defined |
| Compatibility | No security penalty | Security penalty aplica |
| Risk | No security risk factor | Security risk factor aplica |

---

## 4. Fluxo consolidado de versioning por tipo

| Protocolo | Parser | Diff Calculator | Canonical Model | Rule Engine |
|-----------|--------|-----------------|-----------------|-------------|
| OpenApi | OpenApiSpecParser | OpenApiDiffCalculator | BuildFromOpenApi | Full (7 regras) |
| Swagger | SwaggerSpecParser | SwaggerDiffCalculator | BuildFromSwagger | Full |
| Wsdl | WsdlSpecParser | WsdlDiffCalculator | BuildFromWsdl | Sem SecurityDefinition |
| AsyncApi | AsyncApiSpecParser | AsyncApiDiffCalculator | BuildFromAsyncApi | Full |
| WorkerService | BackgroundServiceSpecParser | WorkerServiceDiffCalculator | BuildFromWorkerService | W1-W4 específicas |
| Protobuf / GraphQl | — | EmptyResult | EmptyModel | Full (fallback) |

---

## 5. Frontend / Studio

- `ContractProtocol` TypeScript type: adicionado `'WorkerService'`
- `VersioningSection`: diff header exibe o protocolo do contrato alvo e hint sobre o que é comparado

---

## 6. Validação

```
dotnet build src/modules/catalog/NexTraceOne.Catalog.Domain/  → OK (0 errors)
dotnet build src/modules/catalog/NexTraceOne.Catalog.API/     → OK (0 errors)
dotnet test  tests/modules/catalog/NexTraceOne.Catalog.Tests/ → 642 passed, 0 failed
```

35 novos testes adicionados (607 → 642).

Erros de frontend pré-existentes (ContractPortalPage.tsx JSX, tipo node) não são introduzidos por esta fase.
