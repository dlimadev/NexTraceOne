# Relatório de Auditoria — Isolamento de Tenant

> **Módulo:** `src/modules/identityaccess/` + `NexTraceOne.BuildingBlocks.Security`  
> **Data da análise:** 2025-07  
> **Classificação:** STRONG_ISOLATION_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O NexTraceOne implementa isolamento de tenant em três camadas complementares: claim JWT (`tenant_id`), middleware de resolução com múltiplas fontes, e Row-Level Security (RLS) PostgreSQL via `TenantRlsInterceptor` aplicado a TODAS as operações EF Core. O modelo inclui `TenantMembership` para vinculação utilizador-tenant-papel, configuração OIDC per-tenant e resolução por subdomínio para cenários SaaS.

A classificação STRONG_ISOLATION_APPARENT reflecte uma implementação de isolamento robusta com defesa em profundidade, onde a RLS no PostgreSQL actua como última linha de defesa independente da camada aplicacional.

---

## 2. Arquitectura de Isolamento — Três Camadas

### 2.1 Diagrama Conceptual

```
┌─────────────────────────────────────────────────┐
│  Camada 1: JWT Claim (tenant_id)                │
│  → Token contém tenant_id do utilizador         │
│  → Validado durante autenticação                │
├─────────────────────────────────────────────────┤
│  Camada 2: TenantResolutionMiddleware           │
│  → Resolve tenant com prioridade definida       │
│  → Define contexto para request inteiro         │
├─────────────────────────────────────────────────┤
│  Camada 3: PostgreSQL RLS (TenantRlsInterceptor)│
│  → set_config('app.current_tenant_id') em TODOS │
│    os comandos EF Core                          │
│  → SQL parametrizado                            │
│  → Última linha de defesa                       │
└─────────────────────────────────────────────────┘
```

### 2.2 Posição no Pipeline

```
... → UseAuthentication → TenantResolutionMiddleware → EnvironmentResolutionMiddleware → UseAuthorization
```

O middleware de resolução de tenant executa **após** a autenticação e **antes** da autorização, garantindo que o contexto de tenant está disponível para decisões de autorização.

---

## 3. Resolução de Tenant

### 3.1 Fontes de Resolução (por prioridade)

| Prioridade | Fonte | Cenário de Uso |
|---|---|---|
| 1 (máxima) | JWT claim `tenant_id` | Sessão autenticada normal |
| 2 | Header `X-Tenant-Id` | API calls com tenant explícito |
| 3 | Query string | Fallback, debugging |
| 4 (mínima) | Subdomínio | Multi-tenancy SaaS |

### 3.2 Comportamento

| Cenário | Resultado |
|---|---|
| JWT claim presente | Usa tenant do JWT (fonte mais confiável) |
| Sem JWT, com header | Usa header X-Tenant-Id |
| Conflito JWT vs header | JWT prevalece (prioridade superior) |
| Nenhuma fonte | Request sem contexto de tenant (potencialmente rejeitado) |

**Avaliação:** ✅ Hierarquia de prioridade sensata — fonte autenticada (JWT) prevalece sobre fontes não autenticadas.

---

## 4. RLS Enforcement — TenantRlsInterceptor

### 4.1 Mecanismo

O `TenantRlsInterceptor` intercepta **TODOS** os comandos EF Core e executa `set_config('app.current_tenant_id', @param, false)` antes de cada operação.

### 4.2 Cobertura

| Tipo de Operação EF Core | Interceptado | Evidência |
|---|---|---|
| `DbDataReader` (queries) | ✅ | TenantRlsInterceptor |
| `NonQuery` (INSERT/UPDATE/DELETE) | ✅ | TenantRlsInterceptor |
| `Scalar` (contagens, etc.) | ✅ | TenantRlsInterceptor |

### 4.3 Segurança do SQL

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Tipo de SQL | Parametrizado (`@param`) | ✅ Imune a SQL injection |
| Scope | `false` (session-level, não transaction-level) | ✅ Adequado |
| Configuração PostgreSQL | `set_config` nativo | ✅ Suportado nativamente |

### 4.4 Importância Arquitectural

A RLS actua como **última linha de defesa**: mesmo que exista um bug na camada aplicacional que permita acesso cross-tenant, o PostgreSQL impede o acesso aos dados de outro tenant ao nível da base de dados.

**Avaliação:** ✅ Defesa em profundidade exemplar.

---

## 5. Modelo de Dados de Tenancy

### 5.1 Entidade Tenant

| Campo | Tipo | Detalhe |
|---|---|---|
| Id | GUID | Identificador único |
| Name | string | Nome do tenant |
| Slug | string (unique) | Identificador URL-friendly |
| IsActive | bool | Soft-delete |

### 5.2 TenantMembership

| Campo | Tipo | Detalhe |
|---|---|---|
| UserId | FK → User | Utilizador |
| TenantId | FK → Tenant | Tenant |
| RoleId | FK → Role | Papel no tenant |
| JoinedAt | DateTime | Data de adesão |
| IsActive | bool | Estado da membership |

### 5.3 Relações

```
User ←→ TenantMembership ←→ Tenant
                ↓
              Role
```

Um utilizador pode pertencer a **múltiplos tenants** com **papéis diferentes** em cada um.

**Avaliação:** ✅ Modelo flexível e adequado para multi-tenancy empresarial.

---

## 6. Configuração Per-Tenant

### 6.1 OIDC Per-Tenant

| Aspecto | Implementação |
|---|---|
| Providers | Configuráveis por tenant |
| Mapeamento | SsoGroupMappings por tenant |
| External Identities | ExternalIdentities vinculadas a tenant |

### 6.2 Ambientes Per-Tenant

| Aspecto | Implementação |
|---|---|
| Ambientes | Definidos por tenant |
| Acesso | EnvironmentAccess por tenant |
| IsPrimaryProduction | Unique partial index por tenant activo |

### 6.3 Políticas Per-Tenant

| Aspecto | Implementação |
|---|---|
| MFA Policy | Configurável por tenant |
| Authentication Policy | Configurável por tenant |
| Session Timeout | Configurável por tenant |
| Max Concurrent Sessions | Configurável por tenant |

**Avaliação:** ✅ Configuração abrangente per-tenant.

---

## 7. Dados de Seed e Verificação

### 7.1 Tenants de Seed

| Tenant | Slug | Ambientes |
|---|---|---|
| NexTrace Corp | nextrace-corp | dev, staging, prod |
| Acme Fintech | acme-fintech | dev, prod |

### 7.2 Verificação de Isolamento

Os dados de seed demonstram:
- 2 tenants com dados separados
- Ambientes distintos por tenant
- TenantMemberships específicas
- EnvironmentAccess por tenant

---

## 8. Análise de Ameaças e Mitigações

### 8.1 Ameaças Consideradas

| Ameaça | Mitigação | Eficácia |
|---|---|---|
| Acesso cross-tenant via API | JWT claim + middleware + RLS | ✅ ALTA |
| SQL injection para bypass de RLS | SQL parametrizado | ✅ ALTA |
| Manipulação de header X-Tenant-Id | JWT claim prevalece | ✅ ALTA |
| Bug aplicacional | RLS como última defesa | ✅ ALTA |
| Utilizador em múltiplos tenants acede dados errados | TenantMembership valida pertença | ✅ ALTA |
| Enumeração de tenants | Slug único, sem listagem pública | ✅ MÉDIA |

### 8.2 Ameaças Residuais

| Ameaça | Risco | Mitigação Recomendada |
|---|---|---|
| Auditoria de queries cross-tenant | BAIXO | Adicionar logging de set_config |
| Admin de plataforma acede todos os tenants | BAIXO (esperado) | Audit trail para acções admin |
| Cache partilhado entre tenants | MÉDIO | Verificar chaves de cache incluem tenant_id |

---

## 9. DbSets e Isolamento

### 9.1 DbSets com TenantId

Dos 15 DbSets no `IdentityDbContext`, os seguintes têm dados tenant-scoped:

| DbSet | Tenant-Scoped | RLS Aplicada |
|---|---|---|
| Users | ✅ (via TenantMembership) | ✅ |
| Roles | ✅ | ✅ |
| Permissions | Global (catálogo) | N/A |
| Tenants | Self-reference | ✅ |
| Sessions | ✅ (via User → Tenant) | ✅ |
| TenantMemberships | ✅ | ✅ |
| ExternalIdentities | ✅ | ✅ |
| SsoGroupMappings | ✅ | ✅ |
| BreakGlassRequests | ✅ | ✅ |
| JitAccessRequests | ✅ | ✅ |
| Delegations | ✅ | ✅ |
| AccessReviewCampaigns | ✅ | ✅ |
| AccessReviewItems | ✅ (via Campaign) | ✅ |
| SecurityEvents | ✅ | ✅ |
| Environments | ✅ | ✅ |
| EnvironmentAccesses | ✅ | ✅ |

**Avaliação:** ✅ Cobertura abrangente — apenas Permissions (catálogo global) não é tenant-scoped, o que é o comportamento correcto.

---

## 10. Recomendações

### Prioridade ALTA

1. **Verificar chaves de cache** incluem tenant_id — prevenir data leakage via cache partilhado
2. **Adicionar teste de integração** que verifica isolamento cross-tenant (tenant A não vê dados de tenant B)

### Prioridade MÉDIA

3. **Logging de set_config** para auditoria de queries cross-tenant (ops de plataforma)
4. **Audit trail dedicado** para acções de PlatformAdmin que afectam múltiplos tenants
5. **Teste de carga** com múltiplos tenants para verificar que RLS não degrada performance

### Prioridade BAIXA

6. **Documentar modelo de tenancy** para developers novos no projecto
7. **Considerar tenant-level encryption** para dados sensíveis (evolução futura)

---

## 11. Conformidade

| Requisito | Estado | Evidência |
|---|---|---|
| Isolamento de dados por tenant | ✅ | RLS PostgreSQL |
| Defesa em profundidade | ✅ | 3 camadas |
| SQL parametrizado | ✅ | TenantRlsInterceptor |
| Multi-tenancy per-config | ✅ | OIDC, MFA, sessão por tenant |
| Soft-delete de tenant | ✅ | IsActive flag |
| Membership auditável | ✅ | JoinedAt, IsActive |
| Resolução determinística | ✅ | Prioridade definida |

---

> **Classificação final:** STRONG_ISOLATION_APPARENT — Isolamento de tenant com defesa em profundidade via JWT + middleware + RLS PostgreSQL, cobrindo todos os DbSets tenant-scoped.
