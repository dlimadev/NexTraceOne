# Configuração de Ambiente — NexTraceOne

## Matriz de variáveis por ambiente

### Variáveis obrigatórias em todos os ambientes

| Variável | Descrição | Exemplo |
|---|---|---|
| `ConnectionStrings__IdentityDatabase` | PostgreSQL identity | `Host=pg;Database=nextraceone_identity;...` |
| `ConnectionStrings__CatalogDatabase` | PostgreSQL catalog | `Host=pg;Database=nextraceone_catalog;...` |
| `ConnectionStrings__NexTraceOne` | PostgreSQL operations | `Host=pg;Database=nextraceone_operations;...` |
| `ConnectionStrings__AiDatabase` | PostgreSQL AI | `Host=pg;Database=nextraceone_ai;...` |
| `Jwt__Secret` | Chave JWT (≥32 chars) | `your-secret-at-least-32-chars-long` |
| `Jwt__Issuer` | JWT Issuer | `NexTraceOne` |
| `Jwt__Audience` | JWT Audience | `NexTraceOne` |

### Variáveis obrigatórias em Staging e Production

| Variável | Descrição | Staging | Production |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Ambiente .NET | `Staging` | `Production` |
| `Cors__AllowedOrigins__0` | CORS origem permitida | URL staging | URL produção |

### Variáveis opcionais

| Variável | Padrão | Descrição |
|---|---|---|
| `OpenTelemetry__Endpoint` | `http://localhost:4317` | OTLP endpoint |
| `Serilog__WriteTo__1__Args__serverUrl` | (vazio) | Loki endpoint |
| `NEXTRACE_AUTO_MIGRATE` | `false` | Ativar auto-migrate em Staging |
| `NEXTRACE_SKIP_INTEGRITY` | `false` | Desabilitar check de integridade (só CI) |
| `OllamaRuntime__Endpoint` | `http://localhost:11434` | Ollama AI endpoint |

---

## Configuração por ambiente

### Development

**Fonte**: `appsettings.Development.json` (versionado, sem secrets reais)

```json
{
  "Jwt": {
    "Secret": "NexTraceOne-Development-SecretKey-AtLeast32BytesLong-2024!"
  }
}
```

**Comportamento especial**:
- Auto-migrations sempre ativas
- Seed data executado na inicialização
- IntegrityCheck desabilitado
- Serilog nível Debug
- RequireSecureCookies: false

**Não necessita variáveis de ambiente** — credenciais em `appsettings.Development.json`.

---

### CI (GitHub Actions)

**Fonte**: GitHub Secrets + environment variables

Variáveis injetadas pelo runner:
```yaml
ASPNETCORE_ENVIRONMENT: Staging
NEXTRACE_SKIP_INTEGRITY: "true"  # Sem check de integridade em CI
ConnectionStrings__*: via secrets
Jwt__Secret: via secret
```

**Nunca usar `appsettings.Development.json` em CI.**

---

### Staging

**Fonte**: GitHub Secrets (environment `staging`) + `.env` (se compose)

Requisitos mínimos:
```bash
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__IdentityDatabase=Host=...;Database=nextraceone_identity;...
ConnectionStrings__CatalogDatabase=Host=...;Database=nextraceone_catalog;...
ConnectionStrings__NexTraceOne=Host=...;Database=nextraceone_operations;...
ConnectionStrings__AiDatabase=Host=...;Database=nextraceone_ai;...
Jwt__Secret=<32+ chars, não o dev key>
NEXTRACE_AUTO_MIGRATE=false  # Usar pipeline de migrations
```

---

### Production

**Fonte**: Secrets manager (Vault, AWS Secrets Manager, Azure Key Vault, etc.)

Requisitos adicionais em Production:
- `Jwt__Secret` ≠ secret de desenvolvimento (StartupValidation bloqueia se vazio)
- `NEXTRACE_AUTO_MIGRATE` **NÃO DEVE ser `true`** (startup lança exceção)
- `NEXTRACE_SKIP_INTEGRITY` deve ser `false`
- Todas as connection strings apontando para PostgreSQL de produção com SSL

**Configuração proibida em Production**:
- `NEXTRACE_AUTO_MIGRATE=true` (bloqueado por código)
- Uso do JWT secret de desenvolvimento
- `NEXTRACE_SKIP_INTEGRITY=true`
- `ASPNETCORE_ENVIRONMENT=Development`

---

## Connection strings — formato completo

### PostgreSQL com pool e SSL (Production)

```
Host=<host>;Port=5432;Database=nextraceone_identity;
Username=<user>;Password=<pass>;
Maximum Pool Size=10;
SSL Mode=Require;Trust Server Certificate=false;
```

### PostgreSQL básico (Staging/Dev)

```
Host=localhost;Port=5432;Database=nextraceone_identity;
Username=nextraceone;Password=<pass>;
Maximum Pool Size=10;
```

---

## Secrets obrigatórios por componente

### ApiHost
| Secret | Descrição |
|---|---|
| Connection strings (4 bancos) | Todos os 4 bancos |
| `Jwt__Secret` | Chave de assinatura JWT |

### BackgroundWorkers
| Secret | Descrição |
|---|---|
| `ConnectionStrings__IdentityDatabase` | Banco identity |
| `ConnectionStrings__NexTraceOne` | Banco operations |

### Ingestion.Api
| Secret | Descrição |
|---|---|
| `ConnectionStrings__NexTraceOne` | Banco operations |
| `Jwt__Secret` | Validação de tokens (integrações autenticadas) |

---

## Seeds e dados de demonstração

O seed de dados (`SeedDevelopmentDataAsync`) é executado **apenas quando**:
```csharp
app.Environment.IsDevelopment()
```

Isso garante que dados de demonstração/teste nunca contaminam Staging ou Production.

Para resetar estado local:
```bash
docker compose down -v  # Remove volumes
docker compose up -d    # Recria do zero
```

---

## Health check endpoints

Todos os serviços expõem:

| Endpoint | Propósito | Usado por |
|---|---|---|
| `GET /live` | Liveness probe | Kubernetes, Docker health check |
| `GET /ready` | Readiness probe (com DB check) | Load balancer, orchestrator |
| `GET /health` | Informações completas | Monitoring, dashboards |

Resposta esperada: `{"status":"Healthy"}`
