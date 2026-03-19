# SECURITY-ARCHITECTURE.md — NexTraceOne

> **Data:** Março 2026
> **Scope:** Arquitetura de segurança da plataforma — controles implementados, gaps, planos.
> **Princípio:** Zero Trust. Least Privilege. Defense in Depth.

---

## Modelo de Ameaça

O NexTraceOne opera em ambientes on-premise enterprise. O modelo de ameaça principal:

| Ameaça | Probabilidade | Controles |
|--------|--------------|-----------|
| XSS (Cross-Site Scripting) | Média | CSP, ErrorBoundary, sanitização de URLs, sessionStorage (escopo de aba) |
| CSRF (Cross-Site Request Forgery) | Baixa¹ | N/A para Bearer tokens; CSRF infra pronta para modo cookie |
| Token exfiltração via XSS | Média | Refresh token em memória (nunca no DOM/storage); access token em sessionStorage |
| SQL Injection | Baixa | EF Core parametrizado; nunca raw SQL com interpolação |
| Privilege escalation | Baixa | RBAC granular com `RequirePermission` em todos os endpoints |
| Cross-tenant data leakage | Baixa | TenantRlsInterceptor (EF Core); JWT claims validados |
| Insider threat / audit evasion | Baixa | Audit trail SHA-256 imutável; sem delete de audit logs |
| Assembly tampering (on-premise) | Baixa | AssemblyIntegrityChecker no boot |
| Data at rest (DB exposure) | Média | AesGcmEncryptor disponível; cobertura parcial (ver gaps) |

> ¹ Com Bearer tokens em `Authorization` header, CSRF não é um risco real porque
> browsers não enviam headers automaticamente cross-origin. CSRF torna-se relevante
> apenas no modelo de cookie httpOnly (implementado como opt-in na Fase 8).

---

## Controles Implementados

### Autenticação e Sessão

| Controle | Estado | Descrição |
|---------|--------|-----------|
| JWT Bearer com RS/HS256 | ✅ | Signing key configurável; validação de issuer, audience, lifetime |
| Refresh token em memória | ✅ | Nunca persiste no browser storage — imune a exfiltração via XSS |
| Access token em sessionStorage | ✅ | Escopo de aba; limpo ao fechar; não partilhado entre abas |
| Token refresh com race protection | ✅ | Flag `isRefreshing` + subscriber queue previne refreshes paralelos |
| Session expiry event | ✅ | CustomEvent `auth:session-expired` desacopla auth cleanup |
| Logout server-side | ✅ | `POST /auth/logout` invalida refresh token no backend |
| Multi-tenant RLS | ✅ | `TenantResolutionMiddleware` + `TenantRlsInterceptor` no EF Core |
| API Key para sistemas externos | ✅ | `ApiKeyAuthenticationHandler` com schema dual ("Smart") |
| PBKDF2 password hashing | ✅ | `Pbkdf2PasswordHasher` — sem MD5/SHA1/bcrypt simples |
| Account lockout | ✅ | Bloqueio automático após X falhas consecutivas |
| httpOnly cookie + CSRF (opt-in) | ⚠️ | Infra implementada na Fase 8; ativação em rollout controlado |

### Autorização

| Controle | Estado | Descrição |
|---------|--------|-----------|
| RBAC granular | ✅ | `RequirePermission("x.y.z")` em 22+ endpoint modules |
| Dynamic policies | ✅ | `PermissionPolicyProvider` + `PermissionAuthorizationHandler` |
| JIT Access | ✅ | Acesso temporário com expiração automática |
| Break Glass | ✅ | Acesso de emergência com auditoria obrigatória |
| Delegações com expiração | ✅ | Delegação de permissões com scoping temporal |
| Access Reviews | ✅ | Revisão periódica de acessos |

### Proteção de Rede e API

| Controle | Estado | Descrição |
|---------|--------|-----------|
| CORS restritivo | ✅ | Wildcards bloqueados em build; origens explícitas configuradas |
| Rate Limiting | ✅ | FixedWindow 100 req/min por IP (20 para IPs não resolvidos) |
| Security Headers backend | ✅ | `UseSecurityHeaders()`: CSP, X-Frame-Options, HSTS, nosniff, referrer |
| HTTPS Redirect | ✅ | `UseHttpsRedirection()` no pipeline |
| Response Compression | ✅ | Sem informação sensível exposta via timing do compression |

### Frontend

| Controle | Estado | Descrição |
|---------|--------|-----------|
| CSP meta tag | ✅ | `Content-Security-Policy` no `index.html` (sem fontes externas desde Fase 8) |
| Source maps desativados | ✅ | Configurado no `vite.config.ts` para builds de produção |
| Build hardening | ✅ | terser com `drop_console` e `drop_debugger` |
| Hashed asset names | ✅ | Cache busting + ofuscação de estrutura interna |
| ErrorBoundary global | ✅ | Suprime stack traces em produção |
| Meta referrer | ✅ | `strict-origin-when-cross-origin` |
| URL sanitização | ✅ | `sanitize.ts` previne `javascript:` e `data:` injection |
| Open redirect protection | ✅ | Allowlist de rotas internas para redirect pós-login |
| Self-hosted fonts | ✅ | Inter + JetBrains Mono via `@fontsource` (Fase 8) — sem CDN externo |

### Dados e Persistência

| Controle | Estado | Descrição |
|---------|--------|-----------|
| Encryption at rest (infra) | ✅ | `AesGcmEncryptor` (AES-256-GCM) + `EncryptedStringConverter` EF Core |
| Encryption at rest (cobertura) | ⚠️ | Infra existe; campos sensíveis no DB ainda sem cobertura completa |
| Audit trail imutável | ✅ | SHA-256 hash chain — audit logs não podem ser alterados sem detecção |
| Sem raw SQL interpolado | ✅ | EF Core parameterized queries em todos os repositórios |
| EF Interceptors de tenant | ✅ | `TenantRlsInterceptor` filtra automaticamente por tenant em queries |

### Build e Distribuição

| Controle | Estado | Descrição |
|---------|--------|-----------|
| Assembly integrity check | ✅ | `AssemblyIntegrityChecker.VerifyOrThrow()` no boot |
| Central Package Management | ✅ | `Directory.Packages.props` — sem drift de versões entre projetos |
| `NEXTRACE_ENCRYPTION_KEY` obrigatório | ✅ | Startup falha em non-Development sem a chave |
| `Jwt:Secret` obrigatório | ✅ | Startup falha em non-Development sem a chave |
| Signing de artefatos | ❌ | Não implementado |

---

## Gaps de Segurança — Estado Pós-Fase 8

### Gaps Fechados na Fase 8

| Gap | Solução |
|----|---------|
| Google Fonts CDN (tracking potencial, falha on-premise isolado) | Self-hosting via `@fontsource` + CSP endurecida |
| Ausência de infraestrutura CSRF para modo cookie | `CsrfTokenValidator` + `CookieSessionEndpoints` (opt-in) |
| Ausência de documentação de variáveis de ambiente | `docs/ENVIRONMENT-VARIABLES.md` criado |
| DEPLOYMENT-ARCHITECTURE.md insuficiente (4 linhas) | Documento expandido com topologia real |

### Gaps Parcialmente Tratados na Fase 8

| Gap | Plano | Bloqueio |
|----|-------|---------|
| Access token em sessionStorage (vulnerável a XSS) | Infra de httpOnly cookie implementada; endpoints prontos (`POST /auth/cookie-session`). Ativação requer: (1) frontend migrado para usar novos endpoints, (2) validação em staging, (3) cutover com monitorização | Mudança de comportamento no frontend; risco de quebrar auth se feito sem staging |
| Encryption at rest incompleta | `AesGcmEncryptor` e `EncryptedStringConverter` prontos; falta aplicar `[Encrypted]` nos campos sensíveis das entidades (PasswordHash, tokens, PIIs) | Mudança de schema requer migration + rotação de dados existentes |

### Gaps Remanescentes (Fora do Scope desta Fase)

| Gap | Severidade | Próximo passo |
|----|------------|--------------|
| Sem signing de artefatos (.NET assemblies, frontend bundle) | Baixa | Implementar em pipeline CI/CD com cosign ou Authenticode |
| `appsettings.json` tem credentials de desenvolvimento hardcoded | Média (dev-only) | Usar `appsettings.Development.json` + git-ignore para credenciais locais |
| Dependências sem audit regular de vulnerabilidades | Média | Adicionar `dotnet list package --vulnerable` ao CI |
| CORS configurado apenas com origens de desenvolvimento no appsettings.json | Alta (se esquecido em produção) | Documentado em ENVIRONMENT-VARIABLES.md + checklist de deploy |

---

## Plano de Migração: sessionStorage → httpOnly Cookie

### Condições para ativação (controladas por `Auth:CookieSession:Enabled`)

```
DISABLED (padrão) → STAGING → VALIDADO → PRODUÇÃO
```

**Passos obrigatórios antes de ativar em produção:**

1. **Backend:** `Auth:CookieSession:Enabled=true` em staging.
2. **Frontend:** migrar `tokenStorage.ts` para usar `POST /auth/cookie-session`:
   - Remover `sessionStorage.setItem` para access token.
   - Substituir por `POST /auth/cookie-session` (cookie é gerido automaticamente pelo browser).
   - Armazenar o `csrfToken` retornado em memória (não em storage).
   - Incluir `X-Csrf-Token` header em todas as mutations (POST/PUT/DELETE/PATCH).
3. **Testes:** validar em staging os fluxos:
   - Login → token no cookie (não visível em JavaScript).
   - Mutation com CSRF token → sucesso.
   - Mutation sem CSRF token → 403.
   - Logout → cookies removidos.
   - Refresh da página → sessão mantida (cookie persiste).
4. **Monitorização:** ativar em produção com rate de adoção gradual se multi-region.

### Endpoints implementados (Fase 8)

| Endpoint | Método | Descrição |
|---------|--------|-----------|
| `/api/v1/identity/cookie-session` | POST | Login → cookie httpOnly + CSRF token |
| `/api/v1/identity/cookie-session` | DELETE | Logout → remove cookies |
| `/api/v1/identity/cookie-session/csrf-token` | GET | Retorna CSRF token fresco |

---

## AI Security

| Controle | Estado | Descrição |
|---------|--------|-----------|
| AI requests auditados | ✅ | Via AuditCompliance — cada pedido ao AI Assistant é registado |
| Model access control | ✅ | Endpoints de AI protegidos com `RequirePermission` |
| Data redaction before LLM | ⚠️ | Grounding não redacta PIIs antes de enviar ao LLM — risco a avaliar |
| Prompt injection mitigation | ⚠️ | Sem sanitização explícita de prompts; context builder é estruturado (mitiga parcialmente) |
| LLM response validation | ⚠️ | Respostas do LLM não são sanitizadas antes de exibição no frontend |

---

*Documento atualizado na Fase 8 — Segurança e Prontidão Operacional.*
*Última atualização: Março 2026.*
