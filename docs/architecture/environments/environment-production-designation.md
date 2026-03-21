# Environment Production Designation

**Data:** 2026-03-21  
**Status:** Implementado

---

## Como o Ambiente Produtivo é Definido

O NexTraceOne usa o campo `IsPrimaryProduction` na entidade `Environment` como a fonte de verdade para o **ambiente produtivo principal** de cada tenant.

### Diferença entre `IsProductionLike` e `IsPrimaryProduction`

| Campo | Significado |
|-------|-------------|
| `IsProductionLike` | Indica que o ambiente tem comportamento e políticas similares à produção (Production, DisasterRecovery, etc.) |
| `IsPrimaryProduction` | Indica que este é **o** ambiente de produção principal do tenant — a referência oficial para comparação e análise de risco |

Um tenant pode ter múltiplos ambientes `IsProductionLike=true` (ex.: produção + DR), mas apenas **um** ambiente `IsPrimaryProduction=true`.

## Regra de Unicidade

**Apenas um ambiente ativo com `IsPrimaryProduction=true` pode existir por tenant.**

Esta regra é garantida em três camadas:

1. **Domínio** (`Environment.DesignateAsPrimaryProduction()`): lança `InvalidOperationException` se o ambiente não estiver ativo.
2. **Aplicação** (`SetPrimaryProductionEnvironment.Handler`): revoga o primário anterior e designa o novo em uma única transação.
3. **Banco de dados**: índice parcial único:
   ```sql
   CREATE UNIQUE INDEX "IX_identity_environments_tenant_primary_production_unique"
   ON "identity_environments" ("TenantId", "IsPrimaryProduction")
   WHERE "IsPrimaryProduction" = true AND "IsActive" = true;
   ```

O índice parcial filtra apenas registos onde `IsPrimaryProduction=true AND IsActive=true`, permitindo múltiplos ambientes não-primários e ambientes inativos sem conflito.

## Fluxo de Designação

```
PATCH /api/v1/identity/environments/{environmentId}/primary-production
```

1. Handler obtém o ambiente pelo `EnvironmentId` + `TenantId` (isolamento garantido)
2. Valida que o ambiente está ativo
3. Se já é o primário, retorna sucesso sem alteração
4. Busca o ambiente primário atual e revoga sua designação (`RevokePrimaryProductionDesignation()`)
5. Designa o novo ambiente (`DesignateAsPrimaryProduction()`)
6. Commit via `TransactionBehavior` do pipeline — atomicidade garantida

## Validações de Segurança

- Ambiente não encontrado → 404 `Identity.Environment.NotFound`
- Ambiente inativo → 422 `Identity.Environment.CannotDesignateInactiveAsPrimaryProduction`
- Tenant mismatch → retorna not found (não vaza informação de outros tenants)
- Criar ambiente com `IsPrimaryProduction=true` quando já existe outro → 409 `Identity.Environment.PrimaryProductionAlreadyExists`
- Desativar ambiente designado como primário → `IsPrimaryProduction` é automaticamente revogado

## Implicações no Backend

- `IEnvironmentRepository.GetPrimaryProductionAsync(tenantId)` retorna o ambiente produtivo principal ativo
- `GetPrimaryProductionEnvironment` query expõe o dado via `GET /api/v1/identity/environments/primary-production`
- Handlers de IA recebem a identificação do ambiente produtivo para comparação

## Implicações no Frontend

- `EnvironmentsPage` exibe badge "Primary Production" para o ambiente designado
- Botão "Set as Primary Production" visível para ambientes ativos sem a designação
- `ListEnvironments` retorna `isPrimaryProduction` em cada item

## Implicações na IA

A IA usa o ambiente produtivo principal como referência de comparação:
- `AssessPromotionReadiness`: target deve ser `IsProductionLike=true`
- `CompareEnvironments`: pode comparar não-prod com a produção principal
- `AnalyzeNonProdEnvironment`: identifica se o ambiente é ou não produção
- Consultas futuras de promotion risk usarão `GetPrimaryProductionAsync` para obter a referência
