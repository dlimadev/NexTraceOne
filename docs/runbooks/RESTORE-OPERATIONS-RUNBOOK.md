# Restore Operations — Runbook

## Objetivo

Guiar o operador na restauração segura dos bancos de dados PostgreSQL do NexTraceOne a partir de backups.

---

## Pré-requisitos

- [ ] `psql` instalado (PostgreSQL client tools)
- [ ] `gunzip` / `gzip` instalado
- [ ] Acesso ao servidor PostgreSQL de destino (credenciais e rede)
- [ ] Backups disponíveis no diretório de input
- [ ] **Serviços parados** antes de iniciar restore em produção
- [ ] Variáveis de ambiente configuradas (`PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`)

---

## ⚠️ Avisos Importantes

1. **O restore SUBSTITUI os dados existentes** no banco de destino
2. **Faça backup** do estado atual ANTES de restaurar (se aplicável)
3. **Pare os serviços** que acedem ao banco antes de restaurar
4. **Confirme o ambiente** — o script pede confirmação explícita (a menos que `--force` seja usado)
5. Em **produção**, o restore exibe aviso adicional e requer confirmação `yes`

---

## Procedimento de Restore

### Restore de Banco Individual

```bash
export PGHOST=pg-prod.internal
export PGPORT=5432
export PGUSER=nextraceone_prod
export PGPASSWORD='<password>'

# Restaurar último backup do banco identity
bash scripts/db/restore.sh --database nextraceone_identity --env production --input-dir /mnt/backups
```

### Restore de Ficheiro Específico

```bash
# Restaurar ficheiro específico
bash scripts/db/restore.sh --database nextraceone_identity --file nextraceone_identity_production_20260323_020000.sql.gz --env production
```

### Restore Completo (todos os 4 bancos)

```bash
# Restaurar todos os bancos (com verificação automática)
bash scripts/db/restore-all.sh --env production --input-dir /mnt/backups
```

### Restore sem Confirmação (para pipelines)

```bash
# Bypass da confirmação interativa
bash scripts/db/restore.sh --database nextraceone_identity --env production --force
bash scripts/db/restore-all.sh --env production --force
```

---

## Procedimento Completo de Disaster Recovery

### 1. Parar serviços

```bash
# Docker Compose
docker compose down

# Kubernetes
kubectl scale deployment nextraceone-apihost --replicas=0
kubectl scale deployment nextraceone-workers --replicas=0
kubectl scale deployment nextraceone-ingestion --replicas=0
```

### 2. Identificar backup

```bash
# Listar backups disponíveis (mais recentes primeiro)
ls -lt /mnt/backups/nextraceone_*_production_*.sql.gz | head -8
```

### 3. Executar restore

```bash
bash scripts/db/restore-all.sh --env production --input-dir /mnt/backups
```

### 4. Verificar integridade

```bash
for db in nextraceone_identity nextraceone_catalog nextraceone_operations nextraceone_ai; do
  bash scripts/db/verify-restore.sh --database "$db" --env production
done
```

### 5. Aplicar migrações pendentes (se necessário)

```bash
bash scripts/db/apply-migrations.sh --env Production
```

### 6. Reiniciar serviços

```bash
# Docker Compose
docker compose up -d

# Kubernetes
kubectl scale deployment nextraceone-apihost --replicas=2
kubectl scale deployment nextraceone-workers --replicas=1
kubectl scale deployment nextraceone-ingestion --replicas=1
```

### 7. Smoke check

```bash
bash scripts/deploy/smoke-check.sh --api-url https://api.nextraceone.com --frontend-url https://app.nextraceone.com
```

---

## Verificação Pós-Restore

O script `verify-restore.sh` executa 5 verificações automáticas:

| Verificação | O que valida |
|---|---|
| 1. Database exists | O banco existe no servidor PostgreSQL |
| 2. Table count | Número de tabelas > 0 |
| 3. Key table row counts | Tabelas-chave contêm dados |
| 4. Migrations table | `__EFMigrationsHistory` presente com migrações |
| 5. Schemas | Schemas esperados presentes |

### Tabelas-chave por banco

| Banco | Tabelas verificadas |
|---|---|
| `nextraceone_identity` | `identity.users`, `identity.roles`, `audit.audit_entries` |
| `nextraceone_catalog` | `catalog.services`, `catalog.contracts`, `catalog.api_endpoints` |
| `nextraceone_operations` | `changes.change_records`, `governance.rulesets`, `incidents.incidents` |
| `nextraceone_ai` | `ai.models`, `ai.policies`, `ai.orchestration_sessions` |

---

## Troubleshooting

### psql não encontrado

```
[ERROR] psql não encontrado. Instale PostgreSQL client tools.
```

**Resolução:**
```bash
sudo apt-get install postgresql-client
```

### Nenhum backup encontrado

```
[ERROR] Nenhum backup encontrado para nextraceone_identity em ./backups
```

**Resolução:** Verificar diretório de backups e usar `--input-dir` para apontar para o local correto.

### Restore falhou

```
[ERROR] ✗ nextraceone_identity — Restore falhou.
```

**Verificar:**
1. Backup não corrompido (`gzip -t <file>`)
2. Credenciais de acesso corretas
3. Banco de destino existe
4. Utilizador tem permissão de escrita

### Tabelas-chave não encontradas após restore

Pode ocorrer se:
1. O backup foi de um schema diferente (versão anterior)
2. Migrações não foram aplicadas após restore
3. Os nomes de schema/tabela mudaram

**Resolução:** Aplicar migrações com `apply-migrations.sh` após restore.

---

## Referências

- [restore.sh](../../scripts/db/restore.sh)
- [restore-all.sh](../../scripts/db/restore-all.sh)
- [verify-restore.sh](../../scripts/db/verify-restore.sh)
- [WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md](../execution/WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md)
- [BACKUP-OPERATIONS-RUNBOOK.md](BACKUP-OPERATIONS-RUNBOOK.md)
