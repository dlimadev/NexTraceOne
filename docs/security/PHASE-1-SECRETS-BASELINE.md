# Phase 1 — Secrets Baseline

## Visão Geral

Este documento define a política oficial de secrets para o NexTraceOne.  
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

## Referências

- [REQUIRED-ENVIRONMENT-CONFIGURATION.md](REQUIRED-ENVIRONMENT-CONFIGURATION.md)
- [PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md](PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md)
- [BACKEND-ENDPOINT-AUTH-AUDIT.md](BACKEND-ENDPOINT-AUTH-AUDIT.md)
