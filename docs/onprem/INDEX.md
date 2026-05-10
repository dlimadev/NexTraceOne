# NexTraceOne — Plano de Acção On-Premises

> **Data:** Abril 2026 | **Auditoria de implementação:** Maio 2026
> **Contexto:** Plano de acção consolidado para melhorar a experiência de deployment, operação
> e sustentabilidade do NexTraceOne em ambientes **on-premises / self-hosted**.
> **Baseado em:** análise do codebase actual + pesquisa de mercado 2025/2026.

---

## Motivação

O NexTraceOne já tem uma base sólida para on-prem:
- PostgreSQL como única dependência obrigatória
- Ollama para LLM local sem cloud
- Fontes servidas localmente (sem CDN externo)
- Elasticsearch como provider principal de observabilidade self-hosted
- IIS + Windows suportados
- Docker Compose para POC/avaliação

O que falta é a **camada de operacionalização** — as funcionalidades que transformam
um produto que "funciona num servidor" num produto que uma equipa de infra consegue
**instalar, manter, monitorizar, actualizar e recuperar** com confiança e autonomia.

> Um produto que ajuda empresas a reduzir custos operacionais dos seus serviços
> tem de dar o exemplo na sua própria operação.

---

## Estrutura do Plano

| Wave | Ficheiro | Foco | Prioridade |
|------|----------|------|------------|
| W1 | [WAVE-01-INSTALLATION.md](./WAVE-01-INSTALLATION.md) | Instalação, First-Run Wizard, Preflight | **Crítica** |
| W2 | [WAVE-02-SELF-MONITORING.md](./WAVE-02-SELF-MONITORING.md) | Auto-monitorização, Health Dashboard, Alertas | **Alta** |
| W3 | [WAVE-03-UPDATE-RECOVERY.md](./WAVE-03-UPDATE-RECOVERY.md) | Actualizações, Backup, Disaster Recovery | **Alta** |
| W4 | [WAVE-04-AI-LOCAL.md](./WAVE-04-AI-LOCAL.md) | IA Local, Gestão de Modelos, LLM Governance | **Alta** |
| W5 | [WAVE-05-SECURITY-NETWORK.md](./WAVE-05-SECURITY-NETWORK.md) | Segurança, Rede, Air-Gap, Zero Trust | **Alta** |
| W6 | [WAVE-06-RESOURCES-FINOPS.md](./WAVE-06-RESOURCES-FINOPS.md) | Recursos, FinOps, Sustentabilidade, GreenOps | **Média** |
| W7 | [WAVE-07-OBSERVABILITY.md](./WAVE-07-OBSERVABILITY.md) | Observabilidade, Elasticsearch, Retenção de Dados | **Média** |
| W8 | [WAVE-08-FUTURE.md](./WAVE-08-FUTURE.md) | Evoluções Futuras, Capacidade, Multi-Tenant | **Roadmap** |

---

## Estado Actual (Maio 2026 — pós-auditoria de implementação)

### O que já existe e funciona (base original)

| Capacidade | Estado |
|---|---|
| PostgreSQL como única dependência obrigatória | READY |
| Ollama para LLM local (sem cloud) | READY |
| Fontes servidas localmente via `@fontsource` | READY |
| Elasticsearch como provider principal de observabilidade self-hosted | READY |
| Auto-migration bloqueado em produção | READY |
| Health endpoints `/health`, `/ready`, `/live` | READY |
| Docker Compose para avaliação/POC | READY |
| Suporte a IIS + Windows Server | READY |
| Outbox Pattern com 25 DbContexts | READY |
| Audit Trail com hash chain SHA-256 | READY |
| Break Glass + JIT Access | READY |

### Estado de implementação das Waves (Maio 2026)

| Wave | Item | Estado |
|---|---|---|
| W1 | Preflight Check Engine (`/preflight`) | IMPLEMENTADO |
| W1 | Setup Wizard (`/setup`, `SetupWizardState`) | IMPLEMENTADO |
| W1 | Configuration Validator (`/api/v1/config-health`) | IMPLEMENTADO |
| W1 | Seed de Demonstração (`DemoSeedState`) | IMPLEMENTADO |
| W2 | Admin Health Dashboard (`GetPlatformHealth`) | IMPLEMENTADO |
| W2 | Startup Report (`/startup-report`) | IMPLEMENTADO |
| W2 | Auto-Diagnóstico Proactivo (`PlatformHealthMonitorJob`) | NAO IMPLEMENTADO |
| W2 | Support Bundle Generator (`SupportBundle`) | IMPLEMENTADO |
| W3 | Migration Preview (`/migrations/pending`) | IMPLEMENTADO |
| W3 | Offline Release Bundle | NAO IMPLEMENTADO |
| W3 | Backup Coordinator (feature de config + job) | PARCIAL |
| W3 | Point-in-Time Recovery Wizard | NAO IMPLEMENTADO |
| W3 | Graceful Shutdown (`GetGracefulShutdownConfig`) | IMPLEMENTADO |
| W4 | Model Manager UI (`/admin/ai/models`) | IMPLEMENTADO |
| W4 | LLM Hardware Advisor (`/ai/hardware-assessment`) | IMPLEMENTADO |
| W4 | AI Resource Governor (UI + config) | PARCIAL |
| W4 | AI Governance / Avaliação de Qualidade | NAO IMPLEMENTADO |
| W4 | Offline Model Bundle | NAO IMPLEMENTADO |
| W5 | Network Isolation Mode / Air-Gap | PARCIAL |
| W5 | Proxy Corporativo & Internal CA | NAO IMPLEMENTADO |
| W5 | Audit de Chamadas Externas (`GetExternalHttpAudit`) | IMPLEMENTADO |
| W5 | mTLS interno | NAO IMPLEMENTADO |
| W5 | Fine-Grained Auth por Ambiente | NAO IMPLEMENTADO |
| W5 | Session Security Hardening | NAO IMPLEMENTADO |
| W6 | Waste Detection Engine (`GetWasteSignals`) | PARCIAL |
| W6 | Non-Prod Shutdown Scheduler | NAO IMPLEMENTADO |
| W6 | Resource Budget por Tenant (`GetResourceBudget`) | IMPLEMENTADO |
| W6 | GreenOps / Carbon Score | NAO IMPLEMENTADO |
| W6 | Rightsizing Recommendations | NAO IMPLEMENTADO |
| W7 | Elasticsearch Index Manager (UI + feature) | PARCIAL |
| W7 | Elasticsearch Health Dashboard | IMPLEMENTADO (via W2-01) |
| W7 | Lightweight Mode (ObservabilityMode) | NAO IMPLEMENTADO |
| W7 | PostgreSQL Health Dashboard (`/admin/database-health`) | IMPLEMENTADO |
| W7 | DORA Metrics Dashboard | IMPLEMENTADO |
| W8 | Todos os itens (W8-01..06) | ROADMAP |

### Gaps identificados (originais — maioria resolvidos)

| Gap | Impacto | Wave | Estado |
|---|---|---|---|
| Sem First-Run Wizard | Barreira de adopção alta | W1 | RESOLVIDO |
| Sem Preflight Check | Falhas silenciosas na instalação | W1 | RESOLVIDO |
| Sem auto-diagnóstico da plataforma | Problemas descobertos pelos utilizadores | W2 | PARCIAL |
| Sem Backup Coordinator integrado | Risco de perda de dados | W3 | PARCIAL |
| Sem Migration Preview | Risco em actualizações de produção | W3 | RESOLVIDO |
| Sem Model Manager UI (LLM) | Gestão de IA requer SSH | W4 | RESOLVIDO |
| Sem Air-Gap Mode explícito | Dados podem sair do perímetro | W5 | PARCIAL |
| Sem Resource Budget por Tenant | Consumo descontrolado em shared servers | W6 | RESOLVIDO |
| Sem Elasticsearch Index Manager | Disco pode encher sem aviso | W7 | PARCIAL |
| Sem Offline Release Bundle | Actualização requer internet | W3 | PENDENTE |

---

## Critérios de Priorização

Cada item foi classificado por:

1. **Bloqueante para adopção** — sem isto, o cliente não consegue instalar ou manter o produto
2. **Diferenciador competitivo** — distingue o NexTraceOne de plataformas genéricas
3. **Custo de implementação** — estimativa de esforço relativo (S/M/L/XL)
4. **Risco operacional mitigado** — gravidade do problema que resolve

---

## Princípio Guia

> Toda melhoria on-prem deve responder afirmativamente a:
> — Isto reduz a dependência de intervenção humana especializada?
> — Isto aumenta a confiança da equipa de infra no produto?
> — Isto funciona sem acesso à internet?
> — Isto é auditável e rastreável?
