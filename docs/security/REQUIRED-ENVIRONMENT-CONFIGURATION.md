# Required Environment Configuration

## Propósito

Este documento lista todas as variáveis de ambiente e configurações que o NexTraceOne requer para operar em cada ambiente.  
A aplicação falha no arranque se qualquer configuração marcada como **OBRIGATÓRIA** estiver ausente em ambientes não-Development.

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

## Comportamento de Startup por Ambiente

### ASPNETCORE_ENVIRONMENT = Development
```
✅ Jwt:Secret vazio → Warning, não bloqueia
✅ Connection strings vazias → Warning, não bloqueia
✅ IntegrityCheck desativado por defeito
⚠️  Jwt:Secret < 32 chars → Warning de pré-produção
```

### ASPNETCORE_ENVIRONMENT = Staging
```
🚫 Jwt:Secret vazio → FALHA CRÍTICA, startup abortado
🚫 Jwt:Secret < 32 chars → FALHA CRÍTICA, startup abortado
🚫 Qualquer connection string vazia → FALHA CRÍTICA, startup abortado
✅ IntegrityCheck recomendado ativo
```

### ASPNETCORE_ENVIRONMENT = Production
```
🚫 Jwt:Secret vazio → FALHA CRÍTICA, startup abortado
🚫 Jwt:Secret < 32 chars → FALHA CRÍTICA, startup abortado
🚫 Qualquer connection string vazia → FALHA CRÍTICA, startup abortado
✅ HSTS ativo (Strict-Transport-Security)
✅ Auth cookies com RequireSecureCookies = true obrigatório
✅ NEXTRACE_SKIP_INTEGRITY = false obrigatório
```

---

## Exemplo de Configuração Docker Compose (Staging/Production)

```yaml
services:
  nextraceone-api:
    image: nextraceone-api:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      Jwt__Secret: "${JWT_SECRET}"
      ConnectionStrings__IdentityDatabase: "Host=${DB_HOST};Port=5432;Database=nextraceone_identity;Username=${DB_USER};Password=${DB_PASSWORD};Maximum Pool Size=10"
      ConnectionStrings__CatalogDatabase: "Host=${DB_HOST};Port=5432;Database=nextraceone_catalog;Username=${DB_USER};Password=${DB_PASSWORD};Maximum Pool Size=10"
      ConnectionStrings__NexTraceOne: "Host=${DB_HOST};Port=5432;Database=nextraceone_operations;Username=${DB_USER};Password=${DB_PASSWORD};Maximum Pool Size=10"
      ConnectionStrings__AiGovernanceDatabase: "Host=${DB_HOST};Port=5432;Database=nextraceone_ai;Username=${DB_USER};Password=${DB_PASSWORD};Maximum Pool Size=10"
      Auth__CookieSession__RequireSecureCookies: "true"
    secrets:
      - jwt_secret
      - db_password
```

---

## Verificação de Configuração Antes de Deploy

```bash
# Verificar que os secrets críticos estão definidos
[[ -z "$JWT_SECRET" ]] && echo "ERROR: JWT_SECRET not set" && exit 1
[[ ${#JWT_SECRET} -lt 32 ]] && echo "ERROR: JWT_SECRET too short (min 32 chars)" && exit 1
[[ -z "$DB_PASSWORD" ]] && echo "ERROR: DB_PASSWORD not set" && exit 1

echo "All critical secrets present ✓"
```

---

## Referências

- [PHASE-1-SECRETS-BASELINE.md](PHASE-1-SECRETS-BASELINE.md)
- [PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md](PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md)
- `src/platform/NexTraceOne.ApiHost/StartupValidation.cs` — validação de startup
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/DependencyInjection.cs` — validação JWT
