# Relatório de Auditoria — Autenticação

> **Módulo:** `src/modules/identityaccess/`  
> **Data da análise:** 2025-07  
> **Classificação:** ENTERPRISE_READY_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O subsistema de autenticação do NexTraceOne implementa uma arquitectura híbrida com três mecanismos complementares: JWT Bearer (primário para utilizadores), API Key (sistema-a-sistema) e OIDC Federation (login federado). A autenticação local utiliza BCrypt para hashing de palavras-passe, com bloqueio de conta após 5 tentativas falhadas. A gestão de sessões inclui rotação de refresh tokens com hash SHA-256, e a cookie session possui protecção CSRF integrada. O rate limiting é aplicado a todos os endpoints de autenticação.

A classificação ENTERPRISE_READY_APPARENT reflecte uma implementação madura com lacunas conhecidas e documentadas, nomeadamente o enforcement de MFA diferido e o armazenamento de API keys em memória.

---

## 2. Mecanismos de Autenticação

### 2.1 JWT Bearer (Autenticação Primária)

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Algoritmo de assinatura | HMAC-SHA256 | ✅ Adequado para single-issuer |
| Tempo de expiração | 60 minutos | ✅ Razoável |
| Claims incluídos | `sub`, `email`, `name`, `tenant_id`, `role_id`, `permissions` | ✅ Completo |
| Validação de issuer | Sim | ✅ |
| Validação de audience | Sim | ✅ |
| Validação de lifetime | Sim | ✅ |
| Validação de signing key | Sim | ✅ |
| Chave de fallback dev | Existe | ⚠️ Validação de arranque impede uso em produção |

**Evidência:** `JwtTokenService` no projecto `IdentityAccess.Infrastructure`, configuração de autenticação em `Program.cs` (`AddAuthentication`).

**Recomendação:** Considerar migração para RS256 (chave assimétrica) para cenários multi-serviço, permitindo validação sem partilha de chave secreta.

### 2.2 API Key (Sistema-a-Sistema)

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Header | `X-Api-Key` | ✅ Padrão da indústria |
| Comparação | `CryptographicOperations.FixedTimeEquals()` | ✅ Resistente a timing attacks |
| Armazenamento | In-memory via `appsettings.json` | ⚠️ MVP1 — necessita migração |
| Rotação | Não implementada | ❌ Necessário para produção |
| Scoping | Não implementado | ⚠️ Todas as API keys têm acesso total |

**Evidência:** Handler de API Key no `BuildingBlocks.Security`.

**Recomendações:**
1. Migrar armazenamento para BD com encriptação at-rest
2. Implementar rotação de API keys com período de sobreposição
3. Adicionar scoping de permissões por API key

### 2.3 OIDC Federation

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Interface | `IOidcProvider` → `OidcProviderService` | ✅ Abstracto e extensível |
| Fluxo | Authorization Code + PKCE | ✅ Best practice |
| Protecção CSRF | Validação de state | ✅ |
| Armazenamento de tokens do provider | Nunca armazenados | ✅ Minimização de dados |
| Configuração | Per-tenant | ✅ Multi-tenancy nativa |
| Endpoints | 3 endpoints AllowAnonymous + rate-limited | ✅ |

**Endpoints OIDC:**

| Endpoint | Método | Protecção |
|---|---|---|
| `/auth/federated` | POST | AllowAnonymous + rate-limited |
| `/auth/oidc/start` | GET/POST | AllowAnonymous + rate-limited |
| `/auth/oidc/callback` | GET | AllowAnonymous + rate-limited |

**Evidência:** `OidcProviderService`, endpoints em `AuthEndpoints`.

**Lacuna identificada:** SAML não implementado. Organizações enterprise com infraestrutura ADFS/SAML legacy não podem federar directamente.

### 2.4 Hybrid PolicyScheme

O esquema "smart" de roteamento avalia a presença do header `X-Api-Key`:
- Se presente → roteia para handler de API Key
- Se ausente → roteia para JWT Bearer

**Avaliação:** ✅ Elegante e transparente para os endpoints.

---

## 3. Autenticação Local

### 3.1 Hashing de Palavras-passe

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Algoritmo | BCrypt (`Pbkdf2PasswordHasher`) | ✅ Resistente a GPU attacks |
| Salt | Automático (BCrypt nativo) | ✅ |
| Work factor | Padrão da biblioteca | ✅ Verificar ajuste periódico |

### 3.2 Bloqueio de Conta

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Tentativas antes de bloqueio | 5 | ✅ |
| Duração do bloqueio | 15 minutos | ✅ |
| Evento de segurança | `AccountLocked` registado | ✅ |

### 3.3 Factory Methods

- `User.CreateLocal(...)` — criação com palavra-passe local
- `User.CreateFederated(...)` — criação sem palavra-passe, vinculada a provider externo

**Avaliação:** ✅ Separação clara de responsabilidades no domínio.

---

## 4. Gestão de Sessões

### 4.1 Entidade Session

| Campo | Propósito | Avaliação |
|---|---|---|
| RefreshTokenHash | SHA-256 do refresh token | ✅ Token nunca armazenado em claro |
| ExpiresAt | Expiração da sessão | ✅ |
| CreatedByIp | IP de criação | ✅ Rastreabilidade |
| UserAgent | Agente do browser/cliente | ✅ Rastreabilidade |
| RevokedAt | Timestamp de revogação | ✅ Soft-revoke |

### 4.2 Rotação de Tokens

No momento do refresh, o token anterior é invalidado e um novo par access/refresh é emitido.

**Avaliação:** ✅ Mitiga risco de token roubado.

### 4.3 Cookie Session

- Sessão opcional baseada em cookie
- Protecção CSRF via middleware `UseCookieSessionCsrfProtection`
- Header `X-Csrf-Token` exigido em operações mutantes (POST/PUT/PATCH/DELETE)

**Avaliação:** ✅ Protecção adequada contra CSRF.

### 4.4 Lacuna: Validação de IP/UserAgent

Os dados de IP e UserAgent são **recolhidos** mas **não validados** durante a sessão. Não existe detecção de session hijacking baseada em mudança de IP ou UserAgent.

**Recomendação:** Implementar validação opcional (configurável por tenant) que alerte ou invalide sessão em caso de mudança significativa de IP/UserAgent.

---

## 5. Política de MFA

### 5.1 Modelo

```csharp
MfaPolicy {
    RequiredOnLogin: bool
    RequiredForPrivilegedOps: bool
    AllowedMethods: [TOTP, WebAuthn, SMS]
}
```

### 5.2 Factory Methods

| Método | RequiredOnLogin | RequiredForPrivilegedOps | AllowedMethods |
|---|---|---|---|
| ForSaaS | true | true | TOTP, WebAuthn |
| ForSelfHosted | true | true | TOTP, WebAuthn, SMS |
| ForOnPremise | false | true | TOTP |
| Disabled | false | false | — |

### 5.3 Estado Actual

| Aspecto | Estado |
|---|---|
| Value object modelado | ✅ Implementado |
| Persistência | ✅ Implementado |
| Enforcement em login | ❌ ADIADO |
| Step-up para operações privilegiadas | ❌ ADIADO |
| UI de configuração MFA | ❌ Não implementada |
| Enrollment TOTP/WebAuthn | ❌ Não implementado |

**Avaliação:** ⚠️ LACUNA SIGNIFICATIVA — A política está correctamente modelada no domínio mas o enforcement não está implementado. Esta é a lacuna de segurança mais relevante do sistema.

**Recomendação PRIORITÁRIA:** Implementar enforcement de MFA como próxima prioridade de segurança:
1. Fluxo de enrollment (TOTP com QR code)
2. Step-up challenge em login
3. Step-up challenge para operações privilegiadas
4. WebAuthn como método preferencial

---

## 6. Rate Limiting

### 6.1 Políticas Aplicáveis à Autenticação

| Política | Limite | Aplicação |
|---|---|---|
| `auth` | 20 pedidos/minuto | Endpoints gerais de autenticação |
| `auth-sensitive` | 10 pedidos/minuto | Endpoints sensíveis (login, password reset) |
| IP não resolvido | 20 pedidos/minuto global | Limite mais restrito |

### 6.2 Comportamento

- Queue handling com FIFO
- IPs não resolvidos recebem limites mais restritos (20 global vs 100 normal)

**Avaliação:** ✅ Protecção adequada contra brute force e abuso.

---

## 7. Pipeline de Autenticação

Posição no middleware pipeline (`Program.cs` linhas 234-245):

```
1. UseResponseCompression
2. UseHttpsRedirection
3. UseCors
4. UseRateLimiter          ← Rate limiting ANTES da autenticação
5. UseSecurityHeaders
6. UseGlobalExceptionHandler
7. UseCookieSessionCsrfProtection  ← CSRF ANTES da autenticação
8. UseAuthentication       ← AUTENTICAÇÃO
9. TenantResolutionMiddleware
10. EnvironmentResolutionMiddleware
11. UseAuthorization
```

**Avaliação:** ✅ Ordem correcta. Rate limiting protege contra abuso antes de qualquer processamento de autenticação.

---

## 8. Matriz de Cobertura

| Cenário | Coberto | Mecanismo |
|---|---|---|
| Login com credenciais locais | ✅ | BCrypt + JWT |
| Login federado OIDC | ✅ | Authorization Code + PKCE |
| Login federado SAML | ❌ | Não implementado |
| Autenticação sistema-a-sistema | ✅ | API Key |
| Refresh de sessão | ✅ | Rotação de tokens |
| Logout | ✅ | Revogação de sessão |
| Bloqueio de conta | ✅ | 5 tentativas / 15 min |
| MFA em login | ❌ | Modelado, enforcement adiado |
| MFA step-up | ❌ | Modelado, enforcement adiado |
| Protecção CSRF | ✅ | Middleware + X-Csrf-Token |
| Rate limiting auth | ✅ | 20/min geral, 10/min sensível |
| Detecção de anomalia de sessão | ⚠️ | Dados recolhidos, enforcement ausente |

---

## 9. Recomendações Priorizadas

### Prioridade CRÍTICA

1. **Implementar MFA enforcement** — enrollment TOTP, step-up em login e operações privilegiadas

### Prioridade ALTA

2. **Migrar API Keys** para BD encriptada com rotação e scoping
3. **Implementar validação de sessão** por IP/UserAgent (detecção de hijacking)

### Prioridade MÉDIA

4. **Adicionar suporte SAML** para federação enterprise completa
5. **Considerar migração para RS256** para cenários multi-serviço
6. **Implementar scoping de API keys** por permissão e tenant

### Prioridade BAIXA

7. **Ajustar work factor BCrypt** periodicamente
8. **Adicionar logs estruturados** para todos os fluxos de autenticação

---

> **Classificação final:** ENTERPRISE_READY_APPARENT — Arquitectura sólida com lacunas conhecidas em MFA enforcement e armazenamento de API keys.
