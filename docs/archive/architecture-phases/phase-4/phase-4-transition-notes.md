# Phase 4 — Notas de Transição e Retrocompatibilidade

## Princípio de Backward Compatibility

Todas as mudanças da Fase 4 são **não-breaking** e **aditivas**.

## Novos campos são nullable

| Entidade | Campo | Tipo | Default |
|----------|-------|------|---------|
| IncidentRecord | TenantId | Guid? | null |
| IncidentRecord | EnvironmentId | Guid? | null |
| Release | TenantId | Guid? | null |
| Release | EnvironmentId | Guid? | null |

Registros criados antes da Fase 4 continuarão a funcionar normalmente,
com esses campos como `null`. Nenhuma lógica existente depende desses campos.

## Migrações são aditivas

As migrações da Fase 4 apenas adicionam colunas e índices:
- `ALTER TABLE ... ADD COLUMN tenant_id uuid NULL`
- `ALTER TABLE ... ADD COLUMN environment_id uuid NULL`
- `CREATE INDEX ...`

Nenhuma coluna existente é modificada ou removida.

## CreateIncidentInput é retrocompatível

Os novos parâmetros `TenantId` e `EnvironmentId` têm valor padrão `null`:

```csharp
public sealed record CreateIncidentInput(
    // ... parâmetros existentes ...
    Guid? TenantId = null,
    Guid? EnvironmentId = null);
```

Chamadas existentes sem esses parâmetros continuarão a compilar e funcionar.

## SetTenantContext é idempotente

O método usa `??=` (assign-if-null), garantindo que:
- Se chamado com valores, eles são definidos uma vez
- Se chamado novamente, os valores originais são preservados
- Se chamado com `null`, nada muda

## Resolução de ambiente é opcional

O `CreateIncident.Handler` verifica `IsResolved` antes de usar o `EnvironmentId`:

```csharp
var environmentId = currentEnvironment.IsResolved ? currentEnvironment.EnvironmentId : (Guid?)null;
```

Requisições sem header `X-Environment-Id` continuam funcionando, apenas sem o enriquecimento.

## ICurrentTenant.IsActive

O `CreateIncident.Handler` verifica `IsActive` antes de usar o `TenantId`:

```csharp
var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;
```
