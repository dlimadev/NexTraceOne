# Relatório de Segurança, Identidade e Acesso — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado de segurança do sistema: autenticação, autorização, isolamento multi-tenant, encriptação, headers HTTP, CSRF, rate limiting, configuração de segredos e vulnerabilidades potenciais.

---

## 2. Problemas CRÍTICOS — Acção Imediata Requerida

### 2.1 Credenciais de Base de Dados Hardcoded

**Ficheiros:**
- `src/platform/NexTraceOne.ApiHost/appsettings.json` (linhas 3–22)
- `src/platform/NexTraceOne.ApiHost/appsettings.Development.json` (linhas 3–22)

**Evidência:** A senha `ouro18` aparece **51+ vezes** entre os dois ficheiros, em 21 connection strings diferentes (uma por DbContext).

```json
"ConnectionStrings": {
  "IdentityDb": "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=ouro18;...",
  "ContractsDb": "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=ouro18;...",
  ...
}
```

**Risco:** Qualquer pessoa com acesso ao repositório tem acesso directo à base de dados de produção se as credenciais não forem overridden.

**Severidade:** CRITICAL

**Correcção:** Remover todas as passwords dos ficheiros de configuração. Usar apenas `${POSTGRES_PASSWORD}` ou variáveis de ambiente.

---

### 2.2 JWT Secret Vazio no Config Principal

**Ficheiro:** `src/platform/NexTraceOne.ApiHost/appsettings.json:34`

```json
"Jwt": {
  "Issuer": "NexTraceOne",
  "Audience": "nextraceone-api",
  "Secret": "",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

**Risco:** Se o env var `JWT_SECRET` não for configurado correctamente, o sistema pode usar o fallback hardcoded (ver 2.3) ou falhar ao iniciar sem mensagem clara.

**Severidade:** CRITICAL

**Correcção:** Remover `"Secret": ""` do ficheiro. Tornar `Jwt:Secret` obrigatório via env var com validação explícita no startup.

---

### 2.3 Fallback JWT Key Hardcoded no Código

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/JwtTokenService.cs:48`

**Evidência:** Chave de fallback `"development-signing-key-development-signing-key-1234567890"` hardcoded no código-fonte.

**Risco:** Atacante com acesso ao código pode forjar JWTs válidos. Esta chave é pública no repositório.

**Severidade:** CRITICAL

**Correcção:** Remover o fallback completamente. Falhar explicitamente no startup se a chave não estiver configurada.

---

### 2.4 Fallback AES Key Hardcoded

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs:113`

**Evidência:** Chave `"NexTraceOne-Development-Only-Key-Not-For-Production"` usada como fallback quando `NEXTRACE_ENCRYPTION_KEY` não está configurado.

**Risco:** Dados encriptados (tokens de integração, credenciais, PII) poderiam ser desencriptados por qualquer um com acesso ao código.

**Severidade:** HIGH

**Correcção:** Remover fallback. Falhar no startup se a chave não estiver definida em ambientes não-Development.

---

## 3. Problemas de Severidade ALTA

### 3.1 CORS com Origens de Desenvolvimento no Config Base

**Ficheiro:** `src/platform/NexTraceOne.ApiHost/appsettings.json:81-84`

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```

**Risco:** Se a ordem de carregamento de config falhar, produção poderia aceitar pedidos de localhost.

**Correcção:** Mover para `appsettings.Development.json`. Base deve ter array vazio.

---

### 3.2 NEXTRACE_SKIP_INTEGRITY em Builds de Segurança

**Ficheiro:** `.github/workflows/security.yml` (linha 160 aprox.)

**Evidência:** Pipeline usa `NEXTRACE_SKIP_INTEGRITY=true` durante builds Docker, desactivando verificação de integridade de assemblies.

**Risco:** O mecanismo de anti-tamper é nullificado no pipeline, tornando-o sem valor real.

**Correcção:** Não usar `NEXTRACE_SKIP_INTEGRITY` no pipeline de segurança. Criar processo alternativo de build sem bypass.

---

### 3.3 RequireSecureCookies false em Development

**Ficheiro:** `src/platform/NexTraceOne.ApiHost/appsettings.Development.json:32`

**Risco:** Se este ficheiro for acidentalmente usado em staging/produção, cookies de sessão não são seguros.

**Correcção:** Adicionar validação de startup que falhe se `RequireSecureCookies=false` em ambiente não-Development.

---

## 4. Implementações de Segurança Positivas

### 4.1 JWT e Autenticação

- ✅ `JwtTokenService` com validação completa: issuer, audience, signature, expiração
- ✅ Clock skew de 1 minuto
- ✅ Refresh token rotation
- ✅ Claims incluem `tenant_id`, `user_id`, permissões granulares
- ✅ Tokens em `sessionStorage` no frontend (não localStorage)
- ✅ Refresh tokens em memória no frontend

### 4.2 Encriptação

- ✅ AES-256-GCM com nonce aleatório — autenticado e seguro
- ✅ `[EncryptedField]` attribute para marcação transparente
- ✅ `EncryptionInterceptor` aplica encriptação no EF Core transparentemente

### 4.3 Hashing de Passwords

- ✅ PBKDF2-SHA256 com 100.000 iterações e salt de 16 bytes
- ⚠️ Requisito de password: apenas >= 8 caracteres, sem complexidade
- **Recomendação:** Adicionar requisito de complexidade (maiúsculas, números, especiais) ou adoptar padrão NIST (>= 12 chars)

### 4.4 Rate Limiting

```
auth: 20 pedidos/min
auth-sensitive: 10 pedidos/min
ai: 30 pedidos/min
data-intensive: 50 pedidos/min
operations: 40 pedidos/min
global: 100 pedidos/min
```

**Estado:** CUMPRIDO mas hardcoded — sem parametrização em runtime

### 4.5 CSRF Protection

- ✅ Double-submit cookie pattern
- ✅ `CryptographicOperations.FixedTimeEquals` (constant-time comparison)
- ✅ Token em headers de POST/PUT/PATCH/DELETE

### 4.6 Security Headers HTTP

Verificados em `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs`:

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 0
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=()
Content-Security-Policy: default-src 'none'; frame-ancestors 'none'
Strict-Transport-Security: max-age=63072000 (2 anos, non-Development)
Cache-Control: no-store
Pragma: no-cache
```

**Estado:** EXCELENTE — headers seguros e completos

### 4.7 Multi-Tenancy

- ✅ `TenantResolutionMiddleware` — resolve de JWT claims (primário) ou header (fallback)
- ✅ `TenantRlsInterceptor` — PostgreSQL RLS aplicado em todos os queries
- ✅ `TenantIsolationBehavior` — verificação de tenant no pipeline MediatR
- ⚠️ Header `X-Tenant-Id` aceito sem JWT — abre superfície de ataque em contextos não autenticados

### 4.8 Autorização

- ✅ RBAC granular com 118+ permissões
- ✅ `PermissionAuthorizationHandler` no backend
- ✅ `ProtectedRoute` no frontend (backend é autoridade)
- ✅ Environment-aware authorization via `ICurrentEnvironment`

### 4.9 Identity Avançado

- ✅ `BreakGlassRequest` — acesso de emergência com audit trail
- ✅ `JitAccessRequest` — just-in-time elevation
- ✅ `Delegation` — delegação de papel
- ✅ `AccessReviewCampaign` — revisões periódicas de acesso
- ✅ `SecurityEvent` — log de eventos de segurança
- ✅ SSO via `ExternalIdentity` e `SsoGroupMapping` (mapeado no schema)

### 4.10 Audit Trail

- ✅ `AuditDbContext` com `AuditEvent` e `AuditChainLink`
- ✅ Hash chain SHA-256 para integridade (cada evento liga ao anterior)
- ✅ `SecurityAuditService` para eventos de segurança
- ✅ `AuditInterceptor` nos DbContexts

---

## 5. Frontend — Segurança

| Verificação | Estado | Evidência |
|-------------|--------|-----------|
| Tokens em sessionStorage | ✅ CUMPRIDO | `src/frontend/src/api/client.ts` |
| Sem tokens em localStorage | ✅ CUMPRIDO | Verificado |
| CSRF token automático | ✅ CUMPRIDO | Axios interceptor |
| Sem dangerouslySetInnerHTML | ✅ NÃO ENCONTRADO | Verificado |
| Sem segredos hardcoded | ✅ CUMPRIDO | Verificado |
| Backend como autoridade de autorizacão | ✅ CUMPRIDO | ProtectedRoute espera dados do servidor |
| Tenant/Environment headers | ✅ CUMPRIDO | X-Tenant-Id, X-Environment-Id |
| withCredentials para cookies | ✅ CUMPRIDO | Axios config |

---

## 6. Resumo de Achados

| Severity | Quantidade | Exemplos |
|----------|-----------|---------|
| CRITICAL | 3 | Credenciais BD, JWT Secret vazio, JWT fallback key |
| HIGH | 4 | AES fallback key, CORS dev em base, SKIP_INTEGRITY, RequireSecureCookies |
| MEDIUM | 4 | Rate limit hardcoded, senha sem complexidade, header tenant sem JWT, log retention |
| POSITIVOS | 13+ | Headers, CSRF, PBKDF2, JWT validation, RLS, audit chain, etc. |

---

## 7. Recomendações por Prioridade

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover senha "ouro18" de todos os appsettings |
| P0 | Remover JWT Secret vazio do appsettings.json |
| P0 | Remover fallback JWT key hardcoded do JwtTokenService |
| P0 | Tornar JWT_SECRET e NEXTRACE_ENCRYPTION_KEY obrigatórios em não-Development |
| P1 | Mover CORS localhost para appsettings.Development.json |
| P1 | Corrigir uso de NEXTRACE_SKIP_INTEGRITY no pipeline de segurança |
| P1 | Adicionar validação de startup para RequireSecureCookies |
| P2 | Adicionar requisito de complexidade de password |
| P2 | Parametrizar rate limits em appsettings |
| P2 | Restringir X-Tenant-Id header apenas a contextos pré-autenticação |
| P3 | Mapear OWASP Top 10 em SECURITY.md |
| P3 | Documentar política de retenção de logs por tipo |
