# Wave 1 — Backup and Restore Strategy

## Visão Geral

Este documento define a **estratégia oficial** de backup e restore dos 4 bancos de dados PostgreSQL do NexTraceOne.

---

## Escopo — 4 Bancos Lógicos

| Banco | Contextos | Criticidade |
|---|---|---|
| `nextraceone_identity` | Identity (users, roles), Audit trail | **Crítica** — perda causa incapacidade de autenticação |
| `nextraceone_catalog` | Service catalog, Contracts, Developer portal | **Alta** — perda causa indisponibilidade do catálogo de serviços |
| `nextraceone_operations` | Changes, Incidents, Governance, Workflows, Cost, Runtime, Reliability, Automation | **Alta** — perda causa indisponibilidade de dados operacionais |
| `nextraceone_ai` | AI Governance, External AI, AI Orchestration | **Média** — perda causa indisponibilidade de features de IA |

---

## Política de Backup

### Frequência

| Ambiente | Frequência | Método |
|---|---|---|
| Production | **Diário** (mínimo) | Cron job + `backup.sh` |
| Staging | Semanal | Manual ou CI/CD |
| Development | On-demand | Manual |

### Retenção

| Ambiente | Retenção | Rotação |
|---|---|---|
| Production | **30 dias** | Automática (remover backups > 30 dias) |
| Staging | 7 dias | Automática |
| Development | 3 dias | Manual |

### Formato e Nomenclatura

- **Formato:** `pg_dump` plain SQL comprimido com gzip (`.sql.gz`)
- **Nomenclatura:** `{database}_{environment}_{YYYYMMDD_HHMMSS}.sql.gz`
- **Exemplo:** `nextraceone_identity_production_20260323_020000.sql.gz`

### Armazenamento

| Destino | Descrição |
|---|---|
| Local (primário) | Diretório configurável (default: `./backups`) |
| Remoto (recomendado) | Object storage (S3, Azure Blob, GCS) para disaster recovery |

**Recomendação:** Em produção enterprise, copiar backups para storage remoto após criação local.

### Integridade

Após cada backup, verificar:
1. Ficheiro `.sql.gz` criado com tamanho > 0
2. Ficheiro é descomprimível (`gzip -t`)
3. Conteúdo contém SQL válido (amostragem)

---

## Scripts Disponíveis

| Script | Descrição | Localização |
|---|---|---|
| `backup.sh` | Backup de todos ou bancos específicos | `scripts/db/backup.sh` |
| `restore.sh` | Restore de um banco individual | `scripts/db/restore.sh` |
| `restore-all.sh` | Restore completo de todos os 4 bancos | `scripts/db/restore-all.sh` |
| `verify-restore.sh` | Verificação de integridade pós-restore | `scripts/db/verify-restore.sh` |
| `apply-migrations.sh` | Aplicação de migrações EF Core | `scripts/db/apply-migrations.sh` |

### Exemplos de Uso

```bash
# Backup de todos os bancos em produção
bash scripts/db/backup.sh --env production --output-dir /mnt/backups

# Backup de bancos específicos
bash scripts/db/backup.sh --databases nextraceone_identity,nextraceone_catalog

# Restore de banco individual
bash scripts/db/restore.sh --database nextraceone_identity --env production

# Restore completo (todos os 4 bancos)
bash scripts/db/restore-all.sh --env production --input-dir /mnt/backups

# Verificação pós-restore
bash scripts/db/verify-restore.sh --database nextraceone_identity --env production
```

---

## Estratégia de Restore

### Processo de Restore

1. **Parar serviços** que acedem ao banco
2. **Identificar backup** mais recente ou específico
3. **Executar restore** via `restore.sh` ou `restore-all.sh`
4. **Verificar integridade** via `verify-restore.sh`
5. **Aplicar migrações pendentes** se necessário (`apply-migrations.sh`)
6. **Reiniciar serviços**
7. **Executar smoke check** (`scripts/deploy/smoke-check.sh`)

### Verificações Pós-Restore

O `verify-restore.sh` executa 5 verificações:

1. **Database exists** — banco existe no servidor
2. **Table count** — número de tabelas > 0
3. **Key table row counts** — tabelas-chave com dados
4. **Migrations table** — `__EFMigrationsHistory` presente e com migrações
5. **Schemas** — schemas esperados presentes

### Tempos Estimados (RTO)

| Cenário | Tempo estimado |
|---|---|
| Restore de banco individual | < 15 minutos |
| Restore completo (4 bancos) | < 45 minutos |
| Restore + migrações + smoke | < 60 minutos |

---

## Validação Prática

### Evidência de Validação

Os scripts foram validados com as seguintes verificações:

1. **`backup.sh --help`** — executa corretamente, exibe documentação completa
2. **`restore.sh --help`** — executa corretamente, exibe documentação completa
3. **`restore-all.sh --help`** — executa corretamente, exibe documentação completa
4. **`verify-restore.sh --help`** — executa corretamente, exibe documentação completa
5. **Validação de parâmetros** — scripts rejeitam bancos inválidos, diretórios inexistentes
6. **Confirmação de segurança** — restore exige confirmação `yes` (bypass com `--force`)
7. **Confirmação de produção** — restore em produção exibe aviso adicional
8. **Verificação de pré-requisitos** — scripts verificam `pg_dump`, `psql`, `gzip` antes de executar

### Limitações Conhecidas

1. **Backup incremental** não implementado — apenas full backups
2. **Compressão** é gzip simples — para grandes volumes, considerar `zstd` ou `lz4`
3. **Cron/scheduling** deve ser configurado externamente — scripts não incluem agendamento
4. **Storage remoto** deve ser configurado externamente — scripts produzem ficheiros locais
5. **Encrypt-at-rest** dos backups não implementado — considerar para dados sensíveis

---

## Configuração de Cron para Produção

```bash
# Exemplo de crontab para backup diário às 02:00 UTC
0 2 * * * cd /opt/nextraceone && bash scripts/db/backup.sh --env production --output-dir /mnt/backups 2>&1 | tee -a /var/log/nextraceone-backup.log

# Exemplo de rotação (remover backups > 30 dias)
0 3 * * * find /mnt/backups -name "*.sql.gz" -mtime +30 -delete
```

---

## Referências

- [backup.sh](../../scripts/db/backup.sh)
- [restore.sh](../../scripts/db/restore.sh)
- [restore-all.sh](../../scripts/db/restore-all.sh)
- [verify-restore.sh](../../scripts/db/verify-restore.sh)
- [BACKUP-OPERATIONS-RUNBOOK.md](../runbooks/BACKUP-OPERATIONS-RUNBOOK.md)
- [RESTORE-OPERATIONS-RUNBOOK.md](../runbooks/RESTORE-OPERATIONS-RUNBOOK.md)
- [PHASE-7-BACKUP-AND-RESTORE.md](PHASE-7-BACKUP-AND-RESTORE.md)
