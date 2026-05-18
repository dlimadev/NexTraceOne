# Cenários de Teste — Building Blocks (Infraestrutura Transversal)

**Projeto:** NexTraceOne  
**Camada:** Building Blocks (Cross-Cutting Infrastructure)  
**Versão do Documento:** 1.0  
**Data:** 2026-05-18  
**Responsável:** QA / Arquitetura  

---

## Sumário

| Faixa | Área | Quantidade |
|-------|------|-----------|
| TC-BB-001 a TC-BB-010 | `Result<T>` e `Error` — Semântica e Mapeamento HTTP | 10 |
| TC-BB-011 a TC-BB-018 | Pipeline MediatR — LoggingBehavior e PerformanceBehavior | 8 |
| TC-BB-019 a TC-BB-023 | Pipeline MediatR — TenantIsolationBehavior | 5 |
| TC-BB-024 a TC-BB-028 | Pipeline MediatR — ValidationBehavior | 5 |
| TC-BB-029 a TC-BB-033 | Pipeline MediatR — TransactionBehavior | 5 |
| TC-BB-034 a TC-BB-039 | NexTraceDbContextBase — Domain Events e Outbox | 6 |
| TC-BB-040 a TC-BB-044 | TenantRlsInterceptor — Row-Level Security | 5 |
| TC-BB-045 a TC-BB-049 | AuditInterceptor — Campos de Auditoria | 5 |
| TC-BB-050 a TC-BB-054 | Soft-Delete — Filtro Global de IsDeleted | 5 |
| TC-BB-055 a TC-BB-059 | EncryptedField e EncryptionInterceptor | 5 |
| TC-BB-060 a TC-BB-065 | ModuleOutboxProcessorJob — Retentativas, DLQ e Advisory Lock | 6 |
| TC-BB-066 a TC-BB-070 | AggregateRoot e Domain Events | 5 |
| TC-BB-071 a TC-BB-075 | TypedIdBase e Guards | 5 |
| TC-BB-076 a TC-BB-082 | Security — JWT, CookieSession, TenantResolutionMiddleware | 7 |
| TC-BB-083 a TC-BB-086 | Observability — OpenTelemetry, Health Checks, IngestionMetrics | 4 |

**Total: 86 cenários**

---

## `Result<T>` e `Error` — Semântica e Mapeamento HTTP

### TC-BB-001 — Criação de Result de sucesso com valor

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `Result<T>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `Result<T>` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. Chamar `Result<string>.Success("valor-de-teste")`.
2. Verificar propriedades `IsSuccess`, `IsFailure` e `Value`.
3. Verificar que `Error` lança `InvalidOperationException` quando `IsSuccess == true`.

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.IsFailure == false`
- `result.Value == "valor-de-teste"`
- `result.Error` lança `InvalidOperationException` com mensagem indicando que o resultado é sucesso.

**Critério de Aceite:** Padrão Result protege contra acesso ao erro em caso de sucesso.

---

### TC-BB-002 — Criação de Result de falha via conversão implícita de Error

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `Result<T>` / `Error` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `Result<T>` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. Criar `Error.NotFound("User.NotFound", "Usuário {0} não encontrado.", userId)`.
2. Retornar o erro implicitamente como `Result<SomeResponse>`.
3. Verificar propriedades `IsFailure`, `Error.Code`, `Error.Type` e `Error.FormattedMessage`.

**Resultado Esperado:**
- `result.IsFailure == true`
- `result.Error.Type == ErrorType.NotFound`
- `result.Error.Code == "User.NotFound"`
- `result.Error.FormattedMessage` contém o valor de `userId` interpolado.
- `result.Value` lança `InvalidOperationException`.

**Critério de Aceite:** Conversão implícita de `Error` para `Result<T>` funciona sem boxing explícito.

---

### TC-BB-003 — Mapeamento de ErrorType.NotFound para HTTP 404

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.IsFailure == true` com `result.Error.Type == ErrorType.NotFound`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)` em endpoint Minimal API.
2. Verificar que o status HTTP retornado é 404.

**Resultado Esperado:**
- Resposta HTTP com status code 404.
- Corpo contém `code` e `message` do erro.

**Critério de Aceite:** `NotFound → HTTP 404` mapeado corretamente.

---

### TC-BB-004 — Mapeamento de ErrorType.Validation para HTTP 422

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Validation`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.

**Resultado Esperado:**
- Status HTTP 422.
- Corpo contém erros de validação.

**Critério de Aceite:** `Validation → HTTP 422` mapeado corretamente.

---

### TC-BB-005 — Mapeamento de ErrorType.Business para HTTP 422

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Business`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.

**Resultado Esperado:**
- Status HTTP 422.
- Corpo diferencia regra de negócio de erro de validação de campo pelo código do erro.

**Critério de Aceite:** `Business → HTTP 422` mapeado corretamente.

---

### TC-BB-006 — Mapeamento de ErrorType.Conflict para HTTP 409

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Conflict`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.

**Resultado Esperado:**
- Status HTTP 409.

**Critério de Aceite:** `Conflict → HTTP 409` mapeado corretamente.

---

### TC-BB-007 — Mapeamento de ErrorType.Unauthorized para HTTP 401

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Unauthorized`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.

**Resultado Esperado:**
- Status HTTP 401.

**Critério de Aceite:** `Unauthorized → HTTP 401` mapeado corretamente.

---

### TC-BB-008 — Mapeamento de ErrorType.Forbidden para HTTP 403

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Forbidden`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.

**Resultado Esperado:**
- Status HTTP 403.

**Critério de Aceite:** `Forbidden → HTTP 403` mapeado corretamente.

---

### TC-BB-009 — Mapeamento de ErrorType.Security para HTTP 500 sem detalhes

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `result.ToHttpResult()` |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Classe** | `ResultExtensions` |

**Pré-condições:**
- `result.Error.Type == ErrorType.Security`.

**Passos:**
1. Chamar `result.ToHttpResult(localizer)`.
2. Verificar que o corpo da resposta NÃO contém o código ou mensagem interna do erro.

**Resultado Esperado:**
- Status HTTP 500.
- Corpo da resposta NÃO expõe `result.Error.Code` nem `result.Error.Message`.
- Resposta genérica para o cliente.

**Critério de Aceite:** `Security → HTTP 500` sem vazamento de detalhes internos. Erros de segurança são mascarados.

---

### TC-BB-010 — Projeção de Result com Map<TOut>

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `Result<T>.Map<TOut>()` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `Result<T>` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. Criar `Result<int>.Success(42)`.
2. Chamar `.Map(v => v.ToString())`.
3. Verificar que resultado é `Result<string>` com valor `"42"`.
4. Criar `Result<int>` de falha e chamar `.Map(v => v.ToString())`.
5. Verificar que o erro original é propagado sem chamar o mapeador.

**Resultado Esperado:**
- Sucesso: `result.Value == "42"`.
- Falha: `result.Error` igual ao erro original. Mapeador não é invocado.

**Critério de Aceite:** `Map` funcional para sucesso e transparente para falha.

---

## Pipeline MediatR — LoggingBehavior e PerformanceBehavior

### TC-BB-011 — LoggingBehavior registra request e response com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `LoggingBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `LoggingBehavior` |

**Pré-condições:**
- Logger mockado injetado no behavior.

**Passos:**
1. Configurar um request dummy com handler bem-sucedido.
2. Executar o pipeline através do `LoggingBehavior`.
3. Verificar chamadas ao logger.

**Resultado Esperado:**
- Log de nível `Information` no início do request (nome do request).
- Log de nível `Information` ao final com indicação de sucesso.
- Handler é chamado normalmente e response retornado.

**Critério de Aceite:** Todas as requisições MediatR produzem logs estruturados de entrada e saída.

---

### TC-BB-012 — LoggingBehavior registra falha quando handler retorna Result de falha

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `LoggingBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `LoggingBehavior` |

**Pré-condições:**
- Handler configurado para retornar `Error.NotFound(...)`.

**Passos:**
1. Executar pipeline com handler que retorna Result de falha.
2. Verificar log gerado.

**Resultado Esperado:**
- Log de nível `Warning` ou `Error` indicando falha no processamento.
- `error.Code` e `error.Type` presentes no log estruturado.
- Pipeline não lança exceção.

**Critério de Aceite:** Falhas de Result são registradas em log sem propagar exceção.

---

### TC-BB-013 — PerformanceBehavior não emite warning abaixo de 500ms

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `PerformanceBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `PerformanceBehavior` |

**Pré-condições:**
- Handler configurado para executar em menos de 500ms (simulado com Task.CompletedTask).

**Passos:**
1. Executar pipeline com handler instantâneo.
2. Verificar que `ILogger.LogWarning` NÃO é chamado.

**Resultado Esperado:**
- Nenhum warning de performance emitido.
- Response retornado normalmente.

**Critério de Aceite:** Requests dentro do threshold não poluem os logs com warnings.

---

### TC-BB-014 — PerformanceBehavior emite Warning entre 500ms e 2000ms

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `PerformanceBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `PerformanceBehavior` |

**Pré-condições:**
- Handler mockado para simular execução de 700ms (usando `Task.Delay` ou Stopwatch substituído).

**Passos:**
1. Configurar handler para demorar entre 500ms e 2000ms.
2. Executar pipeline.
3. Verificar que `ILogger.LogWarning` é chamado com `ElapsedMilliseconds` e nome do request.

**Resultado Esperado:**
- `LogWarning` chamado exatamente uma vez.
- Mensagem contém nome do tipo do request e tempo em ms.
- `LogError` NÃO é chamado.

**Critério de Aceite:** Warning de performance emitido na faixa 500ms–2000ms.

---

### TC-BB-015 — PerformanceBehavior emite Error acima de 2000ms

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `PerformanceBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `PerformanceBehavior` |

**Pré-condições:**
- Handler mockado para simular execução acima de 2000ms.

**Passos:**
1. Configurar handler para demorar mais de 2000ms.
2. Executar pipeline.
3. Verificar que `ILogger.LogError` é chamado (não `LogWarning`).

**Resultado Esperado:**
- `LogError` chamado com `ElapsedMilliseconds` acima de 2000.
- `LogWarning` NÃO é chamado separadamente.

**Critério de Aceite:** Requests críticos acima de 2000ms geram log de Error.

---

### TC-BB-016 — Ordem dos behaviors no pipeline é respeitada

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | Pipeline MediatR |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `DependencyInjection` / MediatR |

**Pré-condições:**
- Pipeline configurado com todos os behaviors: Logging, Performance, TenantIsolation, Validation, Transaction.

**Passos:**
1. Enviar request com dados inválidos (deve ser interceptado pelo `ValidationBehavior`).
2. Verificar que Logging foi executado antes da validação.
3. Verificar que handler NÃO foi chamado.
4. Enviar request sem tenant context (deve ser interceptado pelo `TenantIsolationBehavior`).
5. Verificar que `ValidationBehavior` NÃO é chamado antes de `TenantIsolationBehavior`.

**Resultado Esperado:**
- Ordem obrigatória: Logging → Performance → TenantIsolation → Validation → Transaction → Handler.
- Cada barrier bloqueia o subsequente quando falha.

**Critério de Aceite:** Ordem dos behaviors MediatR é determinística e correta.

---

### TC-BB-017 — ContextualLoggingBehavior enriquece logs com contexto de correlação

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ContextualLoggingBehavior` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ContextualLoggingBehavior` |

**Pré-condições:**
- Request com `CorrelationId` ou `TraceId` no contexto distribuído.

**Passos:**
1. Executar pipeline com contexto de correlação configurado.
2. Verificar que logs estruturados contêm `CorrelationId` e `TenantId`.

**Resultado Esperado:**
- Logs contêm campos de correlação para rastreamento cross-service.

**Critério de Aceite:** Logs enriquecidos com contexto de correlação e tenant.

---

### TC-BB-018 — Pipeline propaga CancellationToken ao handler

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | Pipeline MediatR |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | Todos os behaviors |

**Pré-condições:**
- `CancellationTokenSource` configurado para cancelamento antes da execução do handler.

**Passos:**
1. Cancelar o token durante execução do `ValidationBehavior`.
2. Verificar que `OperationCanceledException` é propagada corretamente sem ser engolida.

**Resultado Esperado:**
- `OperationCanceledException` propagada através do pipeline.
- `ValidationBehavior` trata o caso explicitamente com `throw` após `OperationCanceledException`.

**Critério de Aceite:** Cancelamento cooperativo funciona em todo o pipeline.

---

## Pipeline MediatR — TenantIsolationBehavior

### TC-BB-019 — TenantIsolationBehavior bloqueia request sem tenant (Guid.Empty)

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TenantIsolationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TenantIsolationBehavior` |

**Pré-condições:**
- `ICurrentTenant.Id == Guid.Empty`.
- Request não implementa `IPublicRequest`.

**Passos:**
1. Executar qualquer ICommand sem tenant context.
2. Verificar que `next()` NÃO é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Security`.
- `result.Error.Code == "Tenant.Isolation.NoTenant"`.
- Handler não executado.

**Critério de Aceite:** Sem tenant context, zero acesso a dados.

---

### TC-BB-020 — TenantIsolationBehavior bloqueia request para tenant inativo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TenantIsolationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TenantIsolationBehavior` |

**Pré-condições:**
- `ICurrentTenant.Id != Guid.Empty` mas `ICurrentTenant.IsActive == false`.

**Passos:**
1. Executar ICommand com tenant inativo.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Forbidden`.
- `result.Error.Code == "Tenant.Isolation.Inactive"`.
- Mensagem contém nome do tenant.

**Critério de Aceite:** Tenant inativo bloqueia todas as operações não-públicas.

---

### TC-BB-021 — TenantIsolationBehavior permite IPublicRequest sem tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TenantIsolationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TenantIsolationBehavior` |

**Pré-condições:**
- `ICurrentTenant.Id == Guid.Empty`.
- Request implementa `IPublicRequest`.

**Passos:**
1. Executar request que implementa `IPublicRequest`.

**Resultado Esperado:**
- `next()` é chamado diretamente sem verificar tenant.
- Handler executa normalmente.

**Critério de Aceite:** Endpoints públicos (ex.: `LocalLogin`) acessíveis sem tenant pré-estabelecido.

---

### TC-BB-022 — TenantIsolationBehavior permite request com tenant válido e ativo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TenantIsolationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TenantIsolationBehavior` |

**Pré-condições:**
- `ICurrentTenant.Id` é Guid válido e `ICurrentTenant.IsActive == true`.

**Passos:**
1. Executar ICommand com tenant válido e ativo.

**Resultado Esperado:**
- Pipeline prossegue normalmente.
- Handler é chamado.

**Critério de Aceite:** Tenant válido e ativo não encontra barreiras no isolation behavior.

---

### TC-BB-023 — ResultResponseFactory.CreateFailureResponse cria TResponse corretamente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ResultResponseFactory` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ResultResponseFactory` |

**Pré-condições:**
- `TResponse` é `Result<SomeDto>`.

**Passos:**
1. Chamar `ResultResponseFactory.CreateFailureResponse<Result<SomeDto>>(Error.Forbidden(...))`.
2. Verificar que o objeto retornado é do tipo correto.
3. Verificar que `IsSuccessfulResult(response) == false`.

**Resultado Esperado:**
- `CreateFailureResponse` retorna `Result<SomeDto>` com o erro encapsulado.
- `IsSuccessfulResult` retorna `false`.

**Critério de Aceite:** Factory funciona corretamente para todos os tipos genéricos de Result.

---

## Pipeline MediatR — ValidationBehavior

### TC-BB-024 — ValidationBehavior curto-circuita quando há erros de validação

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ValidationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ValidationBehavior` |

**Pré-condições:**
- Pelo menos um `IValidator<TRequest>` registrado.
- Request com campo inválido.

**Passos:**
1. Enviar request com campo obrigatório vazio.
2. Verificar que `next()` NÃO é chamado.

**Resultado Esperado:**
- `result.IsFailure == true`.
- `result.Error.Type == ErrorType.Validation`.
- `result.Error.Code == "Validation.Failed"`.
- Handler nunca executado.

**Critério de Aceite:** Validação falha antes de atingir o handler de negócio.

---

### TC-BB-025 — ValidationBehavior agrega múltiplos erros de validação

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ValidationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ValidationBehavior` |

**Pré-condições:**
- Request com 3 campos inválidos distintos.
- Validator com regras para cada campo.

**Passos:**
1. Enviar request com 3 campos inválidos.
2. Verificar que todos os erros são retornados em uma única resposta.

**Resultado Esperado:**
- `result.Error.FormattedMessage` contém os 3 erros concatenados com `;`.
- Erros de campos distintos não são descartados após o primeiro.

**Critério de Aceite:** Agregação de erros de validação funciona corretamente.

---

### TC-BB-026 — ValidationBehavior passa adiante quando não há validators

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ValidationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ValidationBehavior` |

**Pré-condições:**
- Nenhum `IValidator<TRequest>` registrado para o tipo do request.

**Passos:**
1. Enviar request sem validator associado.

**Resultado Esperado:**
- `next()` é chamado imediatamente.
- Handler executa normalmente.

**Critério de Aceite:** Ausência de validator não bloqueia a execução.

---

### TC-BB-027 — ValidationBehavior executa múltiplos validators em paralelo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ValidationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ValidationBehavior` |

**Pré-condições:**
- 2 validators registrados para o mesmo TRequest.
- Validator 1 valida campo A, Validator 2 valida campo B.
- Ambos os campos são inválidos.

**Passos:**
1. Executar o behavior com os dois validators.
2. Verificar que `Task.WhenAll` é usado (execução paralela).
3. Verificar que erros de ambos os validators aparecem na resposta.

**Resultado Esperado:**
- Erros de ambos os validators presentes na resposta.
- Execução é paralela (não sequencial).

**Critério de Aceite:** Múltiplos validators executados em paralelo e erros agregados.

---

### TC-BB-028 — ValidationBehavior ignora erros nulos no resultado do validator

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `ValidationBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `ValidationBehavior` |

**Pré-condições:**
- Validator retorna um `ValidationResult` com pelo menos uma `ValidationFailure` nula.

**Passos:**
1. Configurar validator que inclui erro nulo no resultado.
2. Executar o behavior.

**Resultado Esperado:**
- Erros nulos são filtrados (`.Where(error => error is not null)`).
- Behavior não lança `NullReferenceException`.
- Outros erros válidos são processados normalmente.

**Critério de Aceite:** Erros nulos de validators são descartados defensivamente.

---

## Pipeline MediatR — TransactionBehavior

### TC-BB-029 — TransactionBehavior chama CommitAsync após command bem-sucedido

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TransactionBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TransactionBehavior` |

**Pré-condições:**
- Request implementa `ICommand<TResponse>`.
- Handler retorna Result de sucesso.
- `IUnitOfWork` mockado.

**Passos:**
1. Executar command com handler bem-sucedido.
2. Verificar que `IUnitOfWork.CommitAsync` é chamado uma vez.

**Resultado Esperado:**
- `IUnitOfWork.CommitAsync` chamado exatamente uma vez.
- Response retornado ao caller.

**Critério de Aceite:** Commands bem-sucedidos fazem commit automático da transação.

---

### TC-BB-030 — TransactionBehavior NÃO chama CommitAsync quando command falha

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TransactionBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TransactionBehavior` |

**Pré-condições:**
- Request implementa `ICommand<TResponse>`.
- Handler retorna `Error.NotFound(...)`.

**Passos:**
1. Executar command com handler que retorna falha.
2. Verificar que `IUnitOfWork.CommitAsync` NÃO é chamado.

**Resultado Esperado:**
- `IUnitOfWork.CommitAsync` nunca chamado.
- Nenhuma mutação persistida.

**Critério de Aceite:** Falha no handler não persiste dados parciais.

---

### TC-BB-031 — TransactionBehavior NÃO chama CommitAsync para Queries

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TransactionBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TransactionBehavior` |

**Pré-condições:**
- Request implementa `IQuery<TResponse>` (não `ICommand`).

**Passos:**
1. Executar query com resultado bem-sucedido.
2. Verificar que `IUnitOfWork.CommitAsync` NÃO é chamado.

**Resultado Esperado:**
- `IUnitOfWork.CommitAsync` nunca chamado para queries.

**Critério de Aceite:** Queries são read-only e não disparam commit de transação.

---

### TC-BB-032 — TransactionBehavior detecta ICommand<TResponse> com interface genérica

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TransactionBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TransactionBehavior` |

**Pré-condições:**
- Request implementa `ICommand<SomeResponse>` (interface genérica, não `ICommand` simples).

**Passos:**
1. Executar o command.
2. Verificar que `IsCommandRequest()` retorna `true` via reflexão sobre interfaces genéricas.

**Resultado Esperado:**
- `IUnitOfWork.CommitAsync` é chamado para `ICommand<T>` assim como para `ICommand`.

**Critério de Aceite:** Detecção de command funciona para ambas as formas da interface.

---

### TC-BB-033 — TransactionBehavior propaga exceção sem engolir

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Application |
| **Feature** | `TransactionBehavior<TRequest, TResponse>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TransactionBehavior` |

**Pré-condições:**
- Handler lança `ConcurrencyException` durante execução.

**Passos:**
1. Executar command com handler que lança exceção.
2. Verificar que exceção é propagada ao chamador.
3. Verificar que `CommitAsync` NÃO é chamado.

**Resultado Esperado:**
- Exceção propagada.
- Nenhum dado persistido.

**Critério de Aceite:** Exceções não tratadas não são engolidas pelo TransactionBehavior.

---

## NexTraceDbContextBase — Domain Events e Outbox

### TC-BB-034 — Domain Events são convertidos em OutboxMessages no SaveChanges

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.WriteDomainEventsToOutbox` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Aggregate root com 2 domain events registrados via `RaiseDomainEvent`.
- DbContext in-memory configurado.

**Passos:**
1. Adicionar aggregate root ao DbContext com 2 domain events.
2. Chamar `SaveChangesAsync`.
3. Verificar que 2 `OutboxMessage` foram criados na tabela de outbox.
4. Verificar que `DomainEvents` do aggregate está vazio após `SaveChanges` (limpeza).

**Resultado Esperado:**
- 2 `OutboxMessage` persistidos.
- `OutboxMessage.EventType` contém o nome fully qualified do tipo do evento.
- `OutboxMessage.Payload` é JSON serializado do evento.
- `OutboxMessage.TenantId` igual ao tenant do contexto.
- `DomainEvents` limpos após commit.

**Critério de Aceite:** Domain Events são garantidamente gravados na mesma transação da mutação.

---

### TC-BB-035 — OutboxMessage.Create gera IdempotencyKey determinístico

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `OutboxMessage.Create` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `OutboxMessage` |

**Pré-condições:**
- Evento de domínio com payload idêntico.

**Passos:**
1. Criar dois `OutboxMessage` a partir do mesmo objeto de evento no mesmo timestamp.
2. Comparar `IdempotencyKey` dos dois.

**Resultado Esperado:**
- `IdempotencyKey` é idêntico para o mesmo evento (determinístico).
- Formato: `{EventType}:{ContentHash}:{Timestamp}`.
- `ContentHash` é SHA-256 dos primeiros 16 bytes do payload em hexadecimal.

**Critério de Aceite:** Chave de idempotência determinística evita processamento duplo do mesmo evento.

---

### TC-BB-036 — SaveChangesAsync lança ConcurrencyException em DbUpdateConcurrencyException

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.SaveChangesAsync` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- DbContext configurado com EF Core in-memory que lança `DbUpdateConcurrencyException`.

**Passos:**
1. Simular conflito de concorrência no `SaveChangesAsync`.
2. Verificar que `ConcurrencyException` (não `DbUpdateConcurrencyException`) é lançada.

**Resultado Esperado:**
- `ConcurrencyException` propagada com nome da entidade.
- `DbUpdateConcurrencyException` original é inner exception.

**Critério de Aceite:** `ConcurrencyException` é wrapper controlado da exceção EF Core.

---

### TC-BB-037 — OutboxEventBus publica evento via IIntegrationEventHandler registrado

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `OutboxEventBus` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `OutboxEventBus` |

**Pré-condições:**
- `IIntegrationEventHandler<MyEvent>` registrado no container DI.
- `OutboxMessage` com payload de `MyEvent` pronto para entrega.

**Passos:**
1. Desserializar `OutboxMessage.Payload` para o tipo `MyEvent`.
2. Resolver `IIntegrationEventHandler<MyEvent>` do container.
3. Chamar `Handle(event, cancellationToken)`.

**Resultado Esperado:**
- Handler `IIntegrationEventHandler<MyEvent>` é chamado com evento corretamente desserializado.

**Critério de Aceite:** Event bus entrega eventos ao handler correto por tipo.

---

### TC-BB-038 — Tabela de outbox usa nome prefixado por módulo (OutboxTableName)

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.OutboxTableName` |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- DbContext de módulo específico que sobrescreve `OutboxTableName`.

**Passos:**
1. Verificar a configuração do `ModelBuilder` para `OutboxMessage`.
2. Verificar que `builder.ToTable(OutboxTableName)` usa o nome prefixado.

**Resultado Esperado:**
- Tabela de outbox do módulo IAM: `iam_outbox_messages` (ou nome prefixado correspondente).
- Tabelas de outbox de módulos distintos não colidem.

**Critério de Aceite:** Prefixo de tabela de outbox evita colisões entre módulos no mesmo banco.

---

### TC-BB-039 — Domain Events de agregados não rastreados não são escritos no Outbox

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.WriteDomainEventsToOutbox` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Aggregate root criado fora do contexto do DbContext (não rastreado pelo EF).

**Passos:**
1. Criar aggregate root com domain events sem adicioná-lo ao DbContext.
2. Chamar `SaveChangesAsync` no DbContext.

**Resultado Esperado:**
- Nenhum `OutboxMessage` criado.
- Apenas agregados rastreados pelo `ChangeTracker` têm seus eventos coletados.

**Critério de Aceite:** Somente domain events de entidades rastreadas são escritos no Outbox.

---

## TenantRlsInterceptor — Row-Level Security

### TC-BB-040 — TenantRlsInterceptor executa set_config antes de cada query

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `TenantRlsInterceptor` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `TenantRlsInterceptor` |

**Pré-condições:**
- `ICurrentTenant.Id` é Guid válido.
- Conexão de banco de dados PostgreSQL disponível (ou mock de `DbCommand`).

**Passos:**
1. Configurar `TenantRlsInterceptor` com tenant ID `"abc-123"`.
2. Executar uma operação de leitura no DbContext.
3. Verificar que `set_config('app.current_tenant_id', @__tenantId, false)` foi executado antes da query principal.

**Resultado Esperado:**
- `set_config` executado em comando separado na mesma conexão.
- Parâmetro `@__tenantId` contém o tenant ID como string (não interpolação).

**Critério de Aceite:** RLS ativado por parâmetro SQL antes de cada operação (previne SQL injection).

---

### TC-BB-041 — TenantRlsInterceptor não executa set_config quando TenantId é Guid.Empty

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `TenantRlsInterceptor` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `TenantRlsInterceptor` |

**Pré-condições:**
- `ICurrentTenant.Id == Guid.Empty`.

**Passos:**
1. Chamar `ReaderExecutingAsync` do interceptor com tenant vazio.

**Resultado Esperado:**
- Nenhum comando `set_config` executado.
- Operação principal prossegue (útil para background jobs sem tenant context).

**Critério de Aceite:** Jobs de background sem tenant não tentam configurar RLS com ID vazio.

---

### TC-BB-042 — TenantRlsInterceptor aplica RLS em operações NonQuery (INSERT/UPDATE/DELETE)

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `TenantRlsInterceptor` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `TenantRlsInterceptor` |

**Pré-condições:**
- `ICurrentTenant.Id` válido.
- Operação NonQuery (INSERT ou UPDATE) sendo executada.

**Passos:**
1. Executar operação de inserção ou atualização.
2. Verificar que `NonQueryExecutingAsync` chama `ApplyTenantContextAsync`.

**Resultado Esperado:**
- `set_config` executado antes de INSERT/UPDATE/DELETE.
- RLS aplicada para escrita, não apenas leitura.

**Critério de Aceite:** RLS ativa para todas as operações DML, não apenas SELECT.

---

### TC-BB-043 — TenantRlsInterceptor reutiliza conexão e transação do comando principal

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `TenantRlsInterceptor.CreateTenantCommand` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TenantRlsInterceptor` |

**Pré-condições:**
- Comando principal está em transação explícita.

**Passos:**
1. Inspecionar `CreateTenantCommand` para verificar que `configCmd.Transaction = command.Transaction`.

**Resultado Esperado:**
- `configCmd.Connection` é a mesma conexão do comando principal.
- `configCmd.Transaction` é a mesma transação do comando principal.

**Critério de Aceite:** `set_config` executado no mesmo contexto transacional da operação principal.

---

### TC-BB-044 — TenantRlsInterceptor engole OperationCanceledException internamente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `TenantRlsInterceptor` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TenantRlsInterceptor` |

**Pré-condições:**
- `CancellationToken` cancelado durante execução do `set_config`.

**Passos:**
1. Simular cancelamento durante `configCmd.ExecuteNonQueryAsync`.
2. Verificar que `OperationCanceledException` do `set_config` NÃO é propagada.
3. Verificar que a operação principal pode observar cancelamento pelo seu próprio token.

**Resultado Esperado:**
- Interceptor engole `OperationCanceledException` do comando auxiliar.
- Não mascara o cancelamento da operação principal.

**Critério de Aceite:** Cancelamento no `set_config` não corrompe a pilha de execução do EF Core.

---

## AuditInterceptor — Campos de Auditoria

### TC-BB-045 — AuditInterceptor preenche CreatedAt/By e UpdatedAt/By no insert

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `AuditInterceptor.SavingChangesAsync` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `AuditInterceptor` |

**Pré-condições:**
- Entidade herda de `AuditableEntity<T>`.
- Estado da entidade é `EntityState.Added`.
- `ICurrentUser.IsAuthenticated == true` com `Id = "user-abc"`.
- `IDateTimeProvider.UtcNow` retorna data/hora fixa de teste.

**Passos:**
1. Adicionar entidade ao DbContext com estado `Added`.
2. Chamar `SavingChangesAsync` do interceptor.
3. Verificar campos de auditoria via reflexão.

**Resultado Esperado:**
- `entity.CreatedAt == dateTimeProvider.UtcNow`.
- `entity.CreatedBy == "user-abc"`.
- `entity.UpdatedAt == dateTimeProvider.UtcNow`.
- `entity.UpdatedBy == "user-abc"`.

**Critério de Aceite:** Campos de auditoria preenchidos automaticamente em inserções.

---

### TC-BB-046 — AuditInterceptor atualiza apenas UpdatedAt/By em modificações

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `AuditInterceptor.SavingChangesAsync` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `AuditInterceptor` |

**Pré-condições:**
- Entidade herda de `AuditableEntity<T>`.
- Estado da entidade é `EntityState.Modified`.
- `CreatedAt` e `CreatedBy` já definidos com valores originais.

**Passos:**
1. Modificar entidade (estado `Modified`).
2. Chamar `SavingChangesAsync`.
3. Verificar que `CreatedAt`/`CreatedBy` originais são preservados.

**Resultado Esperado:**
- `entity.UpdatedAt` e `entity.UpdatedBy` atualizados.
- `entity.CreatedAt` e `entity.CreatedBy` NÃO alterados.

**Critério de Aceite:** Modificações não sobrescrevem campos de criação.

---

### TC-BB-047 — AuditInterceptor usa "system" como actor quando usuário não autenticado

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `AuditInterceptor` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `AuditInterceptor` |

**Pré-condições:**
- `ICurrentUser.IsAuthenticated == false` (background job sem usuário).

**Passos:**
1. Executar inserção via background job sem usuário autenticado.
2. Verificar campos de auditoria.

**Resultado Esperado:**
- `entity.CreatedBy == "system"`.
- `entity.UpdatedBy == "system"`.

**Critério de Aceite:** Jobs de background usam "system" como ator de auditoria.

---

### TC-BB-048 — AuditInterceptor ignora entidades que não herdam de AuditableEntity

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `AuditInterceptor` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `AuditInterceptor` |

**Pré-condições:**
- Entidade não herda de `AuditableEntity<T>` (ex.: `OutboxMessage`).

**Passos:**
1. Adicionar entidade não auditável ao DbContext.
2. Chamar `SavingChangesAsync`.
3. Verificar que reflexão não causa exceção para entidades sem campos de auditoria.

**Resultado Esperado:**
- Nenhuma exceção lançada.
- Entidade salva normalmente.

**Critério de Aceite:** Interceptor é defensivo para tipos sem campos de auditoria.

---

### TC-BB-049 — AuditInterceptor não cria campos de auditoria quando contexto é nulo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `AuditInterceptor` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `AuditInterceptor` |

**Pré-condições:**
- `DbContextEventData.Context == null` (cenário de edge case).

**Passos:**
1. Simular `SavingChangesAsync` com contexto nulo.

**Resultado Esperado:**
- `base.SavingChangesAsync` chamado diretamente sem processamento.
- Nenhuma exceção `NullReferenceException`.

**Critério de Aceite:** Contexto nulo tratado defensivamente sem crash.

---

## Soft-Delete — Filtro Global de IsDeleted

### TC-BB-050 — Filtro global de soft-delete exclui entidades com IsDeleted == true

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.ApplyGlobalSoftDeleteFilter` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- DbContext in-memory com 3 entidades: 2 com `IsDeleted = false`, 1 com `IsDeleted = true`.

**Passos:**
1. Executar `DbSet<MyEntity>.ToListAsync()`.
2. Verificar que apenas 2 entidades são retornadas.

**Resultado Esperado:**
- Apenas entidades com `IsDeleted == false` retornadas.
- Entidade com `IsDeleted == true` invisível nas queries padrão.

**Critério de Aceite:** Soft-delete aplicado globalmente em todos os AuditableEntity sem filtro explícito.

---

### TC-BB-051 — Soft-delete não afeta entidades que não herdam de AuditableEntity

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.ApplyGlobalSoftDeleteFilter` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Entidade sem `IsDeleted` (ex.: `OutboxMessage`).

**Passos:**
1. Verificar que `OutboxMessage` não tem filtro de soft-delete aplicado.
2. Executar query em `OutboxMessage`.

**Resultado Esperado:**
- Todas as `OutboxMessage` (incluindo processadas) retornadas sem filtro adicional.

**Critério de Aceite:** Filtro de soft-delete seletivo apenas para `AuditableEntity<T>`.

---

### TC-BB-052 — Soft-delete aplicado por expressão lambda, não por interface

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `NexTraceDbContextBase.ApplyGlobalSoftDeleteFilter` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Configuração do `ModelBuilder` para `AuditableEntity<T>`.

**Passos:**
1. Verificar que `HasQueryFilter` usa `Expression.Equal(property, Expression.Constant(false))`.
2. Verificar que o filtro é aplicado via reflexão para tipos genéricos.

**Resultado Esperado:**
- Filtro `IsDeleted == false` aplicado como `Expression<Func<TEntity, bool>>`.
- Compatível com EF Core query translation.

**Critério de Aceite:** Filtro de soft-delete é translatável para SQL pelo EF Core.

---

### TC-BB-053 — Repositório pode ignorar soft-delete com IgnoreQueryFilters para jobs de retenção

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | Soft-delete / Repositório |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `RepositoryBase` |

**Pré-condições:**
- Entidade com `IsDeleted = true` no banco.
- Job de retenção de dados precisa listar entidades deletadas.

**Passos:**
1. Executar query com `.IgnoreQueryFilters()` no repositório de retenção.
2. Verificar que entidade com `IsDeleted = true` aparece no resultado.

**Resultado Esperado:**
- Entidade "deletada" visível quando filtro global é ignorado explicitamente.

**Critério de Aceite:** Jobs de retenção podem acessar dados excluídos para limpeza definitiva.

---

### TC-BB-054 — Exclusão lógica via IsDeleted não remove a linha fisicamente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | Soft-delete |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `AuditableEntity<T>` |

**Pré-condições:**
- Entidade ativa no banco.

**Passos:**
1. Definir `entity.IsDeleted = true`.
2. Chamar `SaveChangesAsync`.
3. Verificar que a linha permanece fisicamente no banco.
4. Verificar que query padrão não retorna a entidade.

**Resultado Esperado:**
- Linha presente fisicamente com `IsDeleted = true`.
- Query filtrada não retorna a entidade.

**Critério de Aceite:** Soft-delete preserva dados históricos para auditoria.

---

## EncryptedField e EncryptionInterceptor

### TC-BB-055 — Propriedade com [EncryptedField] é criptografada ao persistir

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `EncryptedStringConverter` / `ApplyEncryptedFieldConvention` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `NexTraceDbContextBase` / `EncryptedStringConverter` |

**Pré-condições:**
- Entidade com propriedade string marcada com `[EncryptedField]`.
- Chave de criptografia AES-256-GCM configurada.

**Passos:**
1. Salvar entidade com valor sensível na propriedade criptografada.
2. Ler o valor diretamente do banco (sem passar pelo ORM).
3. Verificar que o valor armazenado é ciphertext, não plaintext.

**Resultado Esperado:**
- Valor no banco é ciphertext Base64 (ou formato AES-256-GCM).
- Valor lido via ORM é o plaintext original.

**Critério de Aceite:** Dados sensíveis são criptografados at-rest transparentemente.

---

### TC-BB-056 — Propriedade com [EncryptedField] é descriptografada ao ler

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `EncryptedStringConverter` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `EncryptedStringConverter` |

**Pré-condições:**
- Banco contém registro com campo criptografado.

**Passos:**
1. Carregar entidade via DbContext.
2. Acessar propriedade marcada com `[EncryptedField]`.

**Resultado Esperado:**
- Propriedade retorna valor plaintext original.
- Descriptografia transparente para o código de aplicação.

**Critério de Aceite:** Leitura de campos criptografados retorna valor legível sem código extra.

---

### TC-BB-057 — Propriedade sem [EncryptedField] não é criptografada

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `ApplyEncryptedFieldConvention` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Entidade com propriedade string sem `[EncryptedField]`.

**Passos:**
1. Verificar que `ApplyEncryptedFieldConvention` não aplica converter à propriedade sem atributo.
2. Salvar entidade e ler diretamente do banco.

**Resultado Esperado:**
- Valor armazenado em plaintext (sem criptografia).

**Critério de Aceite:** Apenas campos marcados com `[EncryptedField]` são criptografados.

---

### TC-BB-058 — EncryptedField aplicado apenas a propriedades string

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `ApplyEncryptedFieldConvention` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `NexTraceDbContextBase` |

**Pré-condições:**
- Entidade com `[EncryptedField]` em propriedade `int` (uso incorreto).

**Passos:**
1. Verificar que `ApplyEncryptedFieldConvention` verifica `propertyInfo.PropertyType != typeof(string)`.
2. Confirmar que propriedade `int` com `[EncryptedField]` é ignorada.

**Resultado Esperado:**
- Nenhuma exceção lançada.
- Converter não aplicado a propriedades não-string.

**Critério de Aceite:** Convenção de encriptação é segura para tipos não-string.

---

### TC-BB-059 — AesGcmEncryptor é resistente a adulteração de ciphertext

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `AesGcmEncryptor` |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Classe** | `AesGcmEncryptor` |

**Pré-condições:**
- Ciphertext válido gerado pelo encryptor.

**Passos:**
1. Modificar um byte do ciphertext (adulteração).
2. Tentar descriptografar o ciphertext adulterado.

**Resultado Esperado:**
- Descriptografia falha com `CryptographicException` ou equivalente.
- Dados adulterados não são aceitos silenciosamente.

**Critério de Aceite:** AES-256-GCM detecta adulteração de dados (autenticação de mensagem via GCM tag).

---

## ModuleOutboxProcessorJob — Retentativas, DLQ e Advisory Lock

### TC-BB-060 — OutboxProcessorJob processa mensagens pendentes em ordem FIFO

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- 3 `OutboxMessage` com `ProcessedAt == null` e `RetryCount < 5`.
- Advisory lock disponível (sem concorrência).

**Passos:**
1. Executar um ciclo do `ModuleOutboxProcessorJob`.
2. Verificar que `ProcessedAt` é preenchido em cada mensagem após entrega.
3. Verificar que mensagens são processadas em ordem de `CreatedAt` (FIFO).

**Resultado Esperado:**
- 3 mensagens processadas com `ProcessedAt` definido.
- Handlers invocados para cada mensagem.

**Critério de Aceite:** Outbox garante entrega em ordem cronológica.

---

### TC-BB-061 — OutboxProcessorJob incrementa RetryCount em falha de handler

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- `OutboxMessage` com `RetryCount == 2`.
- Handler lança exceção durante processamento.

**Passos:**
1. Executar ciclo do job com handler que lança exceção.
2. Verificar que `RetryCount` é incrementado para 3.
3. Verificar que `LastError` contém a mensagem de erro.
4. Verificar que `ProcessedAt` permanece nulo.

**Resultado Esperado:**
- `RetryCount == 3`.
- `LastError` preenchido com detalhes da exceção.
- Mensagem disponível para próxima tentativa.

**Critério de Aceite:** Falhas de entrega registradas com contagem de tentativas.

---

### TC-BB-062 — OutboxProcessorJob move mensagem para DLQ após 5 falhas

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- `OutboxMessage` com `RetryCount == 4` (próxima falha = 5).
- Handler lança exceção novamente.
- `IDeadLetterRepository` mockado.

**Passos:**
1. Executar ciclo do job.
2. Verificar que `IDeadLetterRepository.AddAsync` é chamado.
3. Verificar que `ProcessedAt` é definido (mensagem removida da fila ativa).

**Resultado Esperado:**
- `DeadLetterMessage` criado no DLQ.
- Mensagem original marcada como processada (não retentada novamente).

**Critério de Aceite:** Mensagens com 5 falhas são movidas para DLQ sem loop infinito.

---

### TC-BB-063 — OutboxProcessorJob processa no máximo 50 mensagens por ciclo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- 100 `OutboxMessage` pendentes no banco.

**Passos:**
1. Executar um ciclo do job.
2. Verificar que apenas 50 mensagens são processadas.

**Resultado Esperado:**
- 50 mensagens processadas no primeiro ciclo.
- 50 restantes aguardam o próximo ciclo.

**Critério de Aceite:** Limite de 50 por ciclo previne sobrecarga de processamento.

---

### TC-BB-064 — OutboxProcessorJob pula ciclo quando advisory lock não está disponível

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- Outra instância do job está segurando o advisory lock (`pg_try_advisory_lock` retorna `false`).

**Passos:**
1. Simular `pg_try_advisory_lock` retornando falso.
2. Executar o ciclo.
3. Verificar que nenhuma mensagem é processada.

**Resultado Esperado:**
- Ciclo encerrado sem processar mensagens.
- Nenhum erro lançado.
- Log indicando que o lock não foi adquirido.

**Critério de Aceite:** Advisory lock previne processamento duplicado em ambientes multi-instância.

---

### TC-BB-065 — OutboxProcessorJob libera advisory lock mesmo após exceção (finally)

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure / BackgroundWorkers |
| **Feature** | `ModuleOutboxProcessorJob` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `ModuleOutboxProcessorJob<TContext>` |

**Pré-condições:**
- Exceção inesperada lançada durante processamento de mensagens.

**Passos:**
1. Simular exceção durante processamento.
2. Verificar que `pg_advisory_unlock` (ou equivalente) é chamado no bloco `finally`.

**Resultado Esperado:**
- Advisory lock liberado independentemente de sucesso ou falha.
- Próximo ciclo pode adquirir o lock normalmente.

**Critério de Aceite:** Lock liberado em `finally` previne deadlock de advisory lock.

---

## AggregateRoot e Domain Events

### TC-BB-066 — AggregateRoot acumula múltiplos Domain Events

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `AggregateRoot<T>` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `AggregateRoot<T>` |

**Pré-condições:**
- Concrete subclass de `AggregateRoot<T>` que chama `RaiseDomainEvent` 3 vezes.

**Passos:**
1. Criar instância do aggregate root.
2. Executar operação de domínio que raise 3 eventos.
3. Verificar `DomainEvents.Count`.

**Resultado Esperado:**
- `DomainEvents.Count == 3`.
- Eventos disponíveis via `DomainEvents` (somente leitura).

**Critério de Aceite:** Aggregate root acumula corretamente múltiplos eventos antes do commit.

---

### TC-BB-067 — ClearDomainEvents limpa a fila de eventos

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `AggregateRoot<T>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `AggregateRoot<T>` |

**Pré-condições:**
- Aggregate root com 2 domain events acumulados.

**Passos:**
1. Chamar `aggregateRoot.ClearDomainEvents()`.
2. Verificar `DomainEvents.Count`.

**Resultado Esperado:**
- `DomainEvents.Count == 0`.
- Chamadas subsequentes a `DomainEvents` retornam lista vazia.

**Critério de Aceite:** Limpeza de eventos após coleta pelo DbContext é idempotente.

---

### TC-BB-068 — DomainEvents é somente leitura externamente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `AggregateRoot<T>` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `AggregateRoot<T>` |

**Pré-condições:**
- Aggregate root com eventos acumulados.

**Passos:**
1. Tentar adicionar evento diretamente à coleção `DomainEvents`.

**Resultado Esperado:**
- Compilação falha ou operação lança `NotSupportedException` por ser `IReadOnlyList<T>`.

**Critério de Aceite:** Acesso externo à fila de eventos é somente leitura. Apenas `RaiseDomainEvent` pode adicionar.

---

### TC-BB-069 — DomainEventBase contém OccurredOn com data/hora UTC

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `DomainEventBase` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `DomainEventBase` |

**Pré-condições:**
- Evento de domínio concreto herda de `DomainEventBase`.

**Passos:**
1. Instanciar evento de domínio.
2. Verificar propriedade `OccurredOn`.

**Resultado Esperado:**
- `OccurredOn` é `DateTimeOffset` com `Kind == Utc` ou offset zero.

**Critério de Aceite:** Todos os eventos de domínio têm carimbo de data/hora UTC.

---

### TC-BB-070 — IntegrationEventBase tem EventId único por instância

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `IntegrationEventBase` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `IntegrationEventBase` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. Criar dois eventos de integração do mesmo tipo.
2. Comparar `EventId` de cada instância.

**Resultado Esperado:**
- `EventId` é Guid único por instância.
- `OccurredOn` é timestamp da criação.

**Critério de Aceite:** Cada evento de integração tem identidade única para rastreamento.

---

## TypedIdBase e Guards

### TC-BB-071 — TypedIdBase.Value é exposição do Guid subjacente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `TypedIdBase` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TypedIdBase` |

**Pré-condições:**
- Record concreto que herda de `TypedIdBase`.

**Passos:**
1. Criar `MyEntityId id = new(Guid.NewGuid())`.
2. Verificar que `id.Value` retorna o Guid original.
3. Verificar que `id` não é implicitamente conversível para `Guid` (compile-time safety).

**Resultado Esperado:**
- `id.Value` acessível e correto.
- `guids.Contains(id)` falha na compilação (não comparável diretamente a Guid).

**Critério de Aceite:** Typed IDs fornecem type-safety em tempo de compilação.

---

### TC-BB-072 — TypedIdBase suporta igualdade por valor (record semantics)

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `TypedIdBase` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `TypedIdBase` |

**Pré-condições:**
- Dois IDs do mesmo tipo com o mesmo Guid.

**Passos:**
1. `var id1 = MyEntityId.From(guid); var id2 = MyEntityId.From(guid);`
2. Verificar `id1 == id2` e `id1.Equals(id2)`.

**Resultado Esperado:**
- `id1 == id2 == true`.
- `id1.Equals(id2) == true`.

**Critério de Aceite:** Typed IDs com mesmo valor são iguais (value semantics de record).

---

### TC-BB-073 — NexTraceGuards.InvalidSemanticVersion valida SemVer 2.0

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `NexTraceGuards.InvalidSemanticVersion` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `NexTraceGuards` |

**Pré-condições:**
- Versões de teste: `"1.2.3"` (válida), `"1.2.3-alpha.1"` (válida), `"1.2"` (inválida), `"v1.2.3"` (inválida).

**Passos:**
1. Chamar `Guard.Against.InvalidSemanticVersion` para cada versão.
2. Verificar comportamento para versões válidas e inválidas.

**Resultado Esperado:**
- `"1.2.3"` e `"1.2.3-alpha.1"`: retorna string normalizada.
- `"1.2"` e `"v1.2.3"`: lança `ArgumentException`.

**Critério de Aceite:** Guard SemVer 2.0 rejeita versões malformadas.

---

### TC-BB-074 — NexTraceGuards.UngovernedEnvironment aceita apenas ambientes governados

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `NexTraceGuards.UngovernedEnvironment` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `NexTraceGuards` |

**Pré-condições:**
- Ambientes válidos: `Integration`, `QA`, `UAT`, `Staging`, `Production`.
- Ambientes inválidos: `Development`, `Local`, `""`.

**Passos:**
1. Testar cada ambiente válido e inválido.

**Resultado Esperado:**
- Ambientes válidos: retornam o nome normalizado.
- Ambientes inválidos: lançam `ArgumentException`.

**Critério de Aceite:** Operações governadas (ex.: deploy) só aceitas em ambientes listados.

---

### TC-BB-075 — NexTraceGuards.EmptyTenantId rejeita Guid.Empty

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Core |
| **Feature** | `NexTraceGuards.EmptyTenantId` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `NexTraceGuards` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. `Guard.Against.EmptyTenantId(Guid.Empty)` → deve lançar.
2. `Guard.Against.EmptyTenantId(Guid.NewGuid())` → deve retornar o Guid.

**Resultado Esperado:**
- `Guid.Empty`: `ArgumentException` com mensagem `"TenantId cannot be empty."`.
- Guid válido: retornado sem exceção.

**Critério de Aceite:** Guard protege contra TenantId vazio em nível de aplicação.

---

## Security — JWT, CookieSession, TenantResolutionMiddleware

### TC-BB-076 — JwtTokenService gera token com claims corretos de tenant e permissões

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `JwtTokenService` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `JwtTokenService` |

**Pré-condições:**
- Configuração JWT com secret válido (mínimo 32 chars), issuer e audience.

**Passos:**
1. Chamar `JwtTokenService.GenerateToken(userId, tenantId, role, permissions, capabilities)`.
2. Decodificar o JWT e verificar claims.

**Resultado Esperado:**
- Claim `sub` contém `userId`.
- Claim `tenant_id` contém `tenantId`.
- Claim `role` contém nome do role.
- Claims de permissões presentes.
- Claims de capabilities do plano presentes.
- `exp` é data futura correta.

**Critério de Aceite:** JWT contém todas as informações necessárias para autenticação e autorização stateless.

---

### TC-BB-077 — TenantResolutionMiddleware extrai TenantId do JWT e popula ICurrentTenant

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `TenantResolutionMiddleware` |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Classe** | `TenantResolutionMiddleware` |

**Pré-condições:**
- JWT válido com claim `tenant_id` na requisição.
- Tenant existe e está ativo no repositório.

**Passos:**
1. Processar requisição através do middleware com JWT válido.
2. Verificar que `ICurrentTenant.Id`, `IsActive`, `Name` e capabilities estão populados.

**Resultado Esperado:**
- `ICurrentTenant.Id` igual ao tenant_id do JWT.
- `ICurrentTenant.IsActive == true`.
- Capabilities corretamente carregadas da licença.

**Critério de Aceite:** Contexto de tenant resolvido corretamente para cada requisição.

---

### TC-BB-078 — TenantResolutionMiddleware rejeita JWT com tenant inexistente

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `TenantResolutionMiddleware` |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Classe** | `TenantResolutionMiddleware` |

**Pré-condições:**
- JWT com `tenant_id` que não existe no banco.

**Passos:**
1. Processar requisição com tenant_id inválido no JWT.

**Resultado Esperado:**
- Resposta HTTP 401 ou 403.
- `ICurrentTenant.Id` não preenchido com tenant inválido.

**Critério de Aceite:** Tokens com tenant inexistente são rejeitados pelo middleware.

---

### TC-BB-079 — ApiKeyAuthenticationHandler autentica via header X-Api-Key

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `ApiKeyAuthenticationHandler` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `ApiKeyAuthenticationHandler` |

**Pré-condições:**
- API Key válida registrada no sistema.
- Requisição com header `X-Api-Key: valid-key`.

**Passos:**
1. Processar requisição com API Key válida.
2. Verificar que `AuthenticateResult.Success` é retornado.
3. Verificar que claims de identidade são populados.

**Resultado Esperado:**
- Autenticação bem-sucedida.
- Claims de `sub` e `tenant_id` presentes no ticket de autenticação.

**Critério de Aceite:** Agentes e automações autenticam via API Key com claims corretos.

---

### TC-BB-080 — CsrfTokenValidator rejeita requisição mutante sem token CSRF válido

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `CsrfTokenValidator` |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Classe** | `CsrfTokenValidator` |

**Pré-condições:**
- Cookie de sessão presente.
- Requisição POST sem header CSRF ou com token inválido.

**Passos:**
1. Enviar requisição POST com cookie de sessão mas sem token CSRF válido.

**Resultado Esperado:**
- Requisição rejeitada com HTTP 400 ou 403.
- Nenhuma mutação executada.

**Critério de Aceite:** Proteção CSRF ativa para endpoints com cookie de sessão.

---

### TC-BB-081 — SessionInactivityMiddleware invalida sessão após período de inatividade

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `SessionInactivityMiddleware` |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Classe** | `SessionInactivityMiddleware` |

**Pré-condições:**
- Sessão com último acesso antes do timeout configurado.

**Passos:**
1. Fazer requisição com sessão inativa além do timeout.

**Resultado Esperado:**
- Sessão invalidada.
- HTTP 401 retornado.
- Usuário deve autenticar novamente.

**Critério de Aceite:** Sessões inativas são encerradas automaticamente pelo middleware.

---

### TC-BB-082 — PermissionAuthorizationHandler permite acesso com permissão correta

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Security |
| **Feature** | `PermissionAuthorizationHandler` |
| **Tipo** | Unitário |
| **Prioridade** | Crítica |
| **Classe** | `PermissionAuthorizationHandler` |

**Pré-condições:**
- Usuário com claim de permissão `iam.users.create`.
- Endpoint protegido com `RequirePermission("iam.users.create")`.

**Passos:**
1. Avaliar `PermissionRequirement` para o usuário.

**Resultado Esperado:**
- `AuthorizationResult.Succeeded`.

**Pré-condições (negativo):**
- Usuário sem a permissão requerida.

**Passos (negativo):**
1. Avaliar `PermissionRequirement` para usuário sem permissão.

**Resultado Esperado (negativo):**
- `AuthorizationResult.Failed`.

**Critério de Aceite:** RBAC via claims de permissão funcional para acesso concedido e negado.

---

## Observability — OpenTelemetry, Health Checks, IngestionMetrics

### TC-BB-083 — DbContextConnectivityHealthCheck retorna Healthy quando banco acessível

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Infrastructure |
| **Feature** | `DbContextConnectivityHealthCheck` |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Classe** | `DbContextConnectivityHealthCheck` |

**Pré-condições:**
- DbContext com conexão de banco funcional.

**Passos:**
1. Executar `CheckHealthAsync`.

**Resultado Esperado:**
- `HealthCheckResult.Healthy`.

**Cenário negativo:**
- Banco indisponível.
- `CheckHealthAsync` retorna `HealthCheckResult.Unhealthy`.

**Critério de Aceite:** Health check reflete estado real da conectividade do banco.

---

### TC-BB-084 — IIngestionMetricsCollector registra eventos de ingestão com tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Observability |
| **Feature** | `IngestionMetricsCollector` |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Classe** | `IngestionMetricsCollector` |

**Pré-condições:**
- Medidor OpenTelemetry configurado.

**Passos:**
1. Chamar `IIngestionMetricsCollector.RecordEvent(tenantId, eventType, count)`.
2. Verificar que contador de instrumentação é incrementado.
3. Verificar que tag `tenant.id` está presente na métrica.

**Resultado Esperado:**
- Métrica registrada com `tenant.id` como dimensão.
- Contador incrementado pelo valor de `count`.

**Critério de Aceite:** Métricas de ingestão são isoladas por tenant para análise.

---

### TC-BB-085 — NullIngestionMetricsCollector não lança exceção

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Observability |
| **Feature** | `NullIngestionMetricsCollector` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `NullIngestionMetricsCollector` |

**Pré-condições:**
- `NullIngestionMetricsCollector` registrado no container (modo sem OpenTelemetry).

**Passos:**
1. Chamar todos os métodos públicos do `NullIngestionMetricsCollector`.

**Resultado Esperado:**
- Nenhuma exceção lançada.
- Métodos são no-ops silenciosos.

**Critério de Aceite:** Null implementation segura para ambientes sem coleta de métricas configurada.

---

### TC-BB-086 — NexTraceActivitySources define activity sources por módulo

| Campo | Valor |
|-------|-------|
| **Módulo** | BuildingBlocks.Observability |
| **Feature** | `NexTraceActivitySources` |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Classe** | `NexTraceActivitySources` |

**Pré-condições:**
- Nenhuma.

**Passos:**
1. Verificar que `NexTraceActivitySources` define nomes de activity sources para cada módulo.
2. Criar activity de trace e verificar que `ActivitySource.Name` corresponde ao módulo.

**Resultado Esperado:**
- Activity sources com nomes únicos por módulo.
- Spans de trace rastreáveis por módulo no backend de observabilidade.

**Critério de Aceite:** Distributed tracing com activity sources nomeados por módulo permite diagnóstico preciso.

---

*Fim do documento — Building Blocks. Total: 86 cenários de teste.*
