# NexTraceOne — Análise Forense do Estado Real do Projeto

> **Data:** Abril 2026  
> **Autor:** Análise automática assistida por IA  
> **Âmbito:** Estado real do código em `main` — funcionalidades com problemas, erros, implementações parciais/incompletas  
> **Ficheiro relacionado:** [PLAN-ACTION-2026-04.md](./PLAN-ACTION-2026-04.md)

---

## Sumário Executivo

O NexTraceOne é um projeto de grande dimensão (~5600 ficheiros, 12 módulos backend, 130+ páginas frontend). A maior parte do código de produção está sólida. Os problemas encontrados concentram-se em três categorias:

| Categoria | Severidade | Impacto |
|-----------|------------|---------|
| Páginas frontend com dados hardcoded/mock que ignoram a API | 🔴 Alta | Utilizador vê dados falsos mesmo com backend funcional |
| Handlers backend que retornam `IsSimulated=true` com dados determinísticos | 🟡 Média | Funcionalidade declarada mas não real; UX degradada |
| Infraestrutura de execução NQL incompleta (native entities retornam lista vazia) | 🟡 Média | Widget SDK não funciona para entidades nativas |
| Providers opcionais (DEG-03..08) sem interface dedicada (Nível B vs A) | 🟢 Baixa | Documentado; sem regressão imediata |
| SetupWizard sem integração API | 🟡 Média | Onboarding de plataforma quebrado |

---

## 1. Problemas Críticos — Frontend: Dados Hardcoded a Ignorar a API

### 1.1 `PersonaHomePage.tsx` — Stats Hardcoded

**Ficheiro:** `src/frontend/src/features/governance/pages/PersonaHomePage.tsx`

**Problema:** A página define quatro arrays estáticos de estatísticas (`ENGINEER_STATS`, `TECHLEAD_STATS`, `EXECUTIVE_STATS`, `DEFAULT_STATS`) e renderiza-os diretamente — sem qualquer chamada a `useQuery`. O backend tem o endpoint `GET /api/v1/governance/persona-home` funcional (handler `GetPersonaHome`) com repositório EF Core e tabela `gov_persona_home_configurations`, mas o frontend nunca chama esse endpoint.

**Evidência:**
```tsx
// PersonaHomePage.tsx linha 43 — dados estáticos
const ENGINEER_STATS: StatDef[] = [
  { label: 'My Services', value: '7', ... },
  { label: 'Open Incidents', value: '3', ... },
  ...
];
// linha 224 — renderiza sem API call
return persona === 'engineer' ? ENGINEER_STATS : ...;
```

**Impacto:** Utilizador vê sempre "7 serviços", "3 incidentes" — mesmo que tenha 0 ou 500. Dados completamente desconectados da realidade.

**Backend correspondente:** `GET /api/v1/governance/persona-home` ✅ existe e funciona (mas retorna `IsSimulated: true` por falta de data bridges — ver §2.2)

---

### 1.2 `DashboardReportsPage.tsx` — Select Override que Anula a API

**Ficheiro:** `src/frontend/src/features/governance/pages/DashboardReportsPage.tsx`

**Problema:** A página chama a API (`reportsApi.getUsageAnalytics()`), mas usa `select: () => SIMULATED_REPORTS` para substituir completamente a resposta real por 3 registos hardcoded. Qualquer dado que venha do backend é descartado.

**Evidência:**
```tsx
// linha 27 — dados hardcoded
const SIMULATED_REPORTS: ScheduledReport[] = [
  { id: 'sr-001', name: 'Weekly Executive Summary', ... },
  ...
];

// linha 82 — ANULA a resposta real da API
useQuery({
  queryKey: ['scheduled-reports'],
  queryFn: reportsApi.getUsageAnalytics,
  select: () => SIMULATED_REPORTS,  // 🔴 ignora completamente o resultado real
});
```

**Impacto:** Relatórios criados pelos utilizadores nunca aparecem na UI.

---

### 1.3 `DashboardTemplatesPage.tsx` — Sem Chamada API

**Ficheiro:** `src/frontend/src/features/governance/pages/DashboardTemplatesPage.tsx`

**Problema:** Página inteiramente estática — zero chamadas `useQuery`/`useMutation`/`fetch`. Define `TEMPLATES` como array local com 20+ templates hardcoded e renderiza-os diretamente.

**Evidência:** `grep -c "useQuery\|useMutation\|fetch\|api\." DashboardTemplatesPage.tsx` → `0`

**Impacto:** Templates criados por admins no backend nunca são mostrados. Os "20 templates disponíveis" são sempre os mesmos fictícios.

---

### 1.4 `SetupWizardPage.tsx` — Wizard sem Integração API

**Ficheiro:** `src/frontend/src/features/platform-admin/pages/SetupWizardPage.tsx`

**Problema:** O wizard de setup da plataforma (600+ linhas) usa exclusivamente `useState` local. Não persiste nenhuma configuração no backend, não carrega estado existente, não valida nenhum passo via API.

**Impacto:** Admin que "completa" o wizard fica com a ilusão de ter configurado a plataforma — sem qualquer persistência real.

---

## 2. Problemas Médios — Backend: Handlers com `IsSimulated=true` Estrutural

Estes handlers têm endpoint registado, validação, e estrutura correta — mas retornam dados determinísticos/simulados porque falta a bridge com a fonte de dados real.

### 2.1 `GetPersonaHome` — Cards Estáticos

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetPersonaHome/GetPersonaHome.cs`

**Problema:** Handler carrega config persistida (se existir) mas sempre retorna cards com `IsSimulated: true` e valores estáticos ("4 serviços", "97.2% SLO"). Não agrega dados cross-module em tempo real.

```csharp
// linha 75
return Result<Response>.Success(new Response(
    ...
    IsSimulated: true,  // 🟡 sempre true
    SimulatedNote: "Home cards showing system-default layout. Real-time data bridges pending."));
```

**O que falta:** Injetar `IIncidentModule`, `ICatalogGraphModule`, `IReliabilityModule` e agregar dados reais para cada persona.

---

### 2.2 `GetWidgetDelta` — Delta Gerado com `Random.Shared`

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetWidgetDelta/GetWidgetDelta.cs`

**Problema:** Retorna variações de widget geradas aleatoriamente — não compara snapshots reais.

```csharp
// linha 107
IsSimulated: true,
SimulatedNote: "Widget delta data is simulated — real-time ingestion bridge required."
// Gera rows aleatórios com Random.Shared
```

**O que falta:** Snapshots de estado de widget persistidos e comparação temporal real.

---

### 2.3 `ComposeAiDashboard` — Composição por Keywords, não por IA

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/ComposeAiDashboard/ComposeAiDashboard.cs`

**Problema:** O handler não invoca nenhum LLM. Usa análise de keywords no `description` para sugerir widgets. `IsSimulated: true`.

```csharp
// linha 81
IsSimulated: true,
SimulatedNote: "Dashboard proposal generated via keyword analysis. Connect AI model for LLM-grounded composition."
```

**O que falta:** Integração com `IChatCompletionProvider` para geração real de dashboards por IA.

---

### 2.4 `GetDashboardLiveStream` — SSE Totalmente Simulado

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDashboardLiveStream/GetDashboardLiveStream.cs`

**Problema:** O canal SSE emite eventos `IsSimulated: true` com payload estático ("p50: 120+rand", "p95: 280+rand"). Não consome nenhuma fonte de dados em tempo real.

**O que falta:** Bridge com pipeline de ingestão (OTel/Kafka/internal event bus) para emitir atualizações reais dos widgets.

---

### 2.5 `ExecuteNqlQuery` / `DefaultQueryGovernanceService` — Native Execution Vazia

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/QueryGovernanceService.cs`

**Problema:** O serviço distingue entre entidades "nativas" (`GovernanceTeams`, `GovernanceDomains`) e cross-module. Para entidades nativas, chama `ExecuteNativeAsync` — que retorna **lista vazia** (não é uma simulação, é uma não-implementação).

```csharp
// linha 87
// For now, native execution for Governance entities returns empty result sets;
// real DB execution will be wired in a follow-up as repositories become queryable.
ct.ThrowIfCancellationRequested();
return Task.FromResult(new List<IReadOnlyList<object?>>());
```

**Impacto:** Queries `FROM governance.teams WHERE ...` retornam sempre 0 resultados — mesmo havendo equipas na base de dados.

Para entidades cross-module (`CatalogServices`, `CatalogContracts`, `ChangesReleases`, `OperationsIncidents`, etc.) retorna dados simulados honestamente declarados.

---

## 3. Problemas de Infraestrutura — Providers Opcionais (Nível B vs A)

Os seguintes providers opcionais (DEG-03..08) não têm interface `IXxxProvider` dedicada nem aparecem em `/admin/system-health`. Isto é comportamento documentado e não é uma regressão, mas limita a visibilidade operacional.

| DEG | Provider | Problema Concreto |
|-----|----------|-------------------|
| DEG-03 | Runtime Intelligence | `GetRuntimeModuleMatrix` simula em handler; sem `IRuntimeProvider` |
| DEG-04 | Chaos Engineering | `SubmitChaosExperiment` aceita requests mas retorna estado simulado; sem `IChaosProvider` |
| DEG-05 | mTLS Manager | Lê de `IConfiguration["Mtls:*"]`; sem integração PKI real; lista de certificados sempre vazia |
| DEG-06 | Multi-tenant Schema | Handler retorna proposta simulada; sem executor IaC real |
| DEG-07 | Capacity Forecast | Deriva nota simulada quando não há snapshots de runtime |
| DEG-08 | Feature Flags externo | BD local é fonte; sem read-through LaunchDarkly/Unleash |

---

## 4. Lacunas de Funcionalidade Identificadas

### 4.1 DB Architecture Migration (Documentada, Não Iniciada)

**Ficheiros:** `docs/db-architecture/00-OVERVIEW.md` a `07-CLICKHOUSE-SCHEMA-TEMPLATES.md`

O plano de migração de dados analíticos de PostgreSQL para ClickHouse (5 fases) está **totalmente documentado** mas **zero implementado**. Inclui:
- Migração de tabelas de métricas OI para ClickHouse
- Repositórios nulos temporários durante migração dual-store
- Templates de schema ClickHouse

**Estado atual:** Todo o código está em PostgreSQL via EF Core. A migração é futura mas já tem plano detalhado.

### 4.2 `IPersonaHomeConfigurationRepository` — Repositório Existe mas Cards Nunca São Preenchidos

O backend tem `gov_persona_home_configurations` (tabela + repositório + endpoint), mas o handler nunca usa os dados carregados do repositório para popular os cards — sempre usa defaults estáticos.

### 4.3 `GetDemoSeedStatus` — Estado de Demo Seed Sem Lógica Real

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs`

O handler retorna sempre `{ IsSeeded: false, CanSeed: true }` sem verificar o estado real da base de dados.

### 4.4 `InstantiateTemplate` — Instanciação Retorna `IsSimulated: true`

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/InstantiateTemplate/InstantiateTemplate.cs` linha 91

Instanciação de templates de dashboard retorna resultado simulado em vez de criar um dashboard real.

---

## 5. Qualidade de Código — Baixa Severidade

### 5.1 TODOs em Código Gerado (Correto por Design)

Os TODOs em `GenerateMigrationPatch.cs` são **intencionais** — fazem parte do código gerado para os developers que recebem o patch. Não são dívida técnica do produto.

### 5.2 Stubs DES-01 e DES-02 (Correto por Design)

`ResendMfaCode` e `ResetPassword/ActivateAccount` são stubs controlados documentados em `HONEST-GAPS.md`. Correto por design (plataforma SSO-first).

---

## 6. O Que Está Bem (Para Contexto)

Para não criar uma imagem distorcida, os seguintes componentes estão sólidos:

- **12 módulos backend** com repositórios EF Core reais, migrações, testes unitários
- **15 interfaces cross-module** todas implementadas
- **35 páginas platform-admin** todas com `useQuery` real e tratamento de erros
- **Pipeline de ingestão** (PIP-01..06) completamente implementado
- **Providers opcionais Nível A** (Canary, Backup, Kafka, Cloud Billing, SAML) com pattern completo
- **Sistema AI/LLM** real com streaming, guardrails, tools, grounding
- **SaaS Evolution** (licenças, agentes, alertas) implementado e com migração

---

## Matriz de Criticidade

| ID | Componente | Tipo | Severidade | Esforço |
|----|-----------|------|------------|---------|
| **F-01** | PersonaHomePage — dados hardcoded | Frontend | 🔴 Alta | 3–5h |
| **F-02** | DashboardReportsPage — select override | Frontend | 🔴 Alta | 1–2h |
| **F-03** | DashboardTemplatesPage — sem API | Frontend | 🟡 Média | 4–6h |
| **F-04** | SetupWizardPage — sem persistência | Frontend | 🟡 Média | 8–16h |
| **B-01** | GetPersonaHome — cards estáticos | Backend | 🟡 Média | 4–8h |
| **B-02** | GetWidgetDelta — delta aleatório | Backend | 🟡 Média | 8–16h |
| **B-03** | ComposeAiDashboard — sem LLM | Backend | 🟡 Média | 8–16h |
| **B-04** | GetDashboardLiveStream — SSE fake | Backend | 🟡 Média | 16–24h |
| **B-05** | NQL Native Execution — lista vazia | Backend | 🟡 Média | 4–8h |
| **B-06** | GetDemoSeedStatus — sem verificação real | Backend | 🟢 Baixa | 2–4h |
| **B-07** | InstantiateTemplate — IsSimulated | Backend | 🟢 Baixa | 4–8h |
| **I-01** | DEG-03..08 Nível B → A | Infra | 🟢 Baixa | 16–40h cada |
| **I-02** | DB Architecture Migration | Infra | 🔵 Roadmap | 40–80h |

---

*Gerado por análise automática — Abril 2026*  
*Ver plano de ação em: [PLAN-ACTION-2026-04.md](./PLAN-ACTION-2026-04.md)*
