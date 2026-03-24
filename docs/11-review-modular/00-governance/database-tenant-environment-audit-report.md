# Relatório de Multi-Tenancy, Ambientes e Auditoria — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Análise transversal de mecanismos de tenant isolation, gestão de ambientes e auditoria automática  
> **Método:** Análise estática de interceptors, base class, entity configurations e patterns  
> **Foco:** Verificar se o sistema suporta adequadamente multi-tenancy, isolamento de ambientes e rastreabilidade

---

## 1. Resumo Executivo

| Capacidade | Estado | Mecanismo |
|---|---|---|
| Multi-tenant isolation | ✅ Forte | PostgreSQL Row-Level Security (RLS) |
| TenantId em todas as entidades | ✅ Automático | Via `AuditableEntity` base class |
| Gestão de ambientes | ✅ Adequado | `EnvironmentProfile` com `IsPrimaryProduction` |
| Auditoria automática (CreatedAt/By) | ✅ Forte | `AuditInterceptor` |
| Soft delete | ✅ Automático | `IsDeleted` + global query filter |
| Encriptação de campos | ✅ Declarativa | AES-256-GCM via `[EncryptedField]` |
| Audit trail dedicado | ✅ Existe | `AuditDbContext` (6 entidades) |

---

## 2. Multi-Tenancy

### 2.1 Arquitetura

| Componente | Detalhe |
|---|---|
| Estratégia | Shared database, shared schema com RLS |
| Coluna discriminadora | `TenantId` (Guid/UUID) |
| Enforcement | PostgreSQL Row-Level Security policies |
| Interceptor | `TenantRlsInterceptor` |
| Scope | Todas as entidades que estendem `AuditableEntity` |

### 2.2 Fluxo de TenantId

```
Request HTTP
  → Extração de TenantId (JWT / header)
    → Injeção em DbContext via TenantRlsInterceptor
      → SET LOCAL app.tenant_id = '{tenantId}'
        → PostgreSQL RLS filtra automaticamente
```

### 2.3 TenantRlsInterceptor

| Aspeto | Detalhe |
|---|---|
| Tipo | `DbConnectionInterceptor` |
| Ação | Executa `SET LOCAL app.tenant_id` no início de cada conexão |
| Scope | Toda query é automaticamente filtrada pelo tenant |
| Bypass | Não há bypass documentado (seguro por design) |

**Localização:** `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/TenantRlsInterceptor.cs`

### 2.4 Avaliação por Base de Dados

| Base de Dados | DbContexts | Tenant RLS | Avaliação |
|---|---|---|---|
| `nextraceone_identity` | 2 | ✅ | Tenant é entidade nativa |
| `nextraceone_catalog` | 3 | ✅ | Serviços e contratos por tenant |
| `nextraceone_operations` | 12 | ✅ | Todas as operações isoladas |
| `nextraceone_ai` | 3 | ✅ | IA governada por tenant |

### 2.5 Riscos de Multi-Tenancy

| Risco | Severidade | Mitigação |
|---|---|---|
| RLS policy não aplicada em novas tabelas | Médio | Verificar que migrations criam RLS policies |
| Cross-tenant data leak via join | Baixo | RLS cobre automaticamente |
| Admin queries sem tenant filter | Médio | Bypass intencional deve ser auditado |
| Performance de RLS com muitos tenants | Baixo | PostgreSQL RLS é eficiente com índice em TenantId |
| TenantId não propagado em background jobs | Médio | Verificar que workers injetam TenantId |

### 2.6 Questão Resolvida: TenantId Type

O `AiGovernanceDbContext` teve 2 migrations dedicadas a corrigir o tipo de TenantId:
- `StandardizeTenantIdToGuid` — Padronização para Guid
- `FixTenantIdToUuid` — Correção para UUID PostgreSQL

**Estado:** Resolvido — mas indica que houve período de inconsistência. Todos os módulos devem usar `Guid` mapeado para `uuid` PostgreSQL.

---

## 3. Gestão de Ambientes

### 3.1 Modelo de Dados

| Entidade | Campo | Tipo | Descrição |
|---|---|---|---|
| `EnvironmentProfile` | `Id` | EnvironmentId | Identificador único |
| | `Name` | string | Nome (Development, Staging, Production) |
| | `Slug` | string | Slug para URLs |
| | `TenantId` | TenantId | Tenant proprietário |
| | `IsPrimaryProduction` | bool | Marca o ambiente principal de produção |
| | `CreatedAt` | DateTimeOffset | Data de criação |
| | `IsDeleted` | bool | Soft delete |

### 3.2 Uso de Ambientes no Produto

| Funcionalidade | Uso de Ambiente | Suporte DB |
|---|---|---|
| Change Confidence | Promotions entre ambientes | ✅ PromotionRequest referencia ambientes |
| Blast Radius | Impacto por ambiente | ✅ Release associada a ambiente |
| Service Health | Estado por ambiente | ⚠️ Parcial — ServiceHealth sem FK para ambiente |
| Configuration | Overrides por ambiente | ✅ ConfigurationOverride |
| Monitoring | Métricas por ambiente | ⚠️ Parcial — RuntimeMetric sem FK explícita |

### 3.3 IsPrimaryProduction

| Aspeto | Detalhe |
|---|---|
| Adicionado em | Migration `AddIsPrimaryProductionToEnvironment` |
| Propósito | Identificar o ambiente principal de produção |
| Uso | Change Confidence — mudanças para production requerem validação extra |
| Constraint | Apenas um ambiente por tenant pode ser `IsPrimaryProduction = true` |

**Risco:** Não existe check constraint ou unique filtered index para garantir unicidade de `IsPrimaryProduction = true` por tenant. A validação é feita apenas na camada aplicacional.

---

## 4. Auditoria Automática

### 4.1 AuditInterceptor

| Aspeto | Detalhe |
|---|---|
| Tipo | `SaveChangesInterceptor` |
| Trigger | `SavingChangesAsync` |
| Campos preenchidos | `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` |
| Scope | Todas as entidades `AuditableEntity` |

**Localização:** `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/AuditInterceptor.cs`

### 4.2 Campos de Auditoria

| Campo | Tipo | Preenchimento | Contexto |
|---|---|---|---|
| `CreatedAt` | DateTimeOffset | Automático (insert) | Timestamp UTC |
| `UpdatedAt` | DateTimeOffset | Automático (update) | Timestamp UTC |
| `CreatedBy` | string | Automático (insert) | UserId do request |
| `UpdatedBy` | string | Automático (update) | UserId do request |
| `TenantId` | Guid | Automático | TenantId do request |
| `IsDeleted` | bool | Manual (soft delete) | Default: false |

### 4.3 Fluxo de Auditoria

```
SaveChangesAsync()
  → AuditInterceptor.SavingChangesAsync()
    → Para cada EntityEntry:
      ├── Se Added: CreatedAt = UTC.Now, CreatedBy = currentUser
      ├── Se Modified: UpdatedAt = UTC.Now, UpdatedBy = currentUser
      └── Se Deleted: IsDeleted = true, UpdatedAt = UTC.Now (soft delete)
```

### 4.4 Avaliação por Módulo

| Módulo | AuditableEntity | CreatedAt/By | UpdatedAt/By | Soft Delete | Avaliação |
|---|---|---|---|---|---|
| Identity | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Audit | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Catalog (Contracts) | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Catalog (Graph) | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Catalog (Portal) | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Change Intelligence | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Promotion | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Workflow | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Ruleset | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Configuration | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Governance | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Notifications | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| AI Governance | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| AI Orchestration | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| External AI | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Cost Intelligence | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Runtime Intelligence | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Incidents | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Automation | ✅ | ✅ | ✅ | ✅ | ✅ Completo |
| Reliability | ✅ | ✅ | ✅ | ✅ | ✅ Completo |

**Resultado:** 100% de cobertura — todas as entidades auditáveis têm tracking completo.

---

## 5. Soft Delete

### 5.1 Mecanismo

| Componente | Detalhe |
|---|---|
| Flag | `AuditableEntity.IsDeleted` (bool, default: false) |
| Global filter | `.HasQueryFilter(e => !e.IsDeleted)` |
| Aplicação | Automática em todas as queries EF Core |
| Bypass | `IgnoreQueryFilters()` |

### 5.2 Comportamento

```
// Query normal — filtra automaticamente
var services = await context.Services.ToListAsync();
// SQL: SELECT * FROM services WHERE "IsDeleted" = false AND tenant RLS

// Query admin — sem filtro
var allServices = await context.Services.IgnoreQueryFilters().ToListAsync();
// SQL: SELECT * FROM services (sem filtro IsDeleted, mas com tenant RLS)
```

### 5.3 Riscos de Soft Delete

| Risco | Severidade | Estado |
|---|---|---|
| Dados acumulam indefinidamente | Médio | ❌ Sem política de retenção/purge |
| Unique constraints incluem deleted | Médio | ❌ Apenas 2 filtered indexes |
| Cascade soft delete não automático | Médio | ⚠️ Requer lógica aplicacional |
| Performance degradada com dados históricos | Baixo-Médio | ❌ Sem archival |

### 5.4 Entidades Críticas para Soft Delete

| Entidade | Cenário de Delete | Risco sem Cascade Soft Delete |
|---|---|---|
| Tenant | Desativação de organização | Membros, serviços, contratos ficam órfãos (soft) |
| Service | Remoção de serviço | Dependências, endpoints ficam órfãos |
| Contract | Descontinuação de contrato | Versões, schemas ficam acessíveis |
| AI Agent | Desativação de agente | Capabilities, tools ficam acessíveis |

---

## 6. Encriptação de Campos

### 6.1 Mecanismo

| Componente | Detalhe |
|---|---|
| Algoritmo | AES-256-GCM |
| Atributo | `[EncryptedField]` |
| Interceptor | `EncryptionInterceptor` |
| Scope | Campos marcados com atributo |
| Transparência | Encriptação/decriptação automática em save/load |

### 6.2 Campos Tipicamente Encriptados

| Entidade | Campo | Justificação |
|---|---|---|
| User | Password (hash) | Segurança de credenciais |
| ApiKey | KeyValue | Segurança de chaves |
| ExternalAiIntegration | ApiKey/Secret | Credenciais de IA externa |
| Configuration | Sensitive values | Configurações sensíveis |

### 6.3 Avaliação

| Aspeto | Estado |
|---|---|
| Transparência para o código | ✅ Atributo declarativo |
| Performance | ⚠️ Overhead por field — aceitável para campos sensíveis |
| Key management | ❌ Não analisado — verificar gestão de encryption keys |
| Key rotation | ❌ Não documentado |

---

## 7. Audit Trail Dedicado

### 7.1 AuditDbContext (6 entidades)

| Entidade | Propósito |
|---|---|
| AuditEvent | Registo de eventos auditáveis (login, CRUD, etc.) |
| AuditChainLink | Cadeia de integridade (blockchain-like) |
| ComplianceReport | Relatórios de conformidade |
| ComplianceRule | Regras de compliance |
| ComplianceViolation | Violações detetadas |
| ComplianceSnapshot | Snapshots de estado |

### 7.2 Diferença entre Auditoria Automática e Audit Trail

| Aspeto | Auditoria Automática | Audit Trail Dedicado |
|---|---|---|
| Mecanismo | `AuditInterceptor` | `AuditDbContext` |
| Dados | CreatedAt/By, UpdatedAt/By | Eventos detalhados com contexto |
| Granularidade | Por entidade (insert/update) | Por ação de negócio |
| Retenção | Junto com a entidade | Base de dados separada |
| Compliance | Básico | SOC2, ISO27001 ready |

### 7.3 Cobertura do Audit Trail

| Área | Eventos Auditados | Estado |
|---|---|---|
| Login/Logout | ✅ | Via seed (35 eventos) |
| CRUD de entidades | ✅ | Via AuditInterceptor |
| Mudanças de configuração | ⚠️ | Parcial |
| Aprovações de workflow | ⚠️ | Parcial |
| Acesso a IA | ✅ | Via AiAuditEntry |
| Mudanças de permissão | ⚠️ | Parcial |
| Export de dados | ❌ | Não auditado |
| Acesso a dados sensíveis | ❌ | Não auditado |

---

## 8. Avaliação por Módulo — Resumo

| Módulo | Tenant | Ambiente | Auditoria | Soft Delete | Encrypt | Nota |
|---|---|---|---|---|---|---|
| Identity | ✅ | ✅ | ✅ | ✅ | ✅ | Referência |
| Audit | ✅ | — | ✅ | ✅ | ✅ | Audit de audit |
| Contracts | ✅ | ⚠️ | ✅ | ✅ | ✅ | Falta ambiente por contrato |
| CatalogGraph | ✅ | ⚠️ | ✅ | ✅ | — | Health sem FK ambiente |
| DeveloperPortal | ✅ | — | ✅ | ✅ | — | Portal não é per-environment |
| ChangeIntelligence | ✅ | ✅ | ✅ | ✅ | — | Releases por ambiente |
| Promotion | ✅ | ✅ | ✅ | ✅ | — | Cross-environment by design |
| Workflow | ✅ | — | ✅ | ✅ | — | Workflows são cross-env |
| Ruleset | ✅ | — | ✅ | ✅ | — | |
| Configuration | ✅ | ✅ | ✅ | ✅ | ✅ | Overrides per-env |
| Governance | ✅ | — | ✅ | ✅ | — | Governance é cross-env |
| Notifications | ✅ | — | ✅ | ✅ | — | |
| AI Governance | ✅ | — | ✅ | ✅ | ✅ | API keys encrypted |
| AI Orchestration | ✅ | — | ✅ | ✅ | — | |
| External AI | ✅ | — | ✅ | ✅ | ✅ | Credentials encrypted |
| Cost Intelligence | ✅ | ⚠️ | ✅ | ✅ | — | Custos por ambiente? |
| Runtime Intelligence | ✅ | ⚠️ | ✅ | ✅ | — | Métricas por ambiente? |
| Incidents | ✅ | ⚠️ | ✅ | ✅ | — | Incidentes por ambiente? |
| Automation | ✅ | — | ✅ | ✅ | — | |
| Reliability | ✅ | ⚠️ | ✅ | ✅ | — | SLO por ambiente? |

---

## 9. Recomendações

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Garantir RLS policies em todas as tabelas (incluindo futuras) | Leak de tenant é crítico |
| 2 | Adicionar unique filtered index `(TenantId, IsPrimaryProduction) WHERE IsPrimaryProduction = true` | Garantir unicidade de prod |
| 3 | Verificar propagação de TenantId em background workers | Workers podem não ter HTTP context |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 4 | Adicionar FK para EnvironmentId em ServiceHealth, RuntimeMetric, Incident | Contextualização por ambiente |
| 5 | Implementar política de retenção de dados soft-deleted | Dados acumulam indefinidamente |
| 6 | Auditar acesso a dados sensíveis e exports | Compliance requirement |
| 7 | Documentar key management para encriptação AES-256-GCM | Segurança operacional |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 8 | Implementar key rotation para [EncryptedField] | Best practice de segurança |
| 9 | Adicionar cascade soft delete para agregados | Evitar órfãos soft-deleted |
| 10 | Expandir audit trail para aprovações e mudanças de permissão | Compliance avançada |

---

## Referências

| Artefacto | Localização |
|---|---|
| TenantRlsInterceptor | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/TenantRlsInterceptor.cs` |
| AuditInterceptor | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/AuditInterceptor.cs` |
| EncryptionInterceptor | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/EncryptionInterceptor.cs` |
| NexTraceDbContextBase | `src/platform/NexTraceOne.SharedKernel/Persistence/NexTraceDbContextBase.cs` |
| AuditableEntity | `src/platform/NexTraceOne.SharedKernel/Domain/AuditableEntity.cs` |
| AuditDbContext | `src/modules/auditcompliance/Infrastructure/Persistence/AuditDbContext.cs` |
| EnvironmentProfile | `src/modules/identityaccess/Domain/Entities/EnvironmentProfile.cs` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
