# Massa de Teste — Identity & Access

## Objetivo

Esta massa de teste fornece um conjunto completo de dados fictícios para o módulo **IdentityAccess** do NexTraceOne. Permite validar funcionalmente todos os fluxos de identidade, autenticação, autorização, acesso privilegiado e recertificação sem dependências externas.

Os dados cobrem cenários enterprise reais:
- Multi-tenancy com utilizadores em múltiplos tenants e roles diferentes
- Hierarquia de ambientes (Development → Pre-Production → Production) com acessos granulares
- Autenticação local e federada (OIDC)
- Acesso privilegiado temporário (Break Glass, JIT, Delegação)
- Sessões activas e expiradas para validação de ciclo de vida
- Eventos de segurança com risk scores para validação de auditoria

---

## Pré-requisitos

1. **PostgreSQL 16** em execução (local ou Docker)
2. **Schema de base de dados migrado** — as tabelas do módulo IdentityAccess devem existir antes de executar os seeds

```bash
# Aplicar migrações EF Core (cria as tabelas)
dotnet ef database update \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

3. **Variáveis de conexão** configuradas (ou connection string definida em `appsettings.Development.json`)

---

## Ordem de Execução

Os scripts devem ser executados **na ordem numérica** — cada script depende dos dados criados pelos anteriores (foreign keys).

| # | Ficheiro | Descrição | Registos criados |
|---|---------|-----------|------------------|
| 00 | `00-reset-identity-access-test-data.sql` | **Reset completo** — elimina todos os dados de teste respeitando foreign keys | 0 (delete) |
| 01 | `01-seed-tenants.sql` | Cria 3 tenants: ACME Corp (activo), Globex Inc (activo), Initech (inactivo) | 3 tenants |
| 02 | `02-seed-environments.sql` | Cria 6 ambientes: Development, Pre-Production, Production × 2 tenants activos | 6 environments |
| 03 | `03-seed-users.sql` | Cria 10 utilizadores cobrindo todos os perfis enterprise | 10 users |
| 04 | `04-seed-roles-and-permissions.sql` | Cria 7 roles de sistema e 17 permissões granulares + matriz role→permission | 7 roles, 17 permissions, ~50 mappings |
| 05 | `05-seed-memberships.sql` | Liga utilizadores a tenants com roles | ~12 memberships |
| 06 | `06-seed-environment-access.sql` | Atribui acessos granulares (read/write/admin) por utilizador/ambiente | ~20 acessos |
| 07 | `07-seed-privileged-access.sql` | Cria cenários de Break Glass, JIT Access e Delegação | ~6 pedidos privilegiados |
| 08 | `08-seed-sessions.sql` | Cria sessões activas e expiradas | ~8 sessões |
| 09 | `09-seed-security-events.sql` | Cria 50+ eventos de segurança com risk scores, IPs e metadata | 50+ eventos |

---

## Credenciais de Teste

### Utilizadores Locais

Todos os utilizadores locais têm a password **`Test@12345`** (hash PBKDF2 fictício nos seeds).

| Email | Nome | Role (ACME Corp) | Role (Globex Inc) | Tipo |
|-------|------|-------------------|-------------------|------|
| `admin@acme-corp.test` | Carlos Admin | PlatformAdmin | — | Local |
| `techlead@acme-corp.test` | Ana TechLead | TechLead | — | Local |
| `dev@acme-corp.test` | Bruno Developer | Developer | — | Local |
| `viewer@acme-corp.test` | Diana Viewer | Viewer | — | Local |
| `security@acme-corp.test` | *(Security)* | SecurityReview | — | Local |
| `approver@acme-corp.test` | *(Approver)* | ApprovalOnly | — | Local |
| `multi@globex-inc.test` | *(Multi-tenant)* | Developer | TechLead | Local |
| `devonly@globex-inc.test` | *(DevOnly)* | — | Developer | Local |

### Utilizadores Federados (OIDC)

| Email | Nome | Provider | Tipo |
|-------|------|----------|------|
| `oidc@acme-corp.test` | *(OIDC User)* | OIDC externo | Federado (sem password local) |
| `localfallback@acme-corp.test` | *(Hybrid User)* | OIDC + Local | Híbrido (password + link OIDC) |

### Tenants

| Slug | Nome | Estado | ID |
|------|------|--------|-----|
| `acme-corp` | ACME Corp | ✅ Activo | `a1000000-0000-0000-0000-000000000001` |
| `globex-inc` | Globex Inc | ✅ Activo | `a2000000-0000-0000-0000-000000000002` |
| `initech-inactive` | Initech | ❌ Inactivo | `a3000000-0000-0000-0000-000000000003` |

### Ambientes (por tenant activo)

| Ambiente | Slug | Sort Order | Nível típico |
|----------|------|------------|-------------|
| Development | `development` | 0 | Todos os developers |
| Pre-Production | `pre-production` | 1 | TechLead + PlatformAdmin |
| Production | `production` | 2 | PlatformAdmin (admin), TechLead (write) |

---

## Cenários de Teste Viabilizados

### Autenticação

| Cenário | Utilizador | Resultado esperado |
|---------|-----------|-------------------|
| Login local com sucesso | `dev@acme-corp.test` / `Test@12345` | `LoginResponse` com token e permissões |
| Login com password errada | `dev@acme-corp.test` / `wrong` | Erro `InvalidCredentials`, incremento `FailedLoginCount` |
| Login de conta desactivada | *(desactivar via API primeiro)* | Erro `AccountDeactivated` |
| Login de conta bloqueada | *(provocar lockout via tentativas falhadas)* | Erro `AccountLocked` |
| Login OIDC (fluxo completo) | `oidc@acme-corp.test` | Redirect → Provider → Callback → Sessão criada |
| Login híbrido (local fallback) | `localfallback@acme-corp.test` | Login local funciona quando OIDC indisponível |

### Multi-Tenancy

| Cenário | Utilizador | Resultado esperado |
|---------|-----------|-------------------|
| Listar tenants do utilizador | `multi@globex-inc.test` | 2 tenants: ACME Corp (Developer) e Globex Inc (TechLead) |
| Seleccionar tenant | `multi@globex-inc.test` → ACME Corp | Token com role Developer e permissões de dev |
| Seleccionar outro tenant | `multi@globex-inc.test` → Globex Inc | Token com role TechLead e permissões de lead |
| Aceder tenant inactivo | Qualquer → Initech | Erro `TenantNotFound` ou `TenantNotActive` |

### Autorização por Ambiente

| Cenário | Utilizador | Ambiente | Resultado esperado |
|---------|-----------|----------|-------------------|
| Developer acede Development | `dev@acme-corp.test` | Development | ✅ Acesso write |
| Developer acede Production | `dev@acme-corp.test` | Production | ❌ Sem acesso ou read-only |
| TechLead acede Pre-Production | `techlead@acme-corp.test` | Pre-Production | ✅ Acesso write |
| Admin acede qualquer ambiente | `admin@acme-corp.test` | Production | ✅ Acesso admin |

### Acesso Privilegiado

| Cenário | Fluxo | Resultado esperado |
|---------|-------|-------------------|
| Break Glass — pedido válido | `dev@acme-corp.test` pede acesso de emergência | BreakGlassRequest criado, SecurityEvent gerado |
| Break Glass — quota excedida | 4º pedido no trimestre | Erro `BreakGlassQuotaExceeded` |
| JIT Access — aprovação | `dev` pede → `techlead` aprova | Acesso concedido por 8 horas |
| JIT Access — auto-aprovação | `dev` pede → `dev` tenta aprovar | Erro `JitSelfApprovalNotAllowed` |
| Delegação — válida | `techlead` delega permissões a `dev` | Delegation criada, SecurityEvent gerado |
| Delegação — system admin | Tentar delegar PlatformAdmin | Erro `DelegationSystemAdminNotAllowed` |

### Sessões

| Cenário | Fluxo | Resultado esperado |
|---------|-------|-------------------|
| Refresh token rotation | `POST /auth/refresh` com token válido | Novo par access + refresh |
| Refresh token replay | Reusar refresh token já rotacionado | Erro `InvalidRefreshToken` |
| Revogação administrativa | Admin revoga sessão de outro user | Sessão marcada como revogada |
| Listagem de sessões activas | `GET /sessions` | Lista com IP, user agent, data de criação |

### Access Review

| Cenário | Fluxo | Resultado esperado |
|---------|-------|-------------------|
| Iniciar campanha | PlatformAdmin cria campanha | AccessReviewCampaign + AccessReviewItems criados |
| Aprovar item | Reviewer aprova acesso | Item marcado como aprovado |
| Revogar item | Reviewer revoga acesso | TenantMembership desactivada, SecurityEvent gerado |
| Expiração automática | Campanha expira sem decisão | Itens pendentes auto-revogados |

---

## Como Executar

### Execução completa (reset + seed)

```bash
# Executar todos os scripts na ordem correcta
cd database/seeds/identity-access/

psql -h localhost -U nextraceone -d nextraceone_dev \
  -f 00-reset-identity-access-test-data.sql \
  -f 01-seed-tenants.sql \
  -f 02-seed-environments.sql \
  -f 03-seed-users.sql \
  -f 04-seed-roles-and-permissions.sql \
  -f 05-seed-memberships.sql \
  -f 06-seed-environment-access.sql \
  -f 07-seed-privileged-access.sql \
  -f 08-seed-sessions.sql \
  -f 09-seed-security-events.sql
```

### Execução com Docker

```bash
# Se PostgreSQL está em Docker
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 00-reset-identity-access-test-data.sql
docker exec -i nextraceone-db psql -U nextraceone -d nextraceone_dev < 01-seed-tenants.sql
# ... (repetir para cada ficheiro na ordem)
```

### Execução selectiva (apenas reset)

```bash
# Limpar dados de teste sem re-inserir
psql -h localhost -U nextraceone -d nextraceone_dev \
  -f 00-reset-identity-access-test-data.sql
```

### Script de conveniência (todos de uma vez)

```bash
# Concatenar e executar todos os scripts
cat database/seeds/identity-access/*.sql | \
  psql -h localhost -U nextraceone -d nextraceone_dev
```

> **Nota:** Os scripts usam `ON CONFLICT DO NOTHING` para idempotência — é seguro re-executar sem o reset prévio, embora o reset garanta um estado limpo.

---

## Aviso de Segurança

> ⚠️ **ATENÇÃO: Dados exclusivamente para desenvolvimento e testes locais.**

- As passwords de teste (`Test@12345`) são **fracas e públicas** — nunca usar em produção.
- Os hashes PBKDF2 nos seeds são **placeholders reconhecíveis** — o login local com estes hashes só funciona se o `Pbkdf2PasswordHasher` do sistema os reconhecer.
- Os IDs dos registos são **determinísticos e previsíveis** (ex: `u1000000-...-01`) para facilitar testes — nunca usar este padrão em produção.
- Os tenants, emails e dados pessoais são **completamente fictícios**.
- O script `00-reset-identity-access-test-data.sql` **elimina dados irrecuperáveis** — confirmar sempre o ambiente antes de executar.
- **Nunca executar estes scripts em ambientes de produção, staging ou qualquer ambiente com dados reais.**

---

## Reaproveitamento para Outros Módulos

Esta estrutura de seed data serve como **padrão** para os restantes bounded contexts do NexTraceOne.

### Convenções a seguir

1. **Directoria:** `database/seeds/{bounded-context-slug}/` (ex: `database/seeds/catalog/`, `database/seeds/change-governance/`)

2. **Nomenclatura de ficheiros:** `{NN}-{verbo}-{entidade}.sql` onde NN é a ordem de execução
   - `00-reset-{context}-test-data.sql` — sempre o primeiro (cleanup)
   - `01-seed-{entidade-raiz}.sql` — entidade sem dependências externas
   - `02-seed-{entidade-dependente}.sql` — entidades que referenciam a anterior

3. **Idempotência:** Usar `ON CONFLICT ("Id") DO NOTHING` em todos os INSERTs

4. **IDs determinísticos:** Padrão `{prefixo}000000-0000-0000-0000-00000000000{N}` para facilitar referências entre scripts
   - Tenants: `a{N}000000-...`
   - Users: `u{N}000000-...`
   - Roles: `r{N}000000-...`
   - Permissions: `p{N}000000-...`

5. **Documentação:** Cada directoria de seeds deve ter um `README.md` em português com a mesma estrutura deste ficheiro

6. **Reset seguro:** O script de reset deve eliminar dados pela **ordem inversa de dependências** (dependentes primeiro) e filtrar pelos slugs/IDs de teste para não afectar dados reais

7. **Comentários SQL:** Cada INSERT deve ter um comentário em português explicando o cenário que cria

8. **Dependências cross-module:** Se um módulo depende de dados de outro (ex: Catalog depende de Tenants do IdentityAccess), documentar a dependência e a ordem de execução no README

### Exemplo para novo módulo

```
database/seeds/catalog/
├── README.md
├── 00-reset-catalog-test-data.sql
├── 01-seed-api-assets.sql
├── 02-seed-service-assets.sql
├── 03-seed-contract-versions.sql
└── 04-seed-dependencies.sql
```

> **Dependência:** Executar primeiro os seeds de `identity-access/` (tenants e users são referenciados como foreign keys).
