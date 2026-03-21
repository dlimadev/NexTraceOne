# Fase 5 — Notas de Transição

## O que foi Implementado

### Novos arquivos criados

#### BuildingBlocks.Application

| Arquivo | Propósito |
|---|---|
| `Context/ContextPropagationHeaders.cs` | Constantes para headers de propagação de contexto |
| `Context/DistributedExecutionContext.cs` | Snapshot imutável de contexto para eventos/jobs |
| `Behaviors/ContextualLoggingBehavior.cs` | Pipeline behavior de enriquecimento de logs/traces |
| `Integrations/IntegrationBindingDescriptor.cs` | Descritor de binding de integração por tenant/ambiente |
| `Integrations/IIntegrationContextResolver.cs` | Contrato de resolução de binding |
| `Integrations/NullIntegrationContextResolver.cs` | Stub padrão (internal) |
| `Correlation/IDistributedSignalCorrelationService.cs` | Contrato de correlação de sinais distribuídos |
| `Correlation/IPromotionRiskSignalProvider.cs` | Contrato de avaliação de risco de promoção |
| `Correlation/NullDistributedSignalCorrelationService.cs` | Stub padrão (internal) |
| `Correlation/NullPromotionRiskSignalProvider.cs` | Stub padrão (internal) |

#### BuildingBlocks.Observability

| Arquivo | Propósito |
|---|---|
| `Telemetry/TelemetryContextEnricher.cs` | Enriquecimento de Activities com contexto NexTraceOne |

### Arquivos modificados

| Arquivo | Mudança |
|---|---|
| `BuildingBlocks.Core/Events/IntegrationEventBase.cs` | Adicionados `Guid? TenantId` e `Guid? EnvironmentId` |
| `BuildingBlocks.Application/DependencyInjection.cs` | Adicionado `ContextualLoggingBehavior` + 3 novos serviços |
| `BuildingBlocks.Observability/Tracing/NexTraceActivitySources.cs` | Adicionado `Integrations` source |
| `BuildingBlocks.Observability/DependencyInjection.cs` | Registrado `Integrations.Name` no `AddSource(...)` |
| `ApiHost/WebApplicationBuilderExtensions.cs` | Adicionados `X-Environment-Id` e `X-Correlation-Id` ao CORS |

### Módulos atualizados (correção de compatibilidade)

| Arquivo | Mudança | Motivo |
|---|---|---|
| `IdentityAccess.Contracts/IntegrationEvents/UserCreatedIntegrationEvent.cs` | `Guid TenantId` → `Guid? TenantId` | Conflito de tipo com base `Guid?` |
| `IdentityAccess.Contracts/IntegrationEvents/UserRoleChangedIntegrationEvent.cs` | `Guid TenantId` → `Guid? TenantId` | Conflito de tipo com base `Guid?` |

---

## Impacto em Módulos Existentes

### IdentityAccess

**Módulo afetado:** `NexTraceOne.IdentityAccess.Contracts`

Os dois eventos de integração tiveram `TenantId` alterado de `Guid` para `Guid?`. Esta mudança é retrocompatível para chamadores:

```csharp
// Chamadores que passavam Guid continuam funcionando (conversão implícita)
var evt = new UserCreatedIntegrationEvent(userId, email, tenant.Id);

// O resultado é Guid? com valor — funciona igual
evt.TenantId // Guid? com valor = tenant.Id
```

### OperationalIntelligence

Sem alterações diretas. O novo `IDistributedSignalCorrelationService` e `IPromotionRiskSignalProvider` são os contratos que este módulo implementará no futuro.

### Todos os módulos (pipeline behavior)

O novo `ContextualLoggingBehavior` é adicionado automaticamente ao pipeline MediatR de todos os módulos que usam `AddBuildingBlocksApplication()`. 

**Pré-requisito:** `ICurrentTenant` e `ICurrentEnvironment` devem estar registrados no DI do módulo. Ambos já são registrados pelos módulos via `AddBuildingBlocksApplication()` e implementações concretas nos módulos.

---

## O que Não Foi Alterado

- Nenhum módulo operacional foi modificado além dos contratos de IdentityAccess
- Nenhuma migration de banco de dados foi criada — Integration Bindings são Infrastructure, não Core
- Nenhum novo pacote NuGet foi adicionado
- A API pública dos módulos existentes não foi alterada

---

## Próximos Passos (Fase 6+)

### Alta prioridade

1. **Integration Bindings — Infrastructure**
   - Criar tabela `integration_bindings` por tenant/ambiente
   - Implementar `DbIntegrationContextResolver` no módulo de Integrações
   - CRUD de bindings via API

2. **Correlation Service — Implementações concretas**
   - `DbDistributedSignalCorrelationService` lendo incidents + releases correlacionados
   - Endpoint REST para avaliação de risco de promoção

3. **ContextualLoggingBehavior — Correlation ID**
   - Propagar `X-Correlation-Id` do header HTTP para o scope do logger
   - Gerar novo correlationId se não presente no header

### Média prioridade

4. **HttpClient propagation**
   - `DelegatingHandler` que lê `ContextPropagationHeaders.PropagatedHeaders` e os adiciona automaticamente em chamadas downstream

5. **OutboxEventBus — contexto nos events**
   - Enriquecer eventos publicados pelo OutboxEventBus com TenantId/EnvironmentId do contexto atual

---

## Testes Criados

| Arquivo de Teste | Testes |
|---|---|
| `Context/DistributedExecutionContextTests.cs` | 7 testes — constructor, IsOperational, From() |
| `Context/ContextPropagationHeadersTests.cs` | 4 testes — valores dos headers, PropagatedHeaders |
| `Correlation/PromotionRiskAssessmentTests.cs` | 5 testes — ShouldBlock, NullProvider |
| `Integrations/IntegrationBindingDescriptorTests.cs` | 4 testes — criação, NullResolver |

**Total de testes da suite BuildingBlocks.Application após Fase 5:** 38 (passando)
