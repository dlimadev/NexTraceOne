# Estratégia de Remoção das Migrations Antigas

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Definir como as 29 migrations existentes e 20 DbContexts atuais serão limpos de forma segura, sem perda de consistência e sem restos de schema antigo.

---

## Inventário Atual de Migrations

| Módulo | DbContext | Migrations | Ficheiros |
|--------|-----------|-----------|----------|
| Identity & Access | IdentityDbContext | 2 | `InitialCreate`, `AddIsPrimaryProductionToEnvironment` |
| AI & Knowledge — Governance | AiGovernanceDbContext | 7 | `InitialCreate` + 6 evoluções |
| AI & Knowledge — ExternalAI | ExternalAiDbContext | 1 | `InitialCreate` |
| AI & Knowledge — Orchestration | AiOrchestrationDbContext | 1 | `InitialCreate` |
| Audit & Compliance | AuditDbContext | 2 | `InitialCreate`, `Phase3ComplianceDomain` |
| Catalog — Contracts | ContractsDbContext | 1 | `InitialCreate` |
| Catalog — Graph | CatalogGraphDbContext | 1 | `InitialCreate` |
| Catalog — DevPortal | DeveloperPortalDbContext | 1 | `InitialCreate` |
| Change Governance — ChangeIntel | ChangeIntelligenceDbContext | 1 | `InitialCreate` |
| Change Governance — Promotion | PromotionDbContext | 1 | `InitialCreate` |
| Change Governance — Ruleset | RulesetGovernanceDbContext | 1 | `InitialCreate` |
| Change Governance — Workflow | WorkflowDbContext | 1 | `InitialCreate` |
| Governance | GovernanceDbContext | 3 | `InitialCreate` + 2 evoluções |
| OpIntel — Incidents | IncidentDbContext | 1 | `InitialCreate` |
| OpIntel — Cost | CostIntelligenceDbContext | 2 | `InitialCreate`, `AddCostImportPipeline` |
| OpIntel — Runtime | RuntimeIntelligenceDbContext | 1 | `InitialCreate` |
| OpIntel — Reliability | ReliabilityDbContext | 1 | `InitialCreate` |
| OpIntel — Automation | AutomationDbContext | 1 | `InitialCreate` |
| Configuration | ConfigurationDbContext | 0 | — |
| Notifications | NotificationsDbContext | 0 | — |
| **TOTAL** | **20 DbContexts** | **29 migrations** | — |

---

## Estratégia de Remoção por Fases

### Fase 1 — Congelamento (pré-remoção)

**Ações:**
1. **Tag do repositório** — Criar tag `pre-migration-reset-v1` no commit atual
2. **Snapshot do schema** — Exportar DDL de todas as tabelas existentes em PostgreSQL para `docs/architecture/legacy-schema-snapshot.sql`
3. **Inventário de dados seed** — Documentar quais dados estão embutidos nas migrations via `HasData()` (Identity roles/permissions/tenant)
4. **Backup dos ModelSnapshots** — Manter cópia dos `*ModelSnapshot.cs` no tag

**Resultado:** Estado atual congelado e recuperável.

---

### Fase 2 — Remoção das Migrations (por módulo, seguindo a ordem de ondas)

**Procedimento por módulo:**

```
1. Remover pasta Migrations/ inteira do módulo
   └─ Apagar todos os ficheiros *.cs dentro de Infrastructure/*/Persistence/Migrations/
   └─ Apagar ModelSnapshot.cs correspondente
   └─ Apagar DesignTimeFactory se já não necessário

2. Verificar que o DbContext continua a compilar
   └─ dotnet build do projecto Infrastructure

3. Verificar que não existem referências quebradas
   └─ grep -r "Migrations" no módulo

4. Confirmar que o DbContext não chama EnsureCreated
   └─ grep -r "EnsureCreated" (já confirmado: 0 chamadas)
```

**Ordem de remoção:** Segue a ordem das ondas definida em `postgresql-baseline-execution-order.md`.

---

### Fase 3 — Limpeza de Schema Antigo

**Ações para evitar restos:**

1. **`__EFMigrationsHistory`** — A tabela de histórico de migrations será removida por módulo:
   ```sql
   -- Para cada DbContext, ao recriar baseline:
   DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
   -- Nota: cada DbContext pode ter esquema diferente
   ```

2. **Tabelas órfãs** — Identificar tabelas que existem no DB mas não estão mapeadas em nenhum DbContext:
   ```sql
   -- Comparar tabelas no banco com tabelas nos Configurations
   SELECT table_name FROM information_schema.tables 
   WHERE table_schema = 'public' 
   AND table_name NOT LIKE '%outbox%'
   ORDER BY table_name;
   ```

3. **Outbox tables** — Cada módulo tem outbox com prefixo diferente; todas devem ser recriadas na nova baseline:
   - `identity_outbox_messages`, `cfg_outbox_messages`, `aud_outbox_messages`, etc.

---

### Fase 4 — Tratamento de Seeds Implícitos

**Problema:** As migrations do Identity & Access contêm `HasData()` para roles, permissions e tenant default. Ao apagar as migrations, esses seeds são perdidos.

**Solução:**
1. **Extrair seeds das migrations** para seeders explícitos antes de apagar
2. **Converter HasData()** → seeder programático idempotente (como `ConfigurationDefinitionSeeder`)
3. **Documentar** quais seeds estavam implícitos nas migrations:

| Módulo | Tipo de Seed Implícito | Dados |
|--------|----------------------|-------|
| Identity & Access | HasData() em `RoleConfiguration.cs` | 7 system roles |
| Identity & Access | HasData() em `PermissionConfiguration.cs` | 73+ permissions |
| Identity & Access | HasData() em `TenantConfiguration.cs` | 1 default tenant |
| Governance | HasData() em configurations | Status enums, categorias |

---

### Fase 5 — Tratamento de Scripts de Setup Antigos

**Verificar e limpar:**
- [ ] Scripts SQL em `scripts/` ou `sql/` que criam schema manualmente
- [ ] Docker init scripts que executam migrations
- [ ] CI/CD pipelines que aplicam migrations
- [ ] docker-compose.yml volumes ou init scripts
- [ ] Documentação que referencia migrations antigas

---

## Tratamento de EnsureCreated

**Estado atual:** ✅ Zero chamadas `EnsureCreated` encontradas no codebase.

**Regra:** `EnsureCreated` NUNCA deve ser adicionado. A criação do schema deve ser exclusivamente via migrations.

**Salvaguarda:** Adicionar analyzer rule ou code review check que rejeite `EnsureCreated` em PRs.

---

## Como Evitar Recriar Problemas do Desenho Atual

| Problema Atual | Mitigação na Nova Baseline |
|---------------|---------------------------|
| Prefixos inconsistentes (`oi_`, `ct_`, `identity_`) | Validar prefixo via convention no DbContext base |
| Seeds em HasData() dentro de migrations | Seeds em seeders explícitos, idempotentes, versionados |
| Módulos partilhando DbContext (Integrations em Governance) | 1 DbContext por módulo obrigatório antes da baseline |
| 7 migrations evolutivas no AI Governance | 1 única baseline migration por DbContext |
| ModelSnapshot drift | Gerar snapshot fresco a partir do modelo final |
| Ausência de RowVersion | Configurar xmin em todas as entidades na nova baseline |
| FK cross-module via tabela | Usar referência lógica (ID stored, sem FK física) entre módulos |

---

## Resumo do Fluxo

```
Tag repositório (snapshot)
    ↓
Para cada módulo (por onda):
    ↓
    Extrair seeds implícitos → seeder explícito
    ↓
    Remover pasta Migrations/ + ModelSnapshot
    ↓
    Validar compilação
    ↓
    Gerar nova InitialCreate migration com modelo final
    ↓
    Validar schema contra modelo esperado
    ↓
    Executar seeds
    ↓
    Smoke test
    ↓
    Próximo módulo
```

---

## Riscos Específicos da Remoção

| Risco | Probabilidade | Impacto | Mitigação |
|-------|-------------|---------|-----------|
| Perda de seed data implícito | ALTA | ALTO | Extrair seeds antes de remover migrations |
| Schema antigo remanescente | MÉDIA | MÉDIO | Script de comparação DDL antes/depois |
| Referências cruzadas quebradas | BAIXA | ALTO | Build completo do solution após remoção |
| CI/CD falha por migrations ausentes | MÉDIA | MÉDIO | Atualizar pipelines antes da remoção |
| Rollback impossível | BAIXA | ALTO | Tag + backup do schema antes de iniciar |
