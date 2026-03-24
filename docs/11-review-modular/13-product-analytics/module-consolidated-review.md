# Product Analytics — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Product Analytics** é dedicado à análise de uso e valor do próprio NexTraceOne como produto:

- Overview de analytics do produto
- Adoção por módulo
- Uso por persona
- Funis de jornada
- Tracking de valor entregue

### Posição na arquitetura

O backend reside **dentro do módulo Governance** (`ProductAnalyticsEndpointModule` em `src/modules/governance/`), similar ao módulo Integrations. Frontend em `src/frontend/src/features/product-analytics/` como feature independente.

---

## 2. Estado Atual

| Dimensão | Valor |
|----------|-------|
| **Maturidade global** | **30%** |
| Backend | 40% (endpoints no Governance, dados possivelmente simulados) |
| Frontend | 45% (5 páginas com UI, integração com dados reais questionável) |
| Documentação | 0% (zero documentação dedicada) |
| Testes | 15% |
| **Prioridade** | P5 (Suporte) |
| **Status** | ⚠️ Preview — funcionalidade parcial, dados possivelmente simulados |

**Causa raiz:** Módulo concebido como diferenciador enterprise (self-analytics), mas implementação pode depender de event tracking real não completamente implementado.

---

## 3. Problemas Críticos e Bloqueadores

### ⚠️ Dados possivelmente simulados

Não é claro se os dados de analytics são baseados em event tracking real ou em valores simulados/mock. Necessita validação.

### ⚠️ Acoplamento com Governance

Tal como Integrations, o backend reside no módulo Governance em vez de bounded context próprio.

### ⚠️ Documentação inexistente

Zero documentação dedicada ao módulo.

---

## 4. Frontend

| Página | Rota | Permissão | Estado |
|--------|------|-----------|--------|
| ProductAnalyticsOverviewPage | `/analytics` | analytics:read | ⚠️ Parcial |
| ModuleAdoptionPage | `/analytics/adoption` | analytics:read | ⚠️ Parcial |
| PersonaUsagePage | `/analytics/personas` | analytics:read | ⚠️ Parcial |
| JourneyFunnelPage | `/analytics/journeys` | analytics:read | ⚠️ Parcial |
| ValueTrackingPage | `/analytics/value` | analytics:read | ⚠️ Parcial |

### Ficheiros de suporte

| Ficheiro | Propósito |
|----------|-----------|
| AnalyticsEventTracker.tsx | Componente de tracking de eventos |
| productAnalyticsApi.ts | API client |

**Nota:** Apenas 1 item no menu (Product Analytics). As outras 4 páginas são sub-rotas.

---

## 5. Classificação de Funcionalidades

| Funcionalidade | Classificação | Justificativa |
|---------------|---------------|---------------|
| Overview | **Parcial** | UI existe, dados podem ser mocks ou limitados |
| Module Adoption | **Parcial** | Depende de event tracking real |
| Persona Usage | **Parcial** | Depende de dados de persona |
| Journey Funnel | **Preview** | Conceito avançado, implementação questionável |
| Value Tracking | **Preview** | Conceito avançado, implementação questionável |

---

## 6. Ações Recomendadas

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | Criar documentação dedicada | P1 | 2h |
| 2 | Avaliar se dados de analytics são reais ou simulados | P1 | 2h |
| 3 | Decidir se Product Analytics deve continuar visível ou ser ocultado | P2 | 1h |
| 4 | Se mantido: implementar event tracking real | P3 | 8h |
| 5 | Se mantido: avaliar extração como módulo backend independente | P3 | 4h |

---

## 7. Dependências

| Módulo | Relação |
|--------|---------|
| Governance | **Forte** — Backend reside neste módulo (acoplamento) |
| Identity & Access | **Média** — Dados de persona e utilizador |
| Todos os módulos | **Fraca** — Event tracking requer instrumentação transversal |

---

## 8. Estado do Consolidado

| Aspeto | Valor |
|--------|-------|
| Consolidado | `CONSOLIDATED_OK` |
| Razão | Module review substantivo com classificação realista de funcionalidades |
| Próximo passo | Validar se dados são reais; decidir sobre visibilidade do módulo |
