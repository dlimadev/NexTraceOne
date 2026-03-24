# Relatório de Integridade e Indexação de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Análise de índices, constraints, relacionamentos e integridade estrutural  
> **Método:** Análise estática de EntityTypeConfiguration, HasIndex, HasForeignKey, OnDelete  
> **Fonte:** 132 ficheiros de configuração de entidades EF Core

---

## 1. Resumo de Métricas

| Métrica | Valor |
|---|---|
| Total de definições `HasIndex` | 353 |
| Índices únicos (`IsUnique`) | 42 |
| Índices compostos (multi-coluna) | 46 |
| Índices filtered/partial (`HasFilter`) | 2 |
| Check constraints (`HasCheckConstraint`) | **0** |
| Relacionamentos com Cascade delete | 12 |
| Relacionamentos com Restrict | 1 |
| Relacionamentos com SetNull | 1 |
| Concurrency tokens (RowVersion) | **0** |
| Global query filters (soft delete) | Todos os `AuditableEntity` |

---

## 2. Análise de Índices

### 2.1 Distribuição de Índices por Tipo

```
Total: 353 definições HasIndex
├── Índices simples (single column) .... 263 (74,5%)
├── Índices compostos (multi-column) .... 46 (13,0%)
├── Índices únicos ...................... 42 (11,9%)
└── Índices filtered/partial ............. 2 (0,6%)
```

### 2.2 Índices Únicos (42)

Os 42 índices únicos protegem a integridade de dados onde combinações devem ser únicas:

| Área | Exemplos Típicos | Quantidade Estimada |
|---|---|---|
| Identity | User.Email, ApiKey.KeyHash, Role.Name per Tenant | ~8 |
| Contracts | ContractVersion (ContractId + Version), Schema.Name | ~5 |
| Catalog | ServiceDefinition.Slug per Tenant, Endpoint.Path | ~6 |
| Change | Release.Tag per Environment, ChangeScore unique key | ~5 |
| AI | AiModel.Name per Provider, AiAgent.Slug per Tenant | ~8 |
| Governance | Policy.Name per Tenant, Standard.Code | ~5 |
| Outros | Diversos across remaining modules | ~5 |

### 2.3 Índices Compostos (46)

Índices compostos suportam queries multi-critério frequentes:

| Padrão Comum | Exemplo | Frequência |
|---|---|---|
| TenantId + EntityId | `(TenantId, ServiceId)` | Alta |
| TenantId + Name/Slug | `(TenantId, Slug)` com IsUnique | Alta |
| TenantId + Status + CreatedAt | Para listagens filtradas por estado | Média |
| ParentId + ChildId | Para relações many-to-many | Média |
| EntityId + Version | Para versionamento | Baixa |

### 2.4 Índices Filtered/Partial (2)

**Estado:** Apenas 2 índices filtered em todo o sistema — insuficiente.

| Índice | Contexto | Filtro |
|---|---|---|
| (Não documentado — análise estática indica 2) | Provavelmente soft-delete related | `WHERE IsDeleted = false` |

#### Impacto da Ausência de Filtered Indexes

Todas as entidades que estendem `AuditableEntity` têm `IsDeleted` com global query filter. Sem filtered indexes:

| Cenário | Impacto |
|---|---|
| Listagem de entidades ativas | Scan completo do índice, incluindo registos deletados |
| Contagem de registos ativos | Performance degradada com crescimento de dados |
| Unique constraints em dados ativos | Não é possível reutilizar slugs/nomes de entidades deletadas |

**Recomendação:** Adicionar filtered indexes `WHERE "IsDeleted" = false` em:
- Todos os índices únicos de entidades com soft-delete
- Índices de lookup mais frequentes (TenantId + slug/name)
- Índices de listagem (TenantId + status + createdAt)

### 2.5 Índices Ausentes Identificados

| Query Comum Esperada | Índice Necessário | Estado |
|---|---|---|
| Busca de incidentes por serviço + período | `(ServiceId, CreatedAt)` em Incident | ⚠️ Verificar |
| Busca de conversas de IA por utilizador | `(UserId, CreatedAt)` em Conversation | ⚠️ Verificar |
| Auditoria por utilizador + período | `(UserId, Timestamp)` em AuditEvent | ⚠️ Verificar |
| Releases por serviço + ambiente | `(ServiceId, EnvironmentId)` em Release | ⚠️ Verificar |
| Cost allocation por período | `(Period, TenantId)` em CostAllocation | ⚠️ Verificar |
| Violations por política + estado | `(PolicyId, Status)` em PolicyViolation | ⚠️ Verificar |
| Token usage por agente + período | `(AgentId, Period)` em AiTokenUsage | ⚠️ Verificar |

---

## 3. Check Constraints

### 3.1 Estado Atual

**Zero check constraints em todo o sistema.**

Toda a validação de valores de coluna é feita exclusivamente na camada aplicacional (domain validation, value objects, guard clauses).

### 3.2 Riscos

| Risco | Probabilidade | Impacto |
|---|---|---|
| Dados inválidos via acesso direto à BD | Baixa (RLS protege) | Alto |
| Dados inválidos via bug no código | Média | Médio |
| Enum values fora do range | Baixa (conversões string) | Baixo |
| Valores numéricos negativos inválidos | Baixa | Médio |

### 3.3 Check Constraints Recomendados

| Entidade | Campo | Constraint |
|---|---|---|
| AiTokenBudget | MaxTokens | `CHECK (max_tokens > 0)` |
| CostAllocation | Amount | `CHECK (amount >= 0)` |
| BudgetAlert | Threshold | `CHECK (threshold > 0 AND threshold <= 100)` |
| PromotionSLA | MaxDurationHours | `CHECK (max_duration_hours > 0)` |
| SLODefinition | TargetPercentage | `CHECK (target_percentage > 0 AND target_percentage <= 100)` |
| ChangeScore | Score | `CHECK (score >= 0 AND score <= 100)` |

---

## 4. Conversões de Enum

### 4.1 Enum → String (91 conversões)

| Módulo | Exemplo de Enum | Conversão |
|---|---|---|
| Identity | UserStatus, RoleType | `.HasConversion<string>()` |
| Contracts | ContractType, ContractStatus | `.HasConversion<string>()` |
| Change | ReleaseStatus, ChangeImpact | `.HasConversion<string>()` |
| AI | AgentStatus, ModelCapability | `.HasConversion<string>()` |
| Governance | PolicySeverity, ViolationStatus | `.HasConversion<string>()` |
| Incidents | IncidentSeverity, IncidentStatus | `.HasConversion<string>()` |
| FinOps | CostCategory, AlertPriority | `.HasConversion<string>()` |

**Vantagem:** Legibilidade em queries diretas e debugging.  
**Desvantagem:** Sem check constraint, qualquer string é aceite pela BD.

### 4.2 Enum → Int (12 conversões)

| Módulo | Exemplo | Razão Provável |
|---|---|---|
| (Diversos) | Prioridades, severidades numéricas | Performance em ordenação |

**Inconsistência:** Ter 91 enum→string e 12 enum→int sugere falta de padronização. Recomendação: uniformizar para string (favorecendo legibilidade) ou documentar critério de escolha.

---

## 5. Conversões de Strongly-Typed IDs

### 5.1 Estado

100+ conversões de strongly-typed IDs para tipos de BD (normalmente `Guid`).

| Padrão | Exemplo |
|---|---|
| `ValueConverter<UserId, Guid>` | UserId → Guid |
| `ValueConverter<TenantId, Guid>` | TenantId → Guid |
| `ValueConverter<ServiceId, Guid>` | ServiceId → Guid |
| `ValueConverter<ContractId, Guid>` | ContractId → Guid |

**Avaliação:** ✅ Excelente prática — previne mistura de IDs de entidades diferentes a nível de compilação.

### 5.2 Value Objects (50+)

| Tipo | Persistência |
|---|---|
| Email | String column com conversão |
| FullName | Owned entity (FirstName, LastName) |
| HashedPassword | String column |
| ContractVersion.Signature | Owned entity |
| ContractVersion.Provenance | Owned entity |

---

## 6. Relacionamentos e Integridade Referencial

### 6.1 Delete Behaviors

| Tipo | Quantidade | Contexto |
|---|---|---|
| **Cascade** | 12 | Parent→children fortemente acoplados |
| **Restrict** | 1 | Previne deleção de parent com children |
| **SetNull** | 1 | Torna FK nullable ao deletar parent |
| **Não especificado** | Maioria | EF Core default (Cascade para required, SetNull para optional) |

### 6.2 Análise de Cascade Deletes

| Relação | Tipo | Risco |
|---|---|---|
| Tenant → TenantMembers | Cascade | ⚠️ Alto — deletar tenant apaga todos os membros |
| Team → TeamMembers | Cascade | Médio — comportamento esperado |
| Workflow → Stages → Evidence | Cascade | Aceitável — workflow é agregado |
| AiAgent → Capabilities, Tools | Cascade | Aceitável — agente é agregado |
| Conversation → Messages | Cascade | Aceitável — conversa é agregado |

**Risco principal:** Com soft delete ativo, cascade deletes físicos são raros. Mas se um administrador executar hard delete direto na BD, as cascades podem ser destrutivas.

### 6.3 Relações Sem Delete Behavior Explícito

A maioria das relações não especifica `OnDelete` explicitamente. EF Core aplica:
- **Cascade** para FKs `required` (não nullable)
- **SetNull** para FKs `optional` (nullable)

**Risco:** Comportamento implícito pode surpreender em edge cases. Recomendação: tornar explícito o delete behavior em todas as relações.

---

## 7. Concorrência (RowVersion)

### 7.1 Estado Atual

**Nenhuma entidade tem RowVersion/ConcurrencyToken.**

O controlo de conflitos é feito via `UpdatedAt` a nível aplicacional:
1. Read entity → obter `UpdatedAt`
2. Update entity → verificar se `UpdatedAt` não mudou
3. Se mudou → rejeitar update

### 7.2 Análise de Risco

| Cenário | Risco sem RowVersion | Mitigação Atual |
|---|---|---|
| Edição concorrente de contrato | Lost update possível | UpdatedAt check (race condition) |
| Aprovação simultânea de workflow | Dupla aprovação possível | UpdatedAt check (race condition) |
| Update simultâneo de configuração | Last write wins | UpdatedAt check (race condition) |
| Update de token budget | Over-spending possível | UpdatedAt check (race condition) |

**Problema:** O check via `UpdatedAt` tem uma window of vulnerability entre o read e o write. Com RowVersion (optimistic concurrency nativa do PostgreSQL), o conflito seria detetado atomicamente pelo motor da BD.

### 7.3 Entidades Candidatas a RowVersion

| Entidade | Prioridade | Justificação |
|---|---|---|
| ContractVersion | Alta | Edição concorrente de contratos é cenário real |
| WorkflowApproval | Alta | Dupla aprovação deve ser impossível |
| AiTokenBudget | Alta | Over-spending de tokens |
| PromotionApproval | Média | Aprovação concorrente |
| GovernancePolicy | Média | Edição concorrente de políticas |
| ConfigurationValue | Média | Configuração concorrente |

---

## 8. Outbox Pattern — Integridade

### 8.1 Estado

Cada DbContext tem uma tabela outbox para domain events → messages. O `OutboxInterceptor` captura events em `SaveChangesAsync`.

### 8.2 Risco de Colisão

Em `nextraceone_operations` (12 DbContexts na mesma BD), se dois ou mais contextos usarem o mesmo nome de tabela outbox (default: `outbox_messages`), haverá colisão.

| Estado | Detalhe |
|---|---|
| Configuração por contexto | Via `OutboxTableName` na classe DbContext |
| Risco | Se algum não customizar, usa default e colide |

**Recomendação:** Verificar e garantir unicidade de `OutboxTableName` em todos os 12 DbContexts de `nextraceone_operations`.

---

## 9. Soft Delete — Integridade

### 9.1 Mecanismo

| Componente | Detalhe |
|---|---|
| Flag | `AuditableEntity.IsDeleted` (bool) |
| Global filter | `.HasQueryFilter(e => !e.IsDeleted)` em NexTraceDbContextBase |
| Aplicação | Automática em todas as queries EF Core |
| Bypass | `IgnoreQueryFilters()` para queries administrativas |

### 9.2 Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Dados soft-deleted acumulam | Performance degradada | Archival/purge process (não existe) |
| Unique constraints incluem deleted | Slug reutilizável após delete? | Filtered unique indexes (apenas 2) |
| Cascading soft-delete | Filhos não marcados automaticamente | Lógica aplicacional necessária |

---

## 10. Resumo de Riscos e Recomendações

### 🔴 Riscos Altos

| # | Risco | Recomendação |
|---|---|---|
| 1 | 0 check constraints — sem validação ao nível da BD | Adicionar check constraints para campos numéricos e ranges |
| 2 | 0 RowVersion — lost updates possíveis | Adicionar ConcurrencyToken a entidades críticas |
| 3 | Apenas 2 filtered indexes — performance com soft-delete | Adicionar `WHERE IsDeleted = false` a índices frequentes |

### 🟡 Riscos Médios

| # | Risco | Recomendação |
|---|---|---|
| 4 | Delete behaviors maioritariamente implícitos | Tornar explícito `OnDelete` em todas as relações |
| 5 | Inconsistência enum→string (91) vs enum→int (12) | Padronizar ou documentar critério |
| 6 | Possível colisão de outbox tables em nextraceone_operations | Verificar unicidade de OutboxTableName |
| 7 | Sem processo de archival de dados soft-deleted | Definir política de retenção e purge |

### 🟢 Riscos Baixos

| # | Risco | Recomendação |
|---|---|---|
| 8 | Cascade delete em Tenant pode ser destrutivo | Garantir que hard delete de Tenant é protegido |
| 9 | Índices ausentes para queries comuns | Audit de performance com dados reais |

---

## Referências de Código

| Artefacto | Localização |
|---|---|
| Entity configurations | `src/modules/*/Infrastructure/Persistence/Configurations/` |
| Base class (global filters) | `src/platform/NexTraceOne.SharedKernel/Persistence/NexTraceDbContextBase.cs` |
| Interceptors | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/` |
| Migrations (com CreateIndex) | `src/modules/*/Infrastructure/Persistence/Migrations/` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
