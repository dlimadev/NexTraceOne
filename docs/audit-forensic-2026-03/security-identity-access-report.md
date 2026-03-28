# Relatório de Segurança, Identidade e Acesso — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

Segurança é requisito não negociável para um produto enterprise self-hosted. O NexTraceOne deve garantir: autenticação robusta, autorização multi-dimensional, isolamento de tenant, auditabilidade de ações sensíveis e proteção de dados.

---

## Estado Atual Encontrado

### Resumo Executivo de Segurança

| Dimensão | Estado | Evidência |
|---|---|---|
| Autenticação JWT | ✅ Forte | Validação obrigatória no startup |
| Autenticação por cookie | ✅ Real | `nxt_at`, `nxt_csrf`, CSRF header |
| OIDC / Login federado | ✅ Configurável | Azure pre-configurado; outros via config |
| SAML | ⚠️ Não confirmado | Não encontrado em código atual |
| MFA / TOTP | ✅ Real | `TotpVerifier`, `MfaChallengeTokenService` |
| Break Glass | ✅ Real | Acesso emergencial com expiração e auditoria |
| JIT (Just-In-Time) access | ✅ Real | Expiração automática |
| Delegações | ✅ Real | Expiração e revogação |
| Access Reviews | ✅ Real | Workflows periódicos |
| Multi-tenancy | ✅ Real (3 camadas) | RLS + aplicação + soft-delete |
| Autorização por módulo/ação | ✅ Real | Permission strings tipadas |
| Environment-aware authorization | ✅ Real | `EnvironmentAccessRequirement` |
| Segredos hardcoded | ✅ Limpo | `REPLACE_VIA_ENV` em todas as passwords |
| AES-256-GCM encryption | ✅ Presente | `BuildingBlocks.Security` |
| `AssemblyIntegrityChecker` | ✅ Presente | Base para anti-tampering |
| Eventos de segurança auditados | ✅ Real | Trilha de auditoria em `AuditDbContext` |
| Frontend autorização apenas no client | ✅ Ausente | Backend é autoridade final |

---

## Identity Access Module — Análise Detalhada

`src/modules/identityaccess/` | 185 ficheiros | 1 DbContext | 3 migrações

### Domain Layer

**Entidades:** `User`, `Role`, `Environment`, `EnvironmentAccess`, `Delegation`, `AccessReview`

**Value Objects com validação real:**
- `Email` — validação via `MailAddress`, imutabilidade, equality
- `HashedPassword` — hashing, nunca exposta em plain text
- `FullName` — validação de comprimento
- `AuthenticationMode` — Local, OIDC, SAML
- `MfaPolicy` — políticas de MFA por grupo/role
- `SessionPolicy` — duração, refresh, invalidação
- `DeploymentModel` — Cloud, OnPrem, Hybrid

### Application Layer

| Feature | Estado |
|---|---|
| Login (local + OIDC) | ✅ Real |
| Refresh token | ✅ Real |
| MFA challenge e verify | ✅ Real — `TotpVerifier` |
| JIT privileged access (request + grant + revoke) | ✅ Real — expiração automática |
| Break Glass (request + activate + close) | ✅ Real — auditado |
| Access Reviews (create + review + approve/reject) | ✅ Real |
| Delegations (create + revoke + expire) | ✅ Real |
| Environment management (CRUD) | ✅ Real |
| User management (CRUD) | ✅ Real |
| Role and permission management | ✅ Real |
| Environment-aware access control | ✅ Real |

### Infrastructure Layer

- `JwtTokenGenerator` — geração real com claims
- `TotpVerifier` — validação TOTP real
- `OidcProviderService` — integração OIDC real
- `MfaChallengeTokenService` — challenge real

---

## BuildingBlocks.Security — Análise Detalhada

`src/building-blocks/NexTraceOne.BuildingBlocks.Security/`

### Authentication
- JWT token validation com `RequireJwtAuthentication()` no startup
- API Key authentication como alternativa
- Cookie session com CSRF protection

### Authorization
- Permission-based: strings como `"ai:governance:read"`, `"ai:assistant:*"`, `"contracts:write"`
- Environment access requirements: `EnvironmentAccessRequirement`
- Policies CORS: 5+ políticas configuradas
- Rate limiting: 6 políticas configuradas

### Encryption
- `AES-256-GCM` para dados sensíveis em repouso
- Chaves via variáveis de ambiente (não hardcoded)

### Integrity
- `AssemblyIntegrityChecker` — hash de assemblies para detecção de tampering
- Verificação no startup (controlada por `NexTraceOne.IntegrityCheck: true`)

---

## Multi-tenancy — 3 Camadas

### Camada 1: Row Level Security (PostgreSQL)
- `TenantRlsInterceptor` ativado globalmente via `NexTraceDbContextBase`
- Todas as queries filtram automaticamente por `tenant_id`
- Sem possibilidade de acesso cross-tenant via EF Core sem bypass explícito

### Camada 2: Application Layer
- `ICurrentTenant` injeta tenant em todos os handlers
- Guards de tenant em handlers críticos

### Camada 3: Soft-delete
- Entidades deletadas ficam retidas por tenant — sem risco de leak cross-tenant

---

## Autorização — Dimensões Verificadas

| Dimensão | Implementada | Evidência |
|---|---|---|
| Módulo | ✅ | Permission strings por módulo |
| Ação (read/write/admin) | ✅ | Permission strings granulares |
| Ambiente | ✅ | `EnvironmentAccessRequirement` |
| Tenant | ✅ | RLS + ICurrentTenant |
| Role/Policy | ✅ | RBAC no IdentityAccess |
| AI capability | ✅ | `"ai:governance:*"` policies |
| Frontend nunca é fonte de verdade | ✅ | `ProtectedRoute.tsx` validado mas backend verifica |

---

## Auditoria de Segurança

- `AuditDbContext` com hash chain SHA-256 — imputabilidade
- Eventos de segurança: login, logout, JIT activation, break glass, access review
- `AuditInterceptor` no `NexTraceDbContextBase` — automático para mudanças em entidades
- Correlação auditável via `correlationId` em todos os requests

---

## Riscos e Gaps Identificados

| Risco | Severidade | Observação |
|---|---|---|
| SAML não encontrado em código | Média | CLAUDE.md especifica SAML como requisito; não confirmado em `identityaccess` |
| `OtlpEndpoint: localhost` em config base | Baixa | Não é segurança mas exposição de config incorreta |
| `RequireSecureCookies` precisa verificar override em development | Baixa | Confirmar que `false` só em dev |
| AssemblyIntegrityChecker sem validação de chave de assinatura | Média | Verifica hash mas sem assinatura criptográfica de código |
| Licensing removido (PR-17) | Alta | Sem enforcement de licença — produto pode correr sem autorização comercial válida |
| Deep-link preservation após OIDC | Baixa | Verificar fluxo completo em integração real |

---

## Frontend — Segurança Verificada

| Check | Estado |
|---|---|
| Sem `dangerouslySetInnerHTML` com conteúdo não sanitizado | ✅ Não encontrado |
| Sem credenciais no browser storage | ✅ Cookies HttpOnly confirmados |
| Autorização via `ProtectedRoute` | ✅ Confirmado (`src/components/ProtectedRoute.tsx`) |
| `usePermissions` hook para UI condicional | ✅ Confirmado (`__tests__/hooks/usePermissions.test.tsx`) |
| URLs e redirects validados | ⚠️ Verificar após OIDC integration completa |

---

## Avaliação Global de Segurança

**Ponto forte:** O NexTraceOne tem um dos aspectos de segurança mais maduros do repositório. A implementação de JIT, Break Glass, RLS multi-tenant, AES-256-GCM e JWT validado no startup é enterprise-grade e vai além do mínimo esperado para MVP.

**Gap principal:** SAML não confirmado; Licensing ausente (impacto na segurança comercial, não técnica).

---

## Recomendações

1. **Alta:** Confirmar/implementar suporte SAML se for requisito imediato
2. **Alta:** Definir estratégia de Licensing (módulo removido — enterprise sem enforcement de licença)
3. **Média:** Verificar deep-link preservation completo em OIDC flow
4. **Média:** Documentar explicitamente quais variáveis de ambiente são obrigatórias para startup seguro
5. **Baixa:** Validar que `RequireSecureCookies` está `false` apenas em `appsettings.Development.json`

---

*Data: 28 de Março de 2026*
