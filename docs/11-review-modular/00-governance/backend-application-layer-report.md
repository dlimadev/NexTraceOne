# Relatório da Camada de Aplicação — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria CQRS, vertical slices, MediatR behaviors  
> **Escopo:** Camada de aplicação de todos os módulos

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Handlers CQRS totais | 369+ |
| Features (vertical slices) | 446 |
| MediatR behaviors | 5 |
| Módulos com CQRS | 9/9 |
| Padrão de organização | Vertical feature slices |

---

## 2. Padrão CQRS

### 2.1 Abstracções Base

O NexTraceOne usa MediatR como mediador CQRS com as seguintes abstracções definidas no building block **Application**:

| Abstracção | Tipo | Finalidade |
|------------|------|-----------|
| `ICommand` | Command | Operação de escrita sem retorno |
| `ICommand<TResult>` | Command | Operação de escrita com retorno |
| `IQuery<TResult>` | Query | Operação de leitura com retorno |

### 2.2 Convenção de Organização

Cada feature segue o padrão de vertical slice:

```
módulo/
  Application/
    Features/
      NomeDaFeature/
        NomeDaFeatureCommand.cs       (ou Query)
        NomeDaFeatureHandler.cs
        NomeDaFeatureValidator.cs      (FluentValidation)
        NomeDaFeatureResponse.cs       (DTO de resposta)
```

Esta estrutura garante:
- Coesão por funcionalidade
- Isolamento de mudanças
- Facilidade de navegação
- Rastreabilidade de ponta a ponta

---

## 3. MediatR Behaviors (Pipeline)

### 3.1 Pipeline de Behaviors

Os behaviors são executados como middlewares no pipeline do MediatR, envolvendo cada handler:

| Ordem | Behavior | Ficheiro (est.) | Finalidade |
|-------|----------|-----------------|-----------|
| 1 | ValidationBehavior | `Application/Behaviors/ValidationBehavior.cs` | Executa validadores FluentValidation antes do handler. Rejeita commands/queries inválidos |
| 2 | TransactionBehavior | `Application/Behaviors/TransactionBehavior.cs` | Envolve o handler em transacção. Commit automático em sucesso, rollback em falha |
| 3 | TenantIsolationBehavior | `Application/Behaviors/TenantIsolationBehavior.cs` | Aplica filtro de tenant (RLS) ao contexto da operação |
| 4 | LoggingBehavior | `Application/Behaviors/LoggingBehavior.cs` | Logging estruturado de entrada (request) e saída (response/exception) |
| 5 | PerformanceBehavior | `Application/Behaviors/PerformanceBehavior.cs` | Monitoriza latência. Emite warning para operações lentas |

### 3.2 Análise dos Behaviors

| Behavior | Cobertura | Classificação |
|----------|-----------|---------------|
| ValidationBehavior | Todos os handlers com validador | ✅ COERENTE |
| TransactionBehavior | Todos os commands | ✅ COERENTE |
| TenantIsolationBehavior | Operações multi-tenant | ✅ COERENTE |
| LoggingBehavior | Todos os handlers | ✅ COERENTE |
| PerformanceBehavior | Todos os handlers | ✅ COERENTE |

### 3.3 Gaps nos Behaviors

| Gap | Impacto | Recomendação |
|-----|---------|-------------|
| Sem behavior de caching | Queries repetidas sem cache | Considerar CachingBehavior para queries frequentes |
| Sem behavior de idempotência | Retries podem causar duplicação | Considerar IdempotencyBehavior para commands críticos |
| Sem behavior de rate limiting interno | Abuso interno não controlado | Rate limiting existe a nível HTTP mas não no pipeline CQRS |

---

## 4. Distribuição de Handlers por Módulo

### 4.1 Contagem de Features (Vertical Slices)

| Módulo | Features | Commands (est.) | Queries (est.) | Ratio C:Q |
|--------|----------|----------------|----------------|-----------|
| catalog | 83 | ~45 | ~38 | 1.2:1 |
| governance | 73 | ~35 | ~38 | 0.9:1 |
| identityaccess | 71 | ~40 | ~31 | 1.3:1 |
| aiknowledge | 70 | ~35 | ~35 | 1.0:1 |
| changegovernance | 57 | ~30 | ~27 | 1.1:1 |
| operationalintelligence | 55 | ~28 | ~27 | 1.0:1 |
| auditcompliance | 15 | ~5 | ~10 | 0.5:1 |
| notifications | 15 | ~8 | ~7 | 1.1:1 |
| configuration | 7 | ~3 | ~4 | 0.8:1 |
| **Total** | **446** | **~229** | **~217** | **1.1:1** |

### 4.2 Análise da Distribuição

- **Ratio equilibrado:** A proporção global commands/queries (~1.1:1) indica equilíbrio entre operações de escrita e leitura
- **auditcompliance** tem ratio 0.5:1 — esperado, pois é predominantemente de leitura (trail, reports)
- **identityaccess** tem ratio 1.3:1 — indica muitas operações de gestão (users, roles, sessions)

---

## 5. Validação

### 5.1 Padrão FluentValidation

Cada command/query que requer validação tem um validador correspondente:

```csharp
public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerId).NotEmpty();
        // ...
    }
}
```

### 5.2 Cobertura de Validação

| Módulo | Features | Validadores (est.) | Cobertura (est.) |
|--------|----------|-------------------|-----------------|
| catalog | 83 | ~50 | ~60% |
| governance | 73 | ~40 | ~55% |
| identityaccess | 71 | ~45 | ~63% |
| aiknowledge | 70 | ~40 | ~57% |
| changegovernance | 57 | ~35 | ~61% |
| operationalintelligence | 55 | ~30 | ~55% |
| auditcompliance | 15 | ~8 | ~53% |
| notifications | 15 | ~8 | ~53% |
| configuration | 7 | ~5 | ~71% |

### 5.3 Observações sobre Validação

- A cobertura de validadores varia entre 53-71% — nem todas as features necessitam de validação explícita (queries simples)
- Queries de leitura simples tipicamente não têm validador, o que é aceitável
- Commands de escrita devem ter validador em todos os casos — verificação recomendada

---

## 6. DTOs e Contratos de Aplicação

### 6.1 Padrão de DTOs

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Request | Command/Query é o request | `CreateServiceCommand` |
| Response | Classe dedicada com sufixo Response | `CreateServiceResponse` |
| List Response | Paginado com metadata | `PagedResult<ServiceResponse>` |
| Error | Result pattern | `Result<T>` com erros tipados |

### 6.2 Análise de DTOs

| Aspecto | Estado | Observação |
|---------|--------|-----------|
| Separação domain/DTO | ✅ COERENTE | Entidades de domínio nunca expostas directamente |
| Mapping | ✅ COERENTE | Conversão explícita nos handlers |
| Paginação | ✅ COERENTE | PagedResult\<T\> padronizado |
| Result pattern | ✅ COERENTE | Erros controlados sem excepções |

---

## 7. Análise por Módulo

### 7.1 catalog (83 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | Vertical slices bem definidas |
| CQRS | ✅ COERENTE | Separação clara commands/queries |
| Validação | ✅ COERENTE | Validadores presentes nos commands principais |
| DTOs | ✅ COERENTE | Response objects dedicados |
| Complexidade | MÉDIA-ALTA | 82 entidades, 3 DbContexts |

### 7.2 governance (73 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | 18 endpoint modules bem segmentados |
| CQRS | ✅ COERENTE | Handlers por funcionalidade |
| Validação | ✅ COERENTE | Validação presente |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | ALTA | 18 endpoints, muitos subdomínios |

### 7.3 identityaccess (71 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | 10 sub-módulos bem definidos |
| CQRS | ✅ COERENTE | Handlers por funcionalidade |
| Validação | ✅ COERENTE | Validação robusta em auth/user |
| DTOs | ✅ COERENTE | Claims e tokens bem estruturados |
| Complexidade | ALTA | Segurança requer validação extensiva |

### 7.4 aiknowledge (70 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ⚠️ PARCIAL | AiGovernanceEndpointModule demasiado grande |
| CQRS | ✅ COERENTE | Handlers por funcionalidade |
| Validação | ✅ COERENTE | Validação de modelos e policies |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | ALTA | 3 DbContexts, governança + orquestração |

### 7.5 changegovernance (57 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | 4 bounded contexts claros |
| CQRS | ✅ COERENTE | Handlers por funcionalidade |
| Validação | ✅ COERENTE | Validação de rulesets e workflows |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | MÉDIA-ALTA | 4 DbContexts |

### 7.6 operationalintelligence (55 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | 7 endpoint modules bem separados |
| CQRS | ✅ COERENTE | Handlers por funcionalidade |
| Validação | ✅ COERENTE | Validação presente |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | ALTA | 5 DbContexts, muitos subdomínios |

### 7.7 auditcompliance (15 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | Módulo compacto e focado |
| CQRS | ✅ COERENTE | Predominância de queries |
| Validação | ✅ COERENTE | Validação simples |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | BAIXA | 1 DbContext, funcionalidade bem delimitada |

### 7.8 notifications (15 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ⚠️ PARCIAL | Estado PARCIAL — funcionalidade incompleta |
| CQRS | ✅ COERENTE | Handlers presentes |
| Validação | ⚠️ PARCIAL | Pode não cobrir todos os cases |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | BAIXA | 1 DbContext, sem migrações |

### 7.9 configuration (7 features)

| Aspecto | Classificação | Detalhe |
|---------|---------------|---------|
| Organização | ✅ COERENTE | Módulo mínimo e focado |
| CQRS | ✅ COERENTE | Poucas features, bem definidas |
| Validação | ✅ COERENTE | Validação presente |
| DTOs | ✅ COERENTE | Separação adequada |
| Complexidade | BAIXA | 1 DbContext, 6 entidades |

---

## 8. Padrões Transversais

### 8.1 Guard Clauses

Todos os módulos seguem a convenção de guard clauses no início dos handlers:

```csharp
public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);
    // guard clauses...
}
```

### 8.2 CancellationToken

O padrão `CancellationToken` é propagado em todas as operações assíncronas — obrigatório por convenção da solução.

### 8.3 Result Pattern

Erros controlados usam `Result<T>` em vez de excepções, mantendo o fluxo previsível e testável.

---

## 9. Recomendações

### Prioridade ALTA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 1 | Garantir validador em todos os commands de escrita | Validação inconsistente pode permitir dados inválidos |
| 2 | Decompor features grandes no módulo aiknowledge | AiGovernanceEndpointModule tem 665 linhas |
| 3 | Completar features do módulo notifications | Estado PARCIAL indica funcionalidade incompleta |

### Prioridade MÉDIA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 4 | Considerar CachingBehavior para queries frequentes | Melhora performance sem alterar handlers |
| 5 | Considerar IdempotencyBehavior para commands críticos | Protege contra duplicação em retries |
| 6 | Documentar convenções de naming para features | Facilita onboarding |

### Prioridade BAIXA

| # | Recomendação | Justificação |
|---|-------------|-------------|
| 7 | Adicionar métricas de handler por módulo | Visibilidade sobre performance por domínio |
| 8 | Padronizar mensagens de erro em todos os validadores | Consistência na UX de erros |
