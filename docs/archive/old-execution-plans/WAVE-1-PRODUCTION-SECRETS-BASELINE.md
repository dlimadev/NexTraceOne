# Wave 1 — Production Secrets Baseline

## Visão Geral

Este documento define a **matriz oficial e completa** de secrets e variáveis de configuração necessárias para operar o NexTraceOne em produção.

---

## Classificação de Variáveis

### 🔴 Secrets Obrigatórios (Produção)

Estes secrets **devem** ser provisionados antes do primeiro deploy em produção. A ausência de qualquer um deles causa falha de startup.

| Secret | Variável de Ambiente (.NET) | GitHub Secret (Workflow) | Descrição | Validação no Startup |
|---|---|---|---|---|
| JWT Signing Key | `Jwt__Secret` | N/A (runtime) | Chave HS256, mínimo 32 caracteres. Gerar com `openssl rand -base64 48` | ✅ Falha se ausente ou < 32 chars |
| Connection String — Identity | `ConnectionStrings__IdentityDatabase` | `PRODUCTION_CONN_IDENTITY` | `nextraceone_identity` (Identity, Audit) | ✅ Falha se vazia |
| Connection String — Catalog | `ConnectionStrings__CatalogDatabase` | `PRODUCTION_CONN_CATALOG` | `nextraceone_catalog` (Catalog, Contracts, Portal) | ✅ Falha se vazia |
| Connection String — Operations | `ConnectionStrings__NexTraceOne` | `PRODUCTION_CONN_OPERATIONS` | `nextraceone_operations` (11 contexts) | ✅ Falha se vazia |
| Connection String — AI | `ConnectionStrings__AiGovernanceDatabase` | `PRODUCTION_CONN_AI` | `nextraceone_ai` (AI Governance, External AI, Orchestration) | ✅ Falha se vazia |

### 🟡 Secrets Opcionais/Recomendados

| Secret | Variável de Ambiente | Descrição | Impacto se ausente |
|---|---|---|---|
| OpenAI API Key | `AiRuntime__OpenAI__ApiKey` | API key para provider OpenAI | AI externa indisponível (Ollama local funciona) |
| OIDC Client Secret | `OidcProviders__{Provider}__ClientSecret` | Secret do provider OIDC (Azure AD, etc.) | SSO federado indisponível |
| OIDC Authority | `OidcProviders__{Provider}__Authority` | Authority URL do provider | SSO federado indisponível |
| OIDC Client ID | `OidcProviders__{Provider}__ClientId` | Client ID do provider | SSO federado indisponível |
| ClickHouse Password | `CLICKHOUSE_PASSWORD` | Password do ClickHouse observability | Observabilidade limitada |
| Elastic API Key | `ELASTIC_API_KEY` | API key para Elastic provider | Relevante apenas se Elastic é o provider |

### 🟢 Configuração Não-Sensível (pode estar em appsettings.json)

| Configuração | Valor Produção | Descrição |
|---|---|---|
| `Jwt:Issuer` | `NexTraceOne` | Issuer do JWT |
| `Jwt:Audience` | `nextraceone-api` | Audience do JWT |
| `Jwt:AccessTokenExpirationMinutes` | `60` | Expiração do access token |
| `Jwt:RefreshTokenExpirationDays` | `7` | Expiração do refresh token |
| `Cors:AllowedOrigins` | URL do frontend | Origens CORS permitidas |
| `NexTraceOne:IntegrityCheck` | `true` | Verificação de integridade de assemblies |
| `NexTraceOne:PerformanceThresholdMs` | `500` | Threshold de performance |
| `AiRuntime:Ollama:BaseUrl` | URL do Ollama | Endpoint do servidor Ollama |
| `Serilog:*` | Configuração de logging | Logging estruturado |
| `OpenTelemetry:*` | Configuração de telemetria | Telemetria |

### 🔵 Variáveis do Workflow de Produção (GitHub)

| Tipo | Nome | Descrição |
|---|---|---|
| Secret | `PRODUCTION_CONN_IDENTITY` | Connection string do banco identity |
| Secret | `PRODUCTION_CONN_CATALOG` | Connection string do banco catalog |
| Secret | `PRODUCTION_CONN_OPERATIONS` | Connection string do banco operations |
| Secret | `PRODUCTION_CONN_AI` | Connection string do banco AI |
| Variable | `PRODUCTION_APIHOST_URL` | URL pública do ApiHost (e.g., `https://api.nextraceone.com`) |
| Variable | `PRODUCTION_FRONTEND_URL` | URL pública do Frontend (e.g., `https://app.nextraceone.com`) |

### 🚫 Proibido em Produção

| Configuração | Razão |
|---|---|
| `NEXTRACE_SKIP_INTEGRITY=true` | Desativa verificação de integridade de assemblies |
| `NEXTRACE_AUTO_MIGRATE=true` | Migrações automáticas devem ser controladas manualmente em produção |
| `Auth:CookieSession:RequireSecureCookies=false` | Cookies devem ser secure (HTTPS) em produção |
| JWT Secret com < 32 caracteres | Material de chave insuficiente para HS256 |
| JWT Secret contendo "Development" | Secret de desenvolvimento não pode ser usado em produção |
| Connection strings com password vazia | Todas as connection strings devem ter password real |

---

## Mapeamento de Connection Strings → Bancos Lógicos

O NexTraceOne usa 19 connection strings no `appsettings.json` que mapeiam para 4 bancos lógicos PostgreSQL:

### nextraceone_identity
- `IdentityDatabase` — Identity contexts (users, roles)
- `AuditDatabase` — Audit trail

### nextraceone_catalog
- `CatalogDatabase` — Service catalog
- `ContractsDatabase` — API/Event contracts
- `DeveloperPortalDatabase` — Developer portal

### nextraceone_operations
- `NexTraceOne` — Core operations
- `ChangeIntelligenceDatabase` — Change tracking
- `WorkflowDatabase` — Workflow engine
- `RulesetGovernanceDatabase` — Governance rules
- `PromotionDatabase` — Promotion management
- `IncidentDatabase` — Incident management
- `CostIntelligenceDatabase` — Cost analysis
- `RuntimeIntelligenceDatabase` — Runtime monitoring
- `ReliabilityDatabase` — Reliability tracking
- `GovernanceDatabase` — Governance
- `AutomationDatabase` — Automation

### nextraceone_ai
- `AiGovernanceDatabase` — AI governance
- `ExternalAiDatabase` — External AI integrations
- `AiOrchestrationDatabase` — AI orchestration

---

## Comportamento por Ambiente

| Comportamento | Development | CI | Staging | Production |
|---|---|---|---|---|
| JWT Secret obrigatório | ⚠️ Warning | ✅ Recomendado | ❌ Falha se ausente | ❌ Falha se ausente |
| JWT Secret mínimo 32 chars | ⚠️ Warning | ✅ Recomendado | ❌ Falha se < 32 | ❌ Falha se < 32 |
| Connection strings vazias | ⚠️ Warning | Testcontainers | ❌ Falha | ❌ Falha |
| IntegrityCheck | `false` | `false` (skip) | `true` | `true` |
| Auto-migrations | Opcional | N/A | `false` | `false` |
| Secure cookies | `false` (HTTP) | N/A | `true` | `true` |
| OIDC obrigatório | Não | Não | Não | Recomendado |

---

## Formato das Connection Strings

```
Host=<hostname>;Port=5432;Database=<database>;Username=<username>;Password=<password>;Maximum Pool Size=10
```

Para produção, adicionar:
```
Host=<hostname>;Port=5432;Database=<database>;Username=<username>;Password=<password>;Maximum Pool Size=10;SSL Mode=Require;Trust Server Certificate=false
```

---

## Geração de Secrets Seguros

```bash
# JWT Secret (64 caracteres Base64, 384 bits entropia)
openssl rand -base64 48

# Database Password (43 caracteres Base64, 256 bits entropia)
openssl rand -base64 32

# PowerShell equivalente
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

---

## Checklist de Provisionamento

- [ ] Gerar JWT secret com `openssl rand -base64 48`
- [ ] Gerar passwords dos 4 bancos PostgreSQL
- [ ] Criar os 4 bancos no servidor PostgreSQL de produção
- [ ] Criar utilizador com mínimo privilégio para cada banco
- [ ] Configurar GitHub Secret `PRODUCTION_CONN_IDENTITY`
- [ ] Configurar GitHub Secret `PRODUCTION_CONN_CATALOG`
- [ ] Configurar GitHub Secret `PRODUCTION_CONN_OPERATIONS`
- [ ] Configurar GitHub Secret `PRODUCTION_CONN_AI`
- [ ] Configurar GitHub Variable `PRODUCTION_APIHOST_URL`
- [ ] Configurar GitHub Variable `PRODUCTION_FRONTEND_URL`
- [ ] Configurar `Jwt__Secret` no runtime do container
- [ ] Configurar `ASPNETCORE_ENVIRONMENT=Production` no runtime
- [ ] Configurar `NEXTRACE_SKIP_INTEGRITY=false`
- [ ] Validar startup com health check `/live`
- [ ] Validar readiness com `/ready`

---

## Referências

- [StartupValidation.cs](../../src/platform/NexTraceOne.ApiHost/StartupValidation.cs)
- [appsettings.json](../../src/platform/NexTraceOne.ApiHost/appsettings.json)
- [.env.example](../../.env.example)
- [production.yml](../../.github/workflows/production.yml)
- [PRODUCTION-SECRETS-PROVISIONING.md](../runbooks/PRODUCTION-SECRETS-PROVISIONING.md)
- [PHASE-1-SECRETS-BASELINE.md](../security/PHASE-1-SECRETS-BASELINE.md)
