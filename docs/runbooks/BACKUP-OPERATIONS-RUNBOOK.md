# Backup Operations — Runbook

## Objetivo

Guiar o operador na execução segura de backups dos 4 bancos de dados PostgreSQL do NexTraceOne.

---

## Pré-requisitos

- [ ] `pg_dump` instalado (PostgreSQL client tools)
- [ ] `gzip` instalado
- [ ] Acesso ao servidor PostgreSQL (credenciais e rede)
- [ ] Diretório de destino com espaço suficiente
- [ ] Variáveis de ambiente configuradas (`PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`)

---

## Procedimento de Backup

### Backup Completo (todos os 4 bancos)

```bash
export PGHOST=pg-prod.internal
export PGPORT=5432
export PGUSER=nextraceone_prod
export PGPASSWORD='<password>'

bash scripts/db/backup.sh --env production --output-dir /mnt/backups
```

### Backup de Bancos Específicos

```bash
# Apenas identity e catalog
bash scripts/db/backup.sh --databases nextraceone_identity,nextraceone_catalog --env production --output-dir /mnt/backups
```

### Backup Pré-Deploy (recomendado)

Antes de qualquer deploy em produção, executar:

```bash
bash scripts/db/backup.sh --env production --output-dir /mnt/backups/pre-deploy-$(date +%Y%m%d)
```

---

## Verificação de Sucesso

### 1. Verificar output do script

O script reporta sucesso ou falha para cada banco:
```
[OK]    ✓ nextraceone_identity — nextraceone_identity_production_20260323_020000.sql.gz (1.2M)
[OK]    ✓ nextraceone_catalog — nextraceone_catalog_production_20260323_020001.sql.gz (856K)
[OK]    ✓ nextraceone_operations — nextraceone_operations_production_20260323_020002.sql.gz (2.1M)
[OK]    ✓ nextraceone_ai — nextraceone_ai_production_20260323_020003.sql.gz (420K)
```

### 2. Verificar ficheiros criados

```bash
ls -lh /mnt/backups/nextraceone_*_production_*.sql.gz
```

Todos os ficheiros devem ter tamanho > 0.

### 3. Verificar integridade do gzip

```bash
for f in /mnt/backups/nextraceone_*_production_*.sql.gz; do
  gzip -t "$f" && echo "OK: $f" || echo "CORRUPTED: $f"
done
```

---

## Configuração de Backup Automático (Cron)

### Crontab de Produção

```bash
# Backup diário às 02:00 UTC
0 2 * * * cd /opt/nextraceone && PGHOST=pg-prod.internal PGUSER=nextraceone_prod PGPASSWORD='<password>' bash scripts/db/backup.sh --env production --output-dir /mnt/backups 2>&1 >> /var/log/nextraceone-backup.log

# Rotação: remover backups > 30 dias
0 3 * * * find /mnt/backups -name "nextraceone_*_production_*.sql.gz" -mtime +30 -delete 2>&1 >> /var/log/nextraceone-backup-rotation.log
```

### Verificação do Cron

```bash
# Verificar se o cron está configurado
crontab -l | grep nextraceone

# Verificar último backup
ls -lt /mnt/backups/nextraceone_*_production_*.sql.gz | head -4
```

---

## Troubleshooting

### pg_dump não encontrado

```
[ERROR] pg_dump não encontrado. Instale PostgreSQL client tools.
```

**Resolução:**
```bash
# Ubuntu/Debian
sudo apt-get install postgresql-client

# RHEL/CentOS
sudo yum install postgresql
```

### Falha de conexão

```
[ERROR] ✗ nextraceone_identity — Backup falhou
```

**Verificar:**
1. `PGHOST` e `PGPORT` corretos
2. `PGUSER` com permissão de leitura
3. `PGPASSWORD` correto
4. Firewall/security group permite conexão
5. `pg_hba.conf` permite o IP de origem

### Espaço em disco insuficiente

**Verificar:**
```bash
df -h /mnt/backups
```

**Resolução:** Limpar backups antigos ou aumentar o volume.

---

## Bancos Disponíveis

| Banco | Contextos | Tamanho Estimado |
|---|---|---|
| `nextraceone_identity` | Identity, Audit | Pequeno-Médio |
| `nextraceone_catalog` | Catalog, Contracts, Portal | Médio |
| `nextraceone_operations` | 11 contexts operacionais | Grande |
| `nextraceone_ai` | AI Governance, External AI, Orchestration | Pequeno-Médio |

---

## Referências

- [backup.sh](../../scripts/db/backup.sh)
- [WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md](../execution/WAVE-1-BACKUP-AND-RESTORE-STRATEGY.md)
- [RESTORE-OPERATIONS-RUNBOOK.md](RESTORE-OPERATIONS-RUNBOOK.md)
