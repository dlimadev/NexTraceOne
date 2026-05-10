# Wave 3 — Actualizações, Backup & Disaster Recovery

> **Prioridade:** Alta
> **Esforço estimado:** L (Large)
> **Módulos impactados:** `platform/ApiHost`, `platform/BackgroundWorkers`, scripts
> **Referência:** [INDEX.md](./INDEX.md)
> **Estado (Maio 2026):** W3-01 IMPLEMENTADO | W3-02 NAO IMPLEMENTADO | W3-03 PARCIAL | W3-04 NAO IMPLEMENTADO | W3-05 IMPLEMENTADO

---

## Contexto

Em ambientes on-prem, actualizar o produto e recuperar de falhas são as operações
de maior risco. O NexTraceOne já tem uma estratégia de migrations correcta
(bloqueio automático em produção). O que falta é a camada que torna estas
operações **seguras, previsíveis e reversíveis** sem depender de um engenheiro
da aplicação em cada janela de mudança.

Benchmark de mercado (2026):
- pgBackRest e Barman são o standard para backup PostgreSQL enterprise on-prem
- Blue-green deployment permite rollback instantâneo sem downtime
- `pgroll` (xata.io) permite zero-downtime schema migrations via expand/contract

---

## W3-01 — Migration Preview API

### Problema
Antes de aplicar migrations em produção, a equipa não sabe quais migrations vão
ser executadas, quais tabelas afectam, se são reversíveis ou quanto tempo demoram.

### Solução
Endpoint `GET /api/v1/admin/migrations/pending`:

```json
{
  "total_pending": 3,
  "is_safe_to_apply": true,
  "estimated_duration_seconds": 8,
  "migrations": [
    {
      "id": "20260410_AddServiceHealthIndex",
      "context": "CatalogGraphDbContext",
      "description": "Adiciona índice GIN em service_tags para search",
      "is_reversible": true,
      "tables_affected": ["catalog_services"],
      "risk_level": "Low",
      "requires_downtime": false
    },
    {
      "id": "20260412_AddCostAttributionPartitioning",
      "context": "CostIntelligenceDbContext",
      "description": "Adiciona particionamento por mês em cost_attributions",
      "is_reversible": false,
      "tables_affected": ["oi_cost_attributions"],
      "risk_level": "Medium",
      "requires_downtime": false,
      "warning": "Migration irreversível — fazer backup antes de aplicar"
    }
  ]
}
```

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Endpoint `GET /migrations/pending`. Feature `GetPendingMigrations`. Page frontend `MigrationPreviewPage.tsx`.
Lista migrations pendentes por DbContext com classificação de risco e indicação de downtime.

### Critério de aceite
- [x] Endpoint disponível apenas para `PlatformAdmin`
- [x] Lista todas as migrations pendentes por DbContext
- [x] Classifica risco: `Low / Medium / High`
- [x] Indica se requer downtime
- [x] Avisa sobre migrations irreversíveis
- [x] Widget no admin health dashboard quando `total_pending > 0`

---

## W3-02 — Offline Release Bundle

### Problema
Actualizar o NexTraceOne requer acesso a GitHub (código), npmjs.com (frontend deps)
e NuGet.org (backend deps). Em ambientes on-prem sem internet, isto bloqueia actualizações.

### Solução
Pacote `.zip` autossuficiente para cada release:

```
nextraceone-v2.5.0-release.zip
├── bin/
│   ├── NexTraceOne.ApiHost.linux-x64/
│   ├── NexTraceOne.ApiHost.win-x64/
│   ├── NexTraceOne.BackgroundWorkers.linux-x64/
│   └── NexTraceOne.Ingestion.Api.linux-x64/
├── frontend/
│   └── dist/                  ← SPA compilada, pronta para servir
├── migrations/
│   └── apply-migrations.sh    ← Script de migrations independente
├── docker/
│   ├── nextraceone-apihost.tar.gz
│   ├── nextraceone-workers.tar.gz
│   └── nextraceone-ingestion.tar.gz
├── checksums.sha256           ← Verificação de integridade
├── RELEASE-NOTES.md           ← Notas de release
├── UPGRADE-GUIDE.md           ← Passos de upgrade desta versão
└── ROLLBACK-GUIDE.md          ← Como reverter para versão anterior
```

### Processo de actualização offline

```
1. Descarregar bundle no ambiente com internet
2. Verificar integridade: sha256sum -c checksums.sha256
3. Transferir para o servidor on-prem (USB, NFS, SCP)
4. Executar: ./upgrade.sh --from 2.4.1 --to 2.5.0
5. Verificar: GET /api/v1/admin/startup-report
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe pipeline de CI configurado para gerar bundles distribuíveis. Não há `upgrade.sh` nem checksums
automáticos. O código existe mas não há artefacto de release empacotado. Item pendente para iteração futura.

### Critério de aceite
- [ ] Bundle gerado como artefacto de CI em cada release
- [ ] Checksums SHA-256 para todos os ficheiros
- [ ] Script `upgrade.sh` com validação pré-upgrade e rollback automático em caso de falha
- [ ] RELEASE-NOTES.md com lista de breaking changes destacados
- [ ] Bundle testado em ambiente air-gapped antes de cada release

---

## W3-03 — Backup Coordinator

### Problema
Não existe automação de backup integrada. A responsabilidade está totalmente
no cliente, sem qualquer assistência da plataforma.

### Solução
Job Quartz `BackupCoordinatorJob` configurável via admin:

```
Configuração de Backup
├── Frequência: Diário às 03:00 (configurável)
├── Retenção: 30 dias (configurável)
├── Destino: /data/backups (local) ou \\server\backups (SMB)
├── Compressão: zstd (eficiente em disco)
└── Verificação: restaurar em BD temporária e validar integridade
```

**Processo de backup:**
```
1. pg_dump de todos os schemas (nextraceone_identity, _catalog, _operations, _ai)
2. Comprimir com zstd (~70% de redução)
3. Calcular SHA-256 do ficheiro
4. Guardar manifesto: timestamp, tamanho, checksum, versão da app
5. Limpar backups mais antigos que o período de retenção
6. Registar resultado no audit trail da plataforma
7. Notificar PlatformAdmin se falhar
```

**Ferramentas de referência:** pgBackRest (enterprise), Barman (enterprise),
pg_dump nativo (simples, fiável, sem dependências externas).

### Estado de Implementação (Maio 2026): PARCIAL
Feature `GetAdminBackup` com `BackupCoordinatorResponse` e endpoints GET/POST para configuração de schedule.
A UI e configuração estão implementadas mas o job automático agendado (Quartz) não está implementado.
O backup efectivo depende de ferramentas externas (pg_dump manual ou scripts).

### Critério de aceite
- [x] Job configurável via UI admin (frequência, retenção, destino)
- [x] Resultado de cada backup visível no Health Dashboard
- [x] Notificação de falha enviada ao PlatformAdmin
- [ ] Verificação de integridade pós-backup (job automático em falta)
- [x] Histórico de backups com tamanho, duração e estado
- [ ] Limpeza automática de backups antigos (job automático em falta)

---

## W3-04 — Point-in-Time Recovery Guide

### Problema
Quando é necessário restaurar, a equipa não sabe o procedimento exacto.
Em situação de stress (incidente de produção), isto causa atrasos críticos.

### Solução
Wizard interactivo de recuperação no painel admin:

```
Recuperação de Dados
├── Passo 1: Escolher ponto de restauro
│   └── Lista de backups disponíveis com data, tamanho e estado
├── Passo 2: Escolher escopo
│   ├── Restauro completo (todos os schemas)
│   └── Restauro parcial (escolher schemas específicos)
├── Passo 3: Confirmação
│   └── Mostrar impacto: dados que serão perdidos entre backup e agora
├── Passo 4: Executar
│   └── Progress em tempo real com estimativa de conclusão
└── Passo 5: Verificação
    └── Preflight check após restauro
```

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
Não existe wizard de recuperação interactivo no painel admin. O procedimento de restauro é manual
e documentado no runbook `docs/runbooks/RESTORE-OPERATIONS-RUNBOOK.md`. Item pendente para iteração futura.

### Critério de aceite
- [ ] Wizard disponível apenas com role `PlatformAdmin`
- [ ] Acção auditada no audit trail com utilizador, timestamp e escopo
- [ ] Dry-run disponível: mostrar o que seria restaurado sem executar
- [ ] RTO alvo documentado: < 30 minutos para restauro completo

---

## W3-05 — Graceful Shutdown

### Problema
Quando o servidor é reiniciado ou o processo termina abruptamente, requests em curso
podem ser cortados e o outbox pode ficar com mensagens parciais.

### Solução
Handler de `SIGTERM` / `SIGINT` que:

```
1. Para de aceitar novos requests (responder 503 imediatamente)
2. Aguarda conclusão dos requests em curso (timeout: 30s)
3. Drena o outbox — processa mensagens pendentes (timeout: 60s)
4. Fecha conexões de BD de forma limpa
5. Regista evento de shutdown no audit trail
6. Termina o processo com exit code 0
```

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Feature `GetGracefulShutdownConfig` em `Governance.Application`. Configuração via `Platform:GracefulShutdown:*`
com opções `RequestDrainTimeout`, `OutboxDrainTimeout`, `HealthCheck503` e `AuditEvents`.
`MaintenanceModeMiddleware.cs` responde 503 durante shutdown.

### Critério de aceite
- [x] Timeout de graceful shutdown configurável
- [x] Evento de shutdown registado no audit trail com motivo
- [x] Health endpoint `/live` retorna 503 durante o shutdown
- [x] Métricas de shutdown no startup report seguinte

---

## Plano de Disaster Recovery (Referência)

| Cenário | RTO Alvo | RPO Alvo | Procedimento |
|---|---|---|---|
| Corrupção de dados da aplicação | < 30 min | < 24h (último backup) | Point-in-Time Recovery (W3-04) |
| Falha de hardware do servidor | < 2h | < 24h | Restauro em novo servidor + Release Bundle |
| Migração para novo servidor | < 4h | 0 (exportação em directo) | Health Snapshot Export + Restore |
| Rollback de versão | < 15 min | 0 | Rollback Guide do Release Bundle |

---

## Referências de Mercado

- pgBackRest: backup enterprise PostgreSQL on-prem (recomendado para BD > 100GB)
- Barman: centralised backup server para PostgreSQL (ambientes tradicionais)
- pgroll (xata.io): zero-downtime migrations via expand/contract pattern
- Replicated: offline bundles como padrão de distribuição self-hosted
- blue-green deployment: rollback instantâneo em actualizações de risco
