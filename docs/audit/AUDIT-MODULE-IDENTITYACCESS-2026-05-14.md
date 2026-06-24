# Auditoria do Módulo IdentityAccess — NexTraceOne
**Data:** 2026-05-14  
**Auditor:** Claude Code (Automated Review)  
**Âmbito:** End-to-end — Domain, Application, Infrastructure, API, Frontend  
**Branch:** `claude/code-review-audit-i0rFs`

---

## Sumário Executivo

O módulo IdentityAccess é a fundação de segurança de toda a plataforma — 336 ficheiros C#, 87 testes, 25 páginas frontend e serviços críticos de autenticação (JWT, refresh tokens, MFA TOTP, Break Glass, OIDC/SAML). A arquitetura geral está bem estruturada: PBKDF2 para hashing de passwords, refresh tokens armazenados como hash SHA-256, tokens MFA com HMAC-SHA256, separação de responsabilidades via DIP (ILoginSessionCreator, ILoginResponseBuilder, ISecurityAuditRecorder). No entanto, foram identificados **problemas graves de segurança** que, em produção, podem permitir persistência de acesso após mudança de password, replay de codes TOTP, e CSRF em fluxo OIDC.

**Total de problemas identificados:** 22  
**P0 (bloqueadores de segurança):** 3  
**P1 (alta prioridade):** 9  
**P2 (média prioridade):** 7  
**P3 (baixa prioridade):** 3  

---

## Índice

1. [Estrutura do Módulo](#1-estrutura-do-módulo)
2. [Domain Layer — Entidades e Value Objects](#2-domain-layer--entidades-e-value-objects)
3. [Application Layer — Fluxos de Autenticação](#3-application-layer--fluxos-de-autenticação)
4. [Infrastructure — Serviços de Segurança](#4-infrastructure--serviços-de-segurança)
5. [Infrastructure — Repositórios](#5-infrastructure--repositórios)
6. [API Layer](#6-api-layer)
7. [Frontend](#7-frontend)
8. [Testes](#8-testes)
9. [Bibliotecas e Dependências](#9-bibliotecas-e-dependências)
10. [Banco de Dados — Placement PostgreSQL vs Analítico](#10-banco-de-dados--placement-postgresql-vs-analítico)
11. [Conformidade com CLAUDE.md e copilot-instructions.md](#11-conformidade-com-claudemd-e-copilot-instructionsmd)
12. [Plano de Correção por Prioridade](#12-plano-de-correção-por-prioridade)

---

## 1. Estrutura do Módulo

```
src/modules/identityaccess/
├── NexTraceOne.IdentityAccess.Domain/
│   ├── Entities/         ← User, Tenant, Session, Role, BreakGlassRequest, Delegation, ...
│   ├── ValueObjects/     ← HashedPassword, Email, RefreshTokenHash, FullName, ...
│   ├── Enums/
│   ├── Errors/
│   └── Events/
├── NexTraceOne.IdentityAccess.Application/
│   ├── Features/         ← ~40 features (LocalLogin, RefreshToken, MFA, OIDC, ...)
│   ├── Abstractions/     ← IUserRepository, IJwtTokenGenerator, IPasswordHasher, ...
│   └── Services/
├── NexTraceOne.IdentityAccess.Infrastructure/
│   ├── Services/         ← JwtTokenGenerator, Pbkdf2PasswordHasher, TotpVerifier, ...
│   ├── Persistence/      ← IdentityDbContext, Repositories, Configurations
│   ├── Authorization/
│   └── Context/
├── NexTraceOne.IdentityAccess.API/
│   └── Endpoints/
└── NexTraceOne.IdentityAccess.Contracts/
```

A estrutura em single-context é adequada para um módulo de identidade centralizado. A separação de serviços via interfaces (ILoginSessionCreator, ILoginResponseBuilder, ISecurityAuditRecorder) é uma boa aplicação de SRP/DIP.

---

## 2. Domain Layer — Entidades e Value Objects

### Aspectos correctos

- `User` estende `AggregateRoot<UserId>` com `UserId.New()` e `UserId.From()` — correcto
- `HashedPassword` encapsula PBKDF2-SHA256 com salt aleatório de 16 bytes e 100.000 iterações
- `RefreshTokenHash` usa SHA-256 — refresh token NUNCA armazenado em claro
- `PasswordResetToken` armazena `TokenHash` — token de reset NUNCA armazenado em claro
- `BreakGlassRequest` tem janela de 2h, limite trimestral de 3 usos, post-mortem obrigatório
- `Session.Rotate()` substitui o hash do refresh token (invalidando o anterior por substituição)
- `User.RegisterFailedLogin()` com lockout após 5 tentativas — correcto

---

### [P0-IA-001] `User.MfaSecret` armazenado em texto simples na base de dados

**Ficheiro:** `Domain/Entities/User.cs:46` e `Infrastructure/Persistence/Configurations/UserConfiguration.cs:52`  
**Severidade:** P0

```csharp
// User.cs:
public string? MfaSecret { get; private set; }

// UserConfiguration.cs:
builder.Property(x => x.MfaSecret).HasMaxLength(256); // sem [EncryptedField]
```

O `MfaSecret` é o secret TOTP base32 do utilizador. Se a base de dados for comprometida, **todos os segredos TOTP ficam expostos em claro**, permitindo ao atacante gerar códigos MFA válidos para todos os utilizadores indefinidamente.

O sistema já possui `[EncryptedField]` e `EncryptedStringConverter` (AES-256-GCM) utilizados noutros módulos — deve ser aplicado aqui.

**Correcção:**
```csharp
// UserConfiguration.cs:
builder.Property(x => x.MfaSecret)
    .HasMaxLength(512)  // aumentar para acomodar ciphertext
    .HasConversion<EncryptedStringConverter>(); // ou adicionar [EncryptedField] ao domínio
```

---

### [P0-IA-002] `ChangePassword` não revoga sessões activas

**Ficheiro:** `Application/Features/ChangePassword/ChangePassword.cs`  
**Severidade:** P0 — violação de segurança de sessão

Após mudança de password, todas as sessões existentes permanecem válidas. Um atacante que comprometeu um refresh token mantém acesso indefinidamente mesmo após a vítima mudar a password.

```csharp
// Actual — handler não acede a sessionRepository:
public sealed class Handler(
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISecurityEventRepository securityEventRepository,
    ISecurityEventTracker securityEventTracker,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
```

**Correcção:**
```csharp
// Adicionar ISessionRepository ao handler e revogar todas as sessões:
var sessions = await sessionRepository.ListActiveByUserIdAsync(UserId.From(userId), cancellationToken);
foreach (var session in sessions)
    session.Revoke(dateTimeProvider.UtcNow);

user.SetPassword(HashedPassword.FromHash(passwordHasher.Hash(request.NewPassword)));
```

O mesmo problema existe em `ResetPassword` — verificar e aplicar a mesma correcção.

---

### [P0-IA-003] OIDC state parameter — nonce não validado server-side (CSRF incompleto)

**Ficheiro:** `Application/Features/StartOidcLogin/StartOidcLogin.cs:116-122` e `Application/Features/OidcCallback/OidcCallback.cs:255-274`  
**Severidade:** P0 — CSRF no fluxo OIDC

`StartOidcLogin` gera o state como `Base64("{nonce}:{returnTo}")` onde `nonce = Guid.NewGuid().ToString("N")`. O nonce **não é armazenado server-side** para verificação posterior.

No callback, `ExtractReturnToFromState` apenas decodifica o Base64 e extrai o `returnTo` — não verifica que o nonce foi realmente gerado pelo servidor para esta sessão:

```csharp
// OidcCallback.cs:255 — apenas extrai, não valida:
private static string ExtractReturnToFromState(string state)
{
    // Decodifica e extrai returnTo — nonce ignorado para validação
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(state));
    var colonIndex = decoded.IndexOf(':');
    // ...
}
```

Um atacante pode fabricar um state válido (`Base64("{qualquerNonce}:{url_maliciosa}")`), injectar na URL de callback, e executar um login CSRF que autentica a vítima na conta do atacante (CSRF login / account takeover).

A documentação refere "O state é vinculado à sessão do browser via cookie seguro no middleware" mas não foi encontrado middleware correspondente no DI.

**Correcção:**
```csharp
// StartOidcLogin: armazenar nonce em cache distribuído com TTL 5 min:
await cache.SetStringAsync($"oidc-nonce:{nonce}", "1", new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
}, cancellationToken);

// OidcCallback: verificar e remover nonce:
var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(state));
var nonce = decoded[..colonIndex];
var storedNonce = await cache.GetStringAsync($"oidc-nonce:{nonce}", cancellationToken);
if (storedNonce is null)
    return IdentityErrors.OidcCallbackFailed("state_invalid"); // CSRF detectado
await cache.RemoveAsync($"oidc-nonce:{nonce}", cancellationToken); // one-time use
```

---

### [P2-IA-004] `User.MfaSecret` documentado como "BCrypt" mas usa PBKDF2

**Ficheiro:** `Domain/Entities/User.cs:24` e `User.cs:54`  
**Severidade:** P2 — documentação incorrecta

```csharp
/// <summary>Hash BCrypt da senha local, quando existir.</summary>
public HashedPassword? PasswordHash { get; private set; }

/// <summary>Cria um usuário local com senha armazenada em BCrypt.</summary>
public static User CreateLocal(...) { }
```

A implementação usa PBKDF2-SHA256 (`Rfc2898DeriveBytes.Pbkdf2`) — não BCrypt. Documentação enganosa pode induzir erros em revisões de segurança.

**Correcção:** Actualizar os XML doc comments para "PBKDF2-SHA256 com 100.000 iterações".

---

### [P2-IA-005] `RowVersion` com setter público em múltiplas entidades

**Ficheiros:** `User.cs:148`, `Session.cs:70`, `BreakGlassRequest.cs:78`, `Tenant.cs` (confirmar), `Delegation.cs`, `TenantMembership.cs`  
**Severidade:** P2

```csharp
// Actual (bug em todas):
public uint RowVersion { get; set; }

// Correcto:
public uint RowVersion { get; internal set; }
```

---

### [P1-IA-006] Delegação não valida que permissões delegadas são subconjunto das do delegante

**Ficheiro:** `Domain/Entities/Delegation.cs`  
**Severidade:** P1

A documentação da entidade diz: *"Não é permitido delegar permissão que o delegante não possui"* e *"Não é permitido delegar permissão de administração de sistema"*. Mas o `Create()` não valida estas regras — apenas verifica que delegante != delegatário, permissões não vazias, e prazo válido.

**Correcção:** No handler `CreateDelegation`, antes de criar a entidade:
```csharp
// Verificar permissões do delegante:
var grantorPermissions = await permissionResolver.ResolveAsync(grantorId, tenantId, ct);
var invalidPerms = permissions.Except(grantorPermissions).ToList();
if (invalidPerms.Any())
    return IdentityErrors.CannotDelegatePermissionsNotOwned(invalidPerms);

// Bloquear delegação de PlatformAdmin:
if (permissions.Any(p => p.Contains("PlatformAdmin") || p.Contains("admin:platform")))
    return IdentityErrors.CannotDelegateAdminPermissions();
```

---

## 3. Application Layer — Fluxos de Autenticação

### [P1-IA-007] TOTP — sem protecção contra replay de código

**Ficheiro:** `Infrastructure/Services/TotpVerifier.cs` e `Application/Features/VerifyMfaChallenge/VerifyMfaChallenge.cs`  
**Severidade:** P1 — replay attack

O `TotpVerifier.Verify` aceita qualquer código válido na janela ±1 step (90 segundos). O mesmo código TOTP pode ser usado múltiplas vezes dentro dessa janela. RFC 6238 recomenda rastrear o último step utilizado para prevenir replay.

```csharp
// TotpVerifier.cs:46 — sem tracking de código já usado:
for (var offset = -1; offset <= 1; offset++)
{
    if (ComputeHotp(keyBytes, (ulong)(counter + offset)) == code)
        return true; // não regista que este código foi consumido
}
```

**Correcção:**
```csharp
// Armazenar o último step válido em cache:
var cacheKey = $"totp-used:{userId}:{counter}";
if (await cache.GetStringAsync(cacheKey) is not null)
    return false; // código já utilizado neste step

// após validação bem sucedida:
await cache.SetStringAsync(cacheKey, "1", new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(90)
});
```

---

### [P1-IA-008] `PasswordResetToken` e `AccountActivationToken` usam SHA-256 sem salt

**Ficheiros:** `Domain/Entities/PasswordResetToken.cs`, `Domain/Entities/AccountActivationToken.cs`  
**Severidade:** P1

Os tokens de reset de password e activação de conta são hashed com SHA-256 puro (sem salt, sem iterações). As passwords usam PBKDF2 com 100.000 iterações — os tokens de curta duração devem usar pelo menos um hash seguro com salt (HMAC-SHA256) para dificultar rainbow tables.

```csharp
// PasswordResetToken.cs — hash SHA-256 sem salt:
public string TokenHash { get; private set; } = string.Empty;
// (o hash é calculado via SHA256.HashData em ForgotPassword.cs)
```

**Correcção:**
Criar um Value Object `SecureTokenHash` com HMAC-SHA256 usando uma chave de aplicação:
```csharp
public static string HashToken(string token, string appKey)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appKey));
    return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
}
```

---

### [P1-IA-009] PBKDF2 com 100.000 iterações — abaixo da recomendação OWASP 2023

**Ficheiro:** `Domain/ValueObjects/HashedPassword.cs:34`  
**Severidade:** P1

```csharp
var hash = Rfc2898DeriveBytes.Pbkdf2(
    Encoding.UTF8.GetBytes(normalized),
    salt,
    100_000,   // OWASP 2023 recomenda 600.000 para PBKDF2-HMAC-SHA256
    HashAlgorithmName.SHA256,
    32);
```

OWASP Password Storage Cheat Sheet (2023) recomenda 600.000 iterações para PBKDF2-HMAC-SHA256, ou migração para Argon2id (mais resistente a ataques com hardware especializado).

**Correcção (sem migração imediata):**
1. Aumentar para 600.000 iterações e adicionar versioning (`v2.`) para migração gradual
2. Na verificação, detectar versão pelo prefixo:
   - `v1.` → 100k iterações (legacy, aceitar mas forçar re-hash no próximo login)
   - `v2.` → 600k iterações

---

### [P1-IA-010] Refresh token rotation sem detecção de reutilização (theft detection)

**Ficheiro:** `Domain/Entities/Session.cs:55-61` e `Application/Features/RefreshToken/RefreshToken.cs`  
**Severidade:** P1

A rotação de refresh token é implementada correctamente (substituição do hash). Porém, não há **detecção de reutilização de token revogado/anterior**: se um atacante rouba T1 e o utiliza *depois* da vítima já ter rotacionado para T2, o servidor retorna simplesmente `InvalidRefreshToken` — sem alertar o utilizador que o seu token foi usado por outra entidade.

OWASP recomenda armazenar o hash anterior (`PreviousRefreshTokenHash`) na sessão e, se apresentado, revogar todos os tokens do utilizador e emitir alerta de segurança.

**Correcção:**
```csharp
// Session.cs — adicionar campo:
public RefreshTokenHash? PreviousRefreshToken { get; private set; }

// Rotate():
PreviousRefreshToken = RefreshToken;
RefreshToken = Guard.Against.Null(refreshToken);
ExpiresAt = expiresAt;

// RefreshToken.cs handler — verificar reuso:
var previousSession = await sessionRepository.GetByPreviousTokenHashAsync(refreshTokenHash, ct);
if (previousSession is not null)
{
    // Token já rotacionado — possível roubo; revogar todas as sessões do user
    await RevokeAllUserSessionsAsync(previousSession.UserId, ct);
    auditRecorder.RecordRefreshTokenReuse(tenantId, previousSession.UserId, request.IpAddress);
    return IdentityErrors.SessionRevoked(previousSession.Id.Value);
}
```

---

### [P1-IA-011] JWT signing key — mínimo de 128 bits aceite (deve ser 256 bits)

**Ficheiro:** `Infrastructure/Services/JwtTokenGenerator.cs:109-118`  
**Severidade:** P1

```csharp
private static string ValidateSigningKey(string key)
{
    var keyBytes = Encoding.UTF8.GetByteCount(key);
    if (keyBytes < 16)  // aceita 128 bits — deveria ser 256 bits (32 bytes)
        throw new InvalidOperationException(...);
    return key;
}
```

Para HS256, NIST SP 800-117 e OWASP recomendam chave mínima de 256 bits (32 bytes). Uma chave de 16 bytes reduz a segurança efectiva do HMAC-SHA256.

**Correcção:**
```csharp
if (keyBytes < 32)
    throw new InvalidOperationException(
        $"JWT signing key is too short ({keyBytes * 8} bits). " +
        "HS256 requires at least 256 bits (32 bytes) per NIST SP 800-117.");
```

---

### [P1-IA-012] `CreateUser` aceita `TenantId` e `RoleId` do body sem validar tenant do caller

**Ficheiro:** `Application/Features/CreateUser/CreateUser.cs`  
**Severidade:** P1

```csharp
public sealed record Command(
    string Email,
    ...
    Guid TenantId,   // do body — qualquer admin pode criar user em qualquer tenant
    Guid RoleId,     // do body — pode atribuir qualquer role
    ...
```

O handler não verifica que o `currentTenant.Id == request.TenantId`. Um administrador de Tenant A pode criar utilizadores em Tenant B.

**Correcção:**
```csharp
// No handler, verificar que o admin só cria no seu tenant:
if (currentTenant.Id != request.TenantId)
    return IdentityErrors.CrossTenantOperationNotAllowed();
```

---

### [P2-IA-013] `ChangePassword` e `ResetPassword` não revogam sessões (P0 já listado para ChangePassword)

**Ficheiro:** `Application/Features/ResetPassword/ResetPassword.cs`  
**Severidade:** P2 (P0 para ChangePassword — ver IA-002)

`ResetPassword` também não revoga sessões após reset bem sucedido. Aplicar a mesma correcção de IA-002.

---

### [P2-IA-014] OidcCallback auto-provisiona membership sem verificar se tenant permite registo livre

**Ficheiro:** `Application/Features/OidcCallback/OidcCallback.cs:146-153`  
**Severidade:** P2

```csharp
// Auto-provisiona como Viewer sem verificar política de registo do tenant:
membership = await AutoProvisionMembershipAsync(user.Id, cancellationToken);
```

Um utilizador com conta OIDC pode aceder a qualquer tenant que tenha OIDC configurado, sem aprovação explícita.

**Correcção:** Verificar propriedade `AllowSelfServiceRegistration` no Tenant antes de auto-provisionar. Se `false`, retornar erro e notificar o admin.

---

## 4. Infrastructure — Serviços de Segurança

### `JwtTokenGenerator` — Aspectos correctos

- Tokens não contêm permissões em claro (resolução server-side via `IClaimsTransformation`) — correcto
- Token sem PasswordHash, MfaSecret ou dados sensíveis no payload — correcto
- `capabilities` claim opcional e controlado pelo SaaS licensing — correcto

### `MfaChallengeTokenService` — Aspectos correctos

- HMAC-SHA256 com chave derivada (`_signingKey + ":mfa-challenge"`) — correcto
- `CryptographicOperations.FixedTimeEquals` para comparação constante — correcto
- TTL de 5 minutos — correcto

### `Pbkdf2PasswordHasher` — Aspectos correctos

- Delega para `HashedPassword` value object — correcto
- Não expõe a lógica de hashing fora do domínio — correcto

### `TotpVerifier` — Aspectos correctos

- Implementação RFC 4226/6238 correcta (HMAC-SHA1, dynamic truncation) — correcto
- Janela ±1 step para tolerância de relógio — correcto
- Falta apenas rastreamento de código utilizado (ver IA-007)

---

## 5. Infrastructure — Repositórios

### [P1-IA-015] `SessionRepository.ListActiveByUserIdAsync` não filtra sessões expiradas

**Ficheiro:** `Infrastructure/Persistence/Repositories/SessionRepository.cs:29-33`  
**Severidade:** P1

```csharp
// Actual — retorna sessões revogadas=null mas não verifica ExpiresAt:
.Where(x => x.UserId == userId && x.RevokedAt == null)

// Correcto:
.Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > dateTimeProvider.UtcNow)
```

Sessões expiradas (mas não revogadas explicitamente) aparecem como "activas" na listagem de sessões do utilizador — experiência enganosa e potencial uso indevido.

---

### Repositórios — sem filtro de tenant (correcto para IdentityAccess)

O módulo IdentityAccess é o único em que `UserRepository`, `RoleRepository`, `SessionRepository` operarem sem filtro de TenantId é **correcto por design** — utilizadores são entidades globais que pertencem a múltiplos tenants via `TenantMembership`. O tenant filtering é feito ao nível de `TenantMembershipRepository`.

---

## 6. API Layer

### [P2-IA-016] Endpoints de reset e activação de conta sem rate limiting explícito documentado

**Severidade:** P2

Os endpoints `/auth/forgot-password` e `/auth/activate` são públicos (`IPublicRequest`). Devem ter rate limiting agressivo (máx. 3 tentativas por email por hora). Verificar que o middleware de rate limiting está activo para estes endpoints.

---

### [P3-IA-017] Endpoint Break Glass `/auth/break-glass/revoke` deve exigir role específico

**Severidade:** P3

A revogação de Break Glass deve exigir a permissão `breakglass:revoke` ou role `PlatformAdmin`. Verificar que o endpoint usa `RequirePermission` e não apenas autenticação genérica.

---

## 7. Frontend

### [P2-IA-018] Tokens e contexto de sessão armazenados em memória — boa prática

**Ficheiro:** `src/frontend/src/utils/tokenStorage.ts`

**Aspectos correctos:**
- Access token em variável de módulo (memória) — não persiste entre reloads
- Refresh token em memória — nunca em localStorage
- `migrateFromLocalStorage()` para remover tokens legados

**Problema residual documentado pelo próprio código:**
- `tenantId` e `userId` em `sessionStorage` — acessível via XSS na mesma origem
- `hasActiveSession()` retorna `true` se apenas `tenantId` está em sessionStorage sem access token

**Correcção para eliminar sessionStorage:** Armazenar tenantId/userId apenas em memória e aceitar que o utilizador re-autentica ao recarregar a página (padrão mais seguro). Alternativamente, migrar para httpOnly cookie com flag Secure e SameSite=Strict no backend.

---

### [P2-IA-019] `LoginPage.tsx` usa `react-router-dom` em vez de TanStack Router

**Ficheiro:** `src/frontend/src/features/identity-access/pages/LoginPage.tsx:2`

```tsx
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
```

Desvio arquitectural — o projecto usa TanStack Router. Migrar para `@tanstack/react-router`.

---

### [P3-IA-020] Formulário de login não limpa password do estado do form em caso de erro de servidor

**Ficheiro:** `src/frontend/src/features/identity-access/pages/LoginPage.tsx:59-61`

```tsx
const clearSensitiveState = useCallback(() => {
    reset({ email: '', password: '' }); // chamado apenas em sucesso (linha 80)
}, [reset]);
```

Em caso de falha de servidor (`catch`), a password permanece no estado do formulário React Hook Form. Deve ser limpa imediatamente após qualquer tentativa de login.

---

## 8. Testes

### [P2-IA-021] Gaps de cobertura em fluxos críticos de segurança

**Severidade:** P2

**Testes existentes (87 ficheiros):** Boa cobertura de features básicas (LocalLogin, ActivateAccount, CreateTenant, etc.)

**Sem cobertura ou cobertura incompleta:**

| Cenário | Ficheiro sugerido |
|---|---|
| MFA replay — mesmo código aceite duas vezes | `VerifyMfaChallengeTests.cs` |
| ChangePassword não revoga sessões (após fix) | `ChangePasswordTests.cs` |
| OIDC state inválido retorna erro CSRF | `OidcCallbackTests.cs` |
| Refresh token após sessão revogada | `RefreshTokenTests.cs` |
| CreateUser com TenantId diferente do caller | `CreateUserTests.cs` |
| Delegação com permissão que delegante não possui | `CreateDelegationTests.cs` |
| BreakGlass após atingir limite trimestral | `RequestBreakGlassTests.cs` |
| Sessões expiradas não listadas em ListActiveSessions | `SessionRepositoryTests.cs` |

---

## 9. Bibliotecas e Dependências

| Biblioteca | Status 2026 | Notas |
|---|---|---|
| `System.IdentityModel.Tokens.Jwt` | ✅ Adequado | JWT padrão Microsoft |
| `Microsoft.IdentityModel.Tokens` | ✅ Adequado | |
| `Rfc2898DeriveBytes.Pbkdf2` (.NET 10) | ⚠️ Aceite com ressalvas | 100k iterações (OWASP recomenda 600k) |
| `HMACSHA256` (System.Security.Cryptography) | ✅ Adequado | Usado em MfaChallengeToken e OIDC state |
| `HMACSHA1` (TotpVerifier) | ⚠️ Aceitável | RFC 6238 usa SHA-1 para TOTP — standard; SHA-256 variant preferível |
| `Ardalis.GuardClauses` | ✅ Adequado | |
| `react-hook-form` + `zod` (frontend) | ✅ Adequado | Validação de formulários robusta |
| `@tanstack/react-router` | ⚠️ Parcial | LoginPage.tsx ainda usa react-router-dom |

### Recomendações para 2026

- Considerar migração de PBKDF2 para **Argon2id** (mais resistente a ASIC/GPU): biblioteca `Konscious.Security.Cryptography.Argon2` ou aguardar suporte nativo .NET
- Adicionar `Content-Security-Policy` header no API Host para mitigar XSS frontend
- Implementar FIDO2/WebAuthn (Passkeys) como alternativa ao TOTP — RFC 4226/TOTP é vulnerable a phishing; WebAuthn é phishing-resistant

---

## 10. Banco de Dados — Placement PostgreSQL vs Analítico

### Entidades em PostgreSQL (correcto)

| Tabela | Justificação |
|---|---|
| `iam_users` | Transaccional — autenticação, lockout, profile |
| `iam_tenants` | Transaccional — provisionamento, licenciamento |
| `iam_sessions` | Transaccional — refresh token, revogação |
| `iam_tenant_memberships` | Transaccional — RBAC por tenant |
| `iam_roles`, `iam_permissions` | Configuração — relacional |
| `iam_password_reset_tokens` | Transaccional — token único, expiração |
| `iam_break_glass_requests` | Auditoria operacional — rastreamento de incidentes |
| `iam_delegations` | Transaccional — ciclo de vida de delegação |

### Candidatos para Analytics (ClickHouse/Elasticsearch)

| Dado | Placement actual | Recomendação |
|---|---|---|
| `iam_security_events` | PostgreSQL | Candidato a Elasticsearch para queries de anomalia em grandes volumes (SIEM) |
| Histórico de logins (`last_login_at`) | Campo em `iam_users` | Extrair para time-series para análise de padrão de acesso |
| `iam_agent_query_records` | PostgreSQL | Volume elevado — candidato a ClickHouse para analytics de uso de agentes |

O módulo usa correctamente `IAnalyticsWriter` para eventos que vão para ClickHouse/Elasticsearch, sem acoplar o domínio ao storage analítico.

---

## 11. Conformidade com CLAUDE.md e copilot-instructions.md

| Regra | Estado | Notas |
|---|---|---|
| Commandos com `ICommand<Response>` | ✅ Compliant | |
| Handlers como `ICommandHandler<C,R>` | ✅ Compliant | |
| `IPublicRequest` em endpoints públicos | ✅ Compliant | LocalLogin, RefreshToken, OidcCallback |
| Result<T> / Error sem excepções | ✅ Compliant | |
| ID fortemente tipado com `New()` e `From()` | ✅ Compliant | |
| `AuditableEntity` para entidades auditáveis | ✅ Parcial | Session estende AggregateRoot, não AuditableEntity |
| XML docs em Português | ⚠️ Parcial | PasswordResetToken sem docs; User docs dizem "BCrypt" incorrectamente |
| TenantId como Guid | ✅ Compliant | (IAM é cross-tenant por design) |
| Repositórios com filtro de tenant | ✅ N/A | IAM opera sem tenant para entidades globais |
| RowVersion com setter restrito | ❌ Falha | Público em User, Session, BreakGlassRequest, Delegation |
| Passwords NUNCA em logs | ✅ Compliant | Command recebe password, handler nunca loga |
| Frontend i18n | ✅ Compliant | LoginPage usa useTranslation |
| TanStack Router | ❌ Parcial | LoginPage.tsx usa react-router-dom |
| Backend como autoridade final | ✅ Compliant | Permissões avaliadas server-side |
| MfaSecret encriptado | ❌ Falha | Sem [EncryptedField] |

---

## 12. Plano de Correção por Prioridade

### P0 — Bloqueadores de Segurança (resolver imediatamente)

| ID | Título | Esforço |
|---|---|---|
| P0-IA-001 | MfaSecret em claro — adicionar EncryptedField | Médio (migration + converter) |
| P0-IA-002 | ChangePassword não revoga sessões | Baixo |
| P0-IA-003 | OIDC state nonce não validado server-side (CSRF) | Médio (requer cache distribuído) |

### P1 — Alta Prioridade (próximo sprint)

| ID | Título | Esforço |
|---|---|---|
| P1-IA-006 | Delegação sem validação de escalada de permissões | Médio |
| P1-IA-007 | TOTP sem replay protection | Médio |
| P1-IA-008 | PasswordResetToken e ActivationToken: SHA-256 sem salt | Médio |
| P1-IA-009 | PBKDF2 100k iterações → 600k (OWASP 2023) | Médio + migration |
| P1-IA-010 | Refresh token sem theft detection | Alto |
| P1-IA-011 | JWT signing key mínimo: 128 → 256 bits | Baixo |
| P1-IA-012 | CreateUser aceita TenantId cross-tenant | Baixo |
| P1-IA-015 | SessionRepository.ListActiveByUserIdAsync não filtra expiradas | Baixo |

### P2 — Média Prioridade (backlog prioritário)

| ID | Título | Esforço |
|---|---|---|
| P2-IA-004 | Docs User.cs incorrectos (BCrypt → PBKDF2) | Baixo |
| P2-IA-005 | RowVersion setter público em User, Session, BreakGlass, Delegation | Baixo |
| P2-IA-013 | ResetPassword não revoga sessões | Baixo |
| P2-IA-014 | OidcCallback auto-provisiona sem verificar política do tenant | Médio |
| P2-IA-018 | tokenStorage.ts: tenantId/userId em sessionStorage | Médio (backend httpOnly cookie) |
| P2-IA-019 | LoginPage.tsx usa react-router-dom | Baixo |
| P2-IA-021 | Gaps de testes em fluxos de segurança críticos | Alto |

### P3 — Baixa Prioridade

| ID | Título | Esforço |
|---|---|---|
| P3-IA-016 | Rate limiting em endpoints de reset/activação | Baixo |
| P3-IA-017 | Break Glass revoke: verificar permissão específica | Baixo |
| P3-IA-020 | LoginPage: limpar password em erro de servidor | Baixo |

---

## Apêndice — Ficheiros-Chave Analisados

| Ficheiro | Problemas identificados |
|---|---|
| `Domain/Entities/User.cs` | P0-IA-001 (MfaSecret), P2-IA-004 (docs), P2-IA-005 (RowVersion) |
| `Domain/Entities/Session.cs` | P2-IA-005 (RowVersion), P1-IA-010 (theft detection) |
| `Domain/Entities/BreakGlassRequest.cs` | P2-IA-005 (RowVersion) |
| `Domain/Entities/Delegation.cs` | P2-IA-005, P1-IA-006 (escalada) |
| `Domain/Entities/PasswordResetToken.cs` | P1-IA-008 (SHA-256 sem salt) |
| `Domain/ValueObjects/HashedPassword.cs` | P1-IA-009 (100k iterações) |
| `Application/Features/LocalLogin/LocalLogin.cs` | Correcto — bem estruturado |
| `Application/Features/RefreshToken/RefreshToken.cs` | P1-IA-010 (theft detection) |
| `Application/Features/ChangePassword/ChangePassword.cs` | P0-IA-002 (sem revogação) |
| `Application/Features/VerifyMfaChallenge/VerifyMfaChallenge.cs` | P1-IA-007 (replay) |
| `Application/Features/OidcCallback/OidcCallback.cs` | P0-IA-003 (CSRF), P2-IA-014 |
| `Application/Features/CreateUser/CreateUser.cs` | P1-IA-012 (cross-tenant) |
| `Infrastructure/Services/JwtTokenGenerator.cs` | P1-IA-011 (key length) |
| `Infrastructure/Services/TotpVerifier.cs` | P1-IA-007 (replay) |
| `Infrastructure/Services/Pbkdf2PasswordHasher.cs` | Correcto — delega para HashedPassword |
| `Infrastructure/Services/MfaChallengeTokenService.cs` | Correcto — HMAC-SHA256 |
| `Infrastructure/Persistence/Configurations/UserConfiguration.cs` | P0-IA-001 (sem EncryptedField) |
| `Infrastructure/Persistence/Repositories/SessionRepository.cs` | P1-IA-015 (expiradas) |
| `src/frontend/src/utils/tokenStorage.ts` | P2-IA-018 (sessionStorage) |
| `src/frontend/src/features/identity-access/pages/LoginPage.tsx` | P2-IA-019, P3-IA-020 |
