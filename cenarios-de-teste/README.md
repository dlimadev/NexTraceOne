# NexTraceOne — Cenários de Teste Funcionais

> Versão: 1.0.0 | Data: 2026-05-18 | Cobertura-alvo: 100%

Este diretório contém a documentação completa de casos de uso e cenários de teste funcionais para todos os módulos do NexTraceOne. Os cenários cobrem fluxos positivos (caminho feliz), negativos (falhas e bordas), segurança, multi-tenancy e comportamento do pipeline CQRS.

---

## Convenções de Nomenclatura

| Campo       | Formato                                     |
|-------------|---------------------------------------------|
| ID do Caso  | `TC-<MÓDULO>-<SEQUENCIAL>`                  |
| Tipo        | `Unitário` / `Integração` / `E2E` / `Carga` |
| Prioridade  | `Crítica` / `Alta` / `Média` / `Baixa`       |
| Status      | `Pendente` / `Aprovado` / `Bloqueado`        |

---

## Índice de Módulos

| Arquivo | Módulo | Qtd. Casos |
|---------|--------|------------|
| [01-identityaccess.md](01-identityaccess.md) | Identity & Access | ~120 |
| [02-catalog.md](02-catalog.md) | Catalog (Contratos, Grafo, Dependências, DX) | ~130 |
| [03-changegovernance.md](03-changegovernance.md) | Change Governance (Releases, Aprovações) | ~100 |
| [04-aiknowledge.md](04-aiknowledge.md) | AI Knowledge (Agents, Governance, Runtime) | ~110 |
| [05-auditcompliance.md](05-auditcompliance.md) | Audit & Compliance | ~60 |
| [06-configuration.md](06-configuration.md) | Configuration | ~50 |
| [07-governance.md](07-governance.md) | Governance (Packs, Rulesets, Políticas) | ~70 |
| [08-integrations.md](08-integrations.md) | Integrations (Connectors, Webhooks) | ~50 |
| [09-knowledge.md](09-knowledge.md) | Knowledge (Documentos, Grafo) | ~50 |
| [10-notifications.md](10-notifications.md) | Notifications | ~40 |
| [11-operationalintelligence.md](11-operationalintelligence.md) | Operational Intelligence (Incidentes, SLO, OI) | ~100 |
| [12-productanalytics.md](12-productanalytics.md) | Product Analytics (Dashboards, Métricas) | ~60 |
| [13-plataforma-e2e.md](13-plataforma-e2e.md) | Plataforma (E2E, Outbox, Background Workers) | ~50 |
| [14-building-blocks.md](14-building-blocks.md) | Building Blocks (Core, Pipeline, Segurança) | ~70 |
| [15-seguranca-transversal.md](15-seguranca-transversal.md) | Segurança Transversal (Multi-tenant, JWT, RLS) | ~40 |

**Total estimado: ~1.050 cenários de teste**

---

## Tipos de Teste por Camada

```
┌─────────────────────────────────────────────────────┐
│  E2E / Selenium  →  Fluxos completos multi-módulo   │
│  Integração      →  Handlers + Repos + DB in-memory  │
│  Unitário        →  Handlers isolados (NSubstitute)  │
│  Carga           →  k6 / NBomber (pasta tests/load)  │
└─────────────────────────────────────────────────────┘
```

---

## Critérios de Aceite Globais

- Toda resposta de erro deve conter `traceId` rastreável via OpenTelemetry.
- Endpoints autenticados retornam `401` sem token e `403` com token de tenant diferente.
- Comandos que alteram estado persistem via `IUnitOfWork.CommitAsync` — falha de commit deve rolar back o domínio.
- Eventos de domínio publicados via Outbox devem ser processados em < 30 s (SLA do `ModuleOutboxProcessorJob`).
- Nenhum dado de tenant A deve ser visível para tenant B (RLS PostgreSQL + filtros de repositório).
