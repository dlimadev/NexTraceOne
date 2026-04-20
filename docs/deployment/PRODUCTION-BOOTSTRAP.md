# Production Bootstrap — NexTraceOne

> **Última atualização:** Abril 2026
> **Objetivo:** Guia passo-a-passo para primeira instalação em produção.

---

## Pré-requisitos

| Componente | Versão Mínima | Notas |
|---|---|---|
| PostgreSQL | 16+ | Recomendado: managed instance (RDS, CloudSQL, Azure Database) |
| .NET Runtime | 10.0 | ASP.NET Core Runtime |
| Node.js | 20 LTS | Para build do frontend |
| SMTP | Qualquer | Para notificações (opcional no bootstrap) |
| Ollama ou OpenAI | Opcional | Para funcionalidades AI |

---

## Passo 1 — Provisionar Base de Dados

### 1.1 Criar databases

O NexTraceOne usa múltiplos schemas dentro de um ou mais databases PostgreSQL. A configuração mínima requer as seguintes connection strings:

```bash
# Connection strings obrigatórias
ConnectionStrings__IdentityDatabase="Host=pg;Database=nextraceone;Username=app;Password=...;SSL Mode=Require"
ConnectionStrings__CatalogDatabase="Host=pg;Database=nextraceone;Username=app;Password=...;SSL Mode=Require"
ConnectionStrings__NexTraceOne="Host=pg;Database=nextraceone;Username=app;Password=...;SSL Mode=Require"
ConnectionStrings__AiDatabase="Host=pg;Database=nextraceone;Username=app;Password=...;SSL Mode=Require"
```

> **Nota:** Todas as connection strings podem apontar para o mesmo database PostgreSQL. O isolamento é feito por schema (prefixo nas tabelas: `iam_`, `cat_`, `chg_`, `aik_`, `gov_`, etc.).

### 1.2 Permissões do utilizador de aplicação

```sql
-- Utilizador da aplicação (recomendado: não usar superuser)
CREATE USER nextraceone_app WITH PASSWORD 'your-strong-password';
GRANT CONNECT ON DATABASE nextraceone TO nextraceone_app;
GRANT CREATE ON DATABASE nextraceone TO nextraceone_app; -- necessário para migrations
```

---

## Passo 2 — Configurar Variáveis de Ambiente

### 2.1 Variáveis obrigatórias

```bash
ASPNETCORE_ENVIRONMENT=Production

# JWT (obrigatório — gerar chave ≥32 caracteres)
Jwt__Secret="<gerar-com-openssl-rand-base64-48>"
Jwt__Issuer="NexTraceOne"
Jwt__Audience="NexTraceOne"

# CORS (obrigatório em produção)
Cors__AllowedOrigins__0="https://your-nextraceone-domain.com"

# Database (ver passo 1)
ConnectionStrings__IdentityDatabase="..."
ConnectionStrings__CatalogDatabase="..."
ConnectionStrings__NexTraceOne="..."
ConnectionStrings__AiDatabase="..."
```

### 2.2 Variáveis opcionais (recomendadas)

```bash
# OpenTelemetry (recomendado para observabilidade)
OpenTelemetry__Endpoint="http://your-otel-collector:4317"

# SMTP (para notificações)
Smtp__Host="smtp.example.com"
Smtp__Port=587
Smtp__Username="..."
Smtp__Password="..."
Smtp__From="noreply@nextraceone.example.com"

# AI (se usar Ollama local ou OpenAI)
OllamaRuntime__Endpoint="http://ollama:11434"
# OU
OpenAI__ApiKey="sk-..."
```

### 2.3 Variáveis que NÃO devem ser usadas em produção

```bash
# NUNCA em produção:
NEXTRACE_SKIP_INTEGRITY=true  # Apenas CI
NEXTRACE_AUTO_MIGRATE=true    # Aplicar migrations manualmente
```

---

## Passo 3 — Aplicar Migrations

```bash
# Em ambiente controlado (não durante runtime da aplicação)
dotnet ef database update --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/catalog/NexTraceOne.Catalog.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/governance/NexTraceOne.Governance.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/configuration/NexTraceOne.Configuration.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/notifications/NexTraceOne.Notifications.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/integrations/NexTraceOne.Integrations.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
dotnet ef database update --project src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost
```

> **Alternativa:** Na primeira instalação, pode-se ativar temporariamente `NEXTRACE_AUTO_MIGRATE=true` e iniciar a aplicação uma vez. Depois desativar.

---

## Passo 4 — Bootstrap Inicial (Tenant + Admin)

### 4.1 Seeds automáticos

O `ConfigurationDefinitionSeeder` executa automaticamente em **todos os ambientes** e é idempotente. Cria:
- 458 parâmetros de configuração (feature flags, políticas, thresholds)
- 7 roles de sistema (PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview, ApprovalOnly)

### 4.2 Primeiro tenant e admin

O NexTraceOne requer pelo menos:
1. **Um tenant** (organização)
2. **Um utilizador PlatformAdmin**
3. **Pelo menos um ambiente** (ex.: Production)

Para o primeiro bootstrap, executar SQL direto ou usar a API após iniciar a aplicação:

```sql
-- Criar primeiro tenant
INSERT INTO iam_tenants ("Id", "Name", "Slug", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (gen_random_uuid(), 'MinhaOrganizacao', 'minha-org', true, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Criar primeiro admin (senha hash PBKDF2 — usar endpoint /api/v1/identity/register)
-- RECOMENDADO: usar a API em vez de SQL direto para hash seguro
```

**Recomendação:** Iniciar a aplicação e usar o endpoint `POST /api/v1/identity/users` ou o fluxo de registo para criar o primeiro utilizador. O primeiro utilizador registado num tenant vazio pode receber a role PlatformAdmin via:

```sql
-- Associar role PlatformAdmin ao primeiro utilizador
INSERT INTO iam_tenant_memberships ("Id", "TenantId", "UserId", "RoleId", "IsActive", "CreatedAt")
SELECT gen_random_uuid(), t."Id", u."Id", r."Id", true, NOW()
FROM iam_tenants t, iam_users u, iam_roles r
WHERE t."Slug" = 'minha-org'
  AND u."Email" = 'admin@example.com'
  AND r."Name" = 'PlatformAdmin'
ON CONFLICT DO NOTHING;
```

### 4.3 Criar ambientes

```sql
INSERT INTO env_environments ("Id", "TenantId", "Name", "Slug", "Type", "IsActive", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), t."Id", 'Production', 'production', 'Production', true, NOW(), NOW()
FROM iam_tenants t WHERE t."Slug" = 'minha-org'
ON CONFLICT DO NOTHING;
```

---

## Passo 5 — Verificar Instalação

### 5.1 Health check

```bash
curl -s https://your-domain/health | jq .
# Deve retornar: { "status": "Healthy", ... }
```

### 5.2 Login do admin

Aceder ao frontend e fazer login com as credenciais do admin criado no passo 4.

### 5.3 Verificar seed data

Após o primeiro login, verificar na interface:
- **Configuration** → parâmetros carregados (458 esperados)
- **Identity** → roles de sistema visíveis (7 roles)
- **Environments** → ambiente Production criado

---

## Passo 6 — Configuração Pós-Bootstrap

### 6.1 Configurar integrações (opcional)

- **OpenTelemetry** — configurar collector endpoint para observabilidade
- **SMTP** — configurar para notificações
- **Ollama/OpenAI** — configurar para funcionalidades AI

### 6.2 Ajustar parâmetros

Via interface web (**Configuration → Parameters**), ajustar:
- Janelas de deploy (change windows)
- Políticas de aprovação
- Thresholds de error budget
- Budget de tokens AI

---

## Seed Data por Ambiente

| Ambiente | Seeds Executados | Dados de Demo |
|----------|-----------------|---------------|
| **Development** | `ConfigurationDefinitionSeeder` + 7 SQL seed files (identidade, catálogo, governança, etc.) | ✅ Sim — dados de demonstração completos |
| **Staging** | `ConfigurationDefinitionSeeder` apenas | ❌ Não — dados devem vir de testes ou importação |
| **Production** | `ConfigurationDefinitionSeeder` apenas | ❌ Não — apenas configuração base e parâmetros |

> Os 7 ficheiros SQL em `src/platform/NexTraceOne.ApiHost/SeedData/` contêm dados de demonstração e **NÃO devem ser executados em produção**. Eles são usados apenas em ambiente de desenvolvimento para facilitar testes locais.

---

## Passo 7 — Providers opcionais (remover `simulatedNote` dos dashboards)

Alguns dashboards do NexTraceOne dependem de integrações externas. Quando essas integrações
não estão configuradas, a plataforma continua a funcionar mas usa fallbacks `Null*` que
descartam silenciosamente as operações correspondentes e marcam a resposta com um campo
`simulatedNote`. A página **Platform Admin → System Health** (`/admin/system-health`) e o
endpoint `GET /api/v1/platform/optional-providers` mostram o estado de cada provider em
tempo real, e os logs de arranque do ApiHost (`OptionalProviderStartupLogger`) emitem um
*warning* por cada provider em modo degradado fora de Development.

Checklist mínimo para uma instalação de produção sem `simulatedNote`:

| Provider | Config keys obrigatórias | Dashboard afetado | Secção de referência |
|---|---|---|---|
| **Canary rollouts** (`ICanaryProvider`) | `Canary:Provider` (ex: `Argo`, `Flagger`, `LaunchDarkly`) + credenciais específicas | `/admin/canary-dashboard` | Configuration → Parameters (Canary) |
| **Backup** (`IBackupProvider`) | `Backup:Provider` + credenciais da solução de backup (pgBackRest, Barman, Velero, …) | `/admin/backup-coordinator` | Configuration → Parameters (Backup) |
| **Kafka event producer** (`IKafkaEventProducer`) | `Kafka:BootstrapServers`, `Kafka:ClientId`, SASL/SSL conforme cluster | Eventos de integração outbound | `docs/deployment/ENVIRONMENT-CONFIGURATION.md` |
| **Cloud billing** (`ICloudBillingProvider`) | `FinOps:Billing:Provider` (`aws`, `azure`, `gcp`) + credenciais e dataset/bucket | FinOps dashboards | Configuration → Parameters (FinOps) |

Passos recomendados:

1. Partir de `/admin/system-health` para ver os providers que ainda estão como `NotConfigured`.
2. Para cada entrada, seguir o link **Setup docs** na página (aponta para a secção deste guia).
3. Preencher as configuration keys via **Configuration → Parameters** (nunca via `appsettings.*.json` — ver convenção em `IConfigurationResolutionService + ConfigurationDefinitionSeeder`).
4. Reiniciar o ApiHost e confirmar nos logs de arranque: `Optional providers configured: canary, backup, kafka, cloudBilling`.
5. Confirmar em `/admin/system-health` que todos os providers aparecem como `Configured`.

### Canary provider

Configure a integração com o controlador de canary rollouts (Argo Rollouts, Flagger,
LaunchDarkly, …) via **Configuration → Parameters**. Até estar configurado,
`/admin/canary-dashboard` devolve `simulatedNote` e lista vazia.

### Backup provider

Configure a integração com a solução de backup (pgBackRest, Barman, Velero, AWS Backup, …).
Até estar configurado, o monitor de backup devolve `simulatedNote`.

### Kafka

Sem `Kafka:BootstrapServers` configurado, `IKafkaEventProducer` resolve para
`NullKafkaEventProducer` e **todos os eventos outbound são descartados silenciosamente**.
Use um cluster Kafka gerido (MSK, Confluent Cloud, Event Hubs com Kafka API) ou self-hosted
com SASL/SSL.

### Cloud billing

Configure a ingestão de billing para FinOps contextual: AWS Cost and Usage Report (CUR),
Azure Cost Management ou GCP BigQuery Billing Export. Até estar configurado, os relatórios
FinOps mostram apenas dados sintéticos.

> **Nota.** O NexTraceOne foi desenhado para arrancar com estes providers em falta — isso
> **não** é um erro nem bloqueia o host. É, no entanto, inaceitável em produção para o
> domínio em questão (ex.: sem `IKafkaEventProducer` configurado, todos os eventos outbound
> são silenciosamente descartados). Use esta checklist como critério de prontidão operacional.

---

## Troubleshooting

| Problema | Solução |
|----------|---------|
| `InvalidOperationException: Cors:AllowedOrigins required` | Configurar `Cors__AllowedOrigins__0` com a URL do frontend |
| Migrations falham | Verificar permissões do utilizador PostgreSQL (precisa de `CREATE`) |
| Seeds não executam | `ConfigurationDefinitionSeeder` é automático; verificar logs para erros |
| AI não responde | Verificar `OllamaRuntime__Endpoint` ou `OpenAI__ApiKey` |
| OpenTelemetry sem dados | Verificar `OpenTelemetry__Endpoint` — default `localhost:4317` não funciona em produção |
| Dashboards mostram `simulatedNote` | Seguir o **Passo 7** acima; verificar `/admin/system-health` |
| Eventos não chegam ao Kafka | `IKafkaEventProducer.IsConfigured = false` → `NullKafkaEventProducer` descarta silenciosamente; configurar `Kafka:BootstrapServers` |

---

## Referências

- [Configuração de Ambiente](ENVIRONMENT-CONFIGURATION.md)
- [Docker e Compose](DOCKER-AND-COMPOSE.md)
- [CI/CD Pipelines](CI-CD-PIPELINES.md)
- [Migration Strategy](MIGRATION-STRATEGY.md)
- [Production Secrets Provisioning](../runbooks/PRODUCTION-SECRETS-PROVISIONING.md)
- [Security Baseline Checklist](../security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md)
