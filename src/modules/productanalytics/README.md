# ProductAnalytics Module — NexTraceOne

> **Bounded Context:** ProductAnalytics
> **Responsabilidade:** Métricas de adoção do produto, análise de jornadas de utilizador, indicadores de fricção, value milestones e insights sobre como diferentes personas utilizam o NexTraceOne.

---

## Propósito

O módulo ProductAnalytics responde a uma questão crítica para qualquer produto enterprise: **"O produto está a gerar valor real para os utilizadores ou apenas gerando cliques?"**

Ao contrário de ferramentas de analytics genéricas, o ProductAnalytics do NexTraceOne é contextualizado pelo domínio do produto — os eventos rastreados são específicos a ações de valor real (publicar um contrato, completar uma mitigação, utilizar o AI Assistant com sucesso), não apenas page views.

O módulo calcula métricas como:
- **Adoption Score**: frequência de uso normalizada por utilizador activo
- **Value Score**: proporção de eventos de valor real em relação ao total
- **Friction Score**: proporção de eventos de fricção (pesquisa sem resultados, abandono de jornada)
- **Time to First Value (TTFV)**: tempo médio até ao primeiro evento de valor numa sessão
- **Time to Core Value (TTCV)**: tempo médio até ao primeiro evento de valor crítico

Estes indicadores alimentam decisões de produto e são expostos no módulo Governance (Executive Views).

## Bounded Context

| Aspecto | Detalhe |
|---------|---------|
| **Assemblies** | `NexTraceOne.ProductAnalytics.Domain`, `.Application`, `.Infrastructure`, `.API`, `.Contracts` |
| **DbContext** | `ProductAnalyticsDbContext` — banco `ProductAnalyticsDatabase` |
| **Outbox table** | — (módulo principalmente de leitura e ingestão de eventos) |
| **Base URL de API** | `/api/v1/product-analytics` |

---

## Entidade Principal

### AnalyticsEvent

O `AnalyticsEvent` é o registo atómico de qualquer interação significativa de um utilizador com o produto.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `Guid` | Identificador do evento |
| `TenantId` | `Guid` | Isolamento multi-tenant |
| `SessionId` | `string` | ID da sessão do utilizador |
| `UserId` | `Guid?` | ID do utilizador (anónimo se nulo) |
| `Persona` | `string?` | Persona do utilizador (Engineer, TechLead, Architect…) |
| `EventType` | `AnalyticsEventType` | Tipo do evento (ver tabela abaixo) |
| `Module` | `ProductModule` | Módulo do produto onde ocorreu |
| `TeamId` | `string?` | Team associado ao utilizador |
| `DomainId` | `string?` | Domínio de negócio associado |
| `OccurredAt` | `DateTimeOffset` | Quando ocorreu o evento |
| `Metadata` | `JsonDocument?` | Dados adicionais contextuais do evento |

---

## Tipos de Evento

Os eventos são categorizados por impacto no valor do produto:

### Eventos de Valor (aumentam Value Score)

| `AnalyticsEventType` | Descrição |
|---|---|
| `ContractPublished` | Um contrato foi publicado com sucesso |
| `OnboardingStepCompleted` | Passo de onboarding completado |
| `AssistantResponseUsed` | Resposta do AI Assistant foi utilizada pelo utilizador |
| `MitigationWorkflowCompleted` | Fluxo de mitigação de incidente concluído |

### Eventos de Core Value (calculam TTCV)

| `AnalyticsEventType` | Descrição |
|---|---|
| `ContractPublished` | — |
| `MitigationWorkflowCompleted` | — |

### Eventos de Fricção (aumentam Friction Score)

| `AnalyticsEventType` | Descrição |
|---|---|
| `ZeroResultSearch` | Pesquisa retornou zero resultados |
| `EmptyStateEncountered` | Utilizador encontrou estado vazio sem orientação |
| `JourneyAbandoned` | Utilizador abandonou uma jornada sem concluir |

---

## Features Disponíveis

| Feature (Application) | Tipo | Descrição |
|-----------------------|------|-----------|
| `RecordAnalyticsEvent` | Command | Regista um evento de analytics (disparado pelo frontend) |
| `GetAnalyticsSummary` | Query | Resumo consolidado: eventos, utilizadores, scores, top módulos, tendências |
| `GetModuleAdoption` | Query | Dados de adopção por módulo com comparação temporal |
| `GetPersonaUsage` | Query | Distribuição de uso por persona |
| `GetFeatureHeatmap` | Query | Heatmap de features com mais e menos uso |
| `GetFrictionIndicators` | Query | Indicadores de fricção com top pontos problemáticos |
| `GetValueMilestones` | Query | Milestones de valor atingidos (TTFV, TTCV, first contract, etc.) |
| `GetJourneys` | Query | Análise de jornadas — taxa de conclusão, abandono, tempo médio |
| `GetAdoptionFunnel` | Query | Funil de adopção por etapa do produto |

---

## GetAnalyticsSummary — Exemplo de Resposta

```json
{
  "totalEvents": 14832,
  "uniqueUsers": 47,
  "activePersonas": 4,
  "topModules": [
    { "module": "ContractStudio", "moduleName": "Contract Studio", "eventCount": 3821, "uniqueUsers": 31 },
    { "module": "ServiceCatalog", "moduleName": "Service Catalog", "eventCount": 2910, "uniqueUsers": 38 }
  ],
  "adoptionScore": 315.6,
  "valueScore": 22.4,
  "frictionScore": 8.1,
  "avgTimeToFirstValueMinutes": 4.2,
  "avgTimeToCoreValueMinutes": 18.7,
  "trendDirection": "Improving",
  "periodLabel": "last_30d"
}
```

---

## Como os Eventos são Registados

O frontend envia eventos via `POST /api/v1/product-analytics/events` com o seguinte payload:

```typescript
// src/frontend/src/features/product-analytics/api/analytics.ts
export const analyticsApi = {
  recordEvent: (event: {
    sessionId: string;
    eventType: string;
    module: string;
    metadata?: Record<string, unknown>;
  }) => apiClient.post('/api/v1/product-analytics/events', event),
};
```

O backend processa via `RecordAnalyticsEvent` Command:

```csharp
public static class RecordAnalyticsEvent
{
    public sealed record Command(
        string SessionId,
        string EventType,
        string Module,
        string? TeamId,
        string? DomainId,
        JsonDocument? Metadata) : ICommand;

    public sealed class Handler(
        IAnalyticsEventRepository repository,
        ICurrentTenant tenant,
        ICurrentUser user,
        IDateTimeProvider clock) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<AnalyticsEventType>(request.EventType, true, out var eventType))
                return Error.Validation("analytics.unknown_event_type", "Unknown event type.");

            if (!Enum.TryParse<ProductModule>(request.Module, true, out var module))
                return Error.Validation("analytics.unknown_module", "Unknown module.");

            var analyticsEvent = new AnalyticsEvent(
                id: Guid.NewGuid(),
                tenantId: tenant.Id,
                sessionId: request.SessionId,
                userId: user.IsAuthenticated ? user.Id : null,
                persona: user.RoleName,
                eventType: eventType,
                module: module,
                teamId: request.TeamId,
                domainId: request.DomainId,
                occurredAt: clock.UtcNow,
                metadata: request.Metadata);

            await repository.AddAsync(analyticsEvent, cancellationToken);
            return Unit.Value;
        }
    }
}
```

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/v1/product-analytics/events` | Regista evento de analytics |
| `GET` | `/api/v1/product-analytics/summary` | Resumo consolidado com filtros |
| `GET` | `/api/v1/product-analytics/adoption/modules` | Adopção por módulo |
| `GET` | `/api/v1/product-analytics/adoption/personas` | Uso por persona |
| `GET` | `/api/v1/product-analytics/heatmap` | Heatmap de features |
| `GET` | `/api/v1/product-analytics/friction` | Indicadores de fricção |
| `GET` | `/api/v1/product-analytics/value-milestones` | Value milestones |
| `GET` | `/api/v1/product-analytics/journeys` | Análise de jornadas |
| `GET` | `/api/v1/product-analytics/adoption/funnel` | Funil de adopção |

### Filtros disponíveis na maioria dos endpoints

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `persona` | `string?` | Filtrar por persona (`Engineer`, `TechLead`, etc.) |
| `module` | `string?` | Filtrar por módulo do produto |
| `teamId` | `string?` | Filtrar por equipa |
| `domainId` | `string?` | Filtrar por domínio de negócio |
| `range` | `string?` | Período: `last_1d`, `last_7d`, `last_30d` (padrão), `last_90d` |

---

## Frontend

O módulo de analytics está em `src/frontend/src/features/product-analytics/`:

As páginas de analytics são acessíveis principalmente para as personas **Platform Admin**, **Executive** e **Product**. Engineers e Tech Leads têm acesso limitado às métricas do seu próprio domínio/equipa.

---

## Integração com Governance

Os dados de analytics alimentam as **Executive Views** no módulo Governance:
- Adoption Score do produto como indicador de saúde da plataforma
- Top módulos utilizados para priorização de roadmap
- Friction Score para identificação de áreas de melhoria urgente
- TTFV para validação de sucesso no onboarding

---

## Módulos do Produto Rastreados (`ProductModule` enum)

| Valor | Nome Display |
|-------|-------------|
| `AiAssistant` | AI Assistant |
| `SourceOfTruth` | Source of Truth |
| `ChangeIntelligence` | Change Intelligence |
| `ContractStudio` | Contract Studio |
| `ServiceCatalog` | Service Catalog |
| `IntegrationHub` | Integration Hub |
| `ExecutiveViews` | Executive Views |

---

## Registro no DI

```csharp
// Program.cs do ApiHost
builder.Services.AddProductAnalyticsInfrastructure(builder.Configuration);

// appsettings.json
"ConnectionStrings": {
  "ProductAnalyticsDatabase": "Host=localhost;Database=nextraceone_analytics;..."
}
```

---

## Privacidade e Multi-Tenancy

- Todos os eventos são isolados por `TenantId`
- `UserId` é `nullable` — suporta registo de eventos anónimos sem identificação pessoal
- Dados de analytics nunca cruzam fronteiras de tenant
- Conforme LGPD/GDPR, utilizadores podem solicitar remoção de dados de analytics via Admin

---

*Última atualização: Abril 2026.*
