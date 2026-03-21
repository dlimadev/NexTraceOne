# Phase 7 — AI Applied Capabilities and Intelligent Surfaces: Architecture Overview

## Scope

Phase 7 introduz superfícies inteligentes assistidas por IA para análise de ambientes,
comparação e avaliação de readiness para promoção. O foco é **prevenção**: detectar
problemas ANTES que cheguem à produção, usando IA como motor de análise governada.

## Princípio central

> A IA não é um chatbot genérico — é um motor de análise contextualizado por tenant,
> ambiente, serviço e contrato. O backend é a fonte de verdade.

---

## Módulo Backend: NexTraceOne.AIKnowledge

### Novos features (VSA / CQRS)

| Feature | Namespace | Endpoint |
|---|---|---|
| `AnalyzeNonProdEnvironment` | `Orchestration.Features.AnalyzeNonProdEnvironment` | `POST /api/v1/aiorchestration/analysis/non-prod` |
| `CompareEnvironments` | `Orchestration.Features.CompareEnvironments` | `POST /api/v1/aiorchestration/analysis/compare-environments` |
| `AssessPromotionReadiness` | `Orchestration.Features.AssessPromotionReadiness` | `POST /api/v1/aiorchestration/analysis/promotion-readiness` |

### Padrão arquitetural (vertical slice)

```
Features/
  AnalyzeNonProdEnvironment/
    AnalyzeNonProdEnvironment.cs     ← Command + Validator + Handler + Response
  CompareEnvironments/
    CompareEnvironments.cs
  AssessPromotionReadiness/
    AssessPromotionReadiness.cs
```

Cada feature é uma `static class` contendo:
- `Command`: record sealed com propriedades imutáveis
- `Validator`: FluentValidation com regras de domínio
- `Handler`: `ICommandHandler<Command, Response>` com injeção por primary constructor
- `Response` + tipos auxiliares (records sealed)

### Camadas

```
AIKnowledge.Domain        ← IExternalAIRoutingPort (porta de saída)
AIKnowledge.Application   ← Handler, Validator, Command, Response
AIKnowledge.API           ← Endpoint Minimal API, RequirePermission
```

### Dependency Injection

Registrado em `NexTraceOne.AIKnowledge.Application.Orchestration.DependencyInjection`:
- `MediatR` via `RegisterServicesFromAssembly`
- `IValidator<T>` com `AddTransient` para cada Command

### Autorização

Todos os endpoints requerem `"ai:runtime:write"` via `RequirePermission`.

---

## Módulo Frontend: ai-hub

### Novo componente principal

`src/features/ai-hub/pages/AiAnalysisPage.tsx`

### API Client

`src/features/ai-hub/api/aiGovernance.ts` — adicionados:
- `analyzeNonProdEnvironment(data)`
- `compareEnvironments(data)`
- `assessPromotionReadiness(data)`

### Rota

`/ai/analysis` — protegida com `permission="ai:runtime:write"`

### Sidebar

Item adicionado na seção `aiHub`:
```ts
{ labelKey: 'sidebar.aiAnalysis', to: '/ai/analysis', icon: <BarChart3 />, permission: 'ai:runtime:write', section: 'aiHub' }
```

---

## Tecnologias e bibliotecas utilizadas

| Camada | Tecnologia |
|---|---|
| Backend handlers | C# 13, MediatR, FluentValidation, Ardalis.GuardClauses |
| AI routing | `IExternalAIRoutingPort` (porta de saída, adapter externo) |
| Frontend | React 18, TypeScript, i18next, TailwindCSS, lucide-react |
| Testes backend | xUnit, NSubstitute, FluentAssertions |
| Testes frontend | Vitest, @testing-library/react |
