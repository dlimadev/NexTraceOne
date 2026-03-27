# Relatório de Segurança, Identidade e Acesso — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Postura Geral de Segurança

**Classificação: FORTE**

O NexTraceOne demonstra arquitetura de segurança enterprise com defesa em profundidade. Nenhuma vulnerabilidade crítica encontrada no código auditado.

---

## 2. Autenticação

### JWT Bearer
**Status: READY**

- Chave obrigatória validada no startup: falha com `InvalidOperationException` se `Jwt:Secret` ausente
- Parâmetros: `ValidateIssuer=true`, `ValidateAudience=true`, `ValidateIssuerSigningKey=true`, `ValidateLifetime=true`
- `ClockSkew`: 1 minuto (tolerância razoável)
- Algoritmo: HmacSha256
- Expiração: 60 minutos (access token), 7 dias (refresh token)

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs`

### API Key Authentication
**Status: READY (MVP1 — precisa migrar para banco)**

- Dual-scheme: JWT para utilizadores, API Key para sistemas
- Roteamento por header `X-Api-Key`
- API Keys com: `ClientId`, `ClientName`, `TenantId`, `Permissions`
- **Gap**: Atualmente em appsettings.json — código documenta migração para banco criptografado como próxima etapa

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authentication/ApiKeyAuthenticationOptions.cs`

### Cookie Session
**Status: READY**

- Cookie `nxt_at` para access token
- `HttpOnly=true`, `Secure=true` em produção
- CSRF protection via `nxt_csrf` cookie
- Fallback gracioso para header Authorization quando cookie ausente

---

## 3. Isolamento de Tenant

**Status: READY — 3 camadas**

### Camada 1: JWT Claim (fonte de verdade)
- `tenant_id` claim no JWT
- Prioridade máxima na resolução

### Camada 2: Aplicação (TenantIsolationBehavior)
- Bloqueia requests sem tenant válido
- Bloqueia tenants inativos
- `IPublicRequest` para rotas que dispensam tenant (ex: login)

### Camada 3: Banco de Dados (RLS)
- `SELECT set_config('app.current_tenant_id', @__tenantId, false)` — parametrizado
- PostgreSQL Row-Level Security por sessão
- Soft-delete preserva dados por tenant

**Segurança adicional:** Header `X-Tenant-Id` aceito apenas para utilizadores autenticados — pedidos não autenticados com este header são ignorados.

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/MultiTenancy/TenantResolutionMiddleware.cs`, `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/TenantRlsInterceptor.cs`

---

## 4. Autorização

**Status: READY**

- Permission-based (não apenas role-based)
- `RequirePermission("resource.action")` por endpoint
- Dynamic policy provider — runtime evaluation
- Permissões no JWT claim `permissions`
- Sem regras de autorização hardcoded

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authorization/`

---

## 5. CORS

**Status: READY — Muito Bem Configurado**

- Produção: requer origens explícitas (falha se vazio)
- Wildcard com credentials: proibido (validação no startup)
- Development: `localhost:5173` e `localhost:3000`
- Headers permitidos: `Content-Type`, `Authorization`, `X-Tenant-Id`, `X-Environment-Id`, `X-Correlation-Id`, `X-Csrf-Token`

**Evidência:** `src/platform/NexTraceOne.ApiHost/WebApplicationBuilderExtensions.cs`

---

## 6. Rate Limiting

**Status: READY**

6 políticas diferenciadas por risco de endpoint:

| Policy | Limite | Janela | Proteção contra |
|---|---|---|---|
| Global | 100/IP (20 não-resolvidos) | 1 min | DoS genérico, bypass de proxy |
| Auth | 20/IP | 1 min | Brute force de login |
| AuthSensitive | 10/IP | 1 min | Abuso de registro/OIDC |
| AI | 30/IP | 1 min | Abuso de operações caras |
| DataIntensive | 50/IP | 1 min | Scraping, exportação excessiva |
| Operations | 40/IP | 1 min | Abuso de operações admin |

**Evidência:** `src/platform/NexTraceOne.ApiHost/RateLimitingOptions.cs`

---

## 7. Criptografia

### AES-256-GCM (dados em repouso)
**Status: READY**

- Algoritmo: AES-256-GCM (authenticated encryption)
- Nonce: 12 bytes aleatórios (fresh por operação)
- Tag: 16 bytes (autenticação)
- Formato payload: `[nonce(12) | tag(16) | ciphertext]`
- Chave: variável de ambiente `NEXTRACE_ENCRYPTION_KEY` — obrigatória (falha no startup se ausente)
- Integração EF Core: campos marcados com `[EncryptedField]` são transparentemente criptografados

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Encryption/AesGcmEncryptor.cs`

---

## 8. Integrity Checking

**Status: READY**

- `AssemblyIntegrityChecker.VerifyOrThrow()` no startup
- Controlado por `NEXTRACE_SKIP_INTEGRITY` (false por padrão)
- Previne execução de assemblies modificados/injetados

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Integrity/AssemblyIntegrityChecker.cs`

---

## 9. Identity Access (Módulo Funcional)

**Status: READY — 100% real**

| Capacidade | Estado |
|---|---|
| JWT Auth + RBAC | Real |
| JIT (Just-in-Time) Access | Real |
| Break Glass | Real |
| Access Reviews | Real |
| Delegated Access | Real |
| Security Events | Real (SecurityEventsEndpoints) |
| Environments | Real |
| Multi-tenancy | Real com RLS |
| Session management | Real |

**Evidência:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/`

---

## 10. Middleware Pipeline — Ordem Correta

```
UseCors()
UseRateLimiter()
UseSecurityHeaders()
UseGlobalExceptionHandler()
UseCookieSessionCsrfProtection()
UseAuthentication()
UseMiddleware<TenantResolutionMiddleware>()   ← DEPOIS de UseAuthentication ✅
UseMiddleware<EnvironmentResolutionMiddleware>()
UseAuthorization()
```

**Avaliação:** Ordem correta — TenantResolution após Authentication garante que tenant só é extraído de contexto autenticado.

---

## 11. Segredos — Verificação

**Resultado: NENHUM SEGREDO HARDCODED ENCONTRADO**

| Verificação | Estado |
|---|---|
| Passwords no código | Não encontrado |
| API keys hardcoded | Não encontrado |
| JWT secrets hardcoded | Não encontrado |
| Encryption keys hardcoded | Não encontrado |
| Passwords em docker-compose | Variáveis de ambiente (POSTGRES_PASSWORD) |
| Passwords em appsettings | Apenas placeholders |

---

## 12. Vulnerabilidades Frontend

| Verificação | Estado |
|---|---|
| `dangerouslySetInnerHTML` sem sanitização | Não detectado |
| Autorização apenas no frontend | Não — backend tem RBAC real |
| Tokens em localStorage | Não detectado — cookies HttpOnly usados |
| Redirects inseguros | Não detectado |
| XSS óbvio | Não detectado |

---

## 13. Observações e Recomendações

| Item | Severidade | Recomendação |
|---|---|---|
| API Keys em appsettings.json (MVP1) | Média | Migrar para armazenamento criptografado em banco |
| ClickHouse sem password em desenvolvimento | Baixa | Configurar credencial para produção |
| OTEL endpoint localhost em produção | Média | Configurar endpoint real por env var |
| `OIDC/SAML` — estado não auditado em detalhe | A verificar | Confirmar suporte para SSO enterprise |

---

## 14. Auditabilidade de Segurança

**Status: READY**

- AuditDbContext com hash chain SHA-256 para imutabilidade
- SecurityEventsEndpoints para eventos de segurança
- CorrelationId em todos os requests para rastreabilidade
- CreatedBy/UpdatedBy em todas as entidades auditáveis
- AuditChainLink para sequência imutável de eventos

**Evidência:** `src/modules/auditcompliance/`, `docs/IMPLEMENTATION-STATUS.md` §Audit Compliance
