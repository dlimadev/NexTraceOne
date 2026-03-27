# P11.3 — Identity MFA Enforcement & Session Hardening Report

**Data:** 2026-03-27  
**Fase:** P11.3 — Completar MFA enforcement, sessão segura e hardening final do runtime de autenticação  
**Status:** ✅ Concluído  

---

## 1. Objetivo

Executar o hardening final do runtime de autenticação no módulo Identity & Access:

- Implementar MFA enforcement real para utilizadores com MFA configurado.
- Implementar step-up MFA para operações privilegiadas (Break Glass, JIT Access).
- Implementar validação de sessão por IP no refresh token.
- Integrar eventos de segurança e auditoria para cada ponto crítico.

---

## 2. Estado anterior do runtime de autenticação

| Área                         | Estado antes do P11.3                                                                 |
|------------------------------|----------------------------------------------------------------------------------------|
| MFA enforcement em login     | `user.MfaEnabled`, `user.MfaSecret`, `user.MfaMethod` existiam no domínio mas **NÃO** eram verificados em runtime. Login sempre completava sem MFA. |
| Step-up MFA em privilegiados | `SecurityEventType.StepUpMfaRequired` definido, `MfaPolicy` existia como value object, mas **nenhum handler enforçava** MFA em Break Glass ou JIT. |
| Validação IP/UserAgent       | `Session.CreatedByIp` e `Session.UserAgent` eram armazenados na criação de sessão, mas **NÃO** comparados no refresh token. |
| `ITotpVerifier`              | Não existia. Sem capacidade de verificar códigos TOTP em runtime.                    |
| `IMfaChallengeTokenService`  | Não existia. Sem token de desafio de curta duração para o fluxo MFA em dois passos.  |
| Eventos MFA                  | `MfaChallengeSucceeded`, `MfaChallengeFailed`, `MfaStepUpDenied`, `SuspiciousSessionContextDetected` **NÃO existiam** como tipos de evento. |

---

## 3. Modelo de MFA enforcement adoptado

### 3.1 Fluxo de login com MFA (dois passos)

```
POST /auth/login
  ↓ Credenciais válidas + user.MfaEnabled = true
  ↓ Handler emite MfaChallengeToken (HMAC-SHA256, TTL 5 min)
  ↓ Resposta: { MfaRequired: true, MfaChallengeToken: "..." }
  
POST /auth/mfa/verify
  ↓ ChallengeToken + Código TOTP 6 dígitos
  ↓ Handler valida token + verifica TOTP (RFC 6238, janela ±1 passo)
  ↓ Sucesso: cria sessão e emite tokens completos
  ↓ Falha: SecurityEvent.MfaChallengeFailed
```

### 3.2 Step-up MFA para operações privilegiadas

```
POST /privileged/break-glass
POST /privileged/jit/request
  ↓ user.MfaEnabled = true
  ↓ MfaCode ausente → SecurityEvent.StepUpMfaRequired → HTTP 403
  ↓ MfaCode presente mas inválido → SecurityEvent.MfaStepUpDenied → HTTP 403
  ↓ MfaCode válido (TOTP) → operação prossegue normalmente
```

### 3.3 Garantias anti-bypass

- O refresh token **não emite novos tokens** sem que a sessão seja válida.
- O fluxo de login com MFA **não cria sessão** no primeiro passo — a sessão só é criada após verificação TOTP bem-sucedida.
- Tentativas de refresh de sessões revogadas ou expiradas continuam a retornar erro.

---

## 4. TOTP Verifier implementado

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/TotpVerifier.cs`

- Implementação RFC 6238 pura, sem dependências externas.
- Usa HMAC-SHA1 com contador de 30 segundos (conforme especificação).
- Aceita códigos da janela anterior, actual e seguinte (tolerância ±1 passo = ±30 segundos).
- Decodificação Base32 (RFC 4648) implementada internamente.
- Retorna `false` silenciosamente para segredos ou códigos inválidos (sem exceções).

---

## 5. MFA Challenge Token Service implementado

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/MfaChallengeTokenService.cs`

- Token stateless: não requer tabela adicional na base de dados.
- Formato: `Base64Url("{userId}:{expiryUnix}:{HMAC-SHA256}")`.
- TTL padrão: 5 minutos.
- Assinado com a chave JWT da plataforma (derivada com sufixo `:mfa-challenge`).
- Comparação HMAC usa `CryptographicOperations.FixedTimeEquals` (timing-safe).
- Configurado via `Jwt:Secret` ou `Security:Jwt:SigningKey` da configuração da aplicação.

---

## 6. Validação de sessão por IP implementado

**Ficheiro modificado:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RefreshToken/RefreshToken.cs`

- O `RefreshToken.Handler` passa a receber `ISecurityAuditRecorder` como dependência.
- Ao processar um refresh, compara `request.IpAddress` (IP actual) com `session.CreatedByIp` (IP original).
- Se diferentes (e sessão não criada com `"unknown"`), emite `SecurityEvent.SuspiciousSessionContextDetected` (risk score 55).
- Comportamento: **detecta e audita** sem revogar sessão por defeito — utilizadores com IPs dinâmicos (mobile, VPN) não são impedidos, mas a anomalia fica registada.
- Este comportamento é configurável em fases posteriores.

---

## 7. Handlers e endpoints ajustados

### 7.1 Novos handlers

| Handler                      | Endpoint                  | Descrição                                     |
|------------------------------|---------------------------|-----------------------------------------------|
| `VerifyMfaChallenge.Handler` | `POST /auth/mfa/verify`   | Valida ChallengeToken + TOTP → emite sessão   |

### 7.2 Handlers modificados

| Handler                         | Alteração                                                              |
|---------------------------------|------------------------------------------------------------------------|
| `LocalLogin.Handler`            | Verifica `user.MfaEnabled`; se true, emite desafio em vez de sessão   |
| `RefreshToken.Handler`          | Adiciona `ISecurityAuditRecorder`; detecta mudança de IP               |
| `RequestBreakGlass.Handler`     | Carrega utilizador; verifica step-up MFA se `user.MfaEnabled`          |
| `RequestJitAccess.Handler`      | Carrega utilizador; verifica step-up MFA se `user.MfaEnabled`          |

### 7.3 Contracts ajustados

| Contract                   | Alteração                                                        |
|----------------------------|------------------------------------------------------------------|
| `LocalLogin.LoginResponse` | Adicionados `MfaRequired: bool = false`, `MfaChallengeToken: string? = null` (compatibilidade mantida) |
| `LocalLogin.Command`       | Sem alteração                                                    |
| `RequestBreakGlass.Command`| Adicionado `MfaCode: string?` (opcional, obrigatório com MFA)   |
| `RequestJitAccess.Command` | Adicionado `MfaCode: string?` (opcional, obrigatório com MFA)   |

---

## 8. Novos tipos de SecurityEvent

Adicionados a `SecurityEventType`:

| Constante                              | Código                                          | Risk Score |
|----------------------------------------|-------------------------------------------------|------------|
| `MfaChallengeSucceeded`               | `security.mfa.challenge_succeeded`              | 0          |
| `MfaChallengeFailed`                  | `security.mfa.challenge_failed`                 | 50         |
| `MfaStepUpGranted`                    | `security.mfa.stepup_granted`                   | 0          |
| `MfaStepUpDenied`                     | `security.mfa.stepup_denied`                    | 60–70      |
| `SuspiciousSessionContextDetected`    | `security.anomaly.suspicious_session_context`   | 55         |

---

## 9. Novos métodos em ISecurityAuditRecorder / SecurityAuditRecorder

| Método                            | Emitido quando                                                  |
|-----------------------------------|-----------------------------------------------------------------|
| `RecordMfaChallengeSuccess`       | TOTP verificado com sucesso em `VerifyMfaChallenge`            |
| `RecordMfaChallengeFailed`        | Código TOTP inválido em `VerifyMfaChallenge`                   |
| `RecordStepUpMfaRequired`         | Step-up MFA exigido (Break Glass, JIT, MFA login) mas não fornecido |
| `RecordMfaStepUpDenied`           | Código TOTP inválido em Break Glass ou JIT                      |
| `RecordSuspiciousSessionContext`  | IP alterado entre criação de sessão e refresh                   |

---

## 10. Novos erros de domínio

Adicionados a `IdentityErrors`:

| Código                                    | HTTP   | Cenário                                             |
|-------------------------------------------|--------|-----------------------------------------------------|
| `Identity.Mfa.ChallengeExpiredOrInvalid` | 401    | Token de desafio inválido ou expirado               |
| `Identity.Mfa.CodeInvalid`               | 403    | Código TOTP incorreto                               |
| `Identity.Mfa.StepUpRequired`            | 403    | Step-up MFA exigido mas código não fornecido        |

---

## 11. Registo DI

**Application DI:**
- `IValidator<VerifyMfaChallenge.Command>` registado.

**Infrastructure DI:**
- `ITotpVerifier` → `TotpVerifier` (Scoped)
- `IMfaChallengeTokenService` → `MfaChallengeTokenService` (Scoped)

---

## 12. Ficheiros alterados

### Novos ficheiros
```
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/ITotpVerifier.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/IMfaChallengeTokenService.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/VerifyMfaChallenge/VerifyMfaChallenge.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/TotpVerifier.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/MfaChallengeTokenService.cs
tests/modules/identityaccess/.../Application/Features/VerifyMfaChallengeTests.cs
tests/modules/identityaccess/.../Application/Features/LocalLoginMfaEnforcementTests.cs
tests/modules/identityaccess/.../Infrastructure/Services/TotpVerifierTests.cs
tests/modules/identityaccess/.../TestDoubles/TestUserFactory.cs
docs/architecture/p11-3-identity-mfa-session-hardening-report.md
docs/architecture/p11-3-post-change-gap-report.md
```

### Ficheiros modificados
```
src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/SecurityEventType.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Errors/IdentityErrors.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/ISecurityAuditRecorder.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/SecurityAuditRecorder.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/LocalLogin/LocalLogin.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RefreshToken/RefreshToken.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RequestBreakGlass/RequestBreakGlass.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RequestJitAccess/RequestJitAccess.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/DependencyInjection.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/DependencyInjection.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/AuthEndpoints.cs
tests/modules/identityaccess/.../Application/Features/LocalLoginTests.cs
tests/modules/identityaccess/.../Application/Features/RefreshTokenTests.cs
```

---

## 13. Validação funcional

- **Build:** ✅ 0 erros de compilação (29 warnings pré-existentes não relacionados)
- **Testes:** ✅ 315 testes passam (up de 297; 18 novos testes adicionados)
- **Cobertura de novos fluxos:**
  - `VerifyMfaChallenge`: 4 testes (sucesso, token inválido, TOTP inválido, utilizador não encontrado)
  - `LocalLogin MFA enforcement`: 2 testes (challenge emitido, sem MFA retorna tokens)
  - `TotpVerifier`: 8 testes (código válido, janelas anterior/próxima, formatos inválidos)

---

## 14. Segurança

- TOTP verificado com HMAC-SHA1 (RFC 6238/4226) sem dependências externas.
- Challenge token usa `FixedTimeEquals` para comparação HMAC (timing-safe).
- Step-up MFA bloqueia Break Glass e JIT para utilizadores com MFA, gerando auditoria.
- IP mismatch no refresh é detectado e auditado (sem revogação automática para preservar UX).
- Sessão não é criada antes do MFA ser completado — o challenge token é um token efémero, não uma sessão.
