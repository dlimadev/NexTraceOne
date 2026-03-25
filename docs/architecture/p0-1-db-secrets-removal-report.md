# P0.1 — DB Secrets Removal Report

**Data de execução:** 2026-03-25  
**Fase:** P0.1 — Remoção de passwords hardcoded das connection strings  
**Estado:** CONCLUÍDO

---

## 1. Contexto

A auditoria de segurança identificou a password `ouro18` hardcoded em múltiplos ficheiros
commitados no repositório do NexTraceOne, constituindo uma violação CRITICAL da política de
configuração segura.

Esta fase resolve exclusivamente o problema das credenciais de base de dados expostas.
Outros problemas de segurança (JWT secret, AES fallback, CORS) são endereçados em fases
subsequentes (P0.2+).

---

## 2. Ficheiros alterados

### 2.1 `src/platform/NexTraceOne.ApiHost/appsettings.Development.json`

**Problema:** 20 connection strings com `Password=ouro18` em texto plano.

**Alteração:** Substituição de `Password=ouro18` por `Password=REPLACE_VIA_ENV` em todas as
entradas da secção `ConnectionStrings`.

Connection strings afetadas:

| Chave | Antes | Depois |
|---|---|---|
| `NexTraceOne` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `IdentityDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `CatalogDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `ContractsDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `DeveloperPortalDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `ChangeIntelligenceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `WorkflowDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `RulesetGovernanceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `PromotionDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `IncidentDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `CostIntelligenceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `RuntimeIntelligenceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `ReliabilityDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `AuditDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `AiGovernanceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `GovernanceDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `ExternalAiDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `AiOrchestrationDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `AutomationDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |
| `ConfigurationDatabase` | `Password=ouro18` | `Password=REPLACE_VIA_ENV` |

> **Nota:** `appsettings.json` (produção) já tinha `Password=REPLACE_VIA_ENV` desde E18.
> Nenhuma alteração adicional foi necessária nesse ficheiro.

---

### 2.2 `tests/platform/NexTraceOne.E2E.Tests/Infrastructure/ApiE2EFixture.cs`

**Problema:** Array `LocalAdminConnectionCandidates` continha dois fallbacks hardcoded com `Password=ouro18`.

**Alteração:** Remoção das entradas com `ouro18`. Mantida apenas:
- Variável de ambiente `NEXTRACE_TEST_ADMIN_CONNECTION_STRING` (prioridade máxima)
- Candidato genérico `Username=postgres;Password=postgres` (conta padrão PostgreSQL)

A lógica de fallback permanece funcional: se Testcontainers (Docker) não estiver disponível,
o teste tentará a variável de ambiente e depois o PostgreSQL com credenciais padrão.

---

### 2.3 `tests/platform/NexTraceOne.IntegrationTests/Infrastructure/ApiHostPostgreSqlFixture.cs`

**Problema:** Mesmo padrão — `LocalAdminConnectionCandidates` com dois fallbacks `ouro18`.

**Alteração:** Idêntica à do E2E fixture — remoção das linhas com `ouro18`, mantendo
`NEXTRACE_TEST_ADMIN_CONNECTION_STRING` e o candidato `postgres/postgres`.

---

## 3. Estratégia adoptada

### 3.1 Resolução de connection strings em runtime

O ASP.NET Core suporta nativamente a substituição de valores de `appsettings.json` via
variáveis de ambiente, usando o separador duplo underscore (`__`):

```
ConnectionStrings__NexTraceOne=Host=...;Password=<real-password>
```

Esta é a estratégia recomendada e já documentada no `.env.example`.

### 3.2 Placeholder `REPLACE_VIA_ENV`

O valor `REPLACE_VIA_ENV` serve como marcador explícito de que a password deve ser fornecida
externamente. O `StartupValidation.cs` do ApiHost já verifica connection strings vazias e
emite avisos/erros conforme o ambiente.

Em Development, a aplicação pode arrancar com o placeholder presente (emite aviso) mas falhará
ao tentar conectar à base de dados, o que é comportamento correto e esperado.

### 3.3 Desenvolvimento local

Para desenvolvimento local, o programador deve:

1. Copiar `.env.example` para `.env` e preencher `POSTGRES_PASSWORD`
2. Usar `dotnet user-secrets` para configurar a password sem a commitar:
   ```
   dotnet user-secrets set "ConnectionStrings:NexTraceOne" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=<local-password>"
   ```
3. Ou definir a variável de ambiente `ConnectionStrings__NexTraceOne` antes de arrancar.

---

## 4. Alinhamento com `.env.example`

O ficheiro `.env.example` já está alinhado com esta estratégia:

```
POSTGRES_PASSWORD=change-me-in-production
CONNECTION_STRING_NEXTRACEONE=Host=postgres;Port=5432;Database=nextraceone;Username=nextraceone;Password=change-me-in-production;Maximum Pool Size=20
```

Nenhuma alteração foi necessária no `.env.example`.

---

## 5. Validação funcional

- Verificado que `grep -rn "ouro18" src/ tests/` retorna zero resultados após a alteração.
- `appsettings.json` já tinha `REPLACE_VIA_ENV` — sem regressão.
- `appsettings.Development.json` agora alinhado com `appsettings.json`.
- Lógica de fallback dos testes preservada; apenas as entradas com credenciais hardcoded foram removidas.
- Suite de testes unitários/integração (2628 testes) não depende de `ouro18` — os test fixtures
  usam Testcontainers como caminho principal.

---

## 6. Ficheiros alterados (sumário)

| Ficheiro | Tipo de alteração |
|---|---|
| `src/platform/NexTraceOne.ApiHost/appsettings.Development.json` | Substituição de `ouro18` por `REPLACE_VIA_ENV` em 20 connection strings |
| `tests/platform/NexTraceOne.E2E.Tests/Infrastructure/ApiE2EFixture.cs` | Remoção de 2 candidatos hardcoded com `ouro18` |
| `tests/platform/NexTraceOne.IntegrationTests/Infrastructure/ApiHostPostgreSqlFixture.cs` | Remoção de 2 candidatos hardcoded com `ouro18` |
