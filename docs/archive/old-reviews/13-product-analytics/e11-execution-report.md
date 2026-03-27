# E11 — Product Analytics Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Product Analytics (atualmente alojado no módulo Governance
pendente extração OI-03). Renomeação de prefixo de tabela, check constraints nas colunas de enum,
índices compostos para consultas analíticas, e permissão `governance:analytics:read` para
TechLead e Viewer.

---

## Ficheiros de Código Alterados

### Persistence — EF Core Configuration (1 ficheiro)
| Ficheiro | Alteração |
|----------|-----------|
| `AnalyticsEventConfiguration.cs` | Tabela gov_analytics_events → pan_analytics_events. 2 check constraints (Module, EventType). 2 composite indexes (TenantId+OccurredAt, TenantId+Module+OccurredAt). |

### Security — Permissions (1 ficheiro)
| Ficheiro | Alteração |
|----------|-----------|
| `RolePermissionCatalog.cs` | `governance:analytics:read` registado para TechLead e Viewer. |

---

## Correções por Parte

### PART 1 — Fronteira Governance vs Product Analytics
- ✅ Tabela renomeada de `gov_analytics_events` para `pan_analytics_events`, separando visualmente o ownership
- ⏳ Extração física para módulo próprio depende de OI-03 (Wave 0)

### PART 2 — Eventos, Métricas e Dashboards
- ✅ Check constraint `CK_pan_analytics_events_module` guarda 17 valores válidos de ProductModule
- ✅ Check constraint `CK_pan_analytics_events_event_type` guarda 25 valores válidos de AnalyticsEventType
- ✅ 25 tipos de evento analítico taxonomizados e protegidos por constraint

### PART 3 — Domínio
- ✅ AnalyticsEvent mantida sem RowVersion (imutável — criada e nunca modificada, mesma decisão que AuditEvent)
- ✅ Entidade já possui validações Guard clauses no factory method Create()

### PART 4 — Persistência
- ✅ 1 tabela renomeada: pan_analytics_events
- ✅ 2 check constraints em 2 colunas de enum (Module, EventType)
- ✅ 2 composite indexes adicionados para consultas por tenant e time-range
- ✅ 5 single-column indexes existentes mantidos (OccurredAt, Module, EventType, Persona, UserId)

### PART 5 — PostgreSQL vs ClickHouse
- ✅ AnalyticsEvent permanece em PostgreSQL (transacional, volume moderado)
- ⏳ Pipeline ClickHouse para analytics de alto volume para fase futura
- ✅ Indexes compostos preparados para padrão de consulta analítica (TenantId+OccurredAt)

### PART 6 — Backend
- ✅ 7 endpoints verificados ativos no ProductAnalyticsEndpointModule
- ✅ Permissões: governance:analytics:read e governance:analytics:write

### PART 7 — Frontend
- ✅ 5 páginas verificadas: ProductAnalyticsOverviewPage, ModuleAdoptionPage, PersonaUsagePage, JourneyFunnelPage, ValueTrackingPage

### PART 8 — Segurança
- ✅ `governance:analytics:read` registado para TechLead (antes apenas PlatformAdmin)
- ✅ `governance:analytics:read` registado para Viewer (leitura de dashboards de adoção)
- ✅ 3 roles com governance:analytics:read (PlatformAdmin, TechLead, Viewer)
- ✅ 1 role com governance:analytics:write (PlatformAdmin)

### PART 9 — Dependências
- ✅ Módulo usa GovernanceDbContext temporariamente (OI-03 pendente)

### PART 10 — Documentação
- ✅ Execution report e gap report criados

---

## Validação

- ✅ Build: 0 erros
- ✅ 163 testes Governance: todos passam
- ✅ 290 testes Identity: todos passam (após alteração RolePermissionCatalog)

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `AnalyticsEventConfiguration` | Check constraints + table rename + composite indexes |
| `RolePermissionCatalog` | governance:analytics:read para TechLead + Viewer |

## Decisões Tomadas

1. **Sem RowVersion**: AnalyticsEvent é imutável (todos os setters são `private init`). Uma vez criada,
   nunca é modificada. Mesma decisão arquitetural que AuditEvent no módulo Audit.

2. **Prefixo pan_ em vez de gov_analytics_**: Seguindo a convenção de cada módulo ter prefixo curto
   (aud_, chg_, ops_, ntf_, int_, etc.), a tabela foi renomeada para `pan_analytics_events`.

3. **TechLead + Viewer com analytics:read**: TechLeads precisam ver métricas de adoção dos serviços
   da equipa. Viewers têm perfil read-only mas precisam ver dashboards de produto. Write permanece
   apenas com PlatformAdmin.

4. **Composite indexes**: Consultas analíticas filtram sempre por TenantId + time-range. O índice
   composto (TenantId, Module, OccurredAt) suporta também filtros por módulo.
