# PARTE 5 — Persistência PostgreSQL Final do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Tabelas atuais do módulo

| Tabela Atual | DbContext | Entidade | Prefix |
|-------------|-----------|----------|--------|
| `gov_analytics_events` | GovernanceDbContext | AnalyticsEvent | ❌ `gov_` (errado) |

**Total**: 1 tabela (dentro de GovernanceDbContext)

---

## 2. Mapeamento entidade → tabela (alvo)

| Entidade | Tabela Alvo | Prefix | Status |
|---------|-------------|--------|--------|
| AnalyticsEvent | `pan_events` | ✅ `pan_` | 🔴 Renomear de `gov_analytics_events` |
| AnalyticsDefinition (NOVA) | `pan_definitions` | ✅ `pan_` | 🔴 Criar |
| JourneyStep (NOVA) | `pan_journey_steps` | ✅ `pan_` | 🔴 Criar |
| ValueMilestone (NOVA) | `pan_value_milestones` | ✅ `pan_` | 🔴 Criar |

---

## 3. Esquema final: `pan_events`

### Primary Key
```
PK: id (uuid) — NOT NULL, DEFAULT gen_random_uuid()
```

### Colunas

| Coluna | Tipo | Nullable | Default | Notas |
|--------|------|----------|---------|-------|
| id | uuid | NOT NULL | gen_random_uuid() | PK |
| tenant_id | uuid | NOT NULL | — | RLS filter |
| user_id | uuid | NOT NULL | — | Quem executou |
| persona | varchar(50) | NOT NULL | — | Persona do utilizador |
| module | varchar(100) | NOT NULL | — | Módulo do produto |
| event_type | integer | NOT NULL | — | Enum AnalyticsEventType |
| feature | varchar(200) | NULL | — | Feature específica |
| entity_type | varchar(100) | NULL | — | Tipo de entidade |
| outcome | varchar(100) | NULL | — | Resultado da ação |
| route | varchar(500) | NULL | — | URL/rota |
| team_id | uuid | NULL | — | Equipa contexto |
| domain_id | uuid | NULL | — | Domínio contexto |
| session_id | varchar(50) | NULL | — | ID de sessão |
| client_type | varchar(50) | NULL | — | Tipo de cliente |
| metadata_json | varchar(2000) | NULL | — | Metadata flexível |
| occurred_at | timestamptz | NOT NULL | — | Timestamp do evento |
| environment_id | uuid | NULL | — | **NOVO** — Ambiente |
| duration | integer | NULL | — | **NOVO** — Duração em ms |
| parent_event_id | uuid | NULL | — | **NOVO** — Evento pai |
| source | varchar(20) | NOT NULL | 'Frontend' | **NOVO** — Origem |
| created_at | timestamptz | NOT NULL | now() | Auditoria |
| created_by | varchar(256) | NOT NULL | — | Auditoria |

### Índices

| Nome | Colunas | Tipo | Justificação |
|------|---------|------|-------------|
| IX_pan_events_tenant_id | tenant_id | B-tree | RLS filter |
| IX_pan_events_occurred_at | occurred_at | B-tree | Filtro temporal |
| IX_pan_events_module | module | B-tree | Filtro por módulo |
| IX_pan_events_event_type | event_type | B-tree | Filtro por tipo |
| IX_pan_events_persona | persona | B-tree | Filtro por persona |
| IX_pan_events_user_id | user_id | B-tree | Filtro por utilizador |
| IX_pan_events_session_id | session_id | B-tree | Correlação de sessão |
| IX_pan_events_tenant_occurred | (tenant_id, occurred_at) | B-tree | Query principal |
| IX_pan_events_tenant_module_occurred | (tenant_id, module, occurred_at) | B-tree | Query por módulo |

### Constraints

| Nome | Tipo | Expressão |
|------|------|-----------|
| PK_pan_events | Primary Key | id |
| CK_pan_events_persona_len | Check | length(persona) > 0 |
| CK_pan_events_module_len | Check | length(module) > 0 |
| CK_pan_events_source_values | Check | source IN ('Frontend', 'Backend', 'API', 'System') |

**Nota**: Sem FK para outras tabelas — eventos são autónomos e desacoplados.

---

## 4. Esquema final: `pan_definitions`

### Primary Key
```
PK: id (uuid) — NOT NULL, DEFAULT gen_random_uuid()
```

### Colunas

| Coluna | Tipo | Nullable | Default | Notas |
|--------|------|----------|---------|-------|
| id | uuid | NOT NULL | gen_random_uuid() | PK |
| tenant_id | uuid | NOT NULL | — | RLS filter |
| name | varchar(200) | NOT NULL | — | Nome da definição |
| type | integer | NOT NULL | — | Enum AnalyticsDefinitionType |
| scope | varchar(50) | NOT NULL | — | Scope (Product/Module/Feature/Persona) |
| description | varchar(1000) | NULL | — | Descrição |
| configuration_json | varchar(4000) | NULL | — | Configuração flexível |
| is_active | boolean | NOT NULL | true | Ativo |
| created_at | timestamptz | NOT NULL | now() | Auditoria |
| created_by | varchar(256) | NOT NULL | — | Auditoria |
| updated_at | timestamptz | NULL | — | Auditoria |
| updated_by | varchar(256) | NULL | — | Auditoria |
| xmin | xid | — | — | RowVersion (PostgreSQL) |

### Índices

| Nome | Colunas | Tipo |
|------|---------|------|
| IX_pan_definitions_tenant_id | tenant_id | B-tree |
| IX_pan_definitions_type | type | B-tree |
| UX_pan_definitions_tenant_name | (tenant_id, name) | Unique |

---

## 5. Esquema final: `pan_journey_steps`

### Colunas

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| id | uuid | NOT NULL | PK |
| definition_id | uuid | NOT NULL | FK → pan_definitions.id |
| step_order | integer | NOT NULL | Ordem no funnel |
| name | varchar(200) | NOT NULL | Nome do passo |
| event_type | integer | NOT NULL | Evento que marca o passo |
| module | varchar(100) | NULL | Módulo associado |
| created_at | timestamptz | NOT NULL | Auditoria |

### Índices e FKs

| Nome | Tipo | Expressão |
|------|------|-----------|
| PK_pan_journey_steps | PK | id |
| FK_pan_journey_steps_definition | FK | definition_id → pan_definitions.id ON DELETE CASCADE |
| IX_pan_journey_steps_definition | B-tree | definition_id |

---

## 6. Esquema final: `pan_value_milestones`

### Colunas

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| id | uuid | NOT NULL | PK |
| definition_id | uuid | NOT NULL | FK → pan_definitions.id |
| name | varchar(200) | NOT NULL | Nome do milestone |
| event_type | integer | NOT NULL | Evento que marca o milestone |
| persona | varchar(50) | NULL | Persona específica |
| target_minutes | integer | NULL | Target de tempo em minutos |
| created_at | timestamptz | NOT NULL | Auditoria |

### Índices e FKs

| Nome | Tipo | Expressão |
|------|------|-----------|
| PK_pan_value_milestones | PK | id |
| FK_pan_value_milestones_definition | FK | definition_id → pan_definitions.id ON DELETE CASCADE |
| IX_pan_value_milestones_definition | B-tree | definition_id |

---

## 7. TenantId e EnvironmentId

| Tabela | TenantId | EnvironmentId |
|--------|----------|---------------|
| pan_events | ✅ Obrigatório (RLS) | ❌ Opcional (campo nullable) |
| pan_definitions | ✅ Obrigatório (RLS) | ❌ Não aplicável |
| pan_journey_steps | Via FK (definitions) | ❌ Não aplicável |
| pan_value_milestones | Via FK (definitions) | ❌ Não aplicável |

---

## 8. RowVersion

| Tabela | RowVersion | Justificação |
|--------|-----------|--------------|
| pan_events | ❌ Não necessário | Eventos são imutáveis (append-only) |
| pan_definitions | ✅ xmin (PostgreSQL) | Definições editáveis com concorrência |
| pan_journey_steps | ❌ Via parent cascade | Gerido pelo parent |
| pan_value_milestones | ❌ Via parent cascade | Gerido pelo parent |

---

## 9. Persistência de status e histórico

Product Analytics é fundamentalmente **append-only** para eventos. Os eventos NÃO são editados nem apagados.

| Dado | PostgreSQL | ClickHouse | Notas |
|------|-----------|------------|-------|
| Eventos recentes (buffer) | ✅ pan_events | ✅ Replicação | PostgreSQL como buffer, ClickHouse como store permanente |
| Eventos históricos | ❌ Purgados após flush | ✅ Permanente | PostgreSQL events purgados após sync com ClickHouse |
| Definições (config) | ✅ pan_definitions | ❌ | Apenas transacional |
| Métricas agregadas | ❌ | ✅ Materialized views | ClickHouse calcula |

---

## 10. Divergências entre estado atual e modelo final

| # | Divergência | Estado Atual | Estado Alvo | Prioridade |
|---|-----------|-------------|------------|-----------|
| 1 | Localização | GovernanceDbContext | ProductAnalyticsDbContext | 🔴 P0_BLOCKER |
| 2 | Prefixo de tabela | `gov_analytics_events` | `pan_events` | 🔴 P0_BLOCKER |
| 3 | Tabela definitions | Não existe | `pan_definitions` | 🟠 P2_HIGH |
| 4 | Tabela journey_steps | Não existe | `pan_journey_steps` | 🟠 P2_HIGH |
| 5 | Tabela value_milestones | Não existe | `pan_value_milestones` | 🟠 P2_HIGH |
| 6 | Campo EnvironmentId | Não existe | Nullable em pan_events | 🟡 P3_MEDIUM |
| 7 | Campo Duration | Não existe | Nullable em pan_events | 🟡 P3_MEDIUM |
| 8 | Campo ParentEventId | Não existe | Nullable em pan_events | 🟡 P3_MEDIUM |
| 9 | Campo Source | Não existe | NOT NULL com default | 🟠 P2_HIGH |
| 10 | RowVersion em definitions | Não existe | xmin | 🟠 P2_HIGH |
| 11 | Check constraints | Nenhum | 3 constraints | 🟡 P3_MEDIUM |
| 12 | Unique constraint (tenant+name) | Não existe | UX_pan_definitions_tenant_name | 🟠 P2_HIGH |
| 13 | Índice composto tenant+occurred | Não existe | IX_pan_events_tenant_occurred | 🟠 P2_HIGH |
