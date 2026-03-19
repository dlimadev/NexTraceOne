# ENVIRONMENT-VARIABLES.md — NexTraceOne

> **Data:** Março 2026
> **Scope:** Variáveis de ambiente obrigatórias e opcionais para deploy de produção.
> **Regra:** Valores sem default marcados como **OBRIGATÓRIO** bloqueiam o startup.

---

## Resumo dos Componentes

| Componente | Porta padrão | Config principal |
|-----------|-------------|-----------------|
| `NexTraceOne.ApiHost` | 8080 (HTTP), 8443 (HTTPS) | `appsettings.json` + env vars |
| `NexTraceOne.BackgroundWorkers` | — (sem HTTP) | Partilha config com ApiHost |
| `NexTraceOne.Ingestion.Api` | 8090 | `appsettings.json` próprio |
| PostgreSQL | 5432 | Connection strings |
| OpenTelemetry Collector | 4317 (gRPC), 4318 (HTTP) | `otel-collector.yaml` |

---

## Variáveis de Ambiente — NexTraceOne.ApiHost

### Obrigatórias em Produção

| Variável | Descrição | Formato | Exemplo |
|---------|-----------|---------|---------|
| `NEXTRACE_ENCRYPTION_KEY` | Chave AES-256-GCM para campos sensíveis cifrados no DB. **Não use o fallback de desenvolvimento em produção.** | Base64 de 32 bytes OU string UTF-8 de 32 chars | `$(openssl rand -base64 32)` |
| `Jwt__Secret` | Chave de assinatura dos JWT. Ausente em non-Development bloqueia o startup. | String ≥ 32 chars | `$(openssl rand -base64 48)` |

> **Nota .NET:** A sintaxe de variáveis de ambiente usa `__` (duplo underscore) como separador de hierarquia.
> Equivalência: `Jwt__Secret` = `Jwt:Secret` no appsettings.json.

### Connection Strings

Cada módulo tem a sua própria connection string. Todas podem apontar para o mesmo servidor PostgreSQL mas bases de dados diferentes.

| Variável de Ambiente | Módulo | Base de Dados padrão |
|--------------------|--------|---------------------|
| `ConnectionStrings__NexTraceOne` | Telemetria (product store) | `nextraceone` |
| `ConnectionStrings__IdentityDatabase` | IdentityAccess | `nextraceone_identity` |
| `ConnectionStrings__CatalogDatabase` | Catalog Graph | `nextraceone_catalog` |
| `ConnectionStrings__ContractsDatabase` | Contratos | `nextraceone_catalog` |
| `ConnectionStrings__DeveloperPortalDatabase` | Developer Portal | `nextraceone_catalog` |
| `ConnectionStrings__ChangeIntelligenceDatabase` | Change Intelligence | `nextraceone_changegovernance` |
| `ConnectionStrings__WorkflowDatabase` | Workflow | `nextraceone_changegovernance` |
| `ConnectionStrings__RulesetGovernanceDatabase` | Ruleset Governance | `nextraceone_changegovernance` |
| `ConnectionStrings__PromotionDatabase` | Promotion | `nextraceone_changegovernance` |
| `ConnectionStrings__IncidentDatabase` | Incidents | `nextraceone_operationalintelligence` |
| `ConnectionStrings__CostIntelligenceDatabase` | Cost Intelligence | `nextraceone_operationalintelligence` |
| `ConnectionStrings__RuntimeIntelligenceDatabase` | Runtime Intelligence | `nextraceone_operationalintelligence` |
| `ConnectionStrings__AuditDatabase` | Audit Compliance | `nextraceone_audit` |
| `ConnectionStrings__AiGovernanceDatabase` | AI Knowledge | `nextraceone_aiknowledge` |

**Formato de connection string PostgreSQL:**
```
Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;
```

### CORS

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `Cors__AllowedOrigins__0` | Primeira origin permitida no CORS | `http://localhost:5173` |
| `Cors__AllowedOrigins__1` | Segunda origin permitida | `http://localhost:3000` |

**Em produção:** configurar apenas a(s) origin(s) do frontend deployado.
```
Cors__AllowedOrigins__0=https://app.yourdomain.com
```

### JWT

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `Jwt__Issuer` | Issuer do token JWT | `NexTraceOne` |
| `Jwt__Audience` | Audience do token JWT | `nextraceone-api` |
| `Jwt__Secret` | **OBRIGATÓRIO** (non-Dev) — chave de assinatura | — |
| `Jwt__AccessTokenExpirationMinutes` | Duração do access token | `60` |
| `Jwt__RefreshTokenExpirationDays` | Duração do refresh token | `7` |

### OpenTelemetry (Observabilidade)

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `OpenTelemetry__Endpoint` | URL do OTLP Collector (gRPC) | `http://localhost:4317` |
| `OpenTelemetry__ServiceName` | Nome do serviço nos traces | `nextraceone-apihost` |

**Se o collector não estiver disponível:** o OTLP exporter falha silenciosamente (sem crash).
**Para desativar exportação OTLP:** remover a chave `OpenTelemetry__Endpoint` ou deixar em branco.

### AI Runtime (Opcional)

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `AiRuntime__Ollama__BaseUrl` | URL do servidor Ollama | `http://localhost:11434` |
| `AiRuntime__Ollama__Enabled` | Ativar provider Ollama | `true` |
| `AiRuntime__OpenAI__ApiKey` | API key OpenAI (opcional) | — |
| `AiRuntime__OpenAI__Enabled` | Ativar provider OpenAI | `false` |
| `AiRuntime__Routing__PreferredProvider` | Provider preferido (`ollama`/`openai`) | `ollama` |

### Sessão Cookie + CSRF (Opt-in, desabilitado por padrão)

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `Auth__CookieSession__Enabled` | Ativar endpoints de sessão cookie httpOnly | `false` |
| `Auth__CookieSession__AccessTokenCookieExpirationMinutes` | Duração do cookie de auth | `60` |

> Ver `docs/security/application-security-review.md` para o plano de migração de sessionStorage → httpOnly cookie.

### Flags de Runtime

| Variável | Descrição | Padrão |
|---------|-----------|--------|
| `NEXTRACE_SKIP_INTEGRITY` | Ignora verificação de integridade de assemblies no boot | `false` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (.NET) | `Production` |
| `ASPNETCORE_URLS` | URLs de escuta do servidor | `http://+:8080` |

---

## Variáveis de Ambiente — NexTraceOne.Ingestion.Api

| Variável | Descrição |
|---------|-----------|
| `ConnectionStrings__NexTraceOne` | Base de dados principal para armazenamento de ingestão |
| `Jwt__Secret` | Mesmo segredo JWT do ApiHost |
| `OpenTelemetry__Endpoint` | Mesmo collector do ApiHost (opcional) |

---

## Health Endpoints

| Endpoint | Método | Descrição |
|---------|--------|-----------|
| `GET /health` | Anónimo | Estado geral da aplicação |
| `GET /ready` | Anónimo | Readiness: DB conectado + migrations aplicadas |
| `GET /live` | Anónimo | Liveness: processo em execução |

**Kubernetes probe recomendado:**
```yaml
livenessProbe:
  httpGet:
    path: /live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30
readinessProbe:
  httpGet:
    path: /ready
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
```

---

## Docker Compose (Desenvolvimento)

O `docker-compose.yml` na raiz inclui os serviços de observabilidade:

| Serviço | Porta | Descrição |
|--------|-------|-----------|
| PostgreSQL | 5432 | Base de dados principal |
| OTel Collector | 4317, 4318 | Receptor de traces e métricas |
| Grafana Tempo | 3200 | Backend de traces |
| Grafana Loki | 3100 | Backend de logs |
| Grafana | 3000 | Dashboard de observabilidade |

---

## Segurança em Produção — Checklist de Deploy

- [ ] `NEXTRACE_ENCRYPTION_KEY` configurado (32 bytes, gerado aleatoriamente)
- [ ] `Jwt__Secret` configurado (≥ 32 chars, gerado aleatoriamente)
- [ ] Connection strings com credenciais específicas de produção (não as do appsettings.json)
- [ ] `Cors__AllowedOrigins` configurado com a origin real do frontend
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Source maps desativados (padrão de build — `vite build`)
- [ ] HTTPS ativado (via proxy reverso ou `ASPNETCORE_URLS=https://...`)
- [ ] Verificar `/ready` retorna 200 antes de ativar em produção
- [ ] Logs estruturados a enviar para centralização (Loki ou sistema equivalente)

---

## Geração de Chaves Seguras

```bash
# NEXTRACE_ENCRYPTION_KEY (32 bytes Base64)
openssl rand -base64 32

# Jwt__Secret (48 bytes Base64 — margem de segurança)
openssl rand -base64 48
```

---

*Documento gerado como parte da Fase 8 — Segurança e Prontidão Operacional.*
*Última atualização: Março 2026.*
