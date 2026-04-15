# Wave 2 — Auto-Monitorização & Health Dashboard

> **Prioridade:** Alta
> **Esforço estimado:** M (Medium)
> **Módulos impactados:** `platform/ApiHost`, `operationalintelligence`, `notifications`
> **Referência:** [INDEX.md](./INDEX.md)

---

## Contexto

O NexTraceOne já expõe `/health`, `/ready` e `/live`. Mas estes endpoints são
pensados para orquestradores (Kubernetes, IIS health probes) — não para um admin
que precisa de perceber o que está a correr mal no servidor on-prem.

Em 2026, plataformas enterprise self-hosted como PagerDuty Runbook Automation
e Shoreline oferecem auto-diagnóstico, alertas proactivos e support bundles
integrados. O NexTraceOne deve seguir o mesmo padrão.

---

## W2-01 — Admin Health Dashboard

### Problema
A equipa de infra não tem visibilidade sobre o estado interno da plataforma sem aceder
a Grafana, Prometheus ou logs do servidor. Qualquer anomalia é descoberta pelos utilizadores.

### Solução
Página `/admin/health` protegida por role `PlatformAdmin` com:

```
┌─────────────────────────────────────────────────────────────────┐
│                  Platform Health — NexTraceOne                  │
├────────────────────┬────────────────────────────────────────────┤
│  Uptime            │  14d 6h 22m                                │
│  Versão            │  2.4.1 (build 20260415.1)                  │
│  Ambiente          │  Production                                │
├────────────────────┼────────────────────────────────────────────┤
│  Request Rate      │  142 req/min (p95: 210ms)                  │
│  Error Rate        │  0.4%  ▼ abaixo do threshold (1%)          │
│  DB Pool (Identity)│  12/50 conexões activas                    │
│  DB Pool (Catalog) │  8/50 conexões activas                     │
│  Outbox Queue      │  0 mensagens pendentes                     │
├────────────────────┼────────────────────────────────────────────┤
│  Background Jobs   │                                            │
│    OutboxProcessor │  ✅ Último run: 5s atrás                   │
│    IdentityExpiry  │  ✅ Último run: 60s atrás                  │
├────────────────────┼────────────────────────────────────────────┤
│  Disco             │  148 GB usados / 500 GB total (29%)        │
│  RAM (processo)    │  1.2 GB RSS                                │
│  CPU (processo)    │  2.4% médio (último minuto)                │
├────────────────────┼────────────────────────────────────────────┤
│  Último backup     │  2026-04-14 03:00 — ✅ OK (2.1 GB)        │
│  Migrations        │  234/234 aplicadas — ✅ Actualizado        │
└────────────────────┴────────────────────────────────────────────┘
```

### Implementação sugerida

```csharp
// platform/NexTraceOne.ApiHost/Admin/Health/PlatformHealthService.cs
public sealed class PlatformHealthService
{
    Task<PlatformHealthReport> GetReportAsync(CancellationToken ct);
}

// Endpoint
app.MapGet("/api/v1/admin/health", ...)
   .RequireAuthorization("PlatformAdmin");
```

Métricas a expor:
- `uptime_seconds` — tempo desde o último arranque
- `request_rate_per_minute` — calculado a partir do middleware
- `error_rate_percent` — % de 5xx no último minuto
- `db_pool_active_{context}` — conexões activas por DbContext
- `outbox_pending_count` — mensagens no outbox não processadas
- `background_job_last_run_{job}` — timestamp do último run de cada job Quartz
- `process_memory_mb` — RSS do processo actual
- `disk_used_gb` / `disk_total_gb` — do disco onde está a BD/logs
- `pending_migrations_count` — migrations em falta (0 = actualizado)

### Critério de aceite
- [ ] Dashboard actualiza automaticamente a cada 30 segundos
- [ ] Semáforo visual (verde/amarelo/vermelho) para cada métrica
- [ ] Thresholds configuráveis por admin (ex: alertar quando outbox > 100)
- [ ] i18n completo

---

## W2-02 — Startup Report

### Problema
Quando o produto reinicia (deploy, reboot do servidor), não fica registo do estado
com que arrancou. Dificulta troubleshooting de problemas pós-deploy.

### Solução
Endpoint `GET /api/v1/admin/startup-report` que retorna o relatório do último arranque:

```json
{
  "started_at": "2026-04-15T09:15:32Z",
  "version": "2.4.1",
  "build": "20260415.1",
  "environment": "Production",
  "hostname": "nextraceone-prod-01",
  "migrations_applied": 0,
  "migrations_total": 234,
  "modules_registered": 12,
  "configuration": {
    "smtp_configured": true,
    "ollama_configured": true,
    "clickhouse_configured": true,
    "cors_origins": ["https://app.acme.com"]
  },
  "warnings": [
    "OTel Collector não acessível — traces desactivados"
  ]
}
```

> **Segurança:** Nunca expor secrets, passwords ou connection strings.
> Apenas flags booleanas e valores não sensíveis.

### Critério de aceite
- [ ] Relatório gerado no startup e persistido na BD
- [ ] Histórico dos últimos 30 arranques acessível
- [ ] Warnings de configuração visíveis no dashboard de health

---

## W2-03 — Auto-Diagnóstico Proactivo

### Problema
Problemas como outbox acumulando, DB com conexões esgotadas ou disco quase cheio
são descobertos pelos utilizadores, não pela equipa de infra.

### Solução
Job Quartz `PlatformHealthMonitorJob` que corre a cada 5 minutos e dispara alertas:

| Condição | Severidade | Acção |
|---|---|---|
| Outbox pendente > 500 mensagens | Warning | Notificação ao PlatformAdmin |
| Outbox pendente > 2000 mensagens | Critical | Notificação + log estruturado |
| DB pool > 80% da capacidade | Warning | Notificação |
| Disco > 80% usado | Warning | Notificação |
| Disco > 95% usado | Critical | Notificação + bloquear novas ingestões |
| Job Quartz sem run há > 2x o intervalo | Warning | Notificação |
| Error rate > 5% | Warning | Notificação |
| Error rate > 20% | Critical | Notificação |

Notificações via:
1. Notification Center existente (InApp)
2. Email para PlatformAdmins (se SMTP configurado)
3. Webhook para canal externo (se configurado)

### Critério de aceite
- [ ] Thresholds configuráveis em `/admin/platform-alerts`
- [ ] Cooldown de 15 minutos entre alertas repetidos do mesmo tipo
- [ ] Alerta resolvido automaticamente quando condição deixar de existir
- [ ] Histórico de alertas da plataforma separado dos alertas de negócio

---

## W2-04 — Support Bundle Generator

### Problema
Quando há um problema e a equipa precisa de suporte externo, recolher logs,
configurações e estado do sistema exige acesso SSH — que pode não estar disponível.

### Solução
Botão no painel admin que gera um ficheiro `.zip` com:

```
support-bundle-2026-04-15-0915.zip
├── system-info.json          ← versão, OS, runtime, hostname
├── startup-report.json       ← último startup report
├── health-snapshot.json      ← estado actual da saúde
├── config-health.json        ← resultado do config validator
├── recent-errors.txt         ← últimas 500 linhas de logs de erro
├── slow-queries.txt          ← queries lentas do PostgreSQL (últimas 24h)
├── background-jobs.json      ← estado de todos os jobs Quartz
└── preflight-report.json     ← resultado do último preflight check
```

> **Segurança:** Nunca incluir passwords, JWT secrets ou connection strings.
> Sanitizar qualquer dado sensível antes de incluir no bundle.

### Critério de aceite
- [ ] Bundle gerado em < 30 segundos
- [ ] Download directo do browser sem SSH
- [ ] Conteúdo auditável — registo de quem gerou e quando
- [ ] Sanitização de dados sensíveis verificada por testes unitários

---

## Dependências e Riscos

| Dependência | Notas |
|---|---|
| Middleware de métricas no ApiHost | Necessário para request rate / error rate |
| `System.Diagnostics.Process` | Para métricas de RAM/CPU do processo |
| `BackgroundWorkers` — registo de last run | Quartz precisa de persistir timestamps |

| Risco | Mitigação |
|---|---|
| Métricas de processo com overhead | Calcular em background, não por request |
| Support bundle com dados sensíveis | Testes unitários de sanitização obrigatórios |

---

## Referências de Mercado

- Replicated: support bundles como feature central de troubleshooting self-hosted
- PagerDuty Runbook Automation Self-Hosted: health dashboard dedicado
- Plane self-hosted: `/health` com checks categorizados por componente
- Coder: auto-diagnóstico com suggested fixes integrado na UI
