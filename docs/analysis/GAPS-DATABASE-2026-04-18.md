# NexTraceOne — Gaps e Problemas: Base de Dados
**Data:** 2026-04-18  
**Modo:** Analysis realista — sem minimizar problemas  
**Referência:** [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)

---

## 1. Resumo

O modelo de dados do NexTraceOne é tecnicamente sólido: 27 DbContexts com isolamento por bounded context, RLS via PostgreSQL, outbox pattern, strongly typed IDs, interceptors de auditoria. No entanto existem problemas críticos que bloqueiam produção e lacunas sistemáticas que comprometem a segurança de multi-tenancy.

**Total de problemas identificados:** 18  
(2 críticos, 5 altos, 7 médios, 4 baixos)

**Stack:** PostgreSQL 16 + pgvector + EF Core 10 + Npgsql

---

## 2. Arquitectura de Base de Dados — O que funciona

Antes dos problemas, o que está genuinamente bem:

- **27 DbContexts isolados** com prefixos de tabela por módulo — sem cruzamento de dados entre contextos
- **Row-Level Security (RLS)** via `TenantRlsInterceptor` — isolamento de tenant a nível de base de dados
- **Outbox pattern** com processadores registados para todos os 27 contextos
- **Soft deletes** via `NexTraceDbContextBase` — dados não são eliminados fisicamente
- **Audit interceptors** — `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` em todas as entidades
- **SHA-256 hash chain** no `AuditDbContext` — imutabilidade verificável do trail de auditoria
- **Migrações organizadas** por contexto com convenção de nomes clara
- **pgvector** instalado para capacidades semânticas futuras

---

## 3. Problemas Críticos

### [C-01] Colisão de tabela entre ChangeIntelligenceDbContext e PromotionDbContext {#c-01}

**Severidade:** CRÍTICO — pode corromper dados e quebrar migrações

**Descrição técnica:**

Dois DbContexts diferentes do módulo `ChangeGovernance` mapeiam entidades distintas para a mesma tabela física:

```csharp
// Em ChangeIntelligenceDbContext:
// PromotionGateConfiguration aponta para: chg_promotion_gates

// Em PromotionDbContext:
// PromotionGate (entidade diferente) também aponta para: chg_promotion_gates
// (deveria ser: prm_promotion_gates conforme convenção do módulo)
```

**Consequências:**

1. **Migrações EF Core**: Quando ambos os contextos geram migrações, a tabela `chg_promotion_gates` tem colunas conflituantes. `dotnet ef database update` pode falhar ou criar schema inconsistente.

2. **Corrupção de dados**: Queries em `ChangeIntelligenceDbContext` lêem colunas que pertencem ao modelo de `PromotionDbContext` e vice-versa.

3. **Comentários na base de dados errados**: Evidência de que a confusão é anterior — comentários da tabela referem prefixo `prm_` mas o nome físico é `chg_`.

4. **Testes que passam mas produção que falha**: Testes unitários mockam o DbContext, pelo que não detectam esta colisão. Apenas em ambiente real com base de dados real o problema manifesta-se.

**Remediação:**
```csharp
// Em PromotionDbContext / PromotionGateConfiguration.cs:
builder.ToTable("prm_promotion_gates"); // corrigir prefixo
// + migration para renomear tabela
// + verificar se existe data em produção a migrar
```

---

### [A-01] ~30 configurações de entidade sem TenantId.IsRequired() {#a-01}

**Severidade:** ALTO — risco de bypass de Row-Level Security

**Descrição técnica:**

O mecanismo de RLS no NexTraceOne depende de cada entidade ter `TenantId` correctamente configurado no EF Core. Quando `TenantId.IsRequired()` não está presente, EF Core pode aceitar registos com `TenantId = null`, que subsequentemente:

1. Não são filtrados pelo `TenantRlsInterceptor`
2. Ficam visíveis para **todos os tenants** que consultam aquela tabela
3. Constituem vazamento de dados cross-tenant

**Módulos afectados (estimativa):**
- `ConfigurationDbContext` — 6 entidades sem verificação
- `KnowledgeDbContext` — 4 entidades
- `NotificationsDbContext` — 5 entidades
- `GovernanceDbContext` — 8 entidades
- `ProductAnalyticsDbContext` — 4 entidades
- Outros — ~3 entidades espalhadas

**Remediação sistemática:**
```csharp
// Em cada EntityTypeConfiguration<T>:
builder.Property(x => x.TenantId)
    .IsRequired()         // obrigatório
    .HasColumnName("tenant_id");

// + índice composto para performance de RLS:
builder.HasIndex(x => new { x.TenantId, x.Id });
```

**Recomendação:** Criar teste de arquitectura (ArchUnit ou equivalente .NET) que verifique automaticamente que toda entidade que herda de `TenantAwareEntity` tem `TenantId.IsRequired()` na sua configuração.

---

## 4. Problemas de Alta Prioridade

### [A-16] pgvector instalado mas não utilizado

**Problema:** A extensão `pgvector` está configurada no `init-databases.sql` e mencionada na documentação como base para pesquisa semântica e RAG. No entanto:

- Nenhuma tabela tem colunas do tipo `vector(1536)` (ou similar)
- Nenhum handler de AI Knowledge usa embeddings reais
- A pesquisa semântica prometida no Developer Portal usa PostgreSQL FTS simples

**Consequência:** O produto promete pesquisa semântica contextualizada como differenciador. A infra existe. A implementação não.

**Custo de manter sem usar:** Baixo. A extensão está instalada mas inactiva.
**Custo de implementar:** Médio. Requer: schema de embeddings, pipeline de geração (Ollama embeddings), retrieval handler.

---

### [A-17] Ausência de índices em colunas de alta frequência de query

**Problema:** Colunas frequentemente usadas em filtros não têm índices explícitos:

```sql
-- Exemplos de queries lentas em produção com volume:
WHERE service_id = @id AND environment_id = @env  -- falta índice composto
WHERE tenant_id = @tid AND created_at > @date     -- falta índice
WHERE contract_id = @id AND version = @ver        -- falta índice composto
```

**Módulos mais afectados:**
- `ContractsDbContext` — queries por `contractId + version`
- `ChangeIntelligenceDbContext` — queries por `serviceId + environment + timestamp`
- `IncidentDbContext` — queries por `serviceId + occurredAt`

**Remediação:** Auditoria de queries EF Core com `EnableSensitiveDataLogging` em staging. Identificar N+1 e full table scans. Adicionar `HasIndex()` nas configurações.

---

### [A-18] Falta de particionamento em tabelas de telemetria e eventos

**Problema:** Tabelas como `telemetry_events`, `audit_events`, `incident_events` acumulam volume linear ao longo do tempo. Sem particionamento por data:

- Queries de janela temporal degradam com o tempo
- VACUUM/ANALYZE tornam-se operações custosas
- Backup e restore demoram mais

**Nota:** A documentação (`DATA-ARCHITECTURE.md`) menciona particionamento como planeado. Há uma migração de "table partitioning" identificada mas a implementação real é parcial.

**Remediação:** `PARTITION BY RANGE (created_at)` com partições mensais para tabelas de eventos de alto volume. Implementar `pg_partman` para gestão automática.

---

### [A-19] Sem estratégia de soft-delete para entidades relacionadas

**Problema:** O `NexTraceDbContextBase` implementa soft delete via `IsDeleted` flag. Mas não há cascade de soft delete:

- `Service` soft-deleted → os seus `Contracts` continuam visíveis
- `Contract` soft-deleted → as suas `ContractVersions` continuam visíveis
- `Change` soft-deleted → os seus `ApprovalWorkflows` continuam activos

Isto cria registos "órfãos" lógicos que podem aparecer em queries de outros módulos.

**Remediação:** Implementar cascade soft delete por relação. Ou adicionar filtros globais por `ServiceId = {id} AND Service.IsDeleted = false` em queries compostas.

---

### [A-20] Ausência de backfill strategy para migrações de dados

**Problema:** Várias migrações adicionam colunas NOT NULL a tabelas existentes sem estratégia de backfill:

```sql
-- Padrão problemático encontrado:
ALTER TABLE cntr_contracts ADD COLUMN health_score_version INTEGER NOT NULL DEFAULT 1;
```

O `DEFAULT 1` funciona para linhas existentes, mas se a coluna for adicionada sem DEFAULT e depois o DEFAULT removido (padrão para produção segura), a migração falha em tabelas com dados.

**Remediação:** Para cada migração de coluna NOT NULL em tabela populada: (1) adicionar coluna nullable, (2) backfill via job ou script, (3) adicionar constraint NOT NULL numa segunda migração.

---

## 5. Problemas de Média Prioridade

### [M-22] Convenção de nomes de tabelas inconsistente

**Problema:** O prefixo `chg_` é usado tanto pelo módulo `ChangeGovernance` quanto, erroneamente, pelo `PromotionDbContext` (que deveria usar `prm_`). Algumas tabelas de `RuntimeIntelligenceDbContext` usam `rtl_` em vez do esperado `opl_` (OperationalIntelligence).

**Tabelas com prefixo incorrecto identificadas:**
- `chg_promotion_gates` → deveria ser `prm_promotion_gates`
- `rtl_runtime_profiles` → deveria ser `opl_runtime_profiles` (ou documentar convenção `rtl_`)

---

### [M-23] Ausência de índices de texto completo para pesquisa

**Problema:** O Developer Portal usa PostgreSQL FTS (`GlobalSearch`), mas os índices `tsvector` não estão materializados como colunas geradas. Cada query recalcula o vector de pesquisa:

```sql
-- Lento:
WHERE to_tsvector('english', name || ' ' || description) @@ to_tsquery(@query)

-- Correcto (com coluna gerada):
ALTER TABLE cat_services ADD COLUMN search_vector tsvector 
    GENERATED ALWAYS AS (to_tsvector('english', name || ' ' || description)) STORED;
CREATE INDEX idx_services_search ON cat_services USING GIN(search_vector);
```

---

### [M-24] Sem constraint de unicidade em entidades que requerem unicidade de negócio

**Exemplos encontrados:**
- `cat_services`: sem constraint `UNIQUE (tenant_id, slug)` — dois serviços com o mesmo slug no mesmo tenant são possíveis
- `cntr_contracts`: sem constraint `UNIQUE (service_id, contract_type, version)` — duplicação de versão de contrato possível
- `cfg_feature_flags`: sem constraint `UNIQUE (tenant_id, flag_key)` — flag duplicada possível

---

### [M-25] Connection strings com todos os contextos a apontar para a mesma base de dados

**Avaliação:** Esta é uma decisão de arquitectura válida para MVP/fase inicial (single PostgreSQL, múltiplos schemas lógicos). No entanto, a configuração actual tem 26 connection strings que são **idênticas** excepto no nome (apontam todas para o mesmo `postgresql://`).

Isto significa:
- Sem isolamento real de performance entre módulos (um módulo pesado afecta todos)
- Sem possibilidade de escalar um módulo independentemente
- Sem possibilidade de aplicar connection pooling diferenciado por módulo

**Não é um problema bloqueante**, mas deve ser documentado como decisão consciente com plano de migração para quando o volume justificar separação.

---

### [M-26] Ausência de verificação periódica da integridade da hash chain de auditoria

**Problema:** O `AuditDbContext` tem SHA-256 hash chain para imutabilidade. Existe um handler `VerifyChainIntegrity`. Mas não há:
- Job Quartz agendado para verificação periódica automática
- Alertas quando a cadeia está corrompida
- Relatório de última verificação bem-sucedida

**Consequência:** A garantia de imutabilidade existe no código mas não é verificada activamente. Um administrador com acesso à BD poderia modificar um registo e a violação só seria detectada se alguém executasse o handler manualmente.

---

### [M-27] Ausência de estratégia de retenção com purge automático

**Problema:** Tabelas de telemetria (`telemetry_events`), notificações entregues, e logs de auditoria menos críticos crescem indefinidamente. A documentação menciona políticas de retenção, mas não há:
- Job Quartz de purge com threshold configurável
- Mecanismo de archiving antes de purge
- Alertas de crescimento de tabela

---

### [M-28] Sem monitorização de migrações pendentes em startup

**Problema:** A aplicação pode arrancar com migrações pendentes sem aviso. O `StartupValidation.cs` existe mas não verifica o estado das migrações de todos os 27 contextos.

**Remediação:** Em startup, verificar `context.Database.GetPendingMigrationsAsync()` para todos os contextos registados. Se pendentes existirem em produção, lançar warning proeminente ou bloquear startup (configurável).

---

## 6. Problemas de Baixa Prioridade

### [L-12] Comentários de tabela desactualizados em algumas migrações

Comentários SQL (`COMMENT ON TABLE`) não foram actualizados após renomeações ou refactorizações, levando a metadados desalinhados com o código actual.

### [L-13] Ausência de diagrama ER actualizado

O `DATA-ARCHITECTURE.md` descreve a arquitectura mas não inclui um diagrama entidade-relação gerado automaticamente do schema real. O schema evoluiu e qualquer diagrama manual está provavelmente desactualizado.

### [L-14] Seed data de desenvolvimento misturado com seed de configuração base

O `DevelopmentSeedDataExtensions.cs` contém dados de desenvolvimento (utilizadores de teste, tenants fictícios) que poderiam acidentalmente ser executados num ambiente de produção mal configurado.

### [L-15] Falta de documentação de decisão sobre single-DB vs multi-DB

A decisão de usar uma única instância PostgreSQL para todos os 27 contextos (em vez de bases de dados físicas separadas) não está documentada como ADR. É uma decisão válida, mas deve ser explícita.

---

## 7. Resumo de riscos por categoria

| Categoria | Risco | Issues |
|-----------|-------|--------|
| Integridade de dados | CRÍTICO | C-01 (colisão de tabela) |
| Segurança multi-tenant | ALTO | A-01 (~30 entidades sem TenantId.IsRequired) |
| Performance em produção | ALTO | A-17 (índices em falta), A-18 (sem particionamento) |
| Capacidades prometidas | ALTO | A-16 (pgvector instalado mas não usado) |
| Consistência do modelo | MÉDIO | M-22 (nomes inconsistentes), M-24 (sem unicidade) |
| Operacionalidade | MÉDIO | M-26 (sem verificação de hash chain), M-27 (sem purge) |

---

## 8. Priorização de remediação de base de dados

```
IMEDIATO (antes de qualquer deploy em produção):
  [C-01] Renomear chg_promotion_gates → prm_promotion_gates   → 2h + migration
  [A-01] Adicionar TenantId.IsRequired() em 30 entidades       → 1 dia

SPRINT 1 (qualidade e integridade):
  [A-17] Adicionar índices compostos em colunas críticas        → 1 dia
  [M-24] Adicionar constraints de unicidade de negócio         → 4h
  [M-23] Índices GIN para PostgreSQL FTS                       → 4h
  [M-28] Verificação de migrações pendentes em startup         → 2h

SPRINT 2 (operacionalidade):
  [A-16] Pipeline de embeddings com pgvector                   → 3 dias
  [A-18] Particionamento de tabelas de eventos                 → 2 dias
  [M-26] Job de verificação de hash chain                      → 4h
  [M-27] Job de purge/archive com retenção configurável        → 1 dia
```

---

*Para análise de backend ver [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md)*  
*Para análise de testes ver [GAPS-TESTS-2026-04-18.md](./GAPS-TESTS-2026-04-18.md)*
