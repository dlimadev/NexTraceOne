# Environment Baseline — Security & Deployment

## Visão Geral

Este documento define os requisitos mínimos verificáveis para configurar, proteger e implantar o NexTraceOne em ambientes reais (Staging / Production).

Toda configuração sensível deve ser fornecida por variável de ambiente ou sistema de gestão de secrets externo.  
**Nunca commitar valores reais neste repositório.**

---

## Classificação: Secret vs. Configuração

### Secrets (nunca em ficheiro, sempre em variável de ambiente ou vault)

| Secret | Variável de Ambiente | Descrição |
|---|---|---|
| JWT Signing Key | `Jwt__Secret` | Chave de assinatura JWT. Mínimo 32 caracteres (material de chave para HS256). Obrigatória em Staging/Production. |
| DB Password — Identity | `ConnectionStrings__IdentityDatabase` | Connection string completa com password do nextraceone_identity. |
| DB Password — Catalog | `ConnectionStrings__CatalogDatabase` | Connection string completa com password do nextraceone_catalog. |
| DB Password — Contracts | `ConnectionStrings__ContractsDatabase` | Connection string completa com password do nextraceone_catalog. |
| DB Password — DeveloperPortal | `ConnectionStrings__DeveloperPortalDatabase` | Connection string completa com password do nextraceone_catalog. |
| DB Password — Operations | `ConnectionStrings__NexTraceOne` | Connection string consolidada para nextraceone_operations. |
| DB Password — ChangeIntelligence | `ConnectionStrings__ChangeIntelligenceDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — Workflow | `ConnectionStrings__WorkflowDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — RulesetGovernance | `ConnectionStrings__RulesetGovernanceDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — Promotion | `ConnectionStrings__PromotionDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — Incident | `ConnectionStrings__IncidentDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — CostIntelligence | `ConnectionStrings__CostIntelligenceDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — RuntimeIntelligence | `ConnectionStrings__RuntimeIntelligenceDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — Audit | `ConnectionStrings__AuditDatabase` | Connection string completa para nextraceone_identity. |
| DB Password — AiGovernance | `ConnectionStrings__AiGovernanceDatabase` | Connection string completa para nextraceone_ai. |
| DB Password — Governance | `ConnectionStrings__GovernanceDatabase` | Connection string completa para nextraceone_operations. |
| DB Password — ExternalAi | `ConnectionStrings__ExternalAiDatabase` | Connection string completa para nextraceone_ai. |
| DB Password — AiOrchestration | `ConnectionStrings__AiOrchestrationDatabase` | Connection string completa para nextraceone_ai. |
| OpenAI API Key | `AiRuntime__OpenAI__ApiKey` | API key do OpenAI. Apenas necessária se OpenAI estiver habilitado. |
| AI Provider Keys | `AiRuntime__{Provider}__ApiKey` | Keys de outros providers de AI externos configurados. |
| Security API Keys | `Security__ApiKeys` | API keys para integração sistema-a-sistema. |

### Configuração (pode estar em appsettings.json sem risco)

| Configuração | Descrição |
|---|---|
| `Jwt:Issuer` | Issuer do JWT — não é sensível |
| `Jwt:Audience` | Audience do JWT — não é sensível |
| `Jwt:AccessTokenExpirationMinutes` | Expiração do access token |
| `Jwt:RefreshTokenExpirationDays` | Expiração do refresh token |
| `Cors:AllowedOrigins` | Origens CORS permitidas |
| `AiRuntime:Ollama:BaseUrl` | URL do servidor Ollama local |
| `AiRuntime:Routing:*` | Configuração de routing de AI |
| `Serilog:*` | Configuração de logging |
| `OpenTelemetry:ServiceName` | Nome do serviço para telemetria |
| `NexTraceOne:PerformanceThresholdMs` | Threshold de performance |

---

## Configurações Obrigatórias por Ambiente

### JWT Authentication

| Variável | Development | Staging | Production | Notas |
|---|---|---|---|---|
| `Jwt__Secret` | Recomendado (warning se ausente) | **OBRIGATÓRIO** | **OBRIGATÓRIO** | Mínimo 32 chars (material de chave HS256). Alta entropia. |
| `Jwt__Issuer` | `NexTraceOne` (default) | `NexTraceOne` (default) | Confirmar | Pode ficar em appsettings.json |
| `Jwt__Audience` | `nextraceone-api` (default) | `nextraceone-api` (default) | Confirmar | Pode ficar em appsettings.json |

### Connection Strings — Base de Dados

Formato completo: `Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;Maximum Pool Size=<n>`

| Variável | Development | Staging | Production |
|---|---|---|---|
| `ConnectionStrings__IdentityDatabase` | Opcional (usa vazio local) | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__CatalogDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__ContractsDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__DeveloperPortalDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__NexTraceOne` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__ChangeIntelligenceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__WorkflowDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__RulesetGovernanceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__PromotionDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__IncidentDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__CostIntelligenceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__RuntimeIntelligenceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__AuditDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__AiGovernanceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__GovernanceDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__ExternalAiDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |
| `ConnectionStrings__AiOrchestrationDatabase` | Opcional | **OBRIGATÓRIO** | **OBRIGATÓRIO** |

> **Nota:** Em Development, connection strings com password vazia são permitidas (warning gerado).  
> Em Staging/Production, qualquer connection string vazia bloqueia o startup.

### AI Runtime

| Variável | Obrigatório? | Notas |
|---|---|---|
| `AiRuntime__OpenAI__ApiKey` | Apenas se `AiRuntime__OpenAI__Enabled=true` | Secret — não commitar |
| `AiRuntime__Ollama__BaseUrl` | Não (default: `http://localhost:11434`) | Configuração pública |

### Segurança e Auth

| Variável | Development | Staging | Production | Notas |
|---|---|---|---|---|
| `Auth__CookieSession__RequireSecureCookies` | `false` (default em dev) | `true` | `true` | Cookies HTTPS only |
| `Security__ApiKeys` | Opcional | Opcional | Opcional | Para integrações sistema-a-sistema |
| `NEXTRACE_SKIP_INTEGRITY` | Pode ser `true` | Não recomendado | **Nunca `true`** | Bypass de integrity check |

### Observabilidade

| Variável | Notas |
|---|---|
| `OpenTelemetry__Endpoint` | Endpoint OTLP para traces/métricas |
| `Serilog__*` | Configuração de logging estruturado |

---

## Convenção de Nomes de Variáveis de Ambiente

O .NET usa `__` (duplo underscore) para separar secções hierárquicas:

```
# Formato: Section__SubSection__Key
Jwt__Secret=<valor>
ConnectionStrings__IdentityDatabase=<connection-string-completa>
AiRuntime__OpenAI__ApiKey=<chave>
```

Em Docker Compose / Kubernetes secrets, use a mesma convenção:

```yaml
env:
  - name: Jwt__Secret
    valueFrom:
      secretKeyRef:
        name: nextraceone-secrets
        key: jwt-secret
```

---

## Política por Ambiente

### Development (local)
- `Jwt__Secret` pode ser configurado em `appsettings.Development.json` ou `dotnet user-secrets`
- Connection strings podem ter password vazia se a DB local não tiver senha
- Startup emite warning se Jwt:Secret estiver ausente, mas não bloqueia
- `NexTraceOne:IntegrityCheck` = false por defeito

### Test (CI/CD)
- Credentials geridas pelo pipeline (GitHub Secrets / CI secrets)
- `Jwt__Secret` deve ser definido com valor de teste forte
- Connection strings fornecidas pelo Testcontainers (PostgreSQL efémero)
- `NEXTRACE_SKIP_INTEGRITY=true` aceitável em CI

### Staging
- Todos os secrets obrigatórios devem estar presentes
- Startup falhará se `Jwt__Secret` ausente ou fraco (< 32 chars)
- Startup falhará se qualquer connection string estiver vazia
- Usar secrets manager (Vault / AWS Secrets Manager / Azure Key Vault) preferido

### Production
- Todos os secrets obrigatórios — sem exceção
- `Jwt__Secret` mínimo 32 caracteres, alta entropia
- Connection strings com usuário de mínimo privilégio
- Secrets geridos por sistema externo (nunca em ficheiro)
- `NexTraceOne:IntegrityCheck` = true (verificação de integridade ativa)
- `NEXTRACE_SKIP_INTEGRITY` nunca deve ser `true`
- `Auth:CookieSession:RequireSecureCookies` = true (cookies apenas HTTPS)

---

## Integração com Sistemas de Gestão de Secrets

### Vault (HashiCorp)
Configuração via `VAULT_ADDR` e `VAULT_TOKEN`. Injetar secrets como variáveis de ambiente no container.

### AWS Secrets Manager / Parameter Store
Usar AWS SDK para .NET com `AddSystemsManager(...)` ou injetar via ECS Task Definition.

### Azure Key Vault
Usar `AddAzureKeyVault(...)` via `Microsoft.Extensions.Configuration.AzureKeyVault`.

### Kubernetes Secrets
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: nextraceone-secrets
type: Opaque
stringData:
  jwt-secret: "<valor-secreto>"
  identity-db-connection: "Host=...;Password=..."
```

---

## Geração de um JWT Secret Seguro

```bash
# Linux/macOS
openssl rand -base64 48

# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

# Python
python3 -c "import secrets; print(secrets.token_urlsafe(48))"
```

O output produz 48 bytes aleatórios (384 bits de entropia), codificados em Base64 como 64 caracteres — bem acima do mínimo de 32 caracteres.

---

## Auditoria de Secrets

- Nunca logar o valor de qualquer secret.
- Startup faz log do comprimento do `Jwt:Secret` mas nunca do valor.
- Toda rotação de secret deve ser registada no audit trail operacional.
- Secrets não devem aparecer em stack traces, health endpoints ou logs estruturados.

---

## Production Baseline Checklist

Cada item deve ser verificado antes de qualquer deploy significativo para Staging ou Production.

### Bloco 1 — Secrets e Configuração Crítica

#### JWT Authentication
- [ ] `Jwt__Secret` está definido como variável de ambiente (não em ficheiro)
- [ ] `Jwt__Secret` tem pelo menos 32 caracteres (verificar com `echo ${#JWT_SECRET}`)
- [ ] `Jwt__Secret` foi gerado com gerador criptográfico (`openssl rand -base64 48`)
- [ ] O valor de `Jwt__Secret` nunca aparece em logs, commits ou outputs de CI
- [ ] Startup valida o secret antes de aceitar tráfego (confirmar via logs)

#### Connection Strings
- [ ] Todas as 17 connection strings estão configuradas como variáveis de ambiente
- [ ] Nenhuma connection string usa `Password=` vazio ou password padrão
- [ ] O utilizador de DB tem apenas os privilégios mínimos necessários (não superuser)
- [ ] Passwords de DB foram geradas com alta entropia
- [ ] Connection strings não estão em ficheiros commitados

#### Startup Validation
- [ ] A aplicação falhou com mensagem clara quando `Jwt__Secret` foi removido (teste executado)
- [ ] A aplicação falhou com mensagem clara quando uma connection string foi removida (teste executado)
- [ ] A aplicação iniciou correctamente com configuração válida

### Bloco 2 — Segurança do Backend

#### Autorização de Endpoints
- [ ] Auditoria de endpoints executada (ver `BACKEND-ENDPOINT-AUTH-AUDIT.md`)
- [ ] Todos os endpoints sensíveis têm `RequirePermission(...)` ou `RequireAuthorization()`
- [ ] Endpoints públicos (`AllowAnonymous`) estão documentados e justificados
- [ ] Nenhum endpoint novo foi adicionado sem política de autorização explícita

#### Autenticação e Tokens
- [ ] JWT Bearer está configurado com validação de issuer, audience e lifetime
- [ ] Refresh tokens têm expiração configurada (padrão: 7 dias)
- [ ] Access tokens têm expiração configurada (padrão: 60 minutos)
- [ ] CSRF protection está activo para cookie sessions
- [ ] `RequireSecureCookies = true` em Staging/Production (cookies apenas HTTPS)

#### Rate Limiting
- [ ] Rate limiting global está activo (100 req/min por IP autenticado)
- [ ] Limite reduzido para IPs não resolvidos (20 req/min)

### Bloco 3 — Security Headers e Hardening

#### Security Headers (verificar com `curl -I https://<host>/health`)
- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: DENY`
- [ ] `Referrer-Policy: strict-origin-when-cross-origin`
- [ ] `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
- [ ] `Strict-Transport-Security: max-age=63072000; includeSubDomains; preload` (apenas HTTPS)
- [ ] `Cache-Control: no-store`
- [ ] `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`

#### HTTPS e TLS
- [ ] Aplicação serve apenas HTTPS em produção
- [ ] `UseHttpsRedirection()` está activo
- [ ] Certificado TLS válido e não expirado
- [ ] TLS 1.2+ apenas (TLS 1.0 e 1.1 desactivados)

#### CORS
- [ ] `AllowedOrigins` está restrito a domínios conhecidos
- [ ] Não existe `AllowAnyOrigin()` em produção

### Bloco 4 — Integridade de Assemblies

#### Assembly Integrity Check
- [ ] `NEXTRACE_SKIP_INTEGRITY` **não** está definido como `true` em produção
- [ ] Ficheiros `.sha256` estão presentes no diretório de deploy (ou verificação está documentada como pendente)
- [ ] Se verificação está desactivada, a razão está documentada

#### Configuração por Ambiente
- [ ] `NexTraceOne:IntegrityCheck = false` apenas em Development
- [ ] `NexTraceOne:IntegrityCheck = true` em Staging e Production (quando suportado pelo build pipeline)

### Bloco 5 — Frontend

#### Ferramentas de Debug
- [ ] `ReactQueryDevtools` **não** renderiza em build de produção
- [ ] Build produtivo foi verificado: `import.meta.env.DEV = false` no bundle
- [ ] Nenhum painel de debug, trace visual ou feature flag de desenvolvimento está acessível ao utilizador final

#### Build de Produção
- [ ] Frontend foi compilado com `npm run build` (não `npm run dev`)
- [ ] Source maps não estão expostos publicamente
- [ ] Variáveis de ambiente com `VITE_` prefixo não contêm secrets

### Bloco 6 — Operacional

#### Logs e Observabilidade
- [ ] Logs não contêm valores de secrets (JWT, passwords, API keys)
- [ ] Serilog está configurado para o nível correcto (Warning em Production)
- [ ] OpenTelemetry endpoint está configurado e acessível
- [ ] Health checks respondem correctamente: `/health`, `/ready`, `/live`

#### Backup e Recuperação
- [ ] Backup de base de dados configurado
- [ ] Processo de restauração testado
- [ ] Rotation de secrets documentada

#### Deployment
- [ ] `ASPNETCORE_ENVIRONMENT` está definido como `Production` ou `Staging`
- [ ] Imagem Docker não contém `appsettings.Development.json` montado
- [ ] Processo de zero-downtime deploy definido

---

## Pré-Deploy Checklist Rápida

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

- `src/platform/NexTraceOne.ApiHost/StartupValidation.cs` — validação de startup
- `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` — headers de segurança
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs` — validação JWT
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Integrity/AssemblyIntegrityChecker.cs` — integridade de assemblies
- [BACKEND-ENDPOINT-AUTH-AUDIT.md](BACKEND-ENDPOINT-AUTH-AUDIT.md)
