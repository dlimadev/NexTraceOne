# PARTE 7 — Backend Functional Corrections

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: BACKLOG DE CORREÇÕES

---

## 1. Endpoints do módulo

| # | Método | Rota | Permissão | Ficheiro |
|---|--------|------|-----------|---------|
| 1 | POST | `/api/v1/product-analytics/events` | `governance:analytics:write` | `ProductAnalyticsEndpointModule.cs` |
| 2 | GET | `/api/v1/product-analytics/summary` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |
| 3 | GET | `/api/v1/product-analytics/adoption/modules` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |
| 4 | GET | `/api/v1/product-analytics/adoption/personas` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |
| 5 | GET | `/api/v1/product-analytics/journeys` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |
| 6 | GET | `/api/v1/product-analytics/value-milestones` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |
| 7 | GET | `/api/v1/product-analytics/friction` | `governance:analytics:read` | `ProductAnalyticsEndpointModule.cs` |

---

## 2. Mapeamento endpoints → casos de uso

| Endpoint | Feature (CQRS) | Caso de uso |
|----------|----------------|-------------|
| POST /events | RecordAnalyticsEvent | Capturar evento de uso do produto |
| GET /summary | GetAnalyticsSummary | Consultar dashboard geral de analytics |
| GET /adoption/modules | GetModuleAdoption | Consultar adoção por módulo |
| GET /adoption/personas | GetPersonaUsage | Consultar uso por persona |
| GET /journeys | GetJourneys | Consultar funnels de jornada |
| GET /value-milestones | GetValueMilestones | Consultar milestones de valor |
| GET /friction | GetFrictionIndicators | Consultar indicadores de fricção |

---

## 3. Endpoints mortos

Nenhum endpoint morto identificado. Todos os 7 endpoints são mapeados para features CQRS.

---

## 4. Endpoints incompletos

| Endpoint | Problema | Severidade |
|----------|----------|-----------|
| GET /adoption/personas | **Retorna mock data hardcoded** — 7 personas com dados simulados | 🔴 P1_CRITICAL |
| GET /summary | Adoption score, value score calculados com lógica simplificada sem dados reais suficientes | 🟠 P2_HIGH |
| GET /adoption/modules | Dados reais do repository mas cálculos de depth score e trends possivelmente simplificados | 🟡 P3_MEDIUM |
| GET /journeys | Sem eventos suficientes para construir funnels reais | 🟠 P2_HIGH |
| GET /value-milestones | Sem definição formal de milestones, dados limitados | 🟠 P2_HIGH |

---

## 5. Revisão de requests/responses

| Endpoint | Request | Response | Avaliação |
|----------|---------|----------|-----------|
| POST /events | Command com EventType, Module, Route, Feature, etc. | 200 OK | ✅ Adequado |
| GET /summary | Query com persona, module, teamId, domainId, range | AnalyticsSummaryDto | ⚠️ Response complexo, dados mistos |
| GET /adoption/modules | Query com persona, teamId, range | List\<ModuleAdoptionDto> | ✅ Adequado |
| GET /adoption/personas | Query com persona, teamId, range | List\<PersonaUsageProfileDto> | 🔴 Mock data |
| GET /journeys | Query com journeyId, persona, range | JourneyFunnelDto | ⚠️ Dados limitados |
| GET /value-milestones | Query com persona, teamId, range | ValueMilestonesDto | ⚠️ Dados limitados |
| GET /friction | Query com persona, module, range | List\<FrictionIndicatorDto> | ✅ Real data |

---

## 6. Revisão de validações

| Feature | Validação | Status |
|---------|-----------|--------|
| RecordAnalyticsEvent | Max lengths, required fields (Validator) | ✅ Presente |
| GetAnalyticsSummary | Range validation | ⚠️ Sem validação de range máximo |
| GetModuleAdoption | Filtros opcionais | ⚠️ Sem validação de range máximo |
| GetPersonaUsage | — | ❌ Sem validação (retorna mock) |
| GetJourneys | — | ⚠️ Sem validação de journeyId válido |
| GetValueMilestones | — | ⚠️ Sem validação |
| GetFrictionIndicators | — | ⚠️ Sem validação de range máximo |

---

## 7. Revisão de tratamento de erro

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Exceções de domínio | ❌ Inexistente | Sem domain exceptions específicas |
| Try-catch nos handlers | ⚠️ Depende do pipeline global | Via middleware |
| Erro de permissão | ✅ Via authorization pipeline | `RequireAuthorization` nos endpoints |
| Erro de validação | ✅ Via FluentValidation pipeline | Para RecordAnalyticsEvent |
| 404 para entidades não encontradas | N/A | Queries retornam dados agregados |
| Rate limiting | ❌ Não implementado | POST /events vulnerável a flood |

---

## 8. Revisão de auditoria

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Domain events publicados | ❌ Zero domain events | Nenhum handler publica eventos |
| Audit events para Audit & Compliance | ❌ Zero audit events | Ações não são auditadas |
| Logs de operação | ⚠️ Via logging global | Sem logs específicos do módulo |

---

## 9. Revisão de permissões por ação

| Ação | Permissão Atual | Permissão Alvo | Status |
|------|----------------|----------------|--------|
| Gravar evento | `governance:analytics:write` | `analytics:write` | 🟠 Renomear |
| Ler summary | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Ler adoption | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Ler personas | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Ler journeys | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Ler milestones | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Ler friction | `governance:analytics:read` | `analytics:read` | 🟠 Renomear |
| Exportar dados | N/A (não existe) | `analytics:export` | 🔴 Criar |
| Gerir definições | N/A (não existe) | `analytics:manage` | 🔴 Criar |

---

## 10. Revisão de fluxos críticos

### Capturar evento de uso

| Passo | Status | Detalhe |
|-------|--------|---------|
| Frontend envia POST /events | ✅ | Via AnalyticsEventTracker + manual calls |
| Validação do command | ✅ | FluentValidation |
| Persistência no PostgreSQL | ✅ | Via repository |
| Domain event publicado | ❌ | Nenhum evento publicado |
| Replicação para ClickHouse | ❌ | Não implementado |
| Rate limiting | ❌ | Não implementado |
| Batch ingestion | ❌ | Não implementado (1 evento por request) |

### Consultar dashboard geral

| Passo | Status | Detalhe |
|-------|--------|---------|
| Frontend chama GET /summary | ✅ | Via productAnalyticsApi.ts |
| Query handler executa | ✅ | GetAnalyticsSummary handler |
| Dados reais do repository | ⚠️ | Parcialmente real, cálculos simplificados |
| Cache de resultados | ❌ | Sem caching (query direto ao DB a cada request) |

### Consultar uso por persona

| Passo | Status | Detalhe |
|-------|--------|---------|
| Frontend chama GET /adoption/personas | ✅ | Via productAnalyticsApi.ts |
| Query handler executa | ✅ | GetPersonaUsage handler |
| Dados reais do repository | 🔴 | **MOCK DATA HARDCODED** |

---

## 11. Backlog de correções backend

| # | ID | Correção | Prioridade | Esforço | Ficheiro(s) |
|---|-----|---------|-----------|---------|-------------|
| 1 | B-01 | Extrair backend de Governance para `src/modules/productanalytics/` | P0_BLOCKER | 8h | 15+ ficheiros |
| 2 | B-02 | Criar `ProductAnalyticsDbContext` com `pan_` prefix | P0_BLOCKER | 4h | Novo ficheiro |
| 3 | B-03 | Eliminar mock data em `GetPersonaUsage` handler | P1_CRITICAL | 4h | `GetPersonaUsage/Handler.cs` |
| 4 | B-04 | Renomear permissões de `governance:analytics:*` para `analytics:*` | P1_CRITICAL | 2h | `ProductAnalyticsEndpointModule.cs`, `RolePermissionCatalog.cs` |
| 5 | B-05 | Adicionar rate limiting no POST /events | P2_HIGH | 3h | `ProductAnalyticsEndpointModule.cs` |
| 6 | B-06 | Publicar domain events (AnalyticsEventRecorded) | P2_HIGH | 3h | `RecordAnalyticsEvent/Handler.cs` |
| 7 | B-07 | Publicar audit events para Audit & Compliance | P2_HIGH | 2h | Todos os handlers |
| 8 | B-08 | Adicionar validação de range máximo em queries | P2_HIGH | 2h | Todos os GET handlers |
| 9 | B-09 | Implementar batch event ingestion (POST /events/batch) | P2_HIGH | 4h | Novo endpoint |
| 10 | B-10 | Adicionar caching para queries de dashboard | P2_HIGH | 3h | Query handlers |
| 11 | B-11 | Criar endpoint de definições CRUD | P2_HIGH | 6h | Novos features |
| 12 | B-12 | Implementar ClickHouse writer service | P1_CRITICAL | 8h | Novo serviço |
| 13 | B-13 | Implementar ClickHouse query adapter para dashboards | P1_CRITICAL | 8h | Novo serviço |
| 14 | B-14 | Melhorar GetAnalyticsSummary com dados reais | P2_HIGH | 4h | `GetAnalyticsSummary/Handler.cs` |
| 15 | B-15 | Melhorar GetJourneys com definições formais | P2_HIGH | 4h | `GetJourneys/Handler.cs` |
| 16 | B-16 | Melhorar GetValueMilestones com definições formais | P2_HIGH | 4h | `GetValueMilestones/Handler.cs` |
| 17 | B-17 | Adicionar endpoint de exportação de dados | P3_MEDIUM | 4h | Novo endpoint |
| 18 | B-18 | Adicionar logs específicos do módulo | P3_MEDIUM | 2h | Todos os handlers |

**Total backend**: 18 itens, ~75h estimadas
