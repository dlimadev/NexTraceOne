# db/seed

Scripts SQL de seed de dados para a plataforma NexTraceOne.

## Pré-requisitos

1. Base de dados PostgreSQL criada e acessível
2. Utilizador `nextraceone` criado com as permissões adequadas
3. Todas as migrations EF Core aplicadas com sucesso:

```powershell
# A partir da raiz do repositório
cd src\platform\NexTraceOne.ApiHost
dotnet run --launch-profile "Development"
# ou aplicar manualmente via dotnet ef database update em cada módulo
```

## Ficheiros

| Ficheiro | Quando usar |
|---|---|
| `seed_production.sql` | Qualquer ambiente — dados de referência obrigatórios |
| `seed_development.sql` | Apenas ambientes de desenvolvimento/teste |

## Ordem de execução

**Sempre nesta sequência:**

```
1. seed_production.sql
2. seed_development.sql  (apenas em dev/test)
```

## Como executar

### Via psql (linha de comandos)

```bash
# Produção / Staging / Qualquer ambiente
psql -h localhost -p 5432 -U nextraceone -d nextraceone -f db/seed/seed_production.sql

# Apenas em desenvolvimento
psql -h localhost -p 5432 -U nextraceone -d nextraceone -f db/seed/seed_development.sql
```

### Via PowerShell (ambiente local)

```powershell
$env:PGPASSWORD = "ouro18"

psql -h localhost -p 5432 -U nextraceone -d nextraceone `
     -f "db\seed\seed_production.sql"

# Apenas em dev
psql -h localhost -p 5432 -U nextraceone -d nextraceone `
     -f "db\seed\seed_development.sql"
```

## Credenciais de teste (seed_development.sql)

> ⚠️ **Apenas para uso local/dev.** Nunca usar estas credenciais em produção.

| Email | Senha | Papel |
|---|---|---|
| `admin@nextraceone.io` | `Admin@2026!` | PlatformAdmin |
| `techlead@nextraceone.io` | `TechLead@2026!` | TechLead |
| `dev@nextraceone.io` | `Dev@2026!` | Developer |
| `viewer@nextraceone.io` | `Viewer@2026!` | Viewer |
| `auditor@nextraceone.io` | `Auditor@2026!` | Auditor |
| `secreview@nextraceone.io` | `SecReview@2026!` | SecurityReview |
| `approval@nextraceone.io` | `Approval@2026!` | ApprovalOnly |

## IDs de referência fixos

Os scripts usam IDs fixos e previsíveis para facilitar debug e referência cruzada.

| Entidade | ID |
|---|---|
| Tenant NexTraceOne | `a0000000-0000-0000-0000-000000000001` |
| Env Development | `c0000000-0000-0000-0000-000000000001` |
| Env Staging | `c0000000-0000-0000-0000-000000000002` |
| Env Production | `c0000000-0000-0000-0000-000000000003` |
| User admin | `b0000000-0000-0000-0000-000000000001` |
| Service: platform-api | `a3000000-0000-0000-0000-000000000001` |
| Service: payment-gateway | `a3000000-0000-0000-0000-000000000002` |
| Service: identity-service | `a3000000-0000-0000-0000-000000000003` |

## Scripts são idempotentes

Ambos os scripts usam `ON CONFLICT DO NOTHING`. É seguro executá-los múltiplas
vezes sem efeitos colaterais.

## Roles e Permissions

Os papéis (`iam_roles`) e permissões (`iam_permissions`) **não são inseridos
pelos scripts SQL** — são seeded automaticamente via `HasData` nas migrations
EF Core com IDs fixos:

| Papel | ID |
|---|---|
| PlatformAdmin | `1e91a557-fade-46df-b248-0f5f5899c001` |
| TechLead | `1e91a557-fade-46df-b248-0f5f5899c002` |
| Developer | `1e91a557-fade-46df-b248-0f5f5899c003` |
| Viewer | `1e91a557-fade-46df-b248-0f5f5899c004` |
| Auditor | `1e91a557-fade-46df-b248-0f5f5899c005` |
| SecurityReview | `1e91a557-fade-46df-b248-0f5f5899c006` |
| ApprovalOnly | `1e91a557-fade-46df-b248-0f5f5899c007` |
