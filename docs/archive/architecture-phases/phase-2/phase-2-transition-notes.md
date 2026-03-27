# Fase 2 — Notas de Transição

**Data:** 2026-03-20  
**Status:** Completo

---

## 1. Compatibilidade Preservada

### Nenhum contrato público foi quebrado

| Componente | Status |
|---|---|
| `ICurrentTenant` / `CurrentTenantAccessor` | ✅ Inalterado |
| `ICurrentUser` / `HttpContextCurrentUser` | ✅ Inalterado |
| `TenantResolutionMiddleware` | ✅ Inalterado, ainda na mesma posição no pipeline |
| `TenantIsolationBehavior` | ✅ Inalterado |
| `PermissionRequirement` + handler | ✅ Inalterado |
| Todos os endpoints existentes | ✅ Sem mudanças |
| Todos os handlers MediatR | ✅ Sem mudanças |
| Database / migrations | ✅ Nenhuma migration nova nesta fase |
| APIs públicas | ✅ Apenas adicionado `GET /api/v1/identity/context/runtime` |

### Adições não-destrutivas ao DI

Os novos serviços foram adicionados ao final do `AddIdentityInfrastructure`:
```csharp
services.AddScoped<EnvironmentContextAccessor>();
services.AddScoped<IEnvironmentContextAccessor>(...);
services.AddScoped<ITenantEnvironmentContextResolver, TenantEnvironmentContextResolver>();
services.AddScoped<IEnvironmentProfileResolver, EnvironmentProfileResolver>();
services.AddScoped<IEnvironmentAccessValidator, EnvironmentAccessValidator>();
services.AddScoped<IOperationalExecutionContext, OperationalExecutionContext>();
services.AddScoped<IAuthorizationHandler, EnvironmentAccessAuthorizationHandler>();
services.AddScoped<IAuthorizationHandler, OperationalContextAuthorizationHandler>();
```

Nada que existia foi removido ou substituído.

---

## 2. Pontos que Ainda Usam o Modelo Antigo

### Handlers operacionais sem IOperationalExecutionContext

Os seguintes módulos ainda usam `environment: string` em suas entidades e handlers:

| Módulo | Entidade | Campo | Estado |
|---|---|---|---|
| ChangeGovernance | `Release` | `Environment: string` | Fase 3 |
| ChangeGovernance | `PromotionRequest` | sem TenantId/EnvironmentId | Fase 3 |
| OperationalIntelligence | `IncidentRecord` | `Environment: string` | Fase 3 |
| Catalog | `ApiAsset`, `ServiceAsset` | sem TenantId | Fase 3 |
| AIKnowledge | Handlers existentes | sem `IAIContextBuilder` | Fase 3 |

Esses módulos continuam funcionando como antes — não foram quebrados, apenas ainda não foram migrados.

### Frontend

O frontend ainda usa o modelo hardcoded de 3 ambientes no `WorkspaceSwitcher`. A migração para consumir `GET /api/v1/identity/context/runtime` e `GET /api/v1/identity/environments` ocorre na Fase 4 (frontend).

### EnvironmentContextAccessor vs Contexto de Background Jobs

Em background jobs que não passam pelo middleware HTTP, o `EnvironmentContextAccessor.IsResolved` será `false`. Para esses cenários, usar `IAIContextBuilder.BuildForAsync(tenantId, environmentId, module)` explicitamente.

---

## 3. Próximos Módulos a Migrar (Fase 3)

### Prioridade 1 — Mais impacto em segurança e governança

**ChangeGovernance**
- Adicionar `EnvironmentId?` nullable em `Release` e `PromotionRequest`
- Migrar handlers de promoção para usar `IOperationalExecutionContext`
- Validar ambiente de destino usando `IEnvironmentAccessValidator`

**OperationalIntelligence**
- Adicionar `EnvironmentId?` nullable em `IncidentRecord`
- Migrar handlers de criação de incidente para receber EnvironmentId via contexto
- Usar `IOperationalExecutionContext` nos handlers de IA de correlação

### Prioridade 2 — Catálogo e contratos

**Catalog**
- Adicionar `TenantId` explícito nas entidades core (`ApiAsset`, `ServiceAsset`)
- Endpoints do catálogo passarão a filtrar por TenantId do contexto

### Prioridade 3 — AIKnowledge handlers

- Integrar `IAIContextBuilder.BuildAsync()` nos handlers de AI
- Garantir que nenhum handler de IA execute sem contexto validado

---

## 4. Riscos e Dívida Técnica Remanescente

### R1 — EnvironmentResolutionMiddleware após UseAuthentication

**Situação:** O middleware precisa do usuário autenticado para validar o ambiente (via ITenantEnvironmentContextResolver que usa o tenant). No `Program.cs` atual, `UseMiddleware<EnvironmentResolutionMiddleware>()` está posicionado corretamente após `UseAuthentication` implícita no pipeline.

**Risco:** Se o pipeline mudar de ordem no futuro, o ambiente poderá ser resolvido sem tenant ativo.

**Mitigação:** O middleware já verifica `if (currentTenant.Id == Guid.Empty)` e não resolve ambiente. Log de debug registra o skip.

### R2 — IOperationalExecutionContext.TenantEnvironmentContext usa criticidade simplificada

**Situação:** A criticidade é inferida como Critical para Production/DR e Low para demais. Os valores reais estão nos novos campos da entidade `Environment` que ainda aguardam migration.

**Risco:** Código que depende do `TenantEnvironmentContext.Criticality` poderá receber valores imprecisos.

**Mitigação:** Nenhum código downstream usa Criticality nesta fase. A Fase 3 adicionará a migration e corrigirá o comportamento.

### R3 — `EnvironmentAccessValidator` só verifica `EnvironmentAccess` do banco

**Situação:** Usuários admin (sem `EnvironmentAccess` explícita) são negados pelo validator.

**Risco:** Handlers que usam `IEnvironmentAccessValidator` bloquearão admins sem acesso explícito.

**Mitigação:** O validator ainda não é usado obrigatoriamente em endpoints existentes (não há `EnvironmentAccessRequirement` aplicado em endpoints hoje). A Fase 3 deve resolver isso adicionando lógica de bypass para admins.

### R4 — `PromotionRiskContextBuilder` lança exceção em vez de Result

**Situação:** Se o source ou target environment não pertencerem ao tenant, uma `InvalidOperationException` é lançada.

**Risco:** Handler que não tratar a exceção retorna 500 ao invés de 400/403.

**Mitigação:** Na Fase 3, migrar para retornar `Result<PromotionRiskAnalysisContext>` (padrão do projeto).

---

## 5. Testes

| Módulo | Antes | Depois | Novos |
|---|---|---|---|
| IdentityAccess.Tests | 217 | 244 | +27 |
| AIKnowledge.Tests | 204 | 204 | 0 |

**Novos testes de contexto (IdentityAccess.Tests):**
- `EnvironmentContextAccessorTests` — 3 cenários
- `TenantEnvironmentContextResolverTests` — 6 cenários
- `EnvironmentProfileResolverTests` — 7 cenários
- `EnvironmentAccessValidatorTests` — 11 cenários

**Cenários cobertos:**
- ✅ Utilizador sem tenant válido
- ✅ Ambiente que não pertence ao tenant (cross-tenant isolation)
- ✅ Acesso negado ao ambiente (sem EnvironmentAccess)
- ✅ Contexto válido resolvido com sucesso
- ✅ Acesso expirado (EnvironmentAccess.IsActiveAt = false)
- ✅ Ambiente inativo
