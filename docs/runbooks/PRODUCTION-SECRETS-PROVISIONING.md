# Production Secrets Provisioning — Runbook

## Objetivo

Guiar o operador no provisionamento completo de secrets e variáveis obrigatórias para o primeiro deploy do NexTraceOne em produção.

---

## Pré-requisitos

- [ ] Acesso de administrador ao repositório GitHub (para configurar secrets/variables)
- [ ] Servidor PostgreSQL de produção provisionado e acessível
- [ ] 4 bancos de dados criados: `nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`
- [ ] Utilizador PostgreSQL com permissões de leitura/escrita nos 4 bancos
- [ ] URL pública do ApiHost definida (DNS + HTTPS)
- [ ] URL pública do Frontend definida (DNS + HTTPS)

---

## Passo 1 — Gerar Secrets

### JWT Secret

```bash
openssl rand -base64 48
```

Resultado: string de 64 caracteres (384 bits de entropia). Guardar de forma segura.

### Passwords do PostgreSQL

Para cada banco, gerar uma password segura:

```bash
# Identity DB
openssl rand -base64 32

# Catalog DB
openssl rand -base64 32

# Operations DB
openssl rand -base64 32

# AI DB
openssl rand -base64 32
```

---

## Passo 2 — Construir Connection Strings

Formato:
```
Host=<hostname>;Port=5432;Database=<database>;Username=<username>;Password=<password>;Maximum Pool Size=10;SSL Mode=Require;Trust Server Certificate=false
```

### Exemplo

```
Host=pg-prod.internal;Port=5432;Database=nextraceone_identity;Username=nextraceone_prod;Password=<generated>;Maximum Pool Size=10;SSL Mode=Require;Trust Server Certificate=false
```

Repetir para os 4 bancos: `nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`.

---

## Passo 3 — Configurar GitHub Secrets

No repositório GitHub, navegar para: **Settings → Environments → production → Environment secrets**

| Secret Name | Valor |
|---|---|
| `PRODUCTION_CONN_IDENTITY` | Connection string completa para `nextraceone_identity` |
| `PRODUCTION_CONN_CATALOG` | Connection string completa para `nextraceone_catalog` |
| `PRODUCTION_CONN_OPERATIONS` | Connection string completa para `nextraceone_operations` |
| `PRODUCTION_CONN_AI` | Connection string completa para `nextraceone_ai` |

---

## Passo 4 — Configurar GitHub Variables

No repositório GitHub, navegar para: **Settings → Environments → production → Environment variables**

| Variable Name | Valor | Exemplo |
|---|---|---|
| `PRODUCTION_APIHOST_URL` | URL pública do ApiHost | `https://api.nextraceone.com` |
| `PRODUCTION_FRONTEND_URL` | URL pública do Frontend | `https://app.nextraceone.com` |

---

## Passo 5 — Configurar Runtime Environment Variables

Nos containers/serviços de produção, configurar:

| Variável | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Jwt__Secret` | JWT secret gerado no Passo 1 |
| `ConnectionStrings__IdentityDatabase` | Connection string do identity |
| `ConnectionStrings__CatalogDatabase` | Connection string do catalog |
| `ConnectionStrings__ContractsDatabase` | Connection string do catalog (mesmo banco) |
| `ConnectionStrings__DeveloperPortalDatabase` | Connection string do catalog (mesmo banco) |
| `ConnectionStrings__NexTraceOne` | Connection string do operations |
| `ConnectionStrings__ChangeIntelligenceDatabase` | Connection string do operations |
| `ConnectionStrings__WorkflowDatabase` | Connection string do operations |
| `ConnectionStrings__RulesetGovernanceDatabase` | Connection string do operations |
| `ConnectionStrings__PromotionDatabase` | Connection string do operations |
| `ConnectionStrings__IncidentDatabase` | Connection string do operations |
| `ConnectionStrings__CostIntelligenceDatabase` | Connection string do operations |
| `ConnectionStrings__RuntimeIntelligenceDatabase` | Connection string do operations |
| `ConnectionStrings__ReliabilityDatabase` | Connection string do operations |
| `ConnectionStrings__GovernanceDatabase` | Connection string do operations |
| `ConnectionStrings__AutomationDatabase` | Connection string do operations |
| `ConnectionStrings__AuditDatabase` | Connection string do identity (mesmo banco) |
| `ConnectionStrings__AiGovernanceDatabase` | Connection string do AI |
| `ConnectionStrings__ExternalAiDatabase` | Connection string do AI (mesmo banco) |
| `ConnectionStrings__AiOrchestrationDatabase` | Connection string do AI (mesmo banco) |
| `NEXTRACE_SKIP_INTEGRITY` | `false` |

**Nota:** Múltiplas connection strings apontam para o mesmo banco físico. Use a mesma connection string para todas as que partilham o mesmo banco lógico.

---

## Passo 6 — Configurar GitHub Environment Protection Rules

No repositório GitHub, navegar para: **Settings → Environments → production**

- [ ] Ativar **Required reviewers** — definir pelo menos 1 reviewer obrigatório
- [ ] Opcionalmente configurar **Wait timer** (e.g., 5 minutos de delay)
- [ ] Opcionalmente restringir para **Deployment branches** → `main` apenas

---

## Passo 7 — Validar Configuração

### 7.1 — Verificar que o startup funciona

```bash
# Definir variáveis de ambiente de produção e iniciar
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/platform/NexTraceOne.ApiHost/
```

O startup deve:
- ✅ Validar JWT secret (>= 32 chars)
- ✅ Validar todas as connection strings (não vazias)
- ✅ Logar "Startup validation summary — Environment: Production, ..."
- ✅ Responder em `/live` com `{"status":"Healthy"}`

### 7.2 — Verificar health checks

```bash
curl -s https://api.nextraceone.com/live | jq .
curl -s https://api.nextraceone.com/ready | jq .
curl -s https://api.nextraceone.com/health | jq .
```

---

## Troubleshooting

### Startup falha com "missing critical configuration sections"

**Causa:** Secção `ConnectionStrings` ou `Jwt` ausente na configuração.
**Resolução:** Verificar que as variáveis de ambiente estão definidas com os nomes corretos (usar `__` como separador).

### Startup falha com "Jwt:Secret must be configured"

**Causa:** `Jwt__Secret` não está definido ou está vazio.
**Resolução:** Definir a variável de ambiente `Jwt__Secret` com o valor gerado no Passo 1.

### Startup falha com "Jwt:Secret ... must be at least 32 characters"

**Causa:** O JWT secret fornecido é curto demais.
**Resolução:** Gerar novo secret com `openssl rand -base64 48` (produz 64 caracteres).

### Startup falha com "connection string ... must be configured"

**Causa:** Uma ou mais connection strings estão vazias em non-Development.
**Resolução:** Verificar que todas as 19 connection strings estão definidas via variáveis de ambiente.

---

## Referências

- [WAVE-1-PRODUCTION-SECRETS-BASELINE.md](../execution/WAVE-1-PRODUCTION-SECRETS-BASELINE.md)
- [StartupValidation.cs](../../src/platform/NexTraceOne.ApiHost/StartupValidation.cs)
- [.env.example](../../.env.example)
- [production.yml](../../.github/workflows/production.yml)
