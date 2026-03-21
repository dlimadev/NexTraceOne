# Environment Management Design

**Data:** 2026-03-21  
**Status:** Estável

---

## Modelo Adotado

O NexTraceOne adota um modelo de **ambientes configuráveis por tenant**, onde cada empresa pode definir os seus próprios ambientes de acordo com o seu ciclo de vida de software.

Não existem ambientes fixos globais. Não se assume DEV/PRE/PROD.

### Entidade principal: `Environment`

```
Environment
├── Id (EnvironmentId)
├── TenantId (TenantId)         // isolamento multitenant
├── Name (string)               // nome de exibição livre
├── Slug (string)               // identificador URL-friendly, único por tenant
├── Code (string?)              // código interno curto (ex.: PROD, QA-EU)
├── SortOrder (int)             // ordenação visual
├── IsActive (bool)             // controle de disponibilidade
├── Profile (EnvironmentProfile)// perfil operacional (sem hardcode textual)
├── Criticality (EnvironmentCriticality)
├── IsProductionLike (bool)     // comportamento similar à produção
├── IsPrimaryProduction (bool)  // ambiente produtivo principal do tenant
├── Description (string?)
├── Region (string?)
└── CreatedAt (DateTimeOffset)
```

## Cadastro de Ambiente por Tenant

### Criação

**Endpoint:** `POST /api/v1/identity/environments`  
**Feature:** `CreateEnvironment`  
**Permissão:** `identity:users:write`

Campos obrigatórios:
- `name` (max 100 chars)
- `slug` (lowercase, hifens, único no tenant)
- `sortOrder` (≥ 0)
- `profile` (EnvironmentProfile válido)
- `criticality` (EnvironmentCriticality válido)
- `isPrimaryProduction` (bool)

### Atualização

**Endpoint:** `PUT /api/v1/identity/environments/{environmentId}`  
**Feature:** `UpdateEnvironment`  
O slug é imutável após criação.

## Perfil Operacional

O `EnvironmentProfile` é o mecanismo central para comportamento por ambiente **sem dependência de nome textual**.

| Profile | Uso típico |
|---------|------------|
| `Development` | Desenvolvimento ativo, menor restrição |
| `Validation` | QA / testes automatizados |
| `Staging` | Homologação próxima de produção |
| `Production` | Produção — restrição máxima |
| `Sandbox` | Experimentação isolada |
| `DisasterRecovery` | Réplica de produção em standby |
| `Training` | Dados fictícios, demonstrações |
| `UserAcceptanceTesting` | UAT — validação pelo negócio |
| `PerformanceTesting` | Testes de carga e stress |

O comportamento do sistema (IA, políticas, auditoria) é dirigido por este perfil, não pelo nome `"PROD"` ou `"DEV"`.

## Regra de Negócio: Ambiente Produtivo Principal

Ver documento dedicado: `environment-production-designation.md`

## Separação Global vs Operacional

- **Contexto global** (`TenantId`): identifica o tenant proprietário
- **Contexto operacional** (`TenantId + EnvironmentId`): contexto mínimo para qualquer operação
- Módulos operacionais usam `TenantEnvironmentContext` como unidade indivisível de contexto
- Telemetria, IA, e relatórios respeitam o par `(TenantId, EnvironmentId)` para isolamento
