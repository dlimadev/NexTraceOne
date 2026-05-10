# Plano 03 — Frontend V3 (Wave AA)

> **Prioridade:** Média  
> **Esforço total:** 16–24 semanas  
> **Spec técnica:** [V3-EVOLUTION-FRONTEND-DASHBOARDS.md](../V3-EVOLUTION-FRONTEND-DASHBOARDS.md)  
> **Contexto:** Wave AA é a contraparte frontend das 155 waves backend já entregues. Transforma o frontend de "app de módulos" em **Operational Intelligence Surface persona-aware**.
> **Estado (Maio 2026):** Em progresso. Base existente: React 19 + 130+ páginas + Custom Dashboards + 7 personas + i18n 4 locales + 1771+ testes frontend. V3.1..V3.12 planeadas — ver sub-waves para estado individual.

---

## Estado Atual do Frontend

O frontend v2 tem:
- React 19 + TypeScript + Vite + TanStack Query + Zustand + Tailwind + Radix UI
- 130+ páginas conectadas a APIs reais
- Custom Dashboards base: 19 widgets, persistência JSONB, clone, partilha, templates
- 7 personas configuradas (Engineer, Tech Lead, Architect, Product, Executive, Admin, Auditor)
- i18n em 4 locales
- 1771+ testes frontend

## Sub-Waves V3

### V3.1 — Dashboard Intelligence Foundation (3–4 semanas)

**Objetivo:** Elevar Custom Dashboards de "grid de widgets" para Governed Intelligence Boards.

**Entregas:**
- **Variables/Tokens**: `$service`, `$environment`, `$team`, `$period` — filtros partilhados por todos os widgets de um dashboard
- **SharingPolicy granular**: Private | Team | Tenant | Public-read | Embedded
- **Dashboard revisions**: histórico de versões com diff visual, revert, auditoria
- **Query-driven widgets**: cada widget declara `query` (ex: `getServiceRiskProfile`, `getDoraMetrics`) com binding automático a variáveis
- **Widget catalog expandido**: 8 novos tipos (Heatmap, Dependency Mini-Graph, Compliance Badge, SLO Burn Rate, Blast Radius Mini, Change Timeline, Knowledge Freshness, Error Budget Gauge)

**Ficheiros chave:**
- `src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx` (renovar)
- `src/frontend/src/shared/dashboard/VariableStore.ts` (novo)
- `src/frontend/src/shared/dashboard/widgets/` (8 novos widgets)

---

### V3.2 — Live Streaming + Annotations (2–3 semanas)

**Objetivo:** Dados em tempo real e contexto de mudanças/incidentes sobre gráficos.

**Entregas:**
- **Live mode**: SignalR subscription para widgets de métricas com refresh automático (5s/15s/1min)
- **Annotations**: marcadores verticais em time-series (deploy, incidente, change freeze) via `ReleaseCalendar` + `IncidentDbContext`
- **Cross-filter**: click numa série de um widget filtra automaticamente os outros widgets do dashboard

---

### V3.3 — Notebooks (2–3 semanas)

**Objetivo:** Superfície de análise narrativa mista (texto + widgets + queries) para Executive e Engineering.

**Entregas:**
- **Notebook entity**: `NotebookDocument` persistido com células (Markdown | Widget | Query | Code)
- **Executive Notebook template**: MTTR trend + Compliance coverage + FinOps burn + Risk heatmap
- **Engineering Notebook template**: Deployment frequency + Error budget + SLO compliance + Incident timeline
- **Export to PDF/HTML**: geração server-side via headless Chromium (ou biblioteca)

---

### V3.4 — Cross-Module Dashboard Widgets (2 semanas)

**Objetivo:** Widgets que cruzam módulos sem sair do dashboard.

**Entregas:**
- **Service 360 Widget**: combina ServiceAsset (Catalog) + SloObservation (OI) + BlastRadius (CG) + RiskProfile (CG) + KnowledgeFreshness (Knowledge) num único card
- **Change Impact Widget**: para uma release, mostra blast radius + confidence score + consumer notifications
- **Compliance Status Widget**: cobertura por standard para um serviço/equipa

---

### V3.5 — Frontend Platform Uplift (2–3 semanas)

**Objetivo:** Melhorar foundation técnica do frontend.

**Entregas:**
- **Design tokens v2**: atualizar `tailwind.config.ts` com tokens semânticos (surface, accent, feedback, etc.)
- **Command Palette** (`Cmd+K`): navegação rápida para qualquer página/serviço/contrato/release
- **WCAG 2.2 compliance**: auditar e corrigir 100% das páginas (focus ring, aria-labels, color contrast)
- **Storybook**: instalar e documentar 75+ componentes do design-system
- **Performance budgets**: Lighthouse CI no pipeline — LCP < 2.5s, TBT < 300ms

---

### V3.6 — Governance Surface + Report Embedding (1–2 semanas)

**Objetivo:** Relatórios de compliance embutíveis e exportáveis.

**Entregas:**
- **Report iframe embedding**: token-based public/private embed de dashboards e relatórios de compliance
- **Scheduled report delivery**: integração frontend com `ScheduledReport` entity (já existe no backend)
- **Export to CSV/XLSX**: qualquer tabela de dados exportável diretamente do frontend

---

### V3.7 — Real-time Collaboration via CRDT (3–4 semanas)

**Objetivo:** Edição colaborativa de dashboards e notebooks em tempo real.

**Entregas:**
- **Yjs integration** (MIT license): CRDT para edição concorrente de layout de dashboards
- **Presence indicators**: avatares de utilizadores ativos no mesmo dashboard
- **Conflict-free edição**: múltiplos utilizadores podem reorganizar widgets simultaneamente
- **SignalR transport** para Yjs awareness protocol (sem servidor externo)

---

### V3.8 — Plugin/Marketplace Architecture (3–4 semanas)

**Objetivo:** Extensibilidade via plugins sem modificar o core.

**Entregas:**
- **Plugin SDK**: interface `IWidgetPlugin` + `IPagePlugin` com Module Federation (Webpack/Rspack)
- **Plugin registry**: endpoint `GET /api/v1/platform/plugins` + UI de gestão
- **3 plugins built-in**: Backstage integration view, Jira ticket linker, PagerDuty schedule widget
- **Plugin sandboxing**: plugins correm em iframe isolado com postMessage API

---

### V3.9 — NQL + Alerting from Widget + PWA (2–3 semanas)

**Objetivo:** Query language e alertas definidos diretamente nos dashboards.

**Entregas:**
- **NQL (NexTrace Query Language)**: DSL simples para queries de dashboard (ex: `services WHERE risk_score > 70 ORDER BY risk_score DESC LIMIT 10`)
- **Widget-based alerts**: definir threshold diretamente num widget (ex: error rate > 5% → notify via Slack)
- **PWA / Mobile on-call**: Service Worker para offline básico + notificações push para on-call engineer

---

### V3.10 — Persona Suites (3–4 semanas)

**Objetivo:** Home page personalizada por persona com widgets e quick actions pre-configurados.

**7 Persona Suites:**
- **Engineer Suite**: My Services, My Contracts, Recent Changes, AI Assistant, On-call Runbooks
- **Tech Lead Suite**: Team Dashboard, Change Velocity, SLO Compliance, Deployment Frequency, Blast Radius heatmap
- **Architect Suite**: Dependency Graph, Contract Adoption, Service Coupling Index, Schema Drift, Technical Debt
- **Product Suite**: Feature Adoption, TTFV, User Journey, Experiment Governance
- **Executive Suite**: Executive Intelligence Dashboard (já existe), MTTR Trend, FinOps Burn, Compliance Overview
- **Platform Admin Suite**: System Health, Risk Center, Compliance Coverage Matrix, EF Migrations, Dead Letters
- **Auditor Suite**: Audit Trail, Evidence Packs, Policy Compliance, Regulatory Change Impact

---

### V3.11 — Source-of-Truth Centers (3–4 semanas)

**Objetivo:** 11 superfícies consolidadas que materializam o NexTraceOne como fonte de verdade.

**11 Centers:**
1. **Compliance Center** — todos os standards (SOC2/ISO27001/PCI-DSS/HIPAA/GDPR/FedRAMP/NIS2/CMMC) numa superfície navegável
2. **Risk Center** — ServiceRiskProfile ranqueado + ZeroTrust posture + Secrets exposure
3. **FinOps Center** — Budget burn + Waste signals + FOCUS export + Cost per release
4. **Change Confidence Center** — Confidence breakdown + Promotion readiness + Release calendar
5. **Release Calendar Center** — Timeline visual de janelas, freezes, hotfixes
6. **Rollback Center** — RollbackAssessment + histórico + viabilidade por release
7. **Blast Radius Center** — Impact cascade + failure simulation + critical path
8. **Evidence Pack Center** — Todos os evidence packs com integrity status e export
9. **Operational Readiness Center** — SLO + Error budget + Chaos coverage + Profiling
10. **Drift Center** — Contract drift + Ownership drift + Configuration drift + Schema drift
11. **SLO + Chaos + Learning Center** — SLO compliance + Chaos experiments + Post-incident learning

---

### V3.12 — Contract Studio Visual + AI Agent Marketplace (4–5 semanas)

**Objetivo:** Visual builders completos e marketplace de agentes IA.

**Entregas:**
- **VisualGraphQlBuilder**: editor SDL com syntax highlighting, type graph visual, breaking change preview
- **VisualProtobufBuilder**: editor .proto com message/service explorer, wire compatibility preview
- **VisualDataContractBuilder**: schema de dados com PII classification visual e SLA freshness config
- **AI Agent Marketplace**: catálogo dos 14+ agentes com configuração por tenant, usage stats, feedback rating
- **IDE Extensions Console**: gestão de tokens para VS Code e Visual Studio extensions
- **Break Glass / JIT UI**: interface visual para solicitação e aprovação de acesso emergencial
- **Knowledge Hub Bridge**: ligação bidirecional entre dashboards e documentos do Knowledge Hub

---

## Pré-requisitos

- Node.js 22+ / pnpm 9+
- Yjs (`npm install yjs y-websocket`) — MIT license ✅
- Module Federation plugin para Vite (`@originjs/vite-plugin-federation`) — MIT ✅

## Critérios de Aceite Globais Wave AA (estado Maio 2026)

- [ ] 7 Persona Suites com home page própria e conteúdo relevante por persona (V3.10 — pendente)
- [ ] 11 Source-of-Truth Centers navegáveis e conectados a APIs reais (V3.11 — pendente)
- [ ] Custom Dashboard suporta variables que filtram todos os widgets em simultâneo (V3.1 — pendente)
- [ ] Edição colaborativa de dashboards funciona com 2+ utilizadores em simultâneo (V3.7 — pendente)
- [ ] WCAG 2.2 compliance em 100% das páginas (Lighthouse score ≥ 90) (V3.5 — pendente)
- [ ] ≥ 525 novos testes frontend dedicados à Wave AA (em progresso)
- [ ] Lighthouse CI: LCP < 2.5s, TBT < 300ms (V3.5 — pendente)
