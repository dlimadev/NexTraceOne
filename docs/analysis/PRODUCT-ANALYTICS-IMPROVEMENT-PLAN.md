# Plano de Melhoria — Módulo ProductAnalytics

**Data:** 2026-04-19  
**Score atual:** 7.2/10 → **8.6/10** (após implementação)  
**Score alvo:** 9.0/10  
**Esforço total estimado:** ~158h

## Estado de Implementação

| Item | Fase | Status |
|------|------|--------|
| BUG-02 FirstAutomationCreated | 1 | ✅ Implementado |
| BUG-03 Rota analytics→Governance | 1 | ✅ Implementado |
| BUG-04 Dashboard vazio (skeleton journeys) | 1 | ✅ Implementado |
| REFACTOR-01 AnalyticsConstants.cs | 2 | ✅ Implementado |
| REFACTOR-02 PersonaType enum | 2 | ✅ Implementado |
| REFACTOR-03 IConfigurationResolutionService (6 handlers) | 2 | ✅ Implementado |
| REFACTOR-04 3 índices DB + migration | 2 | ✅ Implementado |
| REFACTOR-05 README routes corrigidas | 2 | ✅ Implementado |
| REFACTOR-06 DateTimeOffset? sentinel | 2 | ✅ Implementado |
| REFACTOR-08 RequireRateLimiting | 2 | ✅ Implementado |
| TEST-02 MultiTenantIsolationTests (12 testes) | 3 | ✅ Implementado |
| EdgeCaseTests (6 testes) | 3 | ✅ Implementado |
| FEAT-01 Paginação (GetPersonaUsage, GetModuleAdoption, GetAdoptionFunnel) | 4 | ✅ Implementado |
| FEAT-04 Export CSV/JSON (/export/events, /export/summary) | 4 | ✅ Implementado |
| FEAT-06 OpenAPI .WithTags + .WithSummary em todos endpoints | 4 | ✅ Implementado |
| TEST-05 EdgeCaseValidationTests (16 testes) | 3 | ✅ Implementado |
| FEAT-05 Frontend CohortAnalysisPage.tsx + rota /analytics/cohorts | 4 | ✅ Implementado |
| FEAT-03 Frontend JourneyConfigPage.tsx + rota /analytics/config/journeys | 4 | ✅ Implementado |
| **158/158 testes passam** | — | ✅ |
| BUG-01 Cálculo período anterior | 1 | ℹ️ Lógica atual é correta |
| TEST-01 Testes de integração | 3 | ⏳ Requer PostgreSQL/Docker |
| FEAT-02 Cache Redis | 4 | ⏳ Fora do MVP1 (sem Redis) |
| FEAT-03 Journey/Funnel configurável via DB | 4 | ✅ Implementado |
| FEAT-05 Análise de cohorts | 4 | ✅ Implementado |

---

## Sumário Executivo

O módulo ProductAnalytics possui arquitetura sólida (CQRS, Clean Architecture, multi-tenant), mas apresenta **4 bugs críticos em produção**, **violações sérias de DRY**, **ausência de testes de integração** e **parametrização insuficiente**. Este plano organiza as correções e melhorias em 4 fases priorizadas por impacto e risco.

---

## Fase 1 — Correções Críticas (Bugs em Produção)

> **Prazo sugerido:** Sprint atual  
> **Esforço:** ~14h  
> **Impacto:** Alto — afetam métricas apresentadas aos utilizadores

### BUG-01 — Cálculo de período anterior incorreto

**Arquivo:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetAnalyticsSummary/GetAnalyticsSummaryHandler.cs`  
**Linha:** 74  
**Severidade:** Crítica

**Problema:**
```csharp
// ERRADO — usa `from` como base do período anterior
var (previousFrom, previousTo, _) = ResolveRange(from, request.Range);
```
O período anterior é calculado relativo ao início do período atual (`from`) em vez de `clock.UtcNow`, tornando a comparação de tendências matematicamente incorreta.

**Correção:**
```csharp
// CORRETO — usa UtcNow para calcular o período anterior de forma absoluta
var (previousFrom, previousTo, _) = ResolveRange(clock.UtcNow, request.Range);
```

**Impacto:** Todas as métricas de tendência (AdoptionTrend, ValueTrend, FrictionTrend) exibidas no Overview podem estar invertidas ou incorretas.

---

### BUG-02 — Mapeamento errado de `FirstAutomationCreated`

**Arquivos:**
- `GetJourneys/GetJourneysHandler.cs` — linha 51
- `GetValueMilestones/GetValueMilestonesHandler.cs` — linha 44

**Severidade:** Crítica

**Problema:**
```csharp
// ERRADO — mapeia milestone de automação para evento de onboarding
(ValueMilestoneType.FirstAutomationCreated, AnalyticsEventType.OnboardingStepCompleted)
```

**Correção:**
```csharp
// CORRETO — evento correto para criação de automação
(ValueMilestoneType.FirstAutomationCreated, AnalyticsEventType.AutomationWorkflowManaged)
```

**Impacto:** O milestone "Primeira Automação Criada" nunca é contabilizado. Utilizadores que criaram automações aparecem sem progresso neste milestone.

---

### BUG-03 — Rota `/analytics` mapeada ao módulo errado no frontend

**Arquivo:** `src/frontend/src/features/product-analytics/AnalyticsEventTracker.tsx`  
**Linha:** 28  
**Severidade:** Crítica

**Problema:**
```typescript
// ERRADO — rotas de analytics registadas como módulo 'Governance'
if (pathname.startsWith('/analytics')) return 'Governance';
```

**Correção:**
```typescript
// CORRETO — registar como módulo correto ou remover o mapeamento
if (pathname.startsWith('/analytics')) return 'ProductAnalytics';
```

**Impacto:** Todas as visualizações das páginas de analytics são contabilizadas no módulo Governance, distorcendo métricas de adoção dos dois módulos.

---

### BUG-04 — Dashboard vazio quando não há sessões registadas

**Arquivo:** `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetJourneys/GetJourneysHandler.cs`  
**Linhas:** 78–85  
**Severidade:** Alta

**Problema:**
```csharp
// Handler retorna lista vazia em vez de 5 journeys com 0% de conclusão
if (sessionEventTypes.Count == 0)
{
    return Result<Response>.Success(new Response(
        Journeys: Array.Empty<JourneyDto>(), ...));
}
```

**Correção:** Construir os 5 JourneyDto com todos os steps em `0%` de conversão e `0` utilizadores, em vez de retornar array vazio. O frontend deve sempre mostrar a estrutura de journeys, mesmo sem dados.

**Impacto:** Página `/analytics/journeys` aparece completamente em branco para novos tenants.

---

## Fase 2 — Qualidade e Manutenibilidade

> **Prazo sugerido:** Próxima sprint  
> **Esforço:** ~22h  
> **Impacto:** Médio-Alto — previne divergências futuras e simplifica manutenção

### REFACTOR-01 — Criar `AnalyticsConstants.cs` (DRY crítico)

**Severidade:** Alta  
**Esforço:** 4h

Os 15 mapeamentos de milestone para event type estão **triplicados** em:
- `GetPersonaUsage/GetPersonaUsageHandler.cs` (linhas 35–51)
- `GetValueMilestones/GetValueMilestonesHandler.cs` (linhas 28–45)
- `GetJourneys/GetJourneysHandler.cs` (linhas 44–60)

**Ação:** Criar `NexTraceOne.ProductAnalytics.Application/Constants/AnalyticsConstants.cs` com:
```csharp
public static class AnalyticsConstants
{
    public static readonly IReadOnlyDictionary<ValueMilestoneType, AnalyticsEventType> MilestoneEventMap = ...;
    public static readonly IReadOnlyList<JourneyDefinition> JourneyDefinitions = ...;
    public static readonly IReadOnlyList<FunnelDefinition> FunnelDefinitions = ...;
    public static readonly IReadOnlyList<AnalyticsEventType> ValueEvents = ...;
    public static readonly IReadOnlyList<AnalyticsEventType> FrictionEvents = ...;
    public const double TrendThreshold = 0.05;
    public const string DefaultRange = "last_30d";
    public const int TopModulesLimit = 6;
    public const int TopFeaturesLimit = 5;
}
```

---

### REFACTOR-02 — Extrair constante de personas conhecidas

**Arquivo:** `ProductAnalyticsModuleService.cs` — linha 84  
**Esforço:** 1h

```csharp
// ATUAL — hardcoded, quebra ao adicionar nova persona
const int totalKnownPersonas = 7;

// PROPOSTO — derivado dos valores do enum
var totalKnownPersonas = Enum.GetValues<PersonaType>().Length;
```

---

### REFACTOR-03 — Adicionar `appsettings` para configurações de analytics

**Esforço:** 3h

Criar seção em `appsettings.json`:
```json
{
  "ProductAnalytics": {
    "RetentionDays": 90,
    "TrendThresholdPercent": 5,
    "DefaultRange": "last_30d",
    "MaxRangeDays": 180,
    "TopModulesLimit": 6,
    "TopFeaturesLimit": 5
  }
}
```

Injetar via `IOptions<ProductAnalyticsOptions>` nos handlers relevantes.

---

### REFACTOR-04 — Adicionar índices de base de dados em falta

**Arquivo:** `NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Configurations/AnalyticsEventConfiguration.cs`  
**Esforço:** 2h + migration

Índices a adicionar:
```csharp
// Para user journey analysis e TTFV/TTCV
builder.HasIndex(e => new { e.TenantId, e.UserId, e.OccurredAt })
       .HasDatabaseName("IX_pan_analytics_events_TenantId_UserId_OccurredAt");

// Para session reconstruction (journeys)
builder.HasIndex(e => new { e.SessionId, e.OccurredAt })
       .HasDatabaseName("IX_pan_analytics_events_SessionId_OccurredAt");

// Para heatmap queries
builder.HasIndex(e => new { e.Module, e.EventType })
       .HasDatabaseName("IX_pan_analytics_events_Module_EventType");
```

---

### REFACTOR-05 — Corrigir rota na documentação

**Arquivo:** `src/modules/productanalytics/README.md` — linha 94  
**Esforço:** 30min

Documentação refere `/analytics/*` mas o código usa `/api/v1/product-analytics/*`. Corrigir todas as referências no README.

---

### REFACTOR-06 — Corrigir sentinel `DateTimeOffset.MinValue` em `GetValueMilestones`

**Arquivo:** `GetValueMilestonesHandler.cs` — linhas 101–130  
**Esforço:** 2h

Substituir uso de `DateTimeOffset.MinValue` como sentinel por `DateTimeOffset?` nullable, eliminando risco de comparações incorretas em edge cases.

---

### REFACTOR-07 — Adicionar limite máximo de range nas queries

**Esforço:** 2h

Sem validação de range máximo, um request com dados de 2+ anos pode causar problemas de memória. Adicionar validação:
```csharp
// Em ResolveRange — adicionar limite
if (days > ProductAnalyticsOptions.MaxRangeDays)
    throw new ValidationException($"Range não pode exceder {ProductAnalyticsOptions.MaxRangeDays} dias.");
```

---

### REFACTOR-08 — Rate limiting no endpoint de registo de eventos

**Arquivo:** `NexTraceOne.ProductAnalytics.API/Endpoints/`  
**Esforço:** 3h

Endpoint `POST /api/v1/product-analytics/events` sem rate limiting. Adicionar `[EnableRateLimiting("analytics-events")]` com política de 100 req/min por tenant.

---

## Fase 3 — Testes

> **Prazo sugerido:** Sprint +2  
> **Esforço:** ~52h  
> **Impacto:** Alto — elimina regressões e valida comportamentos críticos

### TEST-01 — Testes de integração com base de dados real

**Esforço:** 20h  
**Framework:** xUnit + Testcontainers (PostgreSQL)

Cenários obrigatórios:
- [ ] `RecordAnalyticsEvent` — inserção e verificação no banco
- [ ] `GetAnalyticsSummary` — cálculo de scores com dados reais
- [ ] `GetJourneys` — reconstrução de jornadas por sessão
- [ ] `GetValueMilestones` — progressão de milestones
- [ ] Cálculo correto de período anterior (validar BUG-01 corrigido)
- [ ] Mapeamento correto de `FirstAutomationCreated` (validar BUG-02 corrigido)

---

### TEST-02 — Testes de isolamento multi-tenant

**Esforço:** 8h  
**Prioridade:** Crítica (risco de segurança)

Garantir que queries de um tenant **nunca** retornam dados de outro tenant:
- [ ] `GetAnalyticsSummary` não vaza dados entre tenants
- [ ] `GetModuleAdoption` respeitando TenantId
- [ ] `GetPersonaUsage` com múltiplos tenants simultâneos
- [ ] `RecordAnalyticsEvent` associa corretamente ao tenant do token JWT

---

### TEST-03 — Testes de performance e escala

**Esforço:** 12h

| Cenário | Meta |
|---------|------|
| `GetAnalyticsSummary` com 1M eventos | < 500ms |
| `GetJourneys` com 100k sessões | < 1s |
| `RecordAnalyticsEvent` throughput | > 500 req/s |
| Query com range `last_90d` | < 2s |

---

### TEST-04 — Testes de concorrência

**Esforço:** 8h

- [ ] Registo simultâneo de eventos (sem race conditions)
- [ ] Queries concorrentes entre tenants
- [ ] Consistência de sessão com eventos paralelos

---

### TEST-05 — Testes de edge cases em validação

**Esforço:** 4h

Cobrir casos em falta nos unit tests existentes:
- [ ] `RecordAnalyticsEvent` com `UserId` null (utilizador anónimo)
- [ ] Range `last_1d` em dia sem eventos
- [ ] Persona inexistente nos filtros
- [ ] Module inválido na query
- [ ] Journey com sessão parcial (apenas step 1 de 3)

---

## Fase 4 — Novas Funcionalidades

> **Prazo sugerido:** Roadmap Q3 2026  
> **Esforço:** ~70h

### FEAT-01 — Paginação em endpoints de listagem

**Endpoints afetados:** `GetPersonaUsage`, `GetModuleAdoption`, `GetAdoptionFunnel`  
**Esforço:** 8h

Adicionar `?page=1&pageSize=20` com resposta paginada:
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 85,
  "totalPages": 5
}
```

---

### FEAT-02 — Cache de queries com Redis

**Esforço:** 12h

Queries são read-heavy e os dados mudam lentamente. Implementar cache com TTL configurável:

| Query | TTL sugerido |
|-------|-------------|
| `GetAnalyticsSummary` | 10 min |
| `GetModuleAdoption` | 15 min |
| `GetJourneys` | 15 min |
| `GetFeatureHeatmap` | 30 min |
| `GetPersonaUsage` | 10 min |

Cache key: `{tenantId}:{feature}:{range}:{persona?}:{module?}`  
Invalidação: novo evento via Outbox publica `AnalyticsEventRecorded` que limpa cache do tenant.

---

### FEAT-03 — Journey e Funnel definitions configuráveis via banco

**Esforço:** 16h

Mover definições hardcoded de journeys e funnels para tabela de configuração:

```sql
CREATE TABLE pan_journey_definitions (
    id          uuid PRIMARY KEY,
    tenant_id   uuid NOT NULL,  -- null = global default
    name        varchar(100) NOT NULL,
    key         varchar(50) NOT NULL,
    steps       jsonb NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL
);
```

Endpoints de gestão: `GET/POST/PUT/DELETE /api/v1/product-analytics/config/journeys`

---

### FEAT-04 — Export de dados (CSV / JSON)

**Esforço:** 12h

Novos endpoints:
- `GET /api/v1/product-analytics/export/events?range=last_30d&format=csv`
- `GET /api/v1/product-analytics/export/summary?range=last_30d&format=json`

Implementar com streaming para ficheiros grandes. Resposta assíncrona via job + download link para exports > 10k rows.

---

### FEAT-05 — Análise de cohorts

**Esforço:** 24h

Agrupar utilizadores por data de primeiro evento e comparar curvas de retenção:
- Novo endpoint: `GET /api/v1/product-analytics/cohorts`
- Parâmetros: `granularity=week|month`, `periods=12`, `metric=retention|activation`
- Nova página frontend: `CohortAnalysisPage.tsx`

---

### FEAT-06 — Documentação OpenAPI / Swagger

**Esforço:** 8h

- Adicionar `[SwaggerOperation]` e `[ProducesResponseType]` em todos os endpoints
- Documentar todos os parâmetros de query com descrições
- Adicionar exemplos de request/response
- Publicar Swagger UI em `/api/v1/product-analytics/docs`

---

## Matriz de Prioridades

| ID | Título | Fase | Esforço | Impacto | Risco se não feito |
|----|--------|------|---------|---------|-------------------|
| BUG-01 | Cálculo período anterior | 1 | 2h | Crítico | Métricas de tendência incorretas |
| BUG-02 | Mapeamento FirstAutomationCreated | 1 | 1h | Crítico | Milestone de automação zerado |
| BUG-03 | Rota analytics → Governance | 1 | 1h | Crítico | Adoção Governance inflada artificialmente |
| BUG-04 | Dashboard vazio sem sessões | 1 | 3h | Alto | UX quebrada para novos tenants |
| REFACTOR-01 | AnalyticsConstants.cs | 2 | 4h | Alto | Mapeamentos divergem entre handlers |
| REFACTOR-04 | Índices em falta | 2 | 2h | Alto | Queries lentas em produção com volume |
| REFACTOR-08 | Rate limiting eventos | 2 | 3h | Alto | Risco de flood/DoS |
| TEST-01 | Testes de integração | 3 | 20h | Alto | Regressões silenciosas em produção |
| TEST-02 | Testes multi-tenant | 3 | 8h | Crítico | Vazamento de dados entre clientes |
| REFACTOR-03 | appsettings ProductAnalytics | 2 | 3h | Médio | Configurações exigem redeploy |
| FEAT-02 | Cache Redis | 4 | 12h | Médio | Latência alta com crescimento de dados |
| FEAT-03 | Journey/Funnel configurável | 4 | 16h | Médio | Mudanças exigem redeploy |
| FEAT-01 | Paginação | 4 | 8h | Médio | UX degradada com muitos dados |
| FEAT-04 | Export CSV/JSON | 4 | 12h | Baixo | Feature ausente, não blocante |
| FEAT-05 | Análise de cohorts | 4 | 24h | Baixo | Feature nova, não blocante |
| FEAT-06 | Documentação OpenAPI | 4 | 8h | Baixo | Developer experience inferior |

---

## Resumo por Fase

| Fase | Items | Esforço | Objetivo |
|------|-------|---------|---------|
| **1 — Bugs Críticos** | 4 | ~14h | Eliminar erros em produção |
| **2 — Qualidade** | 8 | ~22h | Manutenibilidade e robustez |
| **3 — Testes** | 5 | ~52h | Cobertura e confiança |
| **4 — Funcionalidades** | 6 | ~70h | Crescimento do produto |
| **Total** | **23** | **~158h** | Score alvo: 9.0/10 |

---

## Ficheiros Chave para Referência

```
src/modules/productanalytics/
├── NexTraceOne.ProductAnalytics.Application/
│   ├── Features/
│   │   ├── GetAnalyticsSummary/GetAnalyticsSummaryHandler.cs   ← BUG-01
│   │   ├── GetJourneys/GetJourneysHandler.cs                   ← BUG-02, BUG-04
│   │   └── GetValueMilestones/GetValueMilestonesHandler.cs     ← BUG-02
│   └── Constants/                                              ← CRIAR (REFACTOR-01)
├── NexTraceOne.ProductAnalytics.Infrastructure/
│   └── Persistence/Configurations/
│       └── AnalyticsEventConfiguration.cs                      ← REFACTOR-04
└── NexTraceOne.ProductAnalytics.API/
    └── Endpoints/                                              ← REFACTOR-08

src/frontend/src/features/product-analytics/
└── AnalyticsEventTracker.tsx                                   ← BUG-03

tests/modules/productanalytics/
├── Unit/                                                       ← existente
└── Integration/                                               ← CRIAR (TEST-01, TEST-02)
```
