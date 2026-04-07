# NexTraceOne — Plano de Ação: Evolução de Customização da Plataforma

> **Data:** Abril 2026
> **Versão:** 1.0
> **Pilar:** Platform Customization & User Experience
> **Princípio:** Nenhuma customização pode alterar a identidade visual da marca (logo, cores, favicon, layout, footer)

---

## Sumário Executivo

Este plano de ação detalha a implementação de funcionalidades de customização que o utilizador pode realizar na plataforma NexTraceOne **sem alterar a identidade visual da marca**. As sugestões foram pesquisadas com base em práticas de plataformas enterprise de referência (Dynatrace, Datadog, Grafana, ServiceNow, PagerDuty, Backstage, Azure DevOps, Jira).

### Restrição Absoluta — Identidade Visual Imutável

| Elemento | Estado | Justificação |
|----------|--------|-------------|
| Logo NexTraceOne | ❌ FIXO | Identidade corporativa |
| Paleta de cores (brand-blue, brand-cyan, brand-mint) | ❌ FIXO | Design system |
| Favicon | ❌ FIXO | Reconhecimento de marca |
| Layout estrutural (sidebar, topbar, footer) | ❌ FIXO | Consistência UX |
| Footer "Powered by NexTraceOne" | ❌ FIXO | Marca registada |
| Tipografia (Inter, JetBrains Mono) | ❌ FIXO | Design system |
| Gradientes e estilos visuais | ❌ FIXO | Design system |

---

## Estado Atual — O Que JÁ Existe

### Parametrização existente (452 seeds, 10 de platform customization)
- `platform.sidebar.user_customization.enabled` — habilitar sidebar customizável ✅
- `platform.sidebar.pinned_items.max` — máximo de itens fixados ✅
- `platform.home.user_customization.enabled` — home customizável ✅
- `platform.home.default_layout` — layout do dashboard (two-column, etc.) ✅
- `platform.home.available_widgets` — catálogo de 12 widgets ✅
- `platform.home.max_widgets` — limite de widgets ✅
- `platform.quick_actions.user_customization.enabled` — quick actions personalizáveis ✅
- `platform.custom_dashboards.enabled` — dashboards customizados ✅
- `platform.custom_dashboards.sharing.enabled` — compartilhamento ✅
- `platform.custom_dashboards.max_per_user` — máximo por utilizador ✅

### Infraestrutura existente
- `UserPreferencesPage.tsx` — página de preferências do utilizador ✅
- `UserPreferencesEndpointModule.cs` — API GET/PUT de preferências ✅
- `GetUserPreferences` / `SetUserPreference` — handlers CQRS ✅
- `SavedGraphView` + `CreateSavedView` / `ListSavedViews` — saved views no catálogo ✅
- `AppSidebar.tsx` — sidebar com pinning funcional ✅

---

## Plano de Ação — 8 Fases

---

### FASE 1 — Personalização de Experiência por Utilizador
**Prioridade:** 🟢 Alta | **Complexidade:** Baixa-Média | **Impacto:** Muito Alto

> Foco: Dar a cada utilizador controlo sobre como vê e navega a plataforma, sem tocar na marca.

#### 1.1 Saved Views / Filtros Salvos em Todas as Listagens
- [x] **Backend:** Generalizar `SavedGraphView` num domínio transversal `UserSavedView` no módulo Configuration
  - [x] Entidade `UserSavedView` (Id, UserId, TenantId, Context, Name, FiltersJson, IsShared, SortOrder, CreatedAt)
  - [x] Repository `IUserSavedViewRepository`
  - [x] Commands: `CreateSavedView`, `UpdateSavedView`, `DeleteSavedView`, `ShareSavedView`
  - [x] Query: `ListSavedViews(context)` — contexto = página/módulo
- [x] **Frontend:** Componente reutilizável `<SavedViewSelector />`
  - [x] Botão "Salvar vista atual" em todas as listagens (serviços, contratos, changes, incidentes, knowledge)
  - [x] Dropdown com vistas salvas + "Default" + "Compartilhadas"
  - [x] Persistir: filtros, ordenação, colunas visíveis, paginação
- [x] **i18n:** Chaves em 4 locales (en, pt-BR, pt-PT, es)
- [x] **Testes:** 6+ testes unitários para CQRS handlers

#### 1.2 Bookmarks / Favoritos
- [x] **Backend:** Entidade `UserBookmark` (Id, UserId, TenantId, EntityType, EntityId, DisplayName, CreatedAt)
  - [x] EntityType enum: Service, Contract, Change, Incident, Runbook, Dashboard, KnowledgeArticle
  - [x] Commands: `AddBookmark`, `RemoveBookmark`
  - [x] Query: `ListBookmarks(entityType?)`
  - [x] Endpoint: `/api/v1/bookmarks`
- [x] **Frontend:** Componente `<BookmarkButton />` — ícone ★ em headers de detalhe
  - [x] Sidebar section "Favoritos" (colapsável, abaixo dos módulos)
  - [x] Quick access via Command Palette (Cmd+K) com favoritos priorizados
- [x] **i18n:** 4 locales
- [x] **Testes:** 4+ testes

#### 1.3 Default Scope / Contexto Padrão
- [x] **Backend:** Preferências: `default.service`, `default.team`, `default.environment`
  - [x] Extensão do `SetUserPreference` para aceitar estes keys
  - [x] API retorna defaults na resposta de `GetUserPreferences`
- [x] **Frontend:** Na UserPreferencesPage, selects de "Serviço padrão", "Equipa padrão", "Ambiente padrão"
  - [x] Ao abrir listagens, pré-filtrar pelo scope padrão do utilizador
  - [x] Badge "Filtered by: My Team" visível com botão "Clear"
- [x] **i18n:** 4 locales
- [x] **Testes:** 3+ testes

#### 1.4 Timezone e Formato de Data/Hora
- [x] **Backend:** Preferências: `user.timezone`, `user.date_format`, `user.time_format`
  - [x] Seeds de configuração: timezone (default: "UTC"), date_format (default: "yyyy-MM-dd"), time_format (default: "HH:mm:ss")
- [x] **Frontend:** Componente `<FormattedTimestamp />` que respeita preferência do utilizador
  - [x] Dropdown de timezone com pesquisa (IANA timezone database)
  - [x] Select de formato (ISO, US mm/dd/yyyy, EU dd/mm/yyyy, BR dd/MM/yyyy)
  - [x] Todas as timestamps da plataforma usam `<FormattedTimestamp />`
- [x] **i18n:** 4 locales
- [x] **Testes:** 3+ testes

#### 1.5 Colunas Visíveis em Tabelas
- [x] **Backend:** Preferência `table.columns.{context}` — JSON array de colunas visíveis
- [x] **Frontend:** Componente `<ColumnSelector />` (ícone de colunas no header de cada tabela)
  - [x] Drag-and-drop para reordenar colunas
  - [x] Toggle para mostrar/ocultar cada coluna
  - [x] Botão "Reset to Default"
  - [x] Persiste via UserPreference API
- [x] **Contextos:** service-list, contract-list, change-list, incident-list, audit-log, knowledge-list
- [x] **i18n:** 4 locales
- [x] **Testes:** 2+ testes

#### 1.6 Itens por Página (Paginação Customizável)
- [x] **Backend:** Preferência `user.items_per_page` (default: 25, opções: 10, 25, 50, 100)
- [x] **Frontend:** Select no footer de todas as tabelas paginadas
  - [x] Persiste automaticamente via UserPreference API
- [x] **Testes:** 1+ teste

---

### FASE 2 — Dashboards & Visualização Avançada
**Prioridade:** 🟢 Alta | **Complexidade:** Média-Alta | **Impacto:** Muito Alto

#### 2.1 Dashboard Templates por Persona
- [x] **Backend:** Seed de templates pré-definidos no `ConfigurationDefinitionSeeder`
  - [x] `platform.home.template.engineer` — widgets: team-services, recent-changes, active-incidents, dora-metrics
  - [x] `platform.home.template.tech_lead` — widgets: team-services, change-risk, pending-approvals, slo-status, dora-metrics
  - [x] `platform.home.template.architect` — widgets: contract-health, dependency-map, compliance-status, reliability-trend
  - [x] `platform.home.template.executive` — widgets: finops-summary, compliance-status, incident-overview, dora-metrics
  - [x] `platform.home.template.platform_admin` — widgets: ai-usage, security-findings, audit-activity, system-health
- [x] **Frontend:** Modal "Choose Dashboard Template" no onboarding e em Settings
  - [x] Preview visual de cada template (miniatura)
  - [x] "Start from template" → pode modificar depois
- [x] **i18n:** 4 locales
- [x] **Testes:** 3+ testes

#### 2.2 Custom Charts (Query Builder Visual)
- [x] **Backend:** Feature `CreateCustomChart` no módulo OperationalIntelligence
  - [x] Entidade `CustomChart` (Id, UserId, TenantId, Name, ChartType, MetricQuery, TimeRange, Filters, CreatedAt)
  - [x] ChartType enum: Line, Bar, Area, Pie, Gauge, Table, Sparkline
  - [x] MetricQuery: JSON com source, metric, aggregation, groupBy, filters
  - [x] API: `/api/v1/custom-charts` (CRUD)
  - [x] API: `/api/v1/custom-charts/{id}/data` (execução da query)
- [x] **Frontend:** Página `CustomChartBuilderPage`
  - [x] Step 1: Escolher métrica (changes, incidents, contracts, services, SLOs, FinOps)
  - [x] Step 2: Escolher tipo de gráfico
  - [x] Step 3: Definir filtros e agrupamento
  - [x] Step 4: Preview do resultado
  - [x] Step 5: Salvar e adicionar ao dashboard
- [x] **i18n:** 4 locales
- [x] **Testes:** 5+ testes

#### 2.3 Widget de Notas Pessoais (Markdown)
- [x] **Backend:** Preferência `home.widget.notes.content` — texto markdown do utilizador
- [x] **Frontend:** Widget "My Notes" no dashboard
  - [x] Editor markdown inline (toggle edit/view)
  - [x] Auto-save com debounce (2s)
  - [x] Limite de 5000 caracteres
- [x] **i18n:** 4 locales
- [x] **Testes:** 2+ testes

#### 2.4 Dashboard Cloning
- [x] **Backend:** Command `CloneDashboard(sourceId)` — copia widgets e layout
- [x] **Frontend:** Botão "Clone this dashboard" em dashboards compartilhados
  - [x] Modal com nome do novo dashboard
  - [x] Cópia independente (alterações não afetam o original)
- [x] **Testes:** 2+ testes

#### 2.5 Drill-Down Configurável por Widget
- [x] **Frontend:** Cada widget tem config de "Click action":
  - [x] Navegar para listagem filtrada
  - [x] Abrir painel de detalhe
  - [x] Abrir em nova tab
  - [x] Config persistida como parte do widget JSON
- [x] **Testes:** 2+ testes

---

### FASE 3 — Alertas, Notificações & Watch Lists
**Prioridade:** 🟡 Média-Alta | **Complexidade:** Média | **Impacto:** Alto

#### 3.1 Watch Lists (Seguir Entidades)
- [ ] **Backend:** Entidade `UserWatch` (Id, UserId, TenantId, EntityType, EntityId, NotifyOnChange, CreatedAt)
  - [ ] Commands: `WatchEntity`, `UnwatchEntity`
  - [ ] Query: `ListWatches(entityType?)`
  - [ ] Event handler: quando entidade muda → notificar watchers
  - [ ] Endpoint: `/api/v1/watches`
- [ ] **Frontend:** Botão "Watch" (ícone 👁) em detalhe de serviço, contrato, change, incidente
  - [ ] Toggle com opções: "All changes", "Critical only", "None"
  - [ ] Badge no sidebar com contagem de watches activos
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 4+ testes

#### 3.2 Quiet Hours / Do Not Disturb
- [ ] **Backend:** Preferências: `notifications.quiet_hours.enabled`, `notifications.quiet_hours.start`, `notifications.quiet_hours.end`, `notifications.quiet_hours.timezone`
  - [ ] Seeds de configuração no seeder
  - [ ] Avaliação no serviço de envio de notificações
- [ ] **Frontend:** Secção "Quiet Hours" na UserPreferencesPage
  - [ ] Toggle + time pickers para início e fim
  - [ ] Select de timezone
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 3+ testes

#### 3.3 Custom Alert Rules Pessoais
- [ ] **Backend:** Entidade `UserAlertRule` no módulo Configuration
  - [ ] (Id, UserId, TenantId, Name, Condition, Channel, Enabled, CreatedAt)
  - [ ] Conditions: {"entity":"service", "field":"risk_level", "operator":">=", "value":"high"}
  - [ ] Channels: in-app, email, webhook (conforme parametrização do tenant)
  - [ ] Commands: `CreateAlertRule`, `UpdateAlertRule`, `DeleteAlertRule`, `ToggleAlertRule`
  - [ ] Query: `ListAlertRules`
  - [ ] Endpoint: `/api/v1/alert-rules`
- [ ] **Frontend:** Página `PersonalAlertRulesPage`
  - [ ] Lista de regras com toggle enabled/disabled
  - [ ] Builder visual de condição (entity → field → operator → value)
  - [ ] Channel selector
  - [ ] Preview de "quantas vezes teria disparado nos últimos 7 dias"
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 5+ testes

#### 3.4 Digest Personalizado
- [ ] **Backend:** Preferências: `notifications.digest.frequency` (daily, weekly, none), `notifications.digest.sections` (JSON array de secções)
  - [ ] Secções disponíveis: changes, incidents, contracts, compliance, finops, ai-usage
- [ ] **Frontend:** Config na UserPreferencesPage
  - [ ] Select de frequência
  - [ ] Drag-and-drop de secções do digest
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 2+ testes

---

### FASE 4 — Custom Fields, Tags & Metadata
**Prioridade:** 🟢 Alta | **Complexidade:** Alta | **Impacto:** Muito Alto

#### 4.1 Custom Tags em Entidades
- [ ] **Backend:** Entidade `EntityTag` no Building Blocks
  - [ ] (Id, TenantId, EntityType, EntityId, Key, Value, CreatedBy, CreatedAt)
  - [ ] Keys definidas pelo admin do tenant (ex: "cost-center", "business-unit", "squad")
  - [ ] Endpoint transversal: `/api/v1/tags` (CRUD)
  - [ ] Filtros por tag em todas as listagens
  - [ ] Máximo de tags por entidade configurável via seed
- [ ] **Frontend:** Componente `<TagEditor />` reutilizável
  - [ ] Input com autocomplete de keys existentes
  - [ ] Suporte a key:value (ex: "team:payments")
  - [ ] Chips coloridos por key
  - [ ] Presente em: serviço, contrato, change, incidente, runbook, knowledge article
- [ ] **Admin:** Página para admin definir keys permitidas e validações (obrigatório, opcional, formato)
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 6+ testes

#### 4.2 Custom Metadata Fields no Service Catalog
- [ ] **Backend:** Entidade `ServiceCustomField` no módulo Catalog
  - [ ] (Id, TenantId, FieldName, FieldType, IsRequired, DefaultValue, SortOrder, CreatedAt)
  - [ ] FieldType enum: Text, Number, Date, Select, MultiSelect, Url, Email
  - [ ] Entidade `ServiceCustomFieldValue` (Id, ServiceId, FieldId, Value)
  - [ ] Admin endpoint: `/api/v1/catalog/custom-fields` (CRUD)
  - [ ] Incluir custom fields em responses de serviço
- [ ] **Frontend:** Secção "Custom Fields" no ServiceDetailPage
  - [ ] Admin page para definir campos
  - [ ] Campos exibidos dinamicamente no formulário de serviço
  - [ ] Filtro por custom fields na listagem de serviços
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 5+ testes

#### 4.3 Custom Classification Taxonomies
- [ ] **Backend:** Entidade `TaxonomyCategory` e `TaxonomyValue`
  - [ ] Admin define categorias (ex: "Business Domain", "Data Classification", "Tier")
  - [ ] Valores dentro de cada categoria (ex: Tier → "Tier 1", "Tier 2", "Tier 3")
  - [ ] Associação a serviços via `ServiceTaxonomyValue`
- [ ] **Frontend:** Admin page para gerir taxonomias
  - [ ] Filtros por taxonomia na listagem de serviços
  - [ ] Agrupamento por taxonomia na topologia
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 4+ testes

---

### FASE 5 — Workflows & Automação Customizáveis
**Prioridade:** 🟡 Média | **Complexidade:** Alta | **Impacto:** Alto

#### 5.1 Custom Checklists por Tipo de Mudança
- [ ] **Backend:** Extensão do modelo de Change para incluir checklists customizáveis
  - [ ] Entidade `ChangeChecklist` (Id, TenantId, ChangeTypeId, Name, Items[], IsRequired)
  - [ ] Items: array de strings com checkbox
  - [ ] Admin define checklists por tipo de mudança + criticidade + ambiente
  - [ ] Validação: change não pode avançar sem checklist completa (quando required)
- [ ] **Frontend:** Componente `<CustomChecklist />` no ChangeDetailPage
  - [ ] Admin page para definir checklists
  - [ ] Toggle de obrigatoriedade por gate
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 4+ testes

#### 5.2 Automation Rules (If-Then)
- [ ] **Backend:** Entidade `AutomationRule` no módulo Governance
  - [ ] (Id, TenantId, Name, Trigger, Conditions[], Actions[], Enabled, CreatedBy, CreatedAt)
  - [ ] Triggers: on_change_created, on_incident_opened, on_contract_published, on_approval_expired
  - [ ] Conditions: field matches (severity, service, environment, tag)
  - [ ] Actions: send_notification, assign_reviewer, add_tag, require_evidence, create_incident
  - [ ] Endpoint: `/api/v1/automation-rules` (CRUD)
  - [ ] Avaliação em event handlers existentes
- [ ] **Frontend:** Página `AutomationRulesPage`
  - [ ] Builder visual: "When [trigger] and [conditions] then [action]"
  - [ ] Lista de regras com toggle
  - [ ] Execution log (últimas 100 execuções)
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 6+ testes

#### 5.3 Custom Contract Templates por Organização
- [ ] **Backend:** Entidade `ContractTemplate` (Id, TenantId, Name, ContractType, TemplateJson, CreatedBy, CreatedAt)
  - [ ] Admin define templates de contrato (REST, SOAP, Event) com estrutura pré-preenchida
  - [ ] Endpoint: `/api/v1/contract-templates` (CRUD)
- [ ] **Frontend:** Na criação de contrato, step "Choose template"
  - [ ] Lista de templates do tenant + templates padrão NexTraceOne
  - [ ] Preview do template antes de aplicar
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 3+ testes

---

### FASE 6 — Relatórios & Exports Personalizados
**Prioridade:** 🟡 Média | **Complexidade:** Média | **Impacto:** Alto

#### 6.1 Scheduled Reports
- [ ] **Backend:** Entidade `ScheduledReport` no módulo Governance
  - [ ] (Id, TenantId, UserId, Name, ReportType, Filters, Schedule, Recipients[], Format, Enabled, LastSentAt)
  - [ ] Schedule: cron expression ou presets (daily, weekly, monthly)
  - [ ] Formats: PDF, CSV, JSON
  - [ ] Job Quartz.NET para execução programada
  - [ ] Endpoint: `/api/v1/scheduled-reports` (CRUD)
- [ ] **Frontend:** Página `ScheduledReportsPage`
  - [ ] Criar/editar schedule
  - [ ] Preview do relatório
  - [ ] Histórico de envios
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 4+ testes

#### 6.2 Export Configurável
- [ ] **Backend:** Endpoint genérico `/api/v1/export` com parâmetros de formato e colunas
  - [ ] Formatos: CSV, JSON, PDF
  - [ ] Colunas: selecionáveis pelo utilizador
  - [ ] Filtros: mesmos da listagem atual
- [ ] **Frontend:** Botão "Export" em todas as listagens
  - [ ] Modal com: formato, colunas, filtros activos
  - [ ] Download directo ou envio por email (para exports grandes)
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 3+ testes

#### 6.3 Saved Report Templates
- [ ] **Backend:** Preferência `reports.saved_templates` — JSON array de templates de relatório
- [ ] **Frontend:** "Save as template" no export modal
  - [ ] Lista de templates salvos no dropdown de export
- [ ] **Testes:** 2+ testes

---

### FASE 7 — AI Customizável (Governada)
**Prioridade:** 🟡 Média | **Complexidade:** Média | **Impacto:** Alto

#### 7.1 Custom AI Prompts Salvos
- [ ] **Backend:** Entidade `SavedPrompt` no módulo AIKnowledge
  - [ ] (Id, UserId, TenantId, Name, PromptText, ContextType, Tags[], IsShared, CreatedAt)
  - [ ] Endpoint: `/api/v1/ai/saved-prompts` (CRUD)
- [ ] **Frontend:** No AI Hub, secção "My Prompts"
  - [ ] Botão "Save this prompt" em cada resposta do assistente
  - [ ] Lista de prompts salvos com quick-apply
  - [ ] Compartilhamento dentro da equipa
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 3+ testes

#### 7.2 AI Agent Behavior Preferences
- [ ] **Backend:** Preferências: `ai.response_verbosity` (concise, standard, detailed), `ai.preferred_language`, `ai.auto_context_scope` (service, team, all)
  - [ ] Seeds de configuração
  - [ ] Aplicação nos system prompts dos agentes
- [ ] **Frontend:** Secção "AI Preferences" na UserPreferencesPage
  - [ ] Select de verbosidade
  - [ ] Select de idioma
  - [ ] Select de scope automático
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 2+ testes

#### 7.3 Custom AI Knowledge Scope
- [ ] **Backend:** Preferência `ai.knowledge_sources` — quais fontes o assistente consulta
  - [ ] Opções: contracts, services, changes, incidents, runbooks, knowledge-articles, operational-notes
  - [ ] Aplicação no pipeline de grounding (`IKnowledgeModule`)
- [ ] **Frontend:** Multi-select de fontes de conhecimento
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 2+ testes

---

### FASE 8 — Integrations & API Customizável
**Prioridade:** 🔵 Baixa | **Complexidade:** Média | **Impacto:** Médio-Alto

#### 8.1 Custom Webhook Payloads
- [ ] **Backend:** Entidade `WebhookTemplate` (Id, TenantId, Name, EventType, PayloadTemplate, Headers[], Enabled)
  - [ ] PayloadTemplate: Handlebars/Liquid template com variáveis da entidade
  - [ ] Endpoint: `/api/v1/webhook-templates` (CRUD)
  - [ ] Preview com dados de exemplo
- [ ] **Frontend:** Página `WebhookTemplatesPage`
  - [ ] Editor de template com syntax highlighting
  - [ ] Lista de variáveis disponíveis por evento
  - [ ] Botão "Test webhook"
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 3+ testes

#### 8.2 API Keys Management por Utilizador
- [ ] **Backend:** Já existe infraestrutura de API Keys no BuildingBlocks.Security
  - [ ] Extensão: self-service de API keys com escopos customizáveis
  - [ ] Scopes: read:services, write:services, read:contracts, read:changes, etc.
  - [ ] Endpoint: `/api/v1/user/api-keys` (CRUD)
  - [ ] Expiração configurável
- [ ] **Frontend:** Página `APIKeysPage` em preferências do utilizador
  - [ ] Criar key com nome, scopes e expiração
  - [ ] Lista de keys com last used, revoke
  - [ ] Copiar key (visível apenas uma vez)
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 4+ testes

#### 8.3 Custom Integration Field Mappings
- [ ] **Backend:** Extensão do módulo Integrations para mappings customizáveis
  - [ ] Admin mapeia campos NexTraceOne ↔ campos do sistema externo
  - [ ] Configuração por conector (Jira, ServiceNow, etc.)
- [ ] **Frontend:** UI de mapping em cada conector configurado
- [ ] **i18n:** 4 locales
- [ ] **Testes:** 2+ testes

---

## Priorização e Cronograma Recomendado

### Sprint 1-2 — Quick Wins (Fase 1 parcial)
| Item | Effort | Impacto |
|------|--------|---------|
| 1.2 Bookmarks / Favoritos | S | Alto |
| 1.4 Timezone e formato de data | S | Alto |
| 1.6 Itens por página | XS | Médio |
| 1.3 Default scope | S | Alto |

### Sprint 3-4 — Personalização de Experiência (Fase 1 + 2 parcial)
| Item | Effort | Impacto |
|------|--------|---------|
| 1.1 Saved Views generalizados | M | Muito Alto |
| 1.5 Colunas visíveis em tabelas | M | Alto |
| 2.1 Dashboard Templates por Persona | M | Muito Alto |
| 2.3 Widget de Notas | S | Médio |

### Sprint 5-6 — Dashboards Avançados + Watch Lists (Fase 2 + 3 parcial)
| Item | Effort | Impacto |
|------|--------|---------|
| 2.2 Custom Charts (Query Builder) | L | Muito Alto |
| 2.4 Dashboard Cloning | S | Alto |
| 3.1 Watch Lists | M | Alto |
| 3.2 Quiet Hours | S | Alto |

### Sprint 7-8 — Custom Fields + Alertas (Fase 3 + 4 parcial)
| Item | Effort | Impacto |
|------|--------|---------|
| 3.3 Custom Alert Rules | L | Alto |
| 3.4 Digest Personalizado | S | Alto |
| 4.1 Custom Tags | L | Muito Alto |
| 4.2 Custom Metadata Fields | L | Muito Alto |

### Sprint 9-10 — Workflows + Taxonomias (Fase 4 + 5)
| Item | Effort | Impacto |
|------|--------|---------|
| 4.3 Custom Taxonomies | M | Alto |
| 5.1 Custom Checklists | M | Alto |
| 5.2 Automation Rules | L | Alto |
| 5.3 Custom Contract Templates | M | Alto |

### Sprint 11-12 — Relatórios + AI + Integrations (Fase 6 + 7 + 8)
| Item | Effort | Impacto |
|------|--------|---------|
| 6.1 Scheduled Reports | M | Alto |
| 6.2 Export Configurável | M | Alto |
| 7.1 Custom AI Prompts | S | Alto |
| 7.2 AI Behavior Preferences | S | Alto |
| 8.1 Custom Webhook Payloads | M | Médio |
| 8.2 API Keys Management | M | Alto |

**Legenda de Effort:** XS = <1 dia, S = 1-2 dias, M = 3-5 dias, L = 1-2 semanas

---

## Métricas de Sucesso

| Métrica | Alvo | Medição |
|---------|------|---------|
| Adoption rate de saved views | >40% dos utilizadores activos | analytics.saved_views_created |
| Bookmarks criados por utilizador | >3 em média | analytics.bookmarks_count |
| Dashboards customizados activos | >60% dos tenants | analytics.custom_dashboards_active |
| Watch lists activas | >30% dos utilizadores | analytics.watches_count |
| Custom tags por serviço | >2 em média | analytics.tags_per_service |
| Satisfaction score (UX) | >4.0/5.0 | survey NPS |

---

## Notas Técnicas

1. **Bounded Context:** Funcionalidades transversais (bookmarks, saved views, tags, watches) ficam no módulo Configuration. Funcionalidades específicas (custom charts, contract templates) ficam no módulo dono do domínio.
2. **User Preferences API:** Já existente — extender com novos keys sem breaking change.
3. **Parametrização Admin vs User:** Admins configuram limits e defaults; utilizadores personalizam dentro desses limites.
4. **Auditoria:** Todas as ações de customização geram eventos de auditoria quando aplicável.
5. **Tenant Isolation:** Toda customização é scoped por tenant — nenhum vazamento cross-tenant.
6. **Performance:** Saved views e bookmarks devem ter cache eficiente (TanStack Query staleTime).
7. **i18n:** Todo texto novo em 4 locales (en, pt-BR, pt-PT, es) — obrigatório.

---

## Referências

- [BRAND-IDENTITY.md](./BRAND-IDENTITY.md) — Identidade visual imutável
- [DESIGN-SYSTEM.md](./DESIGN-SYSTEM.md) — Design system e componentes
- [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) — Roadmap geral do produto
- [PERSONA-MATRIX.md](./PERSONA-MATRIX.md) — Personas e segmentação
- ConfigurationDefinitionSeeder.cs — Seeds de parametrização existentes
- UserPreferencesPage.tsx — Página de preferências existente

---

*Documento criado em Abril 2026 — Versão 1.0*
*Inspirado em práticas de: Dynatrace, Datadog, Grafana, ServiceNow, PagerDuty, Backstage, Azure DevOps, Jira*
