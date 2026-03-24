# Revisão Modular — Product Analytics

> **Data:** 2026-03-24  
> **Prioridade:** P5 (Suporte)  
> **Módulo Backend:** Endpoints no módulo `governance/` (ProductAnalyticsEndpointModule)  
> **Módulo Frontend:** `src/frontend/src/features/product-analytics/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Product Analytics** é dedicado à análise de uso e valor do próprio NexTraceOne como produto:

- Overview de analytics do produto
- Adoção por módulo
- Uso por persona
- Funis de jornada
- Tracking de valor entregue

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento | ✅ Bom | Self-analytics é diferenciador para plataforma enterprise |
| Frontend | ⚠️ Parcial | 5 páginas com UI, integração com dados reais questionável |
| Backend | ⚠️ **Misturado** | Endpoints residem no módulo Governance |
| Documentação | ❌ **Zero** | Não existe documentação dedicada |
| Autonomia | ❌ **Não é módulo independente** | Depende do Governance backend |

---

## 3. Páginas Frontend

| Página | Rota | Permissão | Estado | Funcionalidade |
|--------|------|-----------|--------|----------------|
| ProductAnalyticsOverviewPage | `/analytics` | analytics:read | ⚠️ Parcial | Overview de analytics |
| ModuleAdoptionPage | `/analytics/adoption` | analytics:read | ⚠️ Parcial | Adoção por módulo |
| PersonaUsagePage | `/analytics/personas` | analytics:read | ⚠️ Parcial | Uso por persona |
| JourneyFunnelPage | `/analytics/journeys` | analytics:read | ⚠️ Parcial | Funis de jornada |
| ValueTrackingPage | `/analytics/value` | analytics:read | ⚠️ Parcial | Tracking de valor |

### Ficheiros de Suporte

| Ficheiro | Propósito |
|----------|-----------|
| AnalyticsEventTracker.tsx | Componente de tracking de eventos |
| productAnalyticsApi.ts | API client |

### Nota de Menu

Apenas 1 item no menu (Product Analytics). As outras 4 páginas são sub-rotas.

---

## 4. Classificação

| Funcionalidade | Classificação | Justificativa |
|---------------|---------------|---------------|
| Overview | **Parcial** | UI existe, dados podem ser mocks ou limitados |
| Module Adoption | **Parcial** | Depende de event tracking real |
| Persona Usage | **Parcial** | Depende de dados de persona |
| Journey Funnel | **Preview** | Conceito avançado, implementação questionável |
| Value Tracking | **Preview** | Conceito avançado, implementação questionável |

---

## 5. Resumo de Ações

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | **Criar documentação** — zero documentação existe | P1 | 2h |
| 2 | Avaliar se dados de analytics são reais ou simulados | P1 | 2h |
| 3 | Decidir se Product Analytics deve continuar visível ou ser ocultado | P2 | 1h |
| 4 | Se mantido: implementar event tracking real | P3 | 8h |
| 5 | Se mantido: avaliar extração como módulo backend independente | P3 | 4h |
