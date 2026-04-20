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
- Pesquisa de mercado (Dynatrace Dashboards/Notebooks & Davis, Grafana 11, Datadog, Observe, Honeycomb Boards, New Relic Workloads) sobre o que hoje é considerado estado-da-arte em dashboards enterprise: **query-driven widgets, live streaming, tokens/variables, notebooks, versionamento, partilha granular, deep-link contextual, AI-assisted creation, embedding, público/privado/equipa, delegação**.
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
| **Dashboard as Code** (YAML/JSON exportável, GitOps) | Grafana, Terraform providers | Média — já temos export JSON; evoluir para DaC |
| **Alerting from widget** (criar alerta a partir duma query num widget) | Grafana alerts, Datadog monitors | Alta |
| **Responsive layouts e mobile companion** | Datadog mobile | Média |
| **Whitelabel / embed iframe assinado** | Grafana, Datadog | Média — para parceiros e intranet corporativa |
| **Scheduled reports / snapshots** (PDF, PNG, email) | Dynatrace Reports, Grafana Reporting | Alta — Exec/Auditor personas |
| **Dark/Light density modes + high-contrast** | Observe, Dynatrace | Média — parte da experiência enterprise |

### 1.2 Tendências em Frontend enterprise

| Tendência | Relevância para NexTraceOne |
|---|---|
| **Command Palette global** (Cmd+K) com ações, navegação, search semântico | Alta — alinha com pilar Source of Truth |
| **Micro-frontends / Module Federation** para plugins de terceiros | Baixa no v3 — adiar, avaliar em v4 |
| **Streaming SSR com React 19** (use hook, Suspense data) | Média — ganhos de performance em páginas pesadas |
| **View Transitions API + route transitions** | Alta — sensação premium enterprise |
| **Server-driven UI** para páginas raramente acedidas | Baixa — não adotar |
| **Real-time collaboration** (cursors partilhados em dashboards/notebooks) | Média — diferenciador para War Room |
| **Accessibility WCAG 2.2 AA completo** | Alta — enterprise compliance |
| **Design tokens unificados** (JSON → Tailwind + CSS vars) | Alta — consolidar o design system |
| **Storybook / UI contract testing** para componentes reutilizáveis | Alta — qualidade e ADR-driven UI |
| **IDE-like dev experience** (split panes, tabs, session restore) | Alta — War Room, Contract Studio |
| **Offline-first / optimistic mutations** em fluxos críticos | Média — ativar onde faz sentido |
| **Performance budgets automatizados** (bundle-size, LCP, INP) | Alta — requisito enterprise |

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

O plano organiza-se em **6 waves** entregáveis de forma incremental. Cada wave é auto-contido e entrega valor observável.

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

## 4. Arquitetura alvo (resumo das decisões)

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

**Trilhos paralelos** viáveis:
- Trilho A: V3.1 → V3.2 → V3.3 → V3.4 (dashboards+notebooks)
- Trilho B: V3.5 (frontend platform uplift) — pode correr em paralelo desde o início
- Trilho C: V3.6 — inicia após V3.2 estar estável

---

## 6. Indicadores de sucesso

| Dimensão | Métrica | Alvo v3 |
|---|---|---|
| Adoção | % de utilizadores ativos com ≥1 dashboard próprio | ≥60% |
| Time-to-first-dashboard | Mediana desde "criar" até "publicar" | <3 minutos (com AI Composer) |
| Reutilização | % de dashboards partilhados (scope ≥ Team) | ≥40% |
| Qualidade | Bugs de regressão em widgets por release | <3 |
| Performance | LCP p75 das páginas dashboard | <2.0s |
| Governança | % de dashboards com owner e variáveis obrigatórias | ≥95% |
| Segurança | Violações CodeQL críticas | 0 |
| Acessibilidade | Violações axe críticas | 0 |
| i18n | Cobertura de keys por locale | 100% (4 locales) |

---

## 7. Riscos & mitigações

| Risco | Mitigação |
|---|---|
| NQL pode virar linguagem inchada | Começar subset minúsculo e seguro; avaliar cada extensão; nunca expor SQL cru |
| Live streaming em multi-tenant pode causar pressão em DB | SSE com coalescing por widget; rate limit; cache de 2ª camada |
| AI Composer pode produzir dashboards ruidosos | Obrigar aprovação humana; rotular "AI-generated"; audit trail total |
| Public links são superfície de risco | Tenant policies bloqueiam por default; expiração máxima configurável; auditoria; redação obrigatória |
| Notebook mode pode confundir com dashboards | UX clara: dashboards = monitorização contínua; notebooks = investigação narrativa |
| Design tokens v2 pode quebrar páginas legadas | Migração incremental com dupla-fonte temporária; testes visuais no CI |
| Scope creep em "Frontend Uplift" | Budget fixo de waves; backlog explícito para v4 |

---

## 8. Fora de escopo da v3 (ficam para v4+)

- Micro-frontends / Module Federation para plugins de terceiros.
- Marketplace público de dashboards/widgets.
- Edição colaborativa em tempo real com cursors partilhados em dashboards (avaliar só para notebooks).
- SDK externo de widgets para parceiros.
- Mobile-native companion app.
- Server-driven UI.
- NQL avançado com joins livres e funções definidas pelo utilizador.

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
- `docs/AI-GOVERNANCE.md`, `docs/AI-ARCHITECTURE.md` — para Wave V3.4.
- `docs/SECURITY-ARCHITECTURE.md` — para Wave V3.6 (public/embed).
- `docs/FUTURE-ROADMAP.md` — este plano estende e detalha a vertente de "Dashboards & Frontend".

---

## 10. Próximos passos imediatos (após fecho da v2)

1. Validar este plano com stakeholders (Product, Platform Admin, Architect).
2. Transformar **Wave V3.1** em backlog executável (ADRs para `DashboardRevision`, `SharingPolicy`, `DashboardVariable`).
3. Iniciar em paralelo o trilho **V3.5.1** (Design Tokens v2 + Storybook) — de baixo risco e alto retorno.
4. Criar spike técnico para NQL (V3.2) com protótipo fechado em 1 módulo (Catalog) antes de generalizar.
5. Atualizar `FUTURE-ROADMAP.md` com ponteiro para este documento.

---

**Lembrete final de alinhamento com a visão do produto:**
Dashboards e notebooks no NexTraceOne não competem com ferramentas genéricas de observabilidade. São **superfícies de decisão enterprise** persistidas, versionadas, partilháveis, auditadas e profundamente integradas com serviços, contratos, mudanças, incidentes e conhecimento — o que consolida o NexTraceOne como **Source of Truth** e como plataforma de **confiança em mudanças de produção**.
