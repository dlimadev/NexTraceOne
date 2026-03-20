# ADR-001 — Tenant-Aware, Environment-Aware Operational Context

**Status:** Accepted  
**Data:** 2026-03-20  
**Autores:** Arquitetura NexTraceOne  
**Revisores:** Engenharia, Produto, Segurança

---

## 1. Contexto Atual

O NexTraceOne é uma plataforma enterprise multitenant de governança operacional. O modelo de autenticação e isolamento via `ICurrentTenant` / `TenantResolutionMiddleware` / `TenantRlsInterceptor` está sólido no domínio de **IdentityAccess** e nos **BuildingBlocks**.

Porém, ao inspecionar os demais módulos operacionais (`ChangeGovernance`, `OperationalIntelligence`, `Catalog`, `AIKnowledge`, `Governance`, `AuditCompliance`) e o frontend, constata-se que:

1. **O conceito de ambiente (Environment) é tratado como string literal** em quase todos os módulos operacionais, sem binding a um `EnvironmentId` bem definido e tenant-scoped.
2. **Muitos módulos operam sem TenantId** nas suas entidades centrais de domínio.
3. **O frontend assume três ambientes fixos:** `Production`, `Staging`, `Development` — hardcoded no componente `WorkspaceSwitcher`.
4. **A IA opera sem contexto de tenant/ambiente** em prompts e buscas de dados.
5. **As integrações de telemetria e topologia** usam `environment: string` como chave sem associação a `TenantId`.

---

## 2. Problema Atual

### 2.1 Ambiente como string livre

A entidade `Release` em `ChangeGovernance.Domain` possui:
```csharp
public string Environment { get; private set; } = string.Empty;
```
Não há referência a `EnvironmentId`, não há validação de que o ambiente pertence ao tenant ativo, e não há perfil operacional parametrizável.

A entidade `IncidentRecord` em `OperationalIntelligence.Domain` possui:
```csharp
public string Environment { get; private set; } = string.Empty;
```
Mesma situação.

### 2.2 Entidades críticas sem TenantId

As seguintes entidades **não possuem TenantId**:
- `Release` (ChangeGovernance) — representa um deployment num ambiente de produção
- `PromotionRequest` / `DeploymentEnvironment` / `PromotionGate` (ChangeGovernance) — governa promoções entre ambientes
- `IncidentRecord` / `MitigationWorkflowRecord` / `RunbookRecord` (OperationalIntelligence)
- `ApiAsset` / `ServiceAsset` / `ContractVersion` / `GraphSnapshot` (Catalog)
- `AiAssistantConversation` e entidades de governança de IA

### 2.3 Módulo `DeploymentEnvironment` sem TenantId

`DeploymentEnvironment` em `ChangeGovernance.Domain` representa um "ambiente de pipeline de deployment" mas **não possui `TenantId`**. Isso significa que ambientes de deployment são globais na plataforma, o que viola o princípio de que cada tenant tem seus próprios ambientes.

### 2.4 Dois conceitos de Environment duplicados

Existem **duas entidades `Environment`** com propósitos diferentes mas sem integração:
- `IdentityAccess.Domain.Entities.Environment` — ambiente de autorização, **tenant-scoped** (tem `TenantId`), usado para controle de acesso
- `ChangeGovernance.Domain.Promotion.Entities.DeploymentEnvironment` — ambiente de deployment pipeline, **sem TenantId**, global

Esses dois conceitos representam realidades parcialmente sobrepostas e devem ser unificados ou explicitamente relacionados.

### 2.5 Catálogo global sem TenantId

`ApiAsset`, `ServiceAsset` e `ContractVersion` não possuem `TenantId`. O catálogo de serviços e contratos é tratado como global, o que impossibilita multi-tenancy real no core do produto.

### 2.6 AutomationActionCatalog hardcoded

```csharp
AllowedEnvironments: new[] { "Staging", "Production" }
AllowedEnvironments: new[] { "Development", "Staging", "Production" }
```
O catálogo de automação (backend) assume exatamente três ambientes fixos. Isso é um hardcode crítico.

### 2.7 Frontend com ambientes hardcoded

```typescript
const AVAILABLE_ENVIRONMENTS = ['Production', 'Staging', 'Development'] as const;
```
O `WorkspaceSwitcher` lista ambientes fixos no cliente, sem consultar o backend. Não há `EnvironmentContext` no React. O ambiente não influencia queries de API — é apenas visual.

### 2.8 IA sem contexto de tenant/ambiente

O endpoint `/api/v1/ai/chat` aceita `TenantId` como campo opcional no body da request (`body.TenantId`), mas não o extrai do contexto autenticado. Prompts não incluem `EnvironmentId`. A busca de dados (`SearchData`) aceita `TenantId?` como parâmetro opcional.

### 2.9 Telemetria com TenantId opcional

Todos os modelos de telemetria (`ObservedTopologyEntry`, `AnomalySnapshot`, `ServiceMetricsSnapshot`, `ReleaseRuntimeCorrelation`, `InvestigationContext`, `TelemetryReference`) possuem `Guid? TenantId` — **nullable**. Isso significa que dados de telemetria podem ser consultados e gravados sem tenant, misturando dados de diferentes clientes.

---

## 3. Limitações Atuais

| Limitação | Impacto |
|-----------|---------|
| Catálogo sem TenantId | Impossibilita multi-tenancy do core |
| ChangeGovernance sem TenantId | Releases e promoções são globais |
| OperationalIntelligence sem TenantId | Incidentes e runbooks são globais |
| DeploymentEnvironment sem TenantId | Ambientes de pipeline são globais |
| AutomationCatalog hardcoded | Não suporta ambientes customizados |
| Frontend com ambientes fixos | UX não reflete ambientes reais do tenant |
| IA sem contexto tenant/ambiente | Risco de dados cross-tenant em prompts |
| Telemetria com TenantId? nullable | Dados de telemetria podem vazar |
| Dois conceitos de Environment desconexos | Inconsistência de modelo |

---

## 4. Decisão Arquitetural

### 4.1 TenantId + EnvironmentId como contexto operacional mínimo

**Todo objeto operacional sensível ao tenant deve portar `TenantId`.** Todo objeto sensível ao ambiente deve portar `EnvironmentId` (tipado), não uma string livre.

O contexto operacional mínimo é:
```
TenantId (Guid, obrigatório em dados operacionais)
EnvironmentId (Guid, tipado, para dados environment-scoped)
```

### 4.2 Ambiente pertence ao tenant, não é global

O modelo unificado de ambiente deve:
- Ter `TenantId` obrigatório
- Ter `EnvironmentId` como identificador único tipado
- Ter `Slug` único dentro do tenant (não globalmente)
- Ter perfil operacional configurável por tenant: `IsProductionLike`, `RequiresApproval`, `AllowAutomation`, `NotificationLevel`, etc.
- Não ser um enum fixo nem assumir nomes específicos como Production/Staging/Dev

### 4.3 Por que ambiente não pode ser enum fixo

- Clientes enterprise têm ambientes como: `DEV-EU`, `QA-LATAM`, `CANARY-PROD`, `DR-PROD`, `PERF-TEST`
- Nomes de ambientes mudam com o tempo e por convenção de cada empresa
- O comportamento deve ser definido por políticas (perfis), não por nome
- A ordenação de promoção deve ser por `SortOrder`, não por hierarquia implícita de nome

### 4.4 Backend como fonte de verdade

O frontend **não deve** decidir quais ambientes existem, qual é o ambiente ativo, nem tomar decisões operacionais baseadas em nome de ambiente. O backend deve expor:
- `GET /api/v1/environments` — lista de ambientes do tenant ativo
- `GET /api/v1/environments/{id}/profile` — perfil operacional do ambiente
- O frontend deve selecionar o ambiente ativo e propagar `EnvironmentId` em queries

### 4.5 IA única e context-aware

A IA do produto deve ser **única** e operar com contexto de `TenantId` + `EnvironmentId` injetado automaticamente a partir do contexto autenticado. Não deve existir uma IA por ambiente nem uma IA por tenant.

O contexto de IA deve incluir:
```
TenantId: <from JWT claim>
EnvironmentId: <from active environment header ou query>
UserId: <from JWT claim>
```

### 4.6 Eliminação de hardcodes de ambiente

Qualquer referência a `"Production"`, `"Staging"`, `"Development"` como valor fixo no backend deve ser substituída por referência a `EnvironmentId` ou a perfil operacional parametrizável.

---

## 5. Por que não Feature Flags

Feature flags controlam comportamento de features, não contexto operacional. O ambiente é uma dimensão de contexto, não uma feature. Usar feature flags para controlar comportamento por ambiente introduz:
- Explosão combinatória (flag × ambiente × tenant)
- Lógica de negócio no framework de feature flags
- Dependência de plataforma externa para decisões de segurança

---

## 6. Por que não criar IA por ambiente

- Aumentaria custo operacional e complexidade de infraestrutura
- Fragmentaria o conhecimento e contexto da IA
- Tornaria impossível análise cross-ambiente (ex: comparar produção com staging)
- A IA já tem mecanismos de contexto (prompt injection) para receber informação de ambiente

---

## 7. Impactos Esperados

### 7.1 Domínio
- Adicionar `TenantId` a: `Release`, `PromotionRequest`, `DeploymentEnvironment`, `IncidentRecord`, `MitigationWorkflowRecord`, `RunbookRecord`, `ApiAsset`, `ServiceAsset`, `ContractVersion`
- Adicionar `EnvironmentId` a: `Release`, `IncidentRecord`, entidades de telemetria
- Unificar o modelo de ambiente entre IdentityAccess e ChangeGovernance
- Remover hardcodes de `"Production"` / `"Staging"` do domínio e application layer

### 7.2 Backend
- Atualizar repositórios para filtrar por `TenantId` explicitamente
- Atualizar handlers para extrair `TenantId` do `ICurrentTenant` (não do body)
- Criar `ICurrentEnvironment` nos BuildingBlocks
- Criar endpoint `/api/v1/environments` no módulo correto
- Atualizar AutomationActionCatalog para perfil operacional

### 7.3 Banco de dados
- Migrations de backfill para adicionar `TenantId` a tabelas existentes
- Índices compostos `(TenantId, EnvironmentId)` em tabelas críticas
- Chaves únicas escoped por tenant (ex: slug de ambiente)

### 7.4 Frontend
- Criar `EnvironmentContext` provider com ambiente ativo
- Remover `AVAILABLE_ENVIRONMENTS` hardcoded do `WorkspaceSwitcher`
- Carregar ambientes do backend por tenant
- Propagar `EnvironmentId` nas queries de API

### 7.5 IA
- Injetar `TenantId` + `EnvironmentId` automaticamente no contexto de chat
- Remover `TenantId` como campo opcional do body do `/ai/chat`
- Atualizar `SearchData` para usar contexto do token

### 7.6 Telemetria
- Tornar `TenantId` obrigatório nos modelos de telemetria
- Adicionar `EnvironmentId` tipado aos modelos de telemetria

---

## 8. Riscos

| Risco | Severidade | Mitigação |
|-------|-----------|-----------|
| Migrations de backfill em tabelas com dados | Alta | Plano de migração incremental com valores default |
| Breaking change nos contratos de integração externa | Alta | Versionamento de API, período de transição |
| Testes existentes sem TenantId/EnvironmentId | Média | Atualização incremental de fixtures de teste |
| Módulos consumindo dados cross-tenant via RLS | Alta | Validação de RLS antes de cada fase |
| Frontend sem ambiente válido durante transição | Média | Fallback para primeiro ambiente disponível |

---

## 9. Fases Seguintes

- **Fase 1** — Domínio: adicionar `TenantId` + `EnvironmentId` às entidades críticas
- **Fase 2** — Contexto: criar `ICurrentEnvironment`, middleware, behavior
- **Fase 3** — Dados: migrations, índices, backfill
- **Fase 4** — Backend modular: atualizar handlers, repositórios, endpoints
- **Fase 5** — Telemetria e integrações: atualizar modelos, propagação de contexto
- **Fase 6** — Frontend: `EnvironmentContext`, remoção de hardcodes
- **Fase 7** — IA: context injection automática
- **Fase 8** — Testes e rollout: cobertura completa, rollout gradual

---

## 10. Consequências

**Positivas:**
- Isolamento real por tenant e ambiente
- Suporte a ambientes customizados por tenant
- IA contextualmente correta e segura
- Telemetria filtrada por tenant/ambiente
- Governança de mudanças por ambiente parametrizável

**Negativas (custo de transição):**
- Esforço de migração muito grande
- Risco de regressão durante backfill
- Testes existentes precisarão de atualização extensiva
