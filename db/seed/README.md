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

## Role Permissions e Module Access Policies

### Abordagem programática (preferencial)

O NexTraceOne executa automaticamente o seed de autorização no arranque da aplicação
em **todos os ambientes** (produção, staging, desenvolvimento) através de
`SeedAuthorizationDataExtensions.SeedAuthorizationDataAsync()`.

Este seed programático:

- Lê diretamente dos catálogos C# (`RolePermissionCatalog` e `ModuleAccessPolicyCatalog`)
- É idempotente (verifica se já existem dados antes de inserir)
- Cobre todos os 7 papéis do sistema
- Não requer intervenção manual
- Garante alinhamento automático com o código

**Sequência de arranque:**
```
Migrations → SeedConfigurationDefinitions → SeedAuthorizationData → SeedDevelopmentData
```

### Abordagem SQL (fallback manual)

Os scripts SQL também incluem dados de autorização para cenários de provisionamento
manual via `psql`:

| Script | Dados de autorização |
|---|---|
| `seed_production.sql` | PlatformAdmin: 93 role permissions + 16 module access policies (wildcard) |
| `seed_development.sql` | 6 papéis restantes: ~190 role permissions + ~100 module access policies granulares |

### Tabelas de autorização

| Tabela | Descrição |
|---|---|
| `iam_role_permissions` | Permissões planas por papel (RoleId + PermissionCode + TenantId) |
| `iam_module_access_policies` | Políticas de acesso granular módulo/página/ação por papel |

### Contagens por papel (role permissions)

| Papel | Nº de permissões | Script |
|---|---|---|
| PlatformAdmin | 93 | `seed_production.sql` |
| TechLead | 52 | `seed_development.sql` |
| Developer | 28 | `seed_development.sql` |
| Viewer | 18 | `seed_development.sql` |
| Auditor | 24 | `seed_development.sql` |
| SecurityReview | 25 | `seed_development.sql` |
| ApprovalOnly | 10 | `seed_development.sql` |

### Pipeline de autorização (4 passos)

A cascata de autorização usa estes dados na seguinte ordem:

```
1. JWT claims (permissões no token)
2. DB RolePermission (iam_role_permissions)
3. DB ModuleAccessPolicy (iam_module_access_policies)
4. JIT grants (acesso temporário just-in-time)
```

Se nenhum passo conceder acesso, o pedido é negado.
Se o DB não tiver dados, o sistema usa o catálogo estático como fallback.
