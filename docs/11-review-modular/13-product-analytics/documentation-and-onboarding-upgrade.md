# PARTE 12 — Documentação e Onboarding do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: PLANO DE DOCUMENTAÇÃO

---

## 1. Revisão do module-review.md

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Existe | ✅ | `docs/11-review-modular/13-product-analytics/module-review.md` (3,031 bytes) |
| Conteúdo | ⚠️ Básico | Lista páginas frontend, endpoints, documenta preocupações com dados mock |
| Prioridade | P5 (Support) | Classificação correta para o estado atual |
| Data | 2026-03-24 | Recente |
| Lacunas | ⚠️ | Não referencia ficheiros backend concretos, não analisa qualidade dos dados |

---

## 2. Revisão do module-consolidated-review.md

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Existe | ✅ | `docs/11-review-modular/13-product-analytics/module-consolidated-review.md` (4,151 bytes) |
| Maturidade global | 30% | Corretamente avaliada |
| Backend | 40% | Endpoints funcionam mas dados mistos |
| Frontend | 45% | 5 páginas com UI, dados questionáveis |
| Documentação | 0% | Zero documentação dedicada |
| Testes | 15% | Mínima cobertura |
| Issues críticos | ✅ Identificados | Dados mock, acoplamento Governance, documentação inexistente |
| Lacunas | ⚠️ | Não detalha ficheiros concretos, não propõe backlog |

---

## 3. Documentação ausente

| # | Documento | Prioridade | Justificação |
|---|----------|-----------|--------------|
| 1 | README.md do módulo (quando extraído) | P1_CRITICAL | Onboarding básico |
| 2 | Guia de instrumentação de eventos | P1_CRITICAL | Como cada módulo deve emitir eventos |
| 3 | Catálogo de eventos (event catalog) | P1_CRITICAL | Referência de todos os AnalyticsEventType |
| 4 | Guia de definições de métricas | P2_HIGH | Como criar/configurar métricas |
| 5 | Guia de journeys e milestones | P2_HIGH | Como definir funnels e value milestones |
| 6 | Guia de uso de ClickHouse | P2_HIGH | Schema, queries, materialized views |
| 7 | Diagrama de arquitetura do módulo | P2_HIGH | Fluxo de dados PG ↔ ClickHouse |
| 8 | Guia de troubleshooting | P3_MEDIUM | Problemas comuns |
| 9 | ADR sobre decisão PG vs ClickHouse | P3_MEDIUM | Justificação arquitetural |

---

## 4. Classes e fluxos que precisam de explicação

### Classes do domínio

| Classe | Ficheiro | Explicação necessária |
|--------|---------|----------------------|
| AnalyticsEvent | `Governance.Domain/Entities/AnalyticsEvent.cs` | Propósito, campos, criação, imutabilidade |
| AnalyticsEventType | `Governance.Domain/Enums/AnalyticsEventType.cs` | Significado de cada valor, quando usar |
| AnalyticsEventId | Strongly-typed ID | Padrão de uso, serialização |

### Fluxos principais

| Fluxo | Componentes | Explicação necessária |
|-------|------------|----------------------|
| Captura de evento (frontend) | AnalyticsEventTracker → POST /events → Handler → Repository | Como eventos são capturados e persistidos |
| Captura de evento (backend) | Módulo emissor → POST /events (ou domain event) → Handler | Como módulos emitem eventos |
| Dashboard query | Frontend page → API client → GET endpoint → Handler → Repository → DTO | Como dashboards consomem dados |
| Flush para ClickHouse | PostgreSQL buffer → Outbox → Writer → ClickHouse | Fluxo de replicação (futuro) |
| Cálculo de friction | GetFrictionIndicators → Repository → Filtered events → Comparison | Como friction é calculado |

### Repositories e Services

| Classe | Explicação necessária |
|--------|----------------------|
| AnalyticsEventRepository | Métodos disponíveis, queries otimizadas, filtros |
| IAnalyticsEventRepository | Interface e DTOs de retorno |
| ProductAnalyticsEndpointModule | Mapeamento de endpoints, permissões |

---

## 5. XML docs necessárias

| Classe/Método | Prioridade |
|--------------|-----------|
| AnalyticsEvent (classe) | P1 |
| AnalyticsEvent.Create() (factory method) | P1 |
| AnalyticsEventType (enum, cada valor) | P1 |
| IAnalyticsEventRepository (interface, cada método) | P1 |
| RecordAnalyticsEvent.Command | P2 |
| RecordAnalyticsEvent.Handler | P2 |
| GetAnalyticsSummary.Handler | P2 |
| GetFrictionIndicators.Handler | P2 |
| Todos os DTOs de response | P3 |

---

## 6. Notas de onboarding necessárias

### Para novos developers

| # | Nota | Conteúdo |
|---|------|---------|
| 1 | "Como adicionar um novo tipo de evento" | Atualizar enum, instrumentar no módulo, testar captura |
| 2 | "Como testar captura de eventos localmente" | Configurar, enviar POST, verificar no DB |
| 3 | "Onde vivem os dados" | PG (buffer + config), ClickHouse (permanente) |
| 4 | "Como consultar métricas" | Endpoints disponíveis, parâmetros, filtros |
| 5 | "Limitações atuais" | Mock data em GetPersonaUsage, ClickHouse não implementado |

### Para tech leads

| # | Nota | Conteúdo |
|---|------|---------|
| 1 | "Como instrumentar o seu módulo" | Guia de integração para emitir eventos |
| 2 | "Que métricas estão disponíveis" | KPIs, dashboards, filtros por módulo |
| 3 | "Roadmap do módulo" | Próximos passos, ClickHouse, instrumentação completa |

### Para product managers

| # | Nota | Conteúdo |
|---|------|---------|
| 1 | "Como interpretar os dashboards" | O que cada métrica significa |
| 2 | "Nível de confiança dos dados" | Quais dashboards têm dados reais vs parciais |
| 3 | "Como definir journeys e milestones" | Quando implementado, como configurar |

---

## 7. Documentação mínima do módulo

### Tier 1 — Obrigatório (antes de considerar módulo "documentado")

| # | Documento | Esforço |
|---|----------|---------|
| 1 | README.md do módulo com propósito, arquitetura, setup | 2h |
| 2 | Catálogo de eventos (AnalyticsEventType reference) | 1h |
| 3 | Guia de instrumentação para outros módulos | 2h |
| 4 | XML docs em entidades e interfaces do domínio | 2h |

### Tier 2 — Importante

| # | Documento | Esforço |
|---|----------|---------|
| 5 | Guia de ClickHouse (schema, queries) | 3h |
| 6 | Diagrama de arquitetura (fluxo PG ↔ CH) | 1h |
| 7 | Notas de onboarding (3 personas) | 2h |

### Tier 3 — Desejável

| # | Documento | Esforço |
|---|----------|---------|
| 8 | Guia de troubleshooting | 1h |
| 9 | ADR de decisões do módulo | 1h |
| 10 | Guia de definições de métricas/journeys | 2h |

---

## 8. Documentação dos fluxos principais

### Fluxo 1: Captura de evento de uso

```
1. Utilizador navega para /catalog
2. AnalyticsEventTracker.tsx detecta mudança de rota
3. Resolve módulo: /catalog → "ServiceCatalog"
4. Envia POST /api/v1/product-analytics/events
   Body: { eventType: "ModuleViewed", module: "ServiceCatalog", route: "/catalog", ... }
5. RecordAnalyticsEvent.Handler valida e persiste em gov_analytics_events (futuro: pan_events)
6. (Futuro) Domain event AnalyticsEventRecorded publicado
7. (Futuro) ClickHouse writer replica evento para pan_events no ClickHouse
```

### Fluxo 2: Consulta de dashboard de adoção

```
1. Utilizador navega para /analytics/adoption
2. ModuleAdoptionPage.tsx chama getModuleAdoption(params)
3. GET /api/v1/product-analytics/adoption/modules?persona=&teamId=&range=30d
4. GetModuleAdoption.Handler consulta IAnalyticsEventRepository
5. Repository executa queries contra gov_analytics_events (futuro: ClickHouse)
6. Handler calcula adoption %, depth score, trends
7. Retorna List<ModuleAdoptionDto>
8. Frontend renderiza lista com barras de adoção e métricas
```

### Fluxo 3: Detecção de fricção

```
1. GET /api/v1/product-analytics/friction?persona=Engineer&module=&range=30d
2. GetFrictionIndicators.Handler consulta repository
3. Filtra eventos de tipo: ZeroResultSearch, EmptyStateEncountered, JourneyAbandoned
4. Conta eventos no período atual e período anterior
5. Calcula impact %, count, trend (up/down/stable)
6. Retorna List<FrictionIndicatorDto>
```

**Total documentação**: 10 documentos, ~17h estimadas
