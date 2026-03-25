# Estratégia de Seeds por Módulo

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Garantir que o produto sobe do zero com dados-base corretos, de forma idempotente, versionável e com ordem de aplicação definida.

---

## Princípios

1. **Seeds são programáticos** — nunca em `HasData()` dentro de migrations
2. **Seeds são idempotentes** — re-executáveis sem duplicação
3. **Seeds são separados por ambiente** — dev, staging, prod
4. **Seeds seguem a ordem de dependência** entre módulos
5. **Seeds de referência** (enums, status) são obrigatórios em produção
6. **Seeds de teste** (dados fake) são exclusivos de desenvolvimento

---

## Ordem de Aplicação de Seeds

A ordem respeita dependências entre módulos:

| Passo | Módulo | Justificação |
|-------|--------|-------------|
| 1 | **Configuration** | Seeds de configuração do sistema (bases para todo o resto) |
| 2 | **Identity & Access** | Roles, permissions, policies, tenant default, admin user |
| 3 | **Environment Management** | Ambientes padrão (Development, Staging, Production) |
| 4 | **Service Catalog** | Categorias de serviço, tipos padrão |
| 5 | **Contracts** | Tipos de contrato, schemas padrão |
| 6 | **Notifications** | Templates base de notificação |
| 7 | **Audit & Compliance** | Categorias de auditoria, retention policies |
| 8 | **Governance** | Compliance frameworks base, risk categories |
| 9 | **Change Governance** | Tipos de mudança, políticas padrão |
| 10 | **Operational Intelligence** | Severidades, categorias de incidente |
| 11 | **Integrations** | Tipos de connector, providers padrão |
| 12 | **Product Analytics** | Tipos de evento, definições analíticas |
| 13 | **AI & Knowledge** | Providers padrão, model configs, policy defaults |

---

## Seeds por Módulo

### 01. Configuration

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ✅ Obrigatório | ~345 ConfigurationDefinition (8 fases) | PROD | ✅ Sim — `ConfigurationDefinitionSeeder` já existe |
| ✅ Obrigatório | ConfigurationEntry defaults | PROD | ✅ Upsert por Key |
| ⚠️ Opcional | Valores de teste | DEV | — |

**Estado actual:** ✅ `ConfigurationDefinitionSeeder` já implementado e idempotente.

**Acção necessária:** Nenhuma. Seeder existente está correcto.

---

### 02. Identity & Access

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ✅ Obrigatório | 7 system roles (PlatformAdmin, TechLead, Architect, Developer, ProductOwner, Executive, Auditor) | PROD | ✅ Upsert por Name |
| ✅ Obrigatório | 73+ permissions (identity:*, catalog:*, governance:*, etc.) | PROD | ✅ Upsert por Key |
| ✅ Obrigatório | Role-Permission mappings | PROD | ✅ Upsert por RoleId+PermissionId |
| ✅ Obrigatório | 1 default tenant ("NexTraceOne") | PROD | ✅ Upsert por Name |
| ⚠️ Opcional | Admin user (plataforma) | PROD | ✅ Create if not exists |
| ❌ Remover | 17 licensing permissions | — | — |

**Estado actual:** ⚠️ Seeds em `HasData()` dentro de EF Configurations. Precisa ser extraído para seeder programático.

**Acções necessárias:**
1. Criar `IdentityAccessSeeder.cs` programático
2. Extrair roles, permissions e tenant default de `HasData()` para o seeder
3. Remover `HasData()` de `RoleConfiguration.cs`, `PermissionConfiguration.cs`, `TenantConfiguration.cs`
4. Remover 17 permissões `licensing:*` do catálogo
5. Tornar seeder idempotente (upsert pattern)

---

### 03. Environment Management

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ✅ Obrigatório | 3 ambientes padrão (Development, Staging, Production) | PROD | ✅ Upsert por Name |
| ⚠️ Opcional | Policies padrão por ambiente | PROD | ✅ Upsert |
| ⚠️ Opcional | Environment bindings de teste | DEV | — |

**Estado actual:** ❌ Sem seeds definidos. Ambientes criados manualmente.

**Acções necessárias:**
1. Criar `EnvironmentSeeder.cs`
2. Seed 3 ambientes default
3. Associar tenant default aos ambientes

---

### 04. Service Catalog

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Categorias de serviço (API, Background, Event) | PROD | ✅ Upsert |
| ⚠️ Opcional | Tipos de dependência | PROD | ✅ Upsert |
| ❌ Não seedar | Serviços reais — criados pelo utilizador | — | — |

**Estado actual:** ❌ Sem seeds definidos.

**Acções necessárias:**
1. Avaliar se categorias de serviço devem ser seed ou criadas pelo utilizador
2. Se sim, criar `CatalogSeeder.cs` com categorias base

---

### 05. Contracts

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Tipos de contrato (REST, SOAP, Kafka, gRPC) | PROD | ✅ Upsert |
| ❌ Não seedar | Contratos reais — criados pelo utilizador | — | — |

**Estado actual:** ❌ Sem seeds definidos.

---

### 06. Notifications

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ✅ Obrigatório | Templates de notificação base (13 tipos) | PROD | ✅ Upsert por Type |
| ⚠️ Opcional | Notification channels default (InApp) | PROD | ✅ |
| ❌ Não seedar | Preferências do utilizador — criadas pelo utilizador | — | — |

**Estado actual:** ⚠️ Templates hardcoded em `NotificationTemplateResolver` (in-code, não persistidos).

**Acções necessárias:**
1. Decidir se templates são persistidos ou in-code (actual: in-code)
2. Se persistidos, criar `NotificationSeeder.cs`
3. Registar permissões `notifications:*` no `RolePermissionCatalog` (bloqueador actual)

---

### 07. Audit & Compliance

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Categorias de evento de auditoria | PROD | ✅ Upsert |
| ⚠️ Opcional | Retention policies default (90d, 365d, 7y) | PROD | ✅ Upsert |
| ❌ Não seedar | Eventos de auditoria — gerados pelo sistema | — | — |

**Estado actual:** ❌ Sem seeds definidos.

---

### 08. Governance

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Risk categories | PROD | ✅ Upsert |
| ⚠️ Opcional | Compliance frameworks padrão | PROD | ✅ Upsert |
| ⚠️ Opcional | FinOps cost categories | PROD | ✅ Upsert |

**Estado actual:** ⚠️ Alguns dados via `HasData()` em configurations (status enums, categorias).

---

### 09. Change Governance

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Tipos de mudança (standard, emergency, normal) | PROD | ✅ Upsert |
| ⚠️ Opcional | Policies de promoção default | PROD | ✅ Upsert |

**Estado actual:** ❌ Sem seeds definidos.

---

### 10. Operational Intelligence

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Severidades de incidente | PROD | ✅ Upsert |
| ⚠️ Opcional | Categorias de incidente | PROD | ✅ Upsert |
| ⚠️ Opcional | Runbook templates base | DEV | ✅ Upsert |

**Estado actual:** ⚠️ `IncidentSeedData` existe com dados de demonstração.

**Acções necessárias:**
1. Separar seeds de referência (severidades, categorias) dos dados de demonstração
2. Seeds de referência → PROD
3. Dados de demonstração → DEV only

---

### 11. Integrations

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Tipos de connector (REST, Kafka, DB, File) | PROD | ✅ Upsert |
| ❌ Não seedar | Connectors reais — configurados pelo utilizador | — | — |

**Estado actual:** ❌ Sem seeds definidos.

---

### 12. Product Analytics

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ✅ Obrigatório | 25 tipos de evento analítico (AnalyticsEventType enum) | PROD | ✅ Upsert |
| ⚠️ Opcional | Dashboard definitions padrão | PROD | ✅ Upsert |

**Estado actual:** ❌ Sem seeds definidos. Enum `AnalyticsEventType` existe com 25 valores mas não é persistido como seed.

---

### 13. AI & Knowledge

| Tipo | Dados | Ambiente | Idempotente |
|------|-------|----------|-------------|
| ⚠️ Opcional | Model providers padrão (OpenAI, Azure OpenAI) | PROD | ✅ Upsert |
| ⚠️ Opcional | Default AI policies | PROD | ✅ Upsert |
| ⚠️ Opcional | Default token budgets | PROD | ✅ Upsert |
| ❌ Não seedar | Conversas, agents custom, knowledge sources — criados pelo utilizador | — | — |

**Estado actual:** ❌ Sem seeds definidos.

---

## Resumo

| Módulo | Seeds Obrigatórios | Seeds Opcionais | Seeder Existe | Acção |
|--------|--------------------|-----------------|---------------|-------|
| Configuration | ✅ 345+ defs | Sim | ✅ Existe | Nenhuma |
| Identity & Access | ✅ Roles, Permissions, Tenant | Sim | ❌ HasData() | Criar seeder, extrair de HasData |
| Environment Management | ✅ 3 ambientes | Sim | ❌ | Criar seeder |
| Catalog | ⚠️ Categorias | Sim | ❌ | Avaliar |
| Contracts | ⚠️ Tipos | Sim | ❌ | Avaliar |
| Notifications | ✅ Templates | Sim | ❌ | Criar seeder + registar permissions |
| Audit | ⚠️ Retention | Sim | ❌ | Avaliar |
| Governance | ⚠️ Frameworks | Sim | ⚠️ HasData | Extrair |
| Change Governance | ⚠️ Tipos | Sim | ❌ | Avaliar |
| OpIntel | ⚠️ Severidades | Sim | ⚠️ Parcial | Separar demo/prod |
| Integrations | ⚠️ Tipos | Sim | ❌ | Avaliar |
| Product Analytics | ✅ Tipos evento | Sim | ❌ | Criar seeder |
| AI & Knowledge | ⚠️ Providers | Sim | ❌ | Avaliar |
