# NexTraceOne v3 — Plano de Evolução: Frontend & Dashboards Customizados

> **Data:** Abril 2026
> **Horizonte:** v3 (inicia após conclusão da v2 em curso)
> **Escopo:** Duas vertentes — **(1) Frontend/Experience** e **(2) Custom Dashboards** (persistidos, partilháveis, extensíveis — inspiração forte Dynatrace Dashboards/Notebooks, Grafana e Datadog, sem clonagem literal)
> **Referências de alinhamento:** [FRONTEND-ARCHITECTURE.md](./FRONTEND-ARCHITECTURE.md), [DESIGN-SYSTEM.md](./DESIGN-SYSTEM.md), [UX-PRINCIPLES.md](./UX-PRINCIPLES.md), [PERSONA-UX-MAPPING.md](./PERSONA-UX-MAPPING.md), [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md), [SOURCE-OF-TRUTH-STRATEGY.md](./SOURCE-OF-TRUTH-STRATEGY.md)

---

## 0. Contexto

Este plano consolida:

- Estado atual do frontend v2 do NexTraceOne (React 19 + TS + Vite 7 + TanStack Query/Router + Tailwind 4 + Radix UI + ECharts + Monaco, i18n 4 locales).
- Base já existente de Custom Dashboards (aggregate `CustomDashboard`, 19 widgets, páginas `CustomDashboardsPage` / `DashboardBuilderPage` / `DashboardViewPage`, persistência JSONB no PostgreSQL, clone, partilha on/off, sistema de templates, export/import JSON).
- Pesquisa de mercado (Dynatrace Dashboards/Notebooks & Davis CoPilot, Grafana 11 + Scenes, Datadog Dashboards, Observe OPAL Worksheets, Honeycomb Boards, New Relic Workloads, Looker/Mode, Superset, Metabase) sobre o que hoje é considerado estado-da-arte em dashboards enterprise: **query-driven widgets, live streaming, tokens/variables, notebooks, versionamento, partilha granular, deep-link contextual, AI-assisted creation, embedding, público/privado/equipa, delegação, colaboração em tempo real, marketplace, plugins, mobile on-call, dashboard-as-code**.

> **Referências validadas nesta sessão (2026-04-20):** durante a re-tentativa, a rede do sandbox bloqueou docs/homepages oficiais (`docs.dynatrace.com`, `grafana.com/docs`, `docs.datadoghq.com`, `honeycomb.io`, `react.dev`, `tanstack.com`, `backstage.io`), mas foi possível aceder ao GitHub. As seguintes referências foram consultadas diretamente nos respetivos repositórios. **Todas as referências abaixo marcadas como "candidata a dependência" têm licença compatível com software comercial self-hosted/distribuído (MIT, Apache-2.0, BSD-2/3, ISC, MPL-2.0). Referências marcadas "UX-only" não entram como dependência por serem copyleft forte (AGPL/GPL) ou source-available com restrição comercial:**
>
> | Referência | Repositório | Licença | Uso permitido no NexTraceOne | O que confirma |
> |---|---|---|---|---|
> | **Grafana core** | `github.com/grafana/grafana` | **AGPL-3.0-only** ❌ | **UX-only — NUNCA como dependência, fork ou código embutido** | Inspiração visual para annotations, variables, history. |
> | Grafana Scenes | `github.com/grafana/scenes` | **Apache-2.0** ✅ | Candidata a dependência | Framework moderno para apps altamente interativas — V3.2/V3.3. |
> | Backstage | `github.com/backstage/backstage` | **Apache-2.0** ✅ (CNCF Incubation) | Candidata a dependência / inspiração arquitetural | Software Catalog + Templates + TechDocs + plugins — V3.8. |
> | Apache Superset | `github.com/apache/superset` | **Apache-2.0** ✅ | UX-only (é produto standalone, não lib) | no-code charts + SQL editor + semantic layer — V3.2/V3.4. |
> | TanStack Router | `github.com/TanStack/router` | **MIT** ✅ | Já em uso | type-safety, schema-driven search params — base V3.5. |
> | **Yjs (CRDT)** | `github.com/yjs/yjs` | **MIT** ✅ | Candidata a dependência | Usado por Evernote, Gitbook, AFFiNE — V3.7. |
> | Module Federation | `github.com/module-federation/module-federation-examples` | **MIT** ✅ (Webpack/Rspack) | Candidata a técnica de build | Plugin loading V3.8. |
> | OpenTelemetry spec | `github.com/open-telemetry/opentelemetry-specification` | **Apache-2.0** ✅ | Spec, não é dependência | Telemetria para V3.2. |
> | AsyncAPI spec 3.1.0 | `github.com/asyncapi/spec` | **Apache-2.0** ✅ | Spec, já suportada | Annotations e event contracts. |
>
> **⚠️ Alerta sobre Grafana:** `grafana/grafana` é **AGPL-3.0**, o que é incompatível com a distribuição comercial self-hosted do NexTraceOne. Fica explicitamente proibido: (a) importar, linkar ou empacotar Grafana core; (b) derivar/fork; (c) copiar trechos não-triviais de código-fonte AGPL. Apenas a biblioteca **Grafana Scenes** (Apache-2.0) seria tecnicamente elegível se algum dia fizesse sentido. Grafana permanece nesta pesquisa **apenas como referência de UX/UX patterns**, que não são protegíveis por licença de código.
>
> Pontos de ação de transparência: quando os domínios de docs oficiais ficarem acessíveis (fora do sandbox), validar Dynatrace Notebooks/Davis CoPilot UX patterns, Grafana 11 Scenes data flow, Datadog live-stream throttling, Honeycomb Boards sharing model, PagerDuty mobile on-call flows, React 19 `use` hook + View Transitions guidelines.

### 1.0 Política obrigatória de licenciamento de dependências (hard constraint)

O NexTraceOne é um produto **comercial self-hosted/on-premises** e portanto **nenhuma dependência transitiva ou direta** pode ter licença que restrinja uso comercial, exija abertura do código do NexTraceOne, ou condicione a distribuição.

**Licenças aceites** (compatíveis com software comercial distribuído): MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, Unlicense/CC0, MPL-2.0 (weak copyleft por ficheiro, aceitável com cuidado).

**Licenças proibidas** como dependência, fork, link, código embutido, "vendor" ou "copy-paste": **GPL (qualquer versão), LGPL, AGPL-3.0** (p.ex. Grafana core), **SSPL** (p.ex. MongoDB, Elasticsearch 7.11+, OpenSearch não se aplica mas atenção), **Elastic License v2 (ELv2)** (p.ex. Kibana, Logstash pós-2021), **Business Source License (BUSL/BSL)** (p.ex. Sentry, Redis 7.4+, HashiCorp Terraform/Vault/Consul pós-2023, CockroachDB), **Commons Clause**, **Confluent Community License**, **Redis Source Available License (RSALv2/SSPLv1)**, qualquer licença "source-available" com cláusula anti-comercial, "non-commercial", "research-only", CC BY-NC/ND, ou que imponha fees de royalty.

**Regra operacional:**

1. Antes de adicionar qualquer dependência nova (NuGet, npm, Go, Maven, etc.), **validar a licença** via `gh-advisory-database` e inspeção direta do repositório.
2. Se a licença não estiver na lista de aceites, **a dependência é rejeitada** — procurar alternativa permissiva ou implementar internamente.
3. **Dependências indiretas** (transitivas) contam. Se uma lib MIT puxar uma AGPL, a combinação é proibida.
4. Atenção a **re-licenciamentos retroativos**: libs que eram permissivas mas mudaram para BUSL/ELv2/SSPL (Redis, Elasticsearch, HashiCorp, Sentry, MongoDB). Fixar versão na última release compatível e planear substituição.
5. **MediatR v12+** e **AutoMapper v13+** passaram a ter licenciamento comercial — qualquer upgrade nestas libs exige reavaliação. Se ainda estivermos em versão pré-comercial, **NÃO atualizar** sem decisão explícita.
6. Documentar **NOTICE** / atribuições Apache-2.0 no release self-hosted.
7. Para código de UX/componente inspirado em referências AGPL/GPL/BUSL, **não copiar código-fonte**; apenas reimplementar de forma independente (clean-room) os *padrões de UX*, que não são protegíveis por direito autoral.

Esta política aplica-se a **todas as waves V3.1–V3.9** e substitui qualquer sugestão anterior de biblioteca que não cumpra os critérios acima. Toda nova referência adicionada a este documento **deve** explicitar a licença.
- Alinhamento obrigatório com a visão oficial do produto: **dashboards não são dashboards genéricos de observabilidade** — são **superfícies de decisão contextualizadas** em serviços, contratos, mudanças, ownership, ambiente, persona e governança. (Copilot Instructions §10, §18, §20, §27)

**Princípio-mestre da v3 para estas duas vertentes:** elevar o frontend de "app de módulos" para **Operational Intelligence Surface** persona-aware, e elevar Custom Dashboards de "grid de widgets" para **Governed Intelligence Boards** — persistidos, versionados, partilháveis, auditáveis e integrados com o Source of Truth.

---

## 1. Oportunidades identificadas (pesquisa de mercado consolidada)

### 1.1 Tendências em Dashboards enterprise

| Tendência | Referência de mercado | Relevância para NexTraceOne |
|---|---|---|
| **Query-driven widgets** (DQL, PromQL, SQL-like) com Monaco editor | Dynatrace DQL, Grafana, Observe OPAL | Alta — permitir widget custom sem criar novo tipo no registry |
| **Notebook mode** (células executáveis + narrativa) | Dynatrace Notebooks, Jupyter | Alta — investigação operacional e post-mortems |
| **Dashboard variables / tokens** (`$service`, `$env`, `$team`, `$timeRange`) | Grafana template variables | Crítica — hoje cada widget fixa seu contexto |
| **Live streaming** (dados em tempo real sem polling manual) | Datadog, Honeycomb | Alta para operação ativa |
| **Cross-filtering** (clique num widget filtra os outros) | Datadog, PowerBI | Alta — UX moderna |
| **Annotations** (change events, deploys, incidents sobrepostos) | Grafana, Datadog | Crítica — liga dashboards ao Change Intelligence |
| **Drill-down contextual** (widget → página de detalhe do módulo) | Dynatrace "Open with…" | Crítica — reforça Source of Truth |
| **Versionamento e histórico** (como um doc Google) | Grafana Dashboard History, Dynatrace | Alta — auditoria + rollback |
| **Partilha granular** (público link, team, role, read/edit, embed) | Datadog public shares, Grafana | Alta — já temos `IsShared`; evoluir |
| **AI-assisted dashboard creation** ("crie-me um dashboard para Payment Service") | Dynatrace Davis CoPilot, Grafana ML | Altíssima — alinha com pilar AI-assisted |
| **Dashboard as Code** (YAML/JSON exportável, GitOps) | Grafana, Terraform providers | Alta — já temos export JSON; evoluir para DaC |
| **Alerting from widget** (criar alerta a partir duma query num widget) | Grafana alerts, Datadog monitors | Alta |
| **Responsive layouts + mobile on-call companion** | Datadog mobile, PagerDuty | Alta — crítico para Engineer on-call |
| **Whitelabel / embed iframe assinado** | Grafana, Datadog | Alta — parceiros e intranet corporativa |
| **Scheduled reports / snapshots** (PDF, PNG, email) | Dynatrace Reports, Grafana Reporting | Alta — Exec/Auditor personas |
| **Dark/Light/High-contrast + density modes** | Observe, Dynatrace | Alta — parte da experiência enterprise |
| **Colaboração em tempo real** (cursors, presença, comments ancorados) | Figma, Notion, Observe | Alta em War Room e Notebooks |
| **Marketplace / template gallery** (interno e comunitário) | Grafana.com dashboards, Power BI templates | Alta — acelera adoção e valor percebido |
| **Plugin SDK para widgets de 3ºs** | Grafana plugins, Superset | Alta — extensibilidade enterprise e por parceiros |
| **Alerting/Action from dashboard** (criar alerta/runbook a partir do widget) | Grafana, Datadog, New Relic | Alta — fecha o ciclo observação→ação |
| **NQL avançado** (joins governados, subqueries, UDFs registadas) | Observe OPAL, Dynatrace DQL | Média-alta — sob governança estrita |

### 1.2 Tendências em Frontend enterprise

| Tendência | Relevância para NexTraceOne |
|---|---|
| **Command Palette global** (Cmd+K) com ações, navegação, search semântico | Alta — alinha com pilar Source of Truth |
| **Micro-frontends / Module Federation** para plugins e extensões | Alta — habilita extensões por parceiros, equipas internas e ISVs |
| **Streaming SSR com React 19** (use hook, Suspense data) | Média — ganhos de performance em páginas pesadas |
| **View Transitions API + route transitions** | Alta — sensação premium enterprise |
| **Real-time collaboration** (cursors partilhados, presença, comentários ancorados) | Alta — diferenciador para War Room, Notebooks e Dashboards colaborativos |
| **Accessibility WCAG 2.2 AA completo** | Alta — enterprise compliance |
| **Design tokens unificados** (JSON → Tailwind + CSS vars) | Alta — consolidar o design system |
| **Storybook / UI contract testing** para componentes reutilizáveis | Alta — qualidade e ADR-driven UI |
| **IDE-like dev experience** (split panes, tabs, session restore) | Alta — War Room, Contract Studio |
| **Offline-first / optimistic mutations** em fluxos críticos | Média — ativar onde faz sentido |
| **Performance budgets automatizados** (bundle-size, LCP, INP) | Alta — requisito enterprise |
| **Mobile companion on-call** (PWA ou nativo) | Alta — on-call, approvals e incident awareness móveis |
| **Internacionalização avançada** (RTL + plural rules completos) | Média — abre mercado EMEA |

---

## 2. Estado atual relevante (ponto de partida v3)

### 2.1 Dashboards
- Aggregate `CustomDashboard` no módulo `Governance` com `Widgets` em JSONB, `IsShared`, `IsSystem`, `TeamId`, `TenantId`, `CreatedByUserId`, `RowVersion` (concorrência otimista), `Clone()`.
- 19 tipos de widget registados em `WidgetRegistry.ts` (dora-metrics, service-scorecard, incident-summary, change-confidence, cost-trend, reliability-slo, knowledge-graph, on-call-status, alert-status, change-timeline, slo-gauge, deployment-frequency, stat, text-markdown, top-services, contract-coverage, blast-radius, team-health, release-calendar).
- Páginas: `CustomDashboardsPage` (lista/busca/ordenação), `DashboardBuilderPage` (slot-based grid com preview, export/import JSON, auto-arrange) e `DashboardViewPage` (viewer).
- Endpoints em `DashboardsAndDebtEndpointModule.cs`.

### 2.2 Frontend
- React 19, TS 5.9, Vite 7, TanStack Router/Query 5, Tailwind 4, Radix UI, ECharts, Monaco, i18next em 4 locales (en, pt-BR, pt-PT, es).
- 130+ páginas feature-based alinhadas com bounded contexts.
- Design system baseado em componentes internos (`Card`, `Button`, `Badge`, `PageHeader`, `EmptyState`, `PageLoadingState`, etc.).
- Auth + `EnvironmentContext` + i18n como contexts transversais.

### 2.3 Gaps relevantes (ponto de partida da v3)
- Dashboard widgets são "hard-coded": não há **query-driven custom widget**.
- Não há **variables/tokens** de dashboard; cada widget fixa `serviceId/teamId/timeRange`.
- Não há **versionamento** (histórico de alterações) nem auditoria por widget.
- Partilha é boolean (`IsShared`); não há **permissões granulares** (read/edit/public-link/embed).
- Sem **annotations** (changes, incidents, deploys) sobrepostas nos widgets.
- Sem **cross-filter** / **drill-down** nativo.
- Sem **AI-assisted creation** dedicado para dashboards.
- Sem **notebook mode** para investigação.
- Sem **scheduled snapshots** ou **exportação PDF/PNG**.
- Frontend sem **Command Palette global**, sem **view transitions**, sem **performance budgets** formais.
- Sem **Storybook** ou testes visuais automatizados para widgets.

---

## 3. Plano de Evolução v3

O plano organiza-se em **12 waves** entregáveis de forma incremental (V3.1–V3.9 cobrem a fundação + extensibilidade + mobilidade; V3.10–V3.12 incrementam personas, Source-of-Truth Surfaces e consoles enterprise estratégicos). Cada wave é auto-contido e entrega valor observável.

---

### WAVE V3.1 — Dashboard Intelligence Foundation
**Objetivo:** preparar a fundação (domínio e UX) para dashboards enterprise-grade.

#### Backend
1. **Versionamento de dashboards**
   - Nova entidade `DashboardRevision` (snapshot de widgets + layout + metadata).
   - Toda `Update()` produz uma revisão; suportar `GetHistory`, `RevertTo`, `DiffRevisions`.
   - Integração com `IAuditTrail` (quem alterou, quando, diff resumido).
2. **Dashboard Variables (tokens)**
   - Novo value object `DashboardVariable { Key, Label, Type (service|team|env|timeRange|text|enum), Default, Source (catalog|static) }`.
   - Widgets passam a resolver placeholders `$service`, `$team`, `$env`, `$timeRange` a partir das variáveis do dashboard.
   - Migration: `dashboard_variables` JSONB em `custom_dashboards`.
3. **Granular sharing (primeira fase)**
   - Substituir `IsShared: bool` por `SharingPolicy { Scope: Private|Team|Tenant|PublicLink, Permissions: Read|Edit, SignedLinkExpiresAt? }`.
   - Backward-compat: `IsShared=true` → `Tenant/Read`. Migration preserva dados.
   - Novo endpoint `POST /governance/dashboards/{id}/share` com validação por `permission: governance:dashboard:share`.
4. **Auditoria e concorrência**
   - Reforçar eventos de domínio: `DashboardCreated`, `DashboardUpdated`, `DashboardShared`, `DashboardRevisionCreated`, `DashboardDeleted`, `DashboardCloned`.
   - Garantir `RowVersion` (xmin) em toda mutação.

#### Frontend
5. **Variables editor** no `DashboardBuilderPage` (painel lateral).
6. **Dashboard header redesign**: seletor de variáveis, time range global, env selector (já existe `EnvironmentContext`), indicadores de partilha e última revisão.
7. **History drawer** com diff semântico (widgets adicionados/removidos/alterados).
8. **i18n completo** em 4 locales.

#### Critérios de aceite
- Toda alteração regista revisão auditável; revert funciona.
- Widgets respondem a variáveis de dashboard (sem necessidade de alterar o widget).
- Partilha granular operacional com 4 scopes; permissões backend autoritativas.
- Testes: ≥25 testes (domain + application + frontend unit).

---

### WAVE V3.2 — Query-driven Widgets & Widget SDK
**Objetivo:** permitir widgets personalizados sem adicionar tipos ao registry; abrir para extensibilidade.

#### Backend
1. **`QueryWidget` type** — widget que executa uma **NexTraceOne Query Language (NQL)** contra módulos governados (Catalog, ChangeGovernance, OperationalIntelligence, Knowledge, FinOps).
   - NQL começa com um **subset read-only seguro** (sem joins livres): seletores por módulo + filtros + agregações + time bucket.
   - Execução passa obrigatoriamente por `IQueryGovernanceService` (tenant, environment, persona, row-level security).
   - Timeout, row cap, cache configuráveis via `IConfigurationResolutionService` (keys em `DashboardConfigKeys`).
2. **Widget SDK (interno primeiro)**
   - Contrato `IWidgetKind { Key, Schema, Executor, DefaultRenderHint }` publicado como interface estável no `BuildingBlocks`.
   - Widgets existentes migram progressivamente para este contrato (sem breaking change na API pública).
   - Visão para v4: SDK externo, mas não escopo de v3.
3. **Annotations API**
   - Novo endpoint `GET /governance/dashboards/annotations?from=&to=&services=` agregando: changes (ChangeGovernance), incidents (OI), deploys, contract breaking changes, policy violations.
   - Annotations são sobrepostas em time-series widgets.

#### Frontend
4. **NQL Editor** baseado em Monaco (auto-complete, syntax highlight, schema-aware).
5. **Live preview** da query com paginação e formato tabular/serial/metric.
6. **QueryWidget renderer** com escolha de visualização (line/area/bar/stat/table/heatmap).
7. **Annotations overlay** em todos os widgets de tempo.
8. **Widget catalog / palette** reorganizado: categorias (Services, Changes, Operations, Knowledge, FinOps, AI, Custom Query).

#### Critérios de aceite
- Utilizador cria widget com NQL e partilha no dashboard sem código.
- Segurança: tenant-aware, environment-aware, persona-aware, row cap, timeout.
- Annotations aparecem corretamente em widgets com eixo temporal.
- Testes: ≥30 testes (incluindo parser/validator NQL e governance).

---

### WAVE V3.3 — Live, Cross-filter, Drill-down
**Objetivo:** transformar dashboards em **superfícies operacionais ativas**.

#### Backend
1. **Live streaming**
   - Canal SSE (Server-Sent Events) em `/governance/dashboards/{id}/live` com sub-assinatura por widget.
   - Fallback gracioso para polling intervalado quando SSE indisponível.
2. **Delta endpoint** para widgets caros (só devolve variações desde timestamp).

#### Frontend
3. **Live toggle** no header do dashboard (per-widget override).
4. **Cross-filter**
   - Clicar num bucket/categoria de um widget aplica filtro contextual aos outros (service/team/envelope temporal).
   - Breadcrumb de filtros ativos com "clear all".
5. **Drill-down**
   - Cada widget define uma `drillRoute(context)` que navega para a página do módulo-dono com filtros pré-aplicados (ex.: stat de incidentes → `/operations/incidents?serviceId=…&from=…&to=…`).
   - Uniformiza o "Open with…" estilo Dynatrace.
6. **Deep-link reintegration**
   - URL do dashboard reflete variáveis, cross-filters e time range (copy-paste partilhável que preserva exatamente o que se vê).

#### Critérios de aceite
- SSE estável; desligamento gracioso em troca de persona/env/tenant.
- Cross-filter e drill-down consistentes em ≥80% dos widgets.
- URL de dashboard estável e partilhável.
- Testes: ≥20 testes (SSE com fakes, cross-filter, drill routes).

---

### WAVE V3.4 — AI-assisted Dashboard Creation & Notebook Mode
**Objetivo:** reduzir drasticamente o time-to-first-dashboard e habilitar investigação operacional.

#### Backend
1. **AI Agent "Dashboard Composer"** sob a governança do módulo AI (respeitando `AI-GOVERNANCE.md`, quotas de token, audit trail, model registry).
   - Input: prompt natural + contexto (persona, team, env, services já selecionados).
   - Output: proposta estruturada (variáveis, layout, widgets, queries NQL) como **draft** — nunca aplica sem aprovação humana.
   - Grounding obrigatório em Catalog, Contracts, Changes, Incidents.
2. **Notebooks**
   - Nova entidade `Notebook` (aggregate no mesmo bounded context de Dashboards ou novo submódulo Governance.Notebooks).
   - Notebook = lista ordenada de células (`MarkdownCell`, `QueryCell`, `WidgetCell`, `ActionCell`, `AiCell`).
   - Persistência, versionamento e partilha alinhados com `SharingPolicy` de dashboards.
   - Ideal para post-mortems, war rooms e runbooks vivos (alinhado com `Knowledge`).

#### Frontend
3. **"Compose with AI" wizard** no `CustomDashboardsPage`.
   - Mostra diff entre estado atual e proposta; utilizador aceita/ajusta/rejeita por widget.
4. **Notebook editor** full-page (inspiração Dynatrace Notebooks, sem clone literal), com:
   - Split view narrativa/resultado, execução por célula, restauro de sessão, export para PDF/HTML.
5. **Cross-link** Dashboard ↔ Notebook (transformar dashboard em notebook de investigação).

#### Critérios de aceite
- AI Composer produz draft válido que passa pelos validadores de NQL/governança.
- Notebooks funcionais com 5 tipos de célula, persistidos, versionados e partilhados.
- Trilha de auditoria de IA regista prompt, modelo, custo, utilizador.
- Testes: ≥35 testes (incluindo grounding, audit, notebook rendering).

---

### WAVE V3.5 — Frontend Platform Uplift
**Objetivo:** elevar a plataforma frontend a padrão enterprise-grade premium e preparar o terreno para plugins no v4.

1. **Design Tokens v2**
   - Fonte única JSON (`src/frontend/src/design/tokens.json`) → gera Tailwind preset + CSS vars + tipos TS.
   - Suporte formal a temas: Light, Dark, High-Contrast; densidade Comfortable/Compact.
2. **Command Palette global (Cmd+K / Ctrl+K)**
   - Ações globais, navegação, busca semântica (Knowledge + Catalog + Contracts + Dashboards).
   - Respeita permissões e persona.
3. **View Transitions + route-level skeletons** para sensação premium.
4. **Accessibility WCAG 2.2 AA**
   - Auditoria automatizada (axe) em CI; correção do backlog identificado; focus management uniforme.
5. **Performance budgets automatizados**
   - LCP/INP/TBT/Bundle-size budgets por rota no CI; falha de PR quando excedido (configurável).
   - Code-splitting agressivo para Monaco/ECharts/Playwright-touched surfaces.
6. **Storybook** para componentes do design system + widgets de dashboard.
   - Testes visuais automatizados (Playwright screenshots em CI para páginas-chave).
7. **Error/Empty/Loading states uniformes**
   - Consolidar `PageErrorState`/`EmptyState`/`PageLoadingState` com variantes contextuais por persona.
8. **Microcopy i18n completo** com `Intl.MessageFormat` para plurais e genderização onde aplicável.
9. **Telemetry de UX** (respeitando privacidade/audit): eventos de uso por módulo alimentando `ProductAnalytics`.
10. **Command Palette + Quick Actions** por persona no dashboard home (ex.: Engineer vê "Open my on-call dashboard", Exec vê "Open Portfolio Scorecard").

#### Critérios de aceite
- Design tokens documentados e aplicados em 100% das páginas novas; migração incremental das antigas.
- Command Palette com ≥20 ações e busca semântica.
- Axe-core: 0 violações críticas; ≤5 sérias em rotas core.
- Perf budget CI ativo; LCP p75 <2.5s em páginas core.
- Storybook publica ≥60 componentes e ≥15 widgets.

---

### WAVE V3.6 — Governance, Reports & Embedding
**Objetivo:** fechar o ciclo enterprise — auditoria, relatórios e integração externa.

1. **Scheduled Reports / Snapshots**
   - Snapshots agendados via Quartz: PDF e PNG, envio por email (SMTP já suportado) ou webhook.
   - Retenção configurável; download via link assinado.
   - Personas Exec/Auditor como consumidores primários.
2. **Public Signed Links & Embedding**
   - Links públicos com expiração, rate limit e escopo imutável (snapshot de variáveis).
   - `iframe embed` assinado (JWT) com allowed origins por tenant.
   - Redação de dados sensíveis configurável no snapshot.
3. **Dashboard as Code (DaC)**
   - Export/import YAML canonicalizado (já existe JSON; adicionar YAML canônico versionado e diffable).
   - CLI `nextraceone dashboard apply -f dash.yaml` (entregue junto com Wave B.2 CLI).
   - Suporte a templating server-side (variáveis + `${{ catalog.services.by_team(...) }}` helpers seguros).
4. **Governance pack**
   - Policies sobre dashboards: naming, required variables, required owners, partilha pública bloqueada por tenant, max widgets.
   - Violations aparecem no `Risk Center` e bloqueiam partilha pública se configurado.
5. **Usage analytics**
   - Quais dashboards são mais vistos, por quem, com que frequência; alimenta decisões de curadoria.
6. **Deprecation lifecycle**
   - Estados: `Draft`, `Published`, `Deprecated`, `Archived`; mensagem ao abrir dashboard deprecado com ponteiro para substituto.

#### Critérios de aceite
- Snapshots PDF/PNG funcionais com retenção; email entregue via SMTP configurável.
- Public/embed link validado contra policies e auditado.
- DaC roundtrip (export → apply) preserva 100% do dashboard.
- Policies ativas e avaliadas em `Risk Center`.

---

### WAVE V3.7 — Real-time Collaboration & War Room
**Objetivo:** transformar dashboards e notebooks em **superfícies colaborativas** onde equipas investigam juntas em tempo real.

#### Backend
1. **Presence & Collaboration Service**
   - Canal WebSocket/SSE por `dashboardId` / `notebookId` com presença (quem está a ver), cursores partilhados e edição concorrente baseada em **CRDTs — candidato de referência: [Yjs](https://github.com/yjs/yjs) (MIT)**, adotado por Evernote, Gitbook, AFFiNE, Huly, Sana, com suporte nativo a shared cursors, offline, undo/redo e version snapshots.
   - Tenant-aware, environment-aware, respeitando `SharingPolicy` (só participa quem tem permissão de leitura/edição).
   - Histórico de sessão colaborativa auditável (quem participou, quando, que alterações propôs).
2. **Comentários ancorados**
   - Entidade `DashboardComment` / `NotebookCellComment` com referência a widget/célula, resolução (open/resolved), @menções a utilizadores e equipas.
   - Notificações via SMTP e webhooks (respeitando `IntegrationsArchitecture`).

#### Frontend
3. **Indicadores de presença** (avatares, cursores por cor) em dashboards e notebooks.
4. **Threads de comentários** ancorados por widget/célula, com resolução e mentions.
5. **Modo "War Room"** (uni-link para stakeholders entrarem rapidamente num incidente com contexto pré-carregado).

#### Critérios de aceite
- Colaboração estável para ≥10 utilizadores simultâneos por dashboard/notebook.
- Sem corrupção de estado em cenários de conflito; log de auditoria coerente.
- Permissões respeitadas em tempo real (revogação propaga em ≤5s).

---

### WAVE V3.8 — Marketplace, Plugin SDK & Widgets de Terceiros
**Objetivo:** abrir o produto a extensões governadas — templates, widgets e plugins — sem perder coerência nem segurança.

#### Backend
1. **Template Gallery interno**
   - Dashboards e notebooks publicados como **templates reutilizáveis** (per-tenant e globais para a plataforma), com versão, autor, tags, persona alvo e dependências (variáveis requeridas, permissões).
   - Instalar = clonar com resolução automática de variáveis contra Catalog/Environments.
2. **Plugin SDK público**
   - Contrato estável `IWidgetKind` (evoluído da Wave V3.2) documentado como SDK, com empacotamento, assinatura de artefactos, verificação de integridade e sandbox de execução.
   - Registry multi-nível: System / Tenant / Private.
   - Compatível com **[Module Federation](https://github.com/module-federation/module-federation-examples)** (Webpack 5 / Rspack / Vite) para carregamento isolado e versionado — padrão já adotado por Netflix, Microsoft, Amazon, Shopify, Auth0.
3. **Governance de plugins**
   - Políticas por tenant: plugins permitidos, bloqueados, fonte (interno/terceiros), revisão obrigatória.
   - Trilha de auditoria de instalação, ativação, desativação.
   - Integração com `AI Governance` se o plugin invocar IA.

#### Frontend
4. **Marketplace/Template Gallery UI** (busca por persona, módulo, popularidade, rating interno).
5. **Plugin Host** isolado com Module Federation; carregamento lazy e fallback gracioso se indisponível.
6. **Publicação curada** de templates a partir de dashboards existentes (botão "Publish as template").

#### Critérios de aceite
- Plugin terceiro carregado via Module Federation executa sem romper CSP nem violar policies.
- Galeria de templates com ≥20 templates iniciais por persona.
- Revogação remota de plugin funciona em runtime.

---

### WAVE V3.9 — Advanced NQL, Alerting from Widget & Mobile On-Call Companion
**Objetivo:** fechar o ciclo observação → ação e levar o valor do produto ao on-call em mobilidade.

#### Backend
1. **NQL avançado governado**
   - Extensões: joins entre seletores de módulos governados (Catalog × Changes × Incidents × FinOps), subqueries, UDFs registadas (whitelisted) pela plataforma.
   - Optimizador com plano auditável; rejeição de queries fora do envelope de custo/timeout.
2. **Alerting & Actions from widget**
   - A partir de qualquer `QueryWidget`, criar **monitor** (condição + janela + severidade + rota) ou **ação** (acionar runbook, abrir incidente, anotar change).
   - Integrado com `Operations` (Incidents) e `Knowledge` (Runbooks).
3. **Mobile API**
   - Endpoints otimizados para mobile on-call (home do engineer, incidentes ativos, approvals pendentes, dashboards-chave em modo read-only compacto).
   - Autenticação via OIDC + push tokens.

#### Frontend
4. **Mobile companion** como **PWA responsiva** no mesmo frontend (primeiro entregável) com possibilidade de app nativa fina no futuro partilhando a API de mobile.
   - Capacidades v3.9: ver alertas/incidentes, acknowledge/ack, ver dashboards partilhados em layout compacto, abrir runbooks, aprovar mudanças de baixo risco (quando permitido por política).
5. **Widget → Alert/Action** (UX inline no widget) com preview de condição e teste de acionamento.

#### Critérios de aceite
- PWA on-call instalável e funcional offline para leitura de dashboards cacheados.
- Monitores criados a partir de widgets disparam e aparecem em `Operations`.
- NQL avançado executa sob governance total; queries maliciosas/excessivas rejeitadas.

---

### WAVE V3.10 — Persona-first Experience Suites
**Objetivo:** eliminar a "home genérica" e entregar experiências primárias **segmentadas por persona e por escopo organizacional**, conforme Copilot Instructions §6 (Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor). Cada persona recebe uma *home* e *quick actions* pensadas para as suas decisões, sem clonar widgets entre personas apenas com filtros.

> **Racional:** hoje a UI tem 130+ páginas feature-based mas não há um ponto de entrada que reflita o **papel funcional + escopo + ownership + ambiente** da persona. A consequência é que todos veem o mesmo produto e precisam navegar até ao valor. V3.10 inverte isso: o valor vem até à persona.

#### Backend
1. **Persona Home Resolver**
   - Novo serviço `IPersonaHomeResolver` que, dado `(tenantId, environmentId, userId, persona, claims)`, devolve uma **composição de home** (top-level cards, quick actions, default scope) assente no aggregate `CustomDashboard` (system-owned, persona-tagged) + layout persistido.
   - 7 dashboards system-owned, read-only por default, **cloneáveis por utilizador** (passam a user-owned sem perder origem).
2. **Scope Resolver unificado**
   - `IUserOperationalScopeResolver` resolve o escopo padrão da persona: teams próprias, serviços owned, domínios responsáveis, ambientes acessíveis. Alimenta todos os widgets por default (sem o utilizador filtrar).
3. **Persona Quick Actions Registry**
   - `IPersonaQuickActionsProvider` por persona, com ações ligadas a permissões reais (`change:approve`, `runbook:execute`, `contract:publish`, `policy:override-breakglass`, etc.). Apenas ações permitidas aparecem.

#### Frontend — 7 suites entregáveis
4. **Engineer Cockpit** — my on-call, my services health, open drifts on my services, failing SLOs on my services, incidents assigned, pending approvals I can execute, last deploys I made, runbooks para os meus serviços.
5. **Tech Lead Team Command Center** — saúde das equipas que lidero (alinhado com `GetTeamOperationalHealthReport` Wave R.3, `GetTeamChangeVelocityReport` Wave M.2), ranking interno vs tenant, blockers de promotion, ownership gaps (Wave L.1), change window conformance (Wave S.1).
6. **Architect Landscape View** — topology cross-domain, dependency risk (Wave I.3), service coupling (Wave W.2), contract adoption (Wave S.2), breaking changes recentes (GraphQL/Protobuf/OpenAPI/SOAP/AsyncAPI), API growth (Wave V.1), maturity distribution (Wave H.3).
7. **Product Portfolio Home** — saúde de capacidades de negócio ligadas a serviços, entrega (velocity, release cadence), experiência (SLO compliance por produto), custo por produto (FinOps contextual Wave I.2/O.2), risco agregado.
8. **Executive Brief Center** — 1-page weekly/monthly brief auto-gerado com: DORA tier tenant-wide (Wave K.3/S.3), compliance scorecard (waves G/H/I/J/K/L), FinOps trend (Wave O.2), risco agregado (Wave F.2/N.2), top incidentes, top changes, evidence pack coverage (Wave N.3). Export PDF agendado via SMTP (herdar V3.6).
9. **Platform Admin Cockpit** — tenants, licensing & entitlements, integrações conectadas, event consumer status (Wave Z.1), AI token/budget utilização, audit volume, break glass ativos, access reviews pendentes, health de workers/hosts.
10. **Auditor Console** — evidence packs por standard (SOC2/ISO27001/PCI/HIPAA/GDPR/CMMC/FedRAMP — waves G/H/I/J/K/L), integridade assinada (SLSA), trilha de auditoria pesquisável, break glass history, access reviews, policy violations, exports assinados com hash.

#### Cross-cutting
11. **Persona Switcher Governado** — utilizadores com múltiplos papéis podem alternar entre personas com re-resolve de home + re-scope + audit log de troca. Nunca cria permissão, apenas muda lente.
12. **Persona Awareness em toda a UI** — `usePersonaContext()` disponível globalmente; widgets e páginas podem adaptar detalhe (densidade, granularidade métrica, linguagem) conforme persona, sem duplicação.

#### Critérios de aceite
- 7 suites publicadas, todas i18n em 4 locales, todas auditáveis, todas persona-aware.
- Persona switcher sem elevação de privilégio (permissões continuam a ser verificadas no backend).
- Default scope resolve em <200ms p95; re-scope em troca de environment/persona sem *flicker*.
- Cobertura por persona: cada home apresenta ≥6 cards relevantes sem necessidade de customização.
- Testes: ≥40 testes (resolver + scope + quick actions + renderização por persona).

---

### WAVE V3.11 — Source-of-Truth Consolidation Surfaces
**Objetivo:** entregar **as superfícies que materializam o NexTraceOne como Source of Truth operacional**. Grande parte do trabalho backend já está feito (waves A–R concluídas) ou planeada (S–Z); o que falta é **transformar esses relatórios/scores em superfícies de decisão frontend coesas** — *não* como widgets isolados, mas como *Centers* dedicados, navegáveis e deep-linkáveis.

> **Racional:** Copilot Instructions §§3.1, 5.1, 7.4, 9 e 26 exigem que Change, Contracts, Risk, FinOps e Compliance sejam **entidades de primeira classe** na UX — não só endpoints. V3.11 entrega cada uma como um **Center** dedicado.

#### 11 Centers entregáveis
1. **Compliance Scorecard Center** — consolida SOC2 (G.1), ISO27001 (G.2), PCI-DSS (H.2), HIPAA (I.1), GDPR (J.1), CMMC 2.0 (K.2), FedRAMP Moderate (L.2). Vista única por standard + consolidada; export PDF assinado por evidence pack; timeline de conformidade; gaps priorizados com ponteiros para mitigação.
2. **Risk Center UX** — materializa Wave F.2 + N.2: heatmap por serviço/equipa, drivers de risco (vuln 40% / change_failure 25% / blast_radius 20% / policy 15%), top high-risk services, trend 30d, ações sugeridas por tipo de sinal.
3. **FinOps Context Views** — materializa Waves I.2 + O.2 + V.3 (se concluído): custo por serviço/equipa/domínio/ambiente, anomalias (non-prod > prod, outliers P75), trend e delta período-a-período, top cost drivers, correlação custo × mudança × incidente. Nunca um "cloud cost dashboard" genérico — sempre contextual ao serviço.
4. **Change Confidence Hub** — uma página por release com: score de confiança, blast radius (Wave Q.3), evidence pack integrity (incluindo SLSA), contract compatibility (OpenAPI/AsyncAPI/GraphQL/Protobuf/SOAP), post-change verification, incident correlation (Wave R.1), rollback recommendation (Wave J.3), promotion gates (Wave O.3). **Decisão de promover/bloquear/rollback feita aqui, com um clique governado.**
5. **Release Calendar & Window Gate** — materializa Wave F.1 + S.1: calendário visual (day/week/month) com janelas Scheduled/Freeze/Hotfix/Maintenance, deploys planeados e executados, conformance por equipa, override com justificativa auditada, integração com approvals.
6. **Rollback Intelligence Cockpit** — materializa Wave J.3: lista priorizada por urgency tier (None/Suggest/Recommend/Critical), detalhe do scoring, preview do impacto, 1-click rollback guiado (respeitando promotion gates inversos), timeline pós-rollback.
7. **Blast Radius Explorer** — materializa Wave Q.3: visualizador interativo do grafo de impacto por release (consumidores diretos, indiretos, domínios, ambientes), buckets (Zero/Small/Medium/Large), comparação entre releases, what-if preview (ligação a Digital Twin — Wave D.1).
8. **Evidence Pack Viewer** — reader dedicado para evidence packs: SLSA provenance (SlsaProvenanceUri), artifact digest, SBOM, cadeia de assinaturas, coverage (Wave N.3), export imutável. Primeiro consumidor é Auditor Console (V3.10.10).
9. **Operational Readiness Board** — materializa Wave L.3: go/no-go visual por serviço para promoção a produção, 5 dimensões ponderadas (SLO 35% / Chaos 25% / Drift 20% / Profiling 10% / Baseline 10%), blockers explícitos com sugestão de ação, histórico.
10. **Drift Center** — materializa Wave M.3 + Q.1: drifts abertos por serviço/ambiente, distribuição de severidade, desvios vs baseline (Wave Q.1), ack/resolve com audit, correlação com última mudança.
11. **SLO Service Center + Chaos Engineering Lab + Post-Incident Learning Board** — três sub-surfaces combinando Waves J.2/N.1/S.3 (SLO compliance + ranking + MTTR trend DORA), Wave K.1/V.2 (chaos experiments + coverage gap), Wave T.1 (post-incident learning, recurring sem runbook). Cada uma deep-linkável e cross-filtrável com as outras via cross-filter da V3.3.

#### Backend
12. **Center Navigation Graph**
    - Cada Center regista no `ISourceOfTruthNavigationGraph` as entidades que "possui" e as rotas de drill-down; AI Assistant (V3.4) usa este grafo para resolver referências em respostas (ex.: "mostra-me o blast radius da última release do Payment" → link direto para Blast Radius Explorer com contexto).
13. **Cross-Center Deep Links** — URLs canônicos estáveis com preservação de tenant/env/persona/time-range/scope, partilháveis.

#### Critérios de aceite
- 11 Centers publicados, cada um com: EmptyState, LoadingState, ErrorState, i18n 4 locales, persona-aware, env-aware, tenant-aware.
- AI Assistant consegue resolver referências cross-Center via `ISourceOfTruthNavigationGraph`.
- Deep links sobrevivem refresh, re-login (quando autorizados) e partilha.
- Testes: ≥60 testes.

---

### WAVE V3.12 — Contract Studio Visual, AI Agents, IDE Console & Admin Consoles
**Objetivo:** completar superfícies enterprise estratégicas: edição visual governada de contratos (todos os tipos), gestão de agentes de IA próprios, console de integração IDE (VS/VS Code), Break Glass/JIT cockpit, Licensing & Entitlements Admin e bridge formal Knowledge Hub ↔ Notebooks ↔ Runbooks.

> **Racional:** Copilot Instructions §§7.3, 7.7, 11, 12, 16.4, 17 — todos estes domínios têm backend forte ou em evolução, mas UX ainda é fragmentado, principalmente para Platform Admin, Architect e Product.

#### 12.1 Contract Studio Visual Builders (todos os tipos)
1. **REST/OpenAPI Visual Designer** — editor split (visual ← → OpenAPI 3.1 YAML/JSON no Monaco), validação contínua, linting governado por policies (V3.6), diff semântico em tempo real vs versão publicada, exemplos sincronizados, publication workflow com approval.
2. **AsyncAPI 3.x Visual Designer** — canais, mensagens, bindings (Kafka/AMQP/SNS/SQS/WebSocket), schemas (Avro/JSON Schema/Protobuf), consumer expectations (Wave A.2 / Q.2), compatibility check.
3. **SOAP/WSDL Visual Designer** — operations, messages, types, bindings; import/export WSDL; generation de XSD canónico; mapping para consumidores internos.
4. **GraphQL Schema Builder** — SDL editor + visual (types/queries/mutations/subscriptions), breaking change detection (Wave G.3), deprecation workflow.
5. **Protobuf Builder** — .proto editor + visual (messages/services/rpcs), breaking change detection (Wave H.1), compatibility (wire + semantic).
6. **Unified Publication Center** — fluxo único independente do tipo: draft → review → approved → locked → published; evidence pack automático com hash + signer + policy check.

#### 12.2 AI Agent Marketplace (interno governado)
7. **Agent Registry UI** — lista de agentes system-provided (Contract Composer, Investigation Agent, Rollback Advisor, Test Scenario Generator, Incident Summarizer, Compliance Explainer) + agentes tenant-custom.
8. **Agent Builder** — wizard para criar agentes próprios: prompt template, knowledge sources permitidos (governados), tools permitidos, modelo permitido por política, budget de tokens, escopo (tenant/team/persona), audit.
9. **AI Token & Budget Governance Console** — consumo por agente/utilizador/persona/modelo, quotas, alertas de budget, bloqueio automático, trilha de auditoria (alinhado com §11.3/11.5).
10. **External AI Integration Console** — providers (OpenAI/Azure/Anthropic/local) sob política por tenant/ambiente; data egress redaction policies visíveis e auditáveis.

#### 12.3 IDE Extensions Console (§12)
11. **IDE Extensions Admin** — gestão das extensões NexTraceOne para Visual Studio e VS Code: versões, enrollment de developers, scopes permitidos (read-only, contract-edit, AI-assist), auditoria de uso no IDE, revogação.
12. **Developer Enrollment UX** — página self-service para o Engineer instalar a extensão, linkar conta OIDC, escolher persona no IDE, ver budget AI restante, abrir Source of Truth a partir do IDE.

#### 12.4 Break Glass & JIT Access Cockpit (§16.4)
13. **Break Glass Request UX** — solicitar acesso emergencial com justificativa, aprovador on-call, expiração compulsória, gravação de sessão, audit visível ao Auditor imediato.
14. **JIT Privileged Access UX** — pedir elevação temporária com approval chain definido por policy; acompanhamento em tempo real e revogação.
15. **Delegated Access Manager** — delegação a cobrir férias/sair do turno, com expiração e revogação.
16. **Access Review Workflows UI** — campanhas periódicas de revisão, confirmações, revogações em massa auditadas.

#### 12.5 Licensing & Entitlements Admin (§17)
17. **License Overview** — status online/offline, expiração, entitlements ativos (features habilitadas), machine fingerprint, heartbeat status, histórico de ativações.
18. **Entitlement Matrix** — visão por módulo/feature de quais planos habilitam o quê; preview de upgrade; trial activation UX.
19. **Offline License Manager** — geração de token offline para ambientes air-gapped (alinhado Wave C.4), import/export do bundle assinado.

#### 12.6 Knowledge Hub ↔ Notebooks ↔ Runbooks Bridge
20. **Unified Knowledge Graph UX** — busca e navegação única entre docs governadas, runbooks, notebooks, operational notes, changelog; tags de freshness (Wave B.4) visíveis; `ProposedRunbook` com aprovação inline.
21. **"Convert to Runbook"** a partir de Notebook (V3.4) com aprovação humana; "Open Runbook" a partir de Incident/Change; "Attach Knowledge" em qualquer widget.

#### 12.7 Dashboards-as-Code GitOps Console
22. **DaC Sync UI** — conectar repo Git (GitHub/GitLab/Azure DevOps), preview de diffs antes de apply, rollback a commit anterior, visualizar drift entre repo e plataforma, conflict resolution assistida.

#### Critérios de aceite
- Os 5 builders de contrato partilham a mesma shell, o mesmo publication workflow e a mesma política de evidência.
- Agent Marketplace respeita AI Governance total (§11) — nenhum agente utilizável fora de política.
- IDE console audita 100% das sessões; revogação propaga ≤5s.
- Break Glass/JIT nunca bypass de audit; gravação imutável por tenant policy.
- License Admin funciona em modo online e offline/air-gapped.
- DaC roundtrip (Git ↔ plataforma) preserva 100% e mostra diff humano-legível.
- Testes: ≥70 testes (builders + agents + IDE + access + licensing + DaC).

---

### 4.1 Backend
- **Módulo**: Custom Dashboards permanecem no `Governance` (aggregate root `CustomDashboard`); Notebooks podem ser novo submódulo `Governance.Notebooks` mantendo fronteira clara.
- **Persistência**: PostgreSQL JSONB para widgets, variables, sharing; revisões em tabela dedicada; annotations agregadas via query cross-módulo (view lógica).
- **Execução de queries**: `IQueryGovernanceService` centraliza tenant/env/persona/row-cap/timeout; NQL parser em `BuildingBlocks.Query`.
- **Eventos**: domínio emite eventos auditáveis; integração com `IAuditTrail`.
- **Configuração**: todos os thresholds/limites via `IConfigurationResolutionService` (NUNCA `appsettings` para parâmetros operacionais — conforme convenção já memorizada do repo).
- **Permissões** (exemplos):
  `governance:dashboard:read`, `:create`, `:update`, `:delete`, `:share`, `:public-link`, `:embed`, `:schedule-report`, `:apply-policy`.

### 4.2 Frontend
- **Feature folder** `features/governance/dashboards/` e `features/governance/notebooks/`.
- **Widget SDK interno** em `features/governance/dashboards/widgets/` com contrato tipado; migração incremental.
- **Estado**: TanStack Query para server state; Zustand apenas para session UI state (ex.: cross-filter transiente).
- **Performance**: lazy-load de widgets, virtualização quando há ≥30 widgets, `IntersectionObserver` para pausar updates de widgets fora da viewport.
- **Acessibilidade**: Radix UI primitives + focus traps + aria-live em live streaming.

### 4.3 Segurança
- Frontend nunca é autoridade para partilha/embed; tudo validado no backend.
- Embeds assinados com JWT de curta duração; CSP restritiva.
- Redação de dados sensíveis em snapshots públicos (email, IP, hostnames internos) configurável por tenant.
- Auditoria obrigatória para criação, partilha, embed, export, delete, revert.

---

## 5. Ordem de execução recomendada

1. **V3.1 Foundation** → desbloqueia tudo o resto (variáveis, histórico, partilha granular).
2. **V3.2 Query-driven** → maior ganho de valor percebido; multiplica o que é possível com widgets.
3. **V3.3 Live/Cross-filter/Drill-down** → transforma dashboards em superfícies ativas.
4. **V3.4 AI + Notebooks** → diferencial competitivo alto; exige fundação das waves anteriores.
5. **V3.5 Frontend Uplift** → paralelizável em parte com as waves anteriores; pode começar cedo em trilho dedicado.
6. **V3.6 Governance/Reports/Embed** → fecha o ciclo enterprise e destrava vendas para Exec/Auditor.
7. **V3.7 Real-time Collaboration** → requer fundação de partilha granular (V3.1) e auditoria.
8. **V3.8 Marketplace & Plugin SDK** → requer Widget SDK interno estável (V3.2) e governance (V3.6).
9. **V3.9 Advanced NQL + Alerting + Mobile On-Call** → consolida o ciclo observação → ação → mobilidade.
10. **V3.10 Persona-first Experience Suites** → pode começar em paralelo desde a V3.1 (depende apenas de `CustomDashboard` + persona claims), mas floresce após V3.2 (variables) e V3.3 (cross-filter/drill-down) estarem disponíveis.
11. **V3.11 Source-of-Truth Consolidation Surfaces** → deve começar só após V3.1/V3.2/V3.3 estabilizadas e das waves backend A–R estarem em produção; cada Center é entregável independentemente, respeitando ordem de negócio (Compliance → Risk → Change Confidence → Release Calendar → FinOps → restantes).
12. **V3.12 Contract Studio Visual + AI Agents + IDE + Admin Consoles** → trilho independente (não bloqueia V3.1–V3.11); os 7 subtrilhos (12.1–12.7) podem ser paralelizados entre equipas distintas.

**Trilhos paralelos** viáveis:
- Trilho A (Dashboards core): V3.1 → V3.2 → V3.3 → V3.4
- Trilho B (Frontend platform): V3.5 — pode correr em paralelo desde o início
- Trilho C (Governance/Enterprise): V3.6 — inicia após V3.2 estar estável
- Trilho D (Extensibilidade & colaboração): V3.7 → V3.8 — inicia após V3.1/V3.2/V3.6 estáveis
- Trilho E (Ciclo de ação + mobilidade): V3.9 — inicia após V3.2/V3.3 estáveis
- **Trilho F (Persona & Source of Truth surfaces): V3.10 → V3.11 — inicia após V3.1 e pode correr paralelo a V3.4/V3.5/V3.6**
- **Trilho G (Contract Studio + AI Agents + IDE + Admin): V3.12 — totalmente independente; pode arrancar cedo com subtrilhos priorizados por urgência de negócio (Contract Studio e Licensing primeiro)**

---

## 6. Indicadores de sucesso

| Dimensão | Métrica | Alvo v3 |
|---|---|---|
| Adoção | % de utilizadores ativos com ≥1 dashboard próprio | ≥60% |
| Time-to-first-dashboard | Mediana desde "criar" até "publicar" | <3 minutos (com AI Composer) |
| Reutilização | % de dashboards partilhados (scope ≥ Team) | ≥40% |
| Reutilização cross-team | % de instalações via Template Gallery | ≥25% |
| Colaboração | % de incidentes P1/P2 com War Room colaborativo | ≥70% |
| Mobilidade | MTTA (mean time to acknowledge) via PWA on-call | -30% vs baseline |
| Qualidade | Bugs de regressão em widgets por release | <3 |
| Performance | LCP p75 das páginas dashboard | <2.0s |
| Governança | % de dashboards com owner e variáveis obrigatórias | ≥95% |
| Segurança | Violações CodeQL críticas | 0 |
| Acessibilidade | Violações axe críticas | 0 |
| i18n | Cobertura de keys por locale | 100% (4 locales) |
| **Persona fit (V3.10)** | % de utilizadores que usam a sua *Persona Home* como ponto de entrada principal | **≥70%** |
| **Source of Truth adoption (V3.11)** | % de decisões de promoção/rollback tomadas via Change Confidence Hub vs fora da plataforma | **≥80%** |
| **Compliance time-to-report** | Tempo mediano para produzir report assinado de um standard (SOC2/ISO/PCI/HIPAA/GDPR/CMMC/FedRAMP) | **<10 minutos** |
| **Contract Studio adoption (V3.12.1)** | % de contratos novos criados via Visual Designer (vs import puro) | **≥50%** |
| **AI Agent governance (V3.12.2)** | % de invocações de IA por agentes registrados (vs prompts ad-hoc) | **≥85%** |
| **IDE usage governance (V3.12.3)** | % de developers com extensão IDE ativa e audit trail completo | **≥60%** |
| **Break Glass safety** | % de sessões Break Glass com gravação + audit + expiração cumprida | **100%** |

---

## 7. Riscos & mitigações

| Risco | Mitigação |
|---|---|
| NQL pode virar linguagem inchada | Começar subset minúsculo e seguro (V3.2); extensões (V3.9) passam por ADR; nunca expor SQL cru |
| Live streaming em multi-tenant pode causar pressão em DB | SSE com coalescing por widget; rate limit; cache de 2ª camada |
| AI Composer pode produzir dashboards ruidosos | Obrigar aprovação humana; rotular "AI-generated"; audit trail total |
| Public links são superfície de risco | Tenant policies bloqueiam por default; expiração máxima configurável; auditoria; redação obrigatória |
| Notebook mode pode confundir com dashboards | UX clara: dashboards = monitorização contínua; notebooks = investigação narrativa |
| Design tokens v2 pode quebrar páginas legadas | Migração incremental com dupla-fonte temporária; testes visuais no CI |
| Plugins de 3ºs podem comprometer segurança/UX | Assinatura de artefactos; sandbox + CSP; policies por tenant; revogação remota; audit trail |
| Colaboração em tempo real com conflitos de edição | CRDT para layout e texto; locking otimista para widgets pesados; histórico versionado (V3.1) |
| PWA on-call pode expor dados sensíveis em dispositivo perdido | Tokens curtos; wipe remoto por admin; modo kiosk/read-only por default; redação conforme policy |
| Scope creep em "Frontend Uplift" | Budget fixo de waves; backlog explícito rolado para a próxima major |
| **V3.10 Persona homes podem virar "mais um dashboard" genérico** | Governar system-owned com review de Product; obrigar `IUserOperationalScopeResolver` como fonte do escopo padrão (não flexibilizar); limitar widgets repetidos entre personas (regra lint); auditoria de satisfação por persona. |
| **V3.11 pode gerar 11 Centers dispersos sem narrativa** | Definir "Source of Truth Navigation Graph" antes do 1º Center; exigir deep-link canônico, EmptyState e 4 locales em cada um; cada Center tem Product owner nomeado. |
| **V3.12 Contract Studio pode vazar lógica de domínio para o frontend** | Parsing/validação permanecem no backend (`Catalog` + `BuildingBlocks`); UI faz apenas apresentação e orquestração de policies; regra de revisão PR obrigatória. |
| **Agentes de IA custom podem escapar à governança** | `IAgentRegistry` centralizado; nenhum agente executa fora do registry; budget+audit+model policy aplicados no gateway de IA, não no agente; bloqueio se política ausente. |
| **IDE Extensions expondo Source of Truth fora da plataforma** | Enrollment OIDC obrigatório; scopes granulares no servidor; session tokens curtos e revogáveis; audit de cada chamada IDE → plataforma; data egress redaction conforme tenant policy. |
| **Break Glass/JIT pode ser usado como atalho sem justificativa real** | Policy obrigatoriamente exige justificativa livre + approver on-call + gravação; review periódica das sessões; flag automático para padrões suspeitos. |
| **Licensing offline pode ser contornado em ambientes air-gapped** | Machine fingerprint + assinatura do bundle + assembly integrity; heartbeat local assinado; revogação via próximo bundle; alinhado Copilot Instructions §17. |
| **DaC Git↔plataforma pode gerar drift silencioso** | Sync job detecta drift e cria alerta em Risk Center; apply obrigatoriamente passa por aprovação quando tenant policy exigir. |

---

## 8. Fora de escopo da v3

Ficam conscientemente de fora **apenas** os itens que não representam evolução real para o produto ou que contradizem a visão oficial:

- **Server-driven UI** — contradiz a arquitetura feature-based, persona-aware e a autonomia do frontend já estabelecida. Não adotar.
- **Clone literal de produtos concorrentes** (Dynatrace/Grafana/Datadog clones visuais ou funcionais) — o NexTraceOne mantém identidade própria (ver Copilot Instructions §2, §27).
- **Marketplace público aberto à internet (monetização/third-party público não revisto)** — a v3 entrega galeria/marketplace **interno e governado** (V3.8). Marketplace público não governado fica explicitamente fora por risco de segurança e curadoria até um programa formal de ISVs existir.
- **Gamer/sci-fi/cyberpunk visual styling** — conforme Copilot Instructions §18.2.

> **Nota:** todas as capacidades que anteriormente podiam parecer "adiáveis" — micro-frontends, marketplace/template gallery, plugin SDK externo, colaboração em tempo real, mobile companion (PWA), NQL avançado, alerting from widget, **persona homes dedicadas, Compliance/Risk/FinOps/Change-Confidence Centers, Contract Studio visual, AI Agent Marketplace interno, IDE Extensions Console, Break Glass/JIT cockpit, Licensing & Entitlements Admin, Dashboards-as-Code GitOps** — foram **absorvidas pelas waves V3.7, V3.8, V3.9, V3.10, V3.11 e V3.12** por representarem evolução real e estratégica alinhada à visão do produto.

---

## 9. Referências cruzadas no repositório

- `src/modules/governance/NexTraceOne.Governance.Domain/Entities/CustomDashboard.cs` — aggregate atual (ponto de partida).
- `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Configurations/CustomDashboardConfiguration.cs` — persistência JSONB.
- `src/modules/governance/NexTraceOne.Governance.API/Endpoints/DashboardsAndDebtEndpointModule.cs` — endpoints atuais.
- `src/frontend/src/features/governance/pages/CustomDashboardsPage.tsx` — lista.
- `src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx` — builder slot-based.
- `src/frontend/src/features/governance/pages/DashboardViewPage.tsx` — viewer.
- `src/frontend/src/features/governance/widgets/WidgetRegistry.ts` — registry atual (19 widgets).
- `docs/FRONTEND-ARCHITECTURE.md`, `docs/DESIGN-SYSTEM.md`, `docs/UX-PRINCIPLES.md`, `docs/PERSONA-UX-MAPPING.md`.
- `docs/AI-GOVERNANCE.md`, `docs/AI-ARCHITECTURE.md` — para Wave V3.4 e V3.12.2 (AI Agent Marketplace, Token/Budget Governance).
- `docs/SECURITY-ARCHITECTURE.md` — para Wave V3.6 (public/embed) e V3.12.4 (Break Glass/JIT).
- `docs/FUTURE-ROADMAP.md` — este plano estende e detalha a vertente de "Dashboards & Frontend" (ver Wave AA — V3 Frontend Evolution).
- `docs/LICENSING-ARCHITECTURE.md` (se existente) / Copilot Instructions §17 — para Wave V3.12.5.
- `docs/INTEGRATIONS-ARCHITECTURE.md`, `docs/IDE-EXTENSIONS.md` (a criar se não existente) — para Wave V3.12.3 (IDE Extensions Console).
- `docs/SOURCE-OF-TRUTH-STRATEGY.md` — para Wave V3.11 (Source-of-Truth Navigation Graph).
- Waves backend A–R concluídas (ver `IMPLEMENTATION-STATUS.md`) — fornecem dados para Wave V3.11 Centers.
- Waves backend S–Z planeadas (`FUTURE-ROADMAP.md`) — completam dados para V3.10/V3.11/V3.12.

---

## 10. Próximos passos imediatos (após fecho da v2)

1. Validar este plano com stakeholders (Product, Platform Admin, Architect).
2. Transformar **Wave V3.1** em backlog executável (ADRs para `DashboardRevision`, `SharingPolicy`, `DashboardVariable`).
3. Iniciar em paralelo o trilho **V3.5.1** (Design Tokens v2 + Storybook) — de baixo risco e alto retorno.
4. Criar spike técnico para NQL (V3.2) com protótipo fechado em 1 módulo (Catalog) antes de generalizar.
5. Atualizar `FUTURE-ROADMAP.md` com ponteiro para este documento. ✅ (Wave AA adicionada em Abril 2026.)
6. **Priorizar desenho das 7 Persona Homes (V3.10)** com Product Design + representantes de cada persona — entregar duas primeiras homes (Engineer Cockpit + Executive Brief Center) ainda dentro do ciclo V3.1/V3.5 como *proof of value*.
7. **Escolher ordem dos 11 Centers da V3.11** com base em valor de negócio: sugestão inicial — Compliance Scorecard Center → Change Confidence Hub → Risk Center → Release Calendar → FinOps Views → restantes.
8. **Criar ADR "Source-of-Truth Navigation Graph"** (V3.11.12) antes de qualquer Center entrar em desenvolvimento — é fundação para deep-links canônicos e para a integração com AI Assistant.
9. **Separar V3.12 em sub-épicos independentes** (12.1–12.7) com Product owner por sub-épico; Contract Studio Visual (12.1) e Licensing Admin (12.5) são candidatos a arrancar cedo por destrave comercial.

---

**Lembrete final de alinhamento com a visão do produto:**
Dashboards e notebooks no NexTraceOne não competem com ferramentas genéricas de observabilidade. São **superfícies de decisão enterprise** persistidas, versionadas, partilháveis, auditadas e profundamente integradas com serviços, contratos, mudanças, incidentes e conhecimento — o que consolida o NexTraceOne como **Source of Truth** e como plataforma de **confiança em mudanças de produção**.
