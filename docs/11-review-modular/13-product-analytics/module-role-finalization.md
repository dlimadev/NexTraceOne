# PARTE 1 — Papel Final do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Papel do módulo Product Analytics no NexTraceOne

O módulo **Product Analytics** é o módulo responsável por **medir, observar e reportar o uso real do produto NexTraceOne** — adoção por módulo, comportamento por persona, funnels de jornada, milestones de valor, sinais de fricção e engagement dos utilizadores.

### Definição oficial

> Product Analytics é a **fonte da verdade sobre como o NexTraceOne é usado** — quem usa, o que usa, quanto usa, onde abandona, onde encontra valor e onde encontra fricção.

### Diferenciação fundamental

| Aspecto | Governance | Product Analytics |
|---------|-----------|-------------------|
| **Foco** | Conformidade, políticas, risco | Uso, adoção, valor, comportamento |
| **Pergunta** | "Está conforme?" | "Está a ser usado? Como?" |
| **Dados** | Compliance checks, waivers, packs | Eventos de uso, métricas de adoção |
| **Audiência** | Auditores, compliance officers | Product managers, tech leads, executives |
| **Armazenamento** | PostgreSQL (transacional) | ClickHouse (analítico) + PostgreSQL (config) |

---

## 2. Ownership confirmado

O módulo Product Analytics é **dono exclusivo** de:

| Responsabilidade | Status Atual | Confirmação |
|-----------------|--------------|-------------|
| Captura de eventos de uso do produto | ✅ Parcial (só ModuleViewed) | ✅ CONFIRMADO como owner |
| Métricas de adoção por módulo | ✅ Implementado (parcialmente mock) | ✅ CONFIRMADO como owner |
| Métricas de uso por persona | ⚠️ Mock data | ✅ CONFIRMADO como owner |
| Funnels de jornada do utilizador | ⚠️ Implementado (dados limitados) | ✅ CONFIRMADO como owner |
| Value milestones (tempo até valor) | ⚠️ Implementado (dados limitados) | ✅ CONFIRMADO como owner |
| Sinais de fricção (abandono, zero results) | ✅ Real data via repository | ✅ CONFIRMADO como owner |
| KPIs de produto (adoption score, value score) | ⚠️ Calculados | ✅ CONFIRMADO como owner |
| Dashboards de analytics do produto | ✅ 5 páginas frontend | ✅ CONFIRMADO como owner |
| Feature usage tracking | ❌ Não implementado | ✅ CONFIRMADO como owner |
| Engagement e retenção | ❌ Não implementado | ✅ CONFIRMADO como owner |
| Configuração de definições analíticas | ❌ Não existe | ✅ CONFIRMADO como owner |

---

## 3. O que o módulo NÃO deve ser dono

| Responsabilidade | Módulo correto |
|-----------------|----------------|
| Compliance reports e governance scorecards | **Governance** |
| Maturity scorecards organizacionais | **Governance** |
| Risk e control assessment | **Governance** |
| FinOps e custos operacionais | **Governance** |
| SLA/SLO/SLI de serviços | **Operational Intelligence** |
| Health de integrações externas | **Integrations** |
| Logs e traces técnicos | **Operational Intelligence** |
| Auditoria de ações do sistema | **Audit & Compliance** |
| Métricas de infraestrutura | **Operational Intelligence** |
| Benchmarking organizacional | **Governance** |
| Onboarding progress (operacional) | **Governance** |
| Executive overview (compliance) | **Governance** |

---

## 4. Relação com Governance

### Estado atual

| Aspecto | Situação |
|---------|----------|
| Backend location | ❌ Dentro de `src/modules/governance/` |
| DbContext | ❌ Usa `GovernanceDbContext` |
| Table prefix | ❌ Usa `gov_` em vez de `pan_` |
| Endpoints | ⚠️ Registados em `ProductAnalyticsEndpointModule` no Governance API |
| Permissions | ⚠️ Usa `governance:analytics:*` em vez de `analytics:*` |
| DI | ❌ Registado no Governance DI |

### Estado alvo

| Aspecto | Alvo |
|---------|------|
| Backend location | `src/modules/productanalytics/` |
| DbContext | `ProductAnalyticsDbContext` (standalone) |
| Table prefix | `pan_` |
| Endpoints | Registados em `ProductAnalyticsEndpointModule` no ProductAnalytics API |
| Permissions | `analytics:read`, `analytics:write`, `analytics:export` |
| DI | Registado no ProductAnalytics DI |

### Comunicação entre módulos

- **Governance → Product Analytics**: Governance NÃO emite eventos para Product Analytics
- **Product Analytics → Governance**: Product Analytics NÃO depende de Governance para funcionar
- **Todos os módulos → Product Analytics**: Cada módulo emite eventos de uso que Product Analytics captura (via AnalyticsEventTracker no frontend ou API POST /events)

---

## 5. Dependências principais do módulo

### Dependências de entrada (o módulo consome)

| Módulo | O que consome | Tipo |
|--------|---------------|------|
| **Identity & Access** | TenantId, UserId, Persona | Contextual (via JWT) |
| **Todos os módulos** | Eventos de uso (page views, actions) | Event capture (frontend) |
| **Building Blocks** | NexTraceDbContextBase, interceptors | Infraestrutura |

### Dependências de saída (o módulo expõe)

| Para quem | O que expõe | Tipo |
|-----------|-------------|------|
| **Frontend** | 7 REST endpoints para dashboards | API REST |
| **Qualquer consumidor** | Métricas de adoção e uso agregadas | API REST (read-only) |

### Dependências técnicas

| Tecnologia | Papel | Status |
|-----------|-------|--------|
| **PostgreSQL** | Configuração analítica, event buffer | ✅ Ativo (via GovernanceDbContext) |
| **ClickHouse** | Armazenamento analítico de alto volume | ❌ REQUIRED mas não implementado |

---

## 6. Conclusão

O módulo Product Analytics tem um **papel claro e bem definido** no NexTraceOne: medir o uso real do produto. O problema principal é que está **fisicamente dentro do módulo Governance** (blocker OI-03), usa o **prefixo errado** (`gov_` em vez de `pan_`), tem **dados parcialmente mockados**, e o **ClickHouse não está integrado** apesar de ser REQUIRED pela matriz de decisão arquitetural.

A maturidade atual é de **~30%**, com boa estrutura frontend mas backend dependente de Governance e dados de qualidade mista.
