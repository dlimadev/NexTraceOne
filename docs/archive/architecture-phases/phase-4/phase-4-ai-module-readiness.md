# Phase 4 — AI Module Readiness

## AI Readiness Surfaces

As interfaces de superfície expostas nesta fase criam a base para consultas contextuais
pela IA interna do NexTraceOne, seguindo os princípios de:

- **Tenant isolation** — toda consulta filtra por TenantId obrigatório
- **Context-aware** — filtragem opcional por EnvironmentId
- **Non-production signals** — detecção de riscos em ambientes de staging/dev antes de prod

## IIncidentContextSurface

```
Namespace: NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions
```

Expõe:
- `ListByContextAsync` — incidentes filtrados por tenant + ambiente + período
- `GetSeverityCountByContextAsync` — contagem por severidade para scoring de readiness
- `ListNonProductionSignalsAsync` — sinais de risco em ambientes não produtivos

**Caso de uso principal**: A IA analisa incidentes em staging antes de uma promoção para produção,
identificando padrões que indicam risco elevado.

## IReleaseContextSurface

```
Namespace: NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions
```

Expõe:
- `ListByContextAsync` — releases filtradas por tenant + ambiente + serviço + período
- `ListNonProductionReleasesAsync` — releases recentes em ambientes não produtivos

**Caso de uso principal**: A IA compara releases entre staging e produção para detectar
regressões ou avaliar o risco de promoção de uma versão.

## Padrão Cross-Module

As interfaces vivem nas camadas `Application` dos seus respectivos módulos.
As implementações stub vivem nas camadas `Infrastructure`, com acesso direto aos DbContexts.
O módulo de IA consumirá essas interfaces via DI, sem depender diretamente das implementações.

## Próximos Passos

- Implementações reais com lógica de análise e scoring
- Adição de `IsProductionLike` filtering usando o campo `EnvironmentId` + lookup no IdentityAccess
- Integração com o AI Assistant (AIOps Insights)
