# Phase 1 — Production Baseline Checklist

## Propósito

Este documento define os requisitos mínimos verificáveis para implantar o NexTraceOne em ambientes reais (Staging / Production).

Cada item deve ser verificado antes de qualquer deploy significativo.

---

## Bloco 1 — Secrets e Configuração Crítica

### JWT Authentication
- [ ] `Jwt__Secret` está definido como variável de ambiente (não em ficheiro)
- [ ] `Jwt__Secret` tem pelo menos 32 caracteres (verificar com `echo ${#JWT_SECRET}`)
- [ ] `Jwt__Secret` foi gerado com gerador criptográfico (`openssl rand -base64 48`)
- [ ] O valor de `Jwt__Secret` nunca aparece em logs, commits ou outputs de CI
- [ ] Startup valida o secret antes de aceitar tráfego (confirmar via logs)

### Connection Strings
- [ ] Todas as 17 connection strings estão configuradas como variáveis de ambiente
- [ ] Nenhuma connection string usa `Password=` vazio ou password padrão
- [ ] O utilizador de DB tem apenas os privilégios mínimos necessários (não superuser)
- [ ] Passwords de DB foram geradas com alta entropia
- [ ] Connection strings não estão em ficheiros commitados

### Startup Validation
- [ ] A aplicação falhou com mensagem clara quando `Jwt__Secret` foi removido (teste executado)
- [ ] A aplicação falhou com mensagem clara quando uma connection string foi removida (teste executado)
- [ ] A aplicação iniciou correctamente com configuração válida

---

## Bloco 2 — Segurança do Backend

### Autorização de Endpoints
- [ ] Auditoria de endpoints executada (ver `BACKEND-ENDPOINT-AUTH-AUDIT.md`)
- [ ] Todos os endpoints sensíveis têm `RequirePermission(...)` ou `RequireAuthorization()`
- [ ] Endpoints públicos (`AllowAnonymous`) estão documentados e justificados
- [ ] Nenhum endpoint novo foi adicionado sem política de autorização explícita

### Autenticação e Tokens
- [ ] JWT Bearer está configurado com validação de issuer, audience e lifetime
- [ ] Refresh tokens têm expiração configurada (padrão: 7 dias)
- [ ] Access tokens têm expiração configurada (padrão: 60 minutos)
- [ ] CSRF protection está activo para cookie sessions
- [ ] `RequireSecureCookies = true` em Staging/Production (cookies apenas HTTPS)

### Rate Limiting
- [ ] Rate limiting global está activo (100 req/min por IP autenticado)
- [ ] Limite reduzido para IPs não resolvidos (20 req/min)

---

## Bloco 3 — Security Headers e Hardening

### Security Headers (verificar com `curl -I https://<host>/health`)
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: DENY`
- [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- [ ] `Strict-Transport-Security: max-age=63072000; includeSubDomains; preload` (apenas HTTPS)
- [ ] `Cache-Control: no-store`
- [ ] `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`

### HTTPS e TLS
- [ ] Aplicação serve apenas HTTPS em produção
- [ ] `UseHttpsRedirection()` está activo
- [ ] Certificado TLS válido e não expirado
- [ ] TLS 1.2+ apenas (TLS 1.0 e 1.1 desactivados)

### CORS
- [ ] `AllowedOrigins` está restrito a domínios conhecidos
- [ ] Não existe `AllowAnyOrigin()` em produção

---

## Bloco 4 — Integridade de Assemblies

### Assembly Integrity Check
- [ ] `NEXTRACE_SKIP_INTEGRITY` **não** está definido como `true` em produção
- [ ] Ficheiros `.sha256` estão presentes no diretório de deploy (ou verificação está documentada como pendente)
- [ ] Se verificação está desactivada, a razão está documentada

### Configuração por Ambiente
- [ ] `NexTraceOne:IntegrityCheck = false` apenas em Development
- [ ] `NexTraceOne:IntegrityCheck = true` em Staging e Production (quando suportado pelo build pipeline)

---

## Bloco 5 — Frontend

### Ferramentas de Debug
- [ ] `ReactQueryDevtools` **não** renderiza em build de produção
- [ ] Build produtivo foi verificado: `import.meta.env.DEV = false` no bundle
- [ ] Nenhum painel de debug, trace visual ou feature flag de desenvolvimento está acessível ao utilizador final

### Build de Produção
- [ ] Frontend foi compilado com `npm run build` (não `npm run dev`)
- [ ] Source maps não estão expostos publicamente
- [ ] Variáveis de ambiente com `VITE_` prefixo não contêm secrets

---

## Bloco 6 — Operacional

### Logs e Observabilidade
- [ ] Logs não contêm valores de secrets (JWT, passwords, API keys)
- [ ] Serilog está configurado para o nível correcto (Warning em Production)
- [ ] OpenTelemetry endpoint está configurado e acessível
- [ ] Health checks respondem correctamente: `/health`, `/ready`, `/live`

### Backup e Recuperação
- [ ] Backup de base de dados configurado
- [ ] Processo de restauração testado
- [ ] Rotation de secrets documentada

### Deployment
- [ ] `ASPNETCORE_ENVIRONMENT` está definido como `Production` ou `Staging`
- [ ] Imagem Docker não contém `appsettings.Development.json` montado
- [ ] Processo de zero-downtime deploy definido

---

## Bloco 7 — Pré-Deploy Checklist Rápida

Antes de cada deploy para Staging ou Production:

```bash
#!/bin/bash
# Pre-deploy safety check

echo "=== NexTraceOne Pre-Deploy Safety Check ==="

# JWT Secret
[[ -z "$Jwt__Secret" ]] && echo "❌ FAIL: Jwt__Secret not set" && exit 1
[[ ${#Jwt__Secret} -lt 32 ]] && echo "❌ FAIL: Jwt__Secret too short (${#Jwt__Secret} < 32)" && exit 1
echo "✅ Jwt__Secret: present (${#Jwt__Secret} chars)"

# DB credentials (spot check)
[[ -z "${ConnectionStrings__IdentityDatabase}" ]] && echo "❌ FAIL: IdentityDatabase not set" && exit 1
echo "✅ IdentityDatabase: present"

# Environment
[[ "$ASPNETCORE_ENVIRONMENT" == "Development" ]] && echo "❌ FAIL: ASPNETCORE_ENVIRONMENT is Development!" && exit 1
echo "✅ Environment: $ASPNETCORE_ENVIRONMENT"

# Integrity
[[ "$NEXTRACE_SKIP_INTEGRITY" == "true" ]] && echo "⚠️  WARNING: NEXTRACE_SKIP_INTEGRITY is true!"
echo "✅ IntegrityCheck bypass: ${NEXTRACE_SKIP_INTEGRITY:-false}"

echo "=== All checks passed ==="
```

---

## Estado Atual (Phase 1)

| Bloco | Status | Notas |
|---|---|---|
| Bloco 1 — Secrets | ✅ Implementado | StartupValidation.cs valida JWT length + connection strings |
| Bloco 2 — Autorização | ✅ Implementado | 32 endpoints ChangeGovernance corrigidos, todos auditados |
| Bloco 3 — Security Headers | ✅ Implementado | Headers presentes em WebApplicationExtensions.cs |
| Bloco 4 — Integrity Check | ✅ Implementado | AssemblyIntegrityChecker com env var bypass documentado |
| Bloco 5 — Frontend | ✅ Implementado | ReactQueryDevtools guarded com import.meta.env.DEV |
| Bloco 6 — Operacional | ⚠️ Parcial | Health checks OK; rotation de secrets pendente para Fase 2 |
| Bloco 7 — Pre-deploy script | 📋 Documentado | Script de exemplo acima |

---

## Pendências para Fase 2

- [ ] Configurar fallback authorization policy global (`deny by default`)
- [ ] Integrar com sistema de secrets management externo (Vault / AWS SM)
- [ ] Configurar rotation automática de secrets
- [ ] Adicionar scanner de secrets ao pipeline CI (git-secrets, trufflehog)
- [ ] Testes de penetração básicos (OWASP Top 10)
- [ ] Completar geração de ficheiros `.sha256` no pipeline de build

---

## Referências

- [PHASE-1-SECRETS-BASELINE.md](PHASE-1-SECRETS-BASELINE.md)
- [REQUIRED-ENVIRONMENT-CONFIGURATION.md](REQUIRED-ENVIRONMENT-CONFIGURATION.md)
- [BACKEND-ENDPOINT-AUTH-AUDIT.md](BACKEND-ENDPOINT-AUTH-AUDIT.md)
- `src/platform/NexTraceOne.ApiHost/StartupValidation.cs`
- `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Integrity/AssemblyIntegrityChecker.cs`
