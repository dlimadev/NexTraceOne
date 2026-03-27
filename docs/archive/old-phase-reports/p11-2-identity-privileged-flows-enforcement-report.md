# P11.2 — Identity & Access: enforcement real de fluxos privilegiados

## Objetivo desta execução

Fechar o enforcement operacional dos fluxos privilegiados (Break Glass, JIT Access, Delegation) no módulo Identity & Access, garantindo:

- self-action prevention real e verificada (self-approval JIT, self-delegation);
- background jobs de expiração automática reais e auditáveis;
- estado dos fluxos privilegiados com trilha consistente e enforcement proativo.

## Ficheiros alterados

Esta fase produziu:

1. **Novos testes de handler** (application layer) para provar enforcement fim a fim:
   - `tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/Application/Features/DecideJitAccessTests.cs` (novo)
   - `tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/Application/Features/CreateDelegationTests.cs` (novo)

2. **Documentação obrigatória** desta fase:
   - `docs/architecture/p11-2-identity-privileged-flows-enforcement-report.md` (este ficheiro)
   - `docs/architecture/p11-2-post-change-gap-report.md`

## Estado anterior e confirmação do que já estava implementado

Ao inventariar o código real antes de fazer alterações, confirmou-se que:

### Self-action prevention (P11.1 implementou, P11.2 valida)

| Fluxo | Onde | Estado |
|---|---|---|
| Self-approval em JIT | `DecideJitAccess.Handler` — retorna `Identity.JitAccess.SelfApprovalNotAllowed` e emite `JitSelfApprovalDenied` | Implementado e testado |
| Self-approval em domínio JIT | `JitAccessRequest.Approve()` — silently rejects se `approvedBy == RequestedBy` | Implementado e testado |
| Self-delegation | `CreateDelegation.Handler` — retorna `Identity.Delegation.SelfNotAllowed` e emite `DelegationToSelfDenied` | Implementado e testado |
| Self-delegation em domínio | `Delegation.Create()` — lança `InvalidOperationException` | Implementado e testado |

### Background jobs de expiração (já totalmente implementados)

| Handler | Entidade | Comportamento | Estado |
|---|---|---|---|
| `BreakGlassExpirationHandler` | `BreakGlassRequest` | `Status == Active && ExpiresAt <= now` → `Expire(now)` + `SecurityEvent.BreakGlassExpired` | Implementado |
| `JitAccessExpirationHandler` | `JitAccessRequest` | `Pending && ApprovalDeadline <= now` OU `Approved && GrantedUntil <= now` → `Expire(now)` + `SecurityEvent.JitAccessExpired` | Implementado |
| `DelegationExpirationHandler` | `Delegation` | `Active && ValidUntil <= now` → `Expire(now)` + `SecurityEvent.DelegationExpired` | Implementado |
| `AccessReviewExpirationHandler` | `AccessReviewCampaign` | Prazo ultrapassado → `ProcessDeadline(now)` + `SecurityEvent.AccessReviewExpiredAutoRevoked` | Implementado |
| `EnvironmentAccessExpirationHandler` | `EnvironmentAccess` | Acesso temporário expirado | Implementado |

### Job orquestrador

- `IdentityExpirationJob` (BackgroundService): executa todos os handlers a cada **60 segundos** de forma resiliente (falha num handler não bloqueia os outros).
- Registado em `Program.cs` do BackgroundWorkers: `AddHostedService<IdentityExpirationJob>()`.
- Health check: `identity-expiration-job` com timeout de 5 minutos.

## Regras de self-action prevention implementadas

### JIT Access — auto-aprovação impossível

```csharp
// DecideJitAccess.Handler
if (decidedBy == jitRequest.RequestedBy)
{
    // Emite SecurityEvent auditável
    securityEventRepository.Add(SecurityEvent.Create(
        tenantId, decidedBy, sessionId: null,
        SecurityEventType.JitSelfApprovalDenied, ...));
    securityEventTracker.Track(deniedEvent);
    return IdentityErrors.JitSelfApprovalNotAllowed();
}
```

Dupla proteção:
1. Handler → erro explícito + evento de segurança auditável.
2. Domínio → `JitAccessRequest.Approve()` ignora silenciosamente se `approvedBy == RequestedBy`.

### Delegation — auto-delegação impossível

```csharp
// CreateDelegation.Handler
if (grantorId == delegateeId)
{
    // Emite SecurityEvent auditável
    securityEventRepository.Add(SecurityEvent.Create(
        tenantId, grantorId, sessionId: null,
        SecurityEventType.DelegationToSelfDenied, ...));
    securityEventTracker.Track(deniedEvent);
    return IdentityErrors.DelegationToSelfNotAllowed();
}
```

Dupla proteção:
1. Handler → erro explícito + evento de segurança auditável.
2. Domínio → `Delegation.Create()` lança `InvalidOperationException("A user cannot delegate permissions to themselves.")`.

## Jobs de expiração implementados

### BreakGlassExpirationHandler

- Filtra: `Status == Active && ExpiresAt != null && ExpiresAt <= now`
- Ação: `request.Expire(now)` → muda status para `BreakGlassStatus.Expired`
- Evento: `SecurityEvent.BreakGlassExpired` com `riskScore: 30`
- Salva via `dbContext.SaveChangesAsync()`

### JitAccessExpirationHandler

- Filtra: `(Status == Pending && ApprovalDeadline <= now) || (Status == Approved && GrantedUntil != null && GrantedUntil <= now)`
- Ação: `request.Expire(now)` → muda status para `JitAccessStatus.Expired`
- Evento: `SecurityEvent.JitAccessExpired` com razão diferenciada (`"approval deadline exceeded"` vs `"access grant period ended"`)
- `riskScore: 20`

### DelegationExpirationHandler

- Filtra: `Status == Active && ValidUntil <= now`
- Ação: `delegation.Expire(now)` → muda status para `DelegationStatus.Expired`
- Evento: `SecurityEvent.DelegationExpired` com `riskScore: 10`

### IdentityExpirationJob (orquestrador)

- Periodicidade: 60 segundos
- Batch size: 100 entidades por handler por ciclo
- Falha isolada: `try/catch` por handler — falha num não afeta os outros
- Logging de quantidade processada por handler e ciclo

## Handlers/endpoints/queries ajustados

Não foi necessário alterar handlers ou endpoints existentes para P11.2, pois toda a implementação estava completa desde P11.1 e implementações anteriores.

## Uso de SecurityEvent, SecurityAuditService e auditoria

- Todos os eventos de expiração usam `dbContext.SecurityEvents.Add()` diretamente (job roda fora do pipeline de request — não tem acesso ao `ISecurityEventTracker`).
- Eventos de self-action prevention usam `securityEventRepository.Add()` + `securityEventTracker.Track()` para propagar para Audit central via `SecurityEventAuditBehavior`.
- Nenhum mecanismo de auditoria paralelo foi criado.

## Validação funcional / compilação

- `dotnet build NexTraceOne.sln --configuration Release`: ✅ sem erros
- `dotnet test tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/`:
  - Resultado: **Passed: 297, Failed: 0**
  - 7 novos testes adicionados (4 `DecideJitAccessTests` + 3 `CreateDelegationTests`)
