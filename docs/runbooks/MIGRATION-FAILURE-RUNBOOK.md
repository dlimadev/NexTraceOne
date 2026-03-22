# Runbook — Falha de Migration

> **NexTraceOne — Operação**  
> Versão: 1.0 | Data: 2026-03-22

---

## Contexto

O NexTraceOne usa Entity Framework Core com 16 DbContexts consolidados em 4 databases (ADR-001).  
Migrations são aplicadas automaticamente no startup do ApiHost (ambiente Development) ou via script em staging/produção.

**Filosofia**: migrations são forward-only. Rollback de migration = forward migration de compensação.

---

## Tipos de Falha

### Tipo 1 — Migration não aplicada (startup falhando)

**Sintoma**: ApiHost não inicia; logs mostram `PendingModelChangesException` ou similar.

**Diagnóstico**:
```bash
docker logs nextraceone-apihost --tail 100 2>&1 | grep -E "(migration|Migration|pending|Pending)"
```

**Causa provável**: migration nova no código mas não aplicada no banco.

### Tipo 2 — Migration aplicada com erro

**Sintoma**: startup OK mas funcionalidade quebrada; erros SQL em runtime.

**Diagnóstico**:
```bash
# Verificar histórico de migrations aplicadas
psql "${CONN_STRING}" -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 10;"

# Verificar se tabela esperada existe
psql "${CONN_STRING}" -c "\d+ <table_name>"
```

### Tipo 3 — Conflito de migration (ambientes divergentes)

**Sintoma**: staging tem migration que produção não tem (ou vice-versa).

**Diagnóstico**:
```bash
# Em staging
psql "${STAGING_CONN}" -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";"

# Em produção  
psql "${PROD_CONN}" -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";"

# Se diferente: listar migrations de cada ambiente
psql "${STAGING_CONN}" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1;"
psql "${PROD_CONN}" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY 1;"
```

---

## Diagnóstico Detalhado

### Identificar qual DbContext/banco falhou

```bash
# Logs detalhados de migrations
docker logs nextraceone-apihost --tail 200 2>&1 | grep -iE "(applying|applied|migration|database|error|exception)" | head -40
```

Os 4 bancos são:
- `nextraceone_identity` — IdentityAccess, AuditCompliance
- `nextraceone_catalog` — Catalog (Graph, Contracts, Portal)  
- `nextraceone_operations` — ChangeGovernance, OI, Governance
- `nextraceone_ai` — AIKnowledge, ExternalAI, AiOrchestration

### Verificar migrations pendentes

```bash
# Dentro do container (se acessível)
docker exec nextraceone-apihost dotnet ef migrations list --project /app/NexTraceOne.ApiHost.dll 2>/dev/null

# Ou via script
bash scripts/db/apply-migrations.sh --dry-run
```

---

## Ações de Recuperação

### Recuperação A — Migration pendente (não aplicada)

```bash
# Aplicar migrations manualmente via script
bash scripts/db/apply-migrations.sh \
  --env production \
  --database identity \
  --connection-string "${PROD_CONN_IDENTITY}"

bash scripts/db/apply-migrations.sh \
  --env production \
  --database catalog \
  --connection-string "${PROD_CONN_CATALOG}"

bash scripts/db/apply-migrations.sh \
  --env production \
  --database operations \
  --connection-string "${PROD_CONN_OPERATIONS}"

bash scripts/db/apply-migrations.sh \
  --env production \
  --database ai \
  --connection-string "${PROD_CONN_AI}"

# Verificar se aplicou
psql "${PROD_CONN_OPERATIONS}" -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
```

### Recuperação B — Migration com erro de constraint

**ATENÇÃO**: esta operação é destrutiva. Requer backup prévio.

```bash
# 1. Fazer backup IMEDIATAMENTE
pg_dump "${PROD_CONN_OPERATIONS}" > /tmp/backup-$(date +%Y%m%d-%H%M%S).sql

# 2. Analisar o erro específico
docker logs nextraceone-apihost 2>&1 | grep -A 20 "migration"

# 3. Se a migration parcialmente aplicada precisa ser revertida:
# NUNCA fazer diretamente sem DBA. Acionar processo de incidente SEV-1.
```

### Recuperação C — Banco não existente

```bash
# Criar o banco manualmente se necessário
psql "${ADMIN_CONN}" -c "CREATE DATABASE nextraceone_operations;"
psql "${ADMIN_CONN}" -c "GRANT ALL PRIVILEGES ON DATABASE nextraceone_operations TO nextraceone;"

# Reaplicar migrations
bash scripts/db/apply-migrations.sh --env production --database operations
```

---

## Forward Migration (sem rollback)

Quando uma migration tem bug e já foi aplicada em produção:

1. **Nunca reverter** a migration com `ef migrations remove` se já foi aplicada no banco
2. **Criar uma nova migration** de compensação que corrige o problema
3. **Seguir processo normal** de deploy com a nova migration

```bash
# Criar migration de compensação (em desenvolvimento)
dotnet ef migrations add Fix_<DescricaoDoProblema> \
  --project src/modules/<modulo>/<modulo>.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

---

## Prevenção

1. **Testar migrations em staging primeiro** — sempre
2. **Validar migrations em CI** — `CoreApiHostIntegrationTests` aplica todas as migrations em Testcontainers
3. **Snapshot de banco** antes de todo deploy de produção
4. **Nunca fazer schema changes manuais** no banco de produção sem migration correspondente
5. **Migration must be idempotent** quando possível (use `CREATE TABLE IF NOT EXISTS`)

---

## Checklist Pós-Recovery

- [ ] Todos os bancos têm as migrations esperadas aplicadas
- [ ] `__EFMigrationsHistory` está consistente entre ambientes
- [ ] ApiHost iniciou sem erros de migration
- [ ] `/ready` retorna `Healthy` (confirma DB acessível)
- [ ] Funcionalidade afetada testada manualmente
- [ ] Root cause documentado

---

*Runbook mantido pelo Tech Lead. Toda intervenção manual no banco de produção deve ser registrada e aprovada.*
