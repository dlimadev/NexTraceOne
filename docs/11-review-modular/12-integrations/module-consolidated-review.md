# Integrations — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Integrations** é o hub de integrações do NexTraceOne, responsável por:

- Gestão de conectores de integração
- Execuções de ingestão de dados
- Monitorização de freshness dos dados ingeridos
- Detalhe e configuração de cada conector

### Posição na arquitetura

O backend reside **dentro do módulo Governance** (`IntegrationHubEndpointModule` em `src/modules/governance/`), criando um acoplamento arquitetural. Frontend em `src/frontend/src/features/integrations/` como feature independente.

---

## 2. Estado Atual

| Dimensão | Valor |
|----------|-------|
| **Maturidade global** | **45%** |
| Backend | 55% (funcional mas acoplado ao Governance) |
| Frontend | 70% (4 páginas completas) |
| Documentação | 0% (zero documentação dedicada) |
| Testes | 30% |
| **Prioridade** | P5 (Suporte) |
| **Status** | ⚠️ Funcional mas arquiteturalmente acoplado |

**Causa raiz:** Integrations não é um bounded context independente — endpoints e entidades residem no módulo Governance.

---

## 3. Problemas Críticos e Bloqueadores

### ⚠️ Acoplamento arquitetural com Governance

| Componente | Localização atual | Localização ideal |
|-----------|------------------|-------------------|
| IntegrationHubEndpointModule | Governance API | Integrations API (dedicado) |
| IntegrationConnector entity | Governance Domain | Integrations Domain |
| IngestionSource entity | Governance Domain | Integrations Domain |
| IngestionExecution entity | Governance Domain | Integrations Domain |

### ⚠️ Documentação inexistente

Zero documentação dedicada ao módulo. Nenhum README, nenhum doc de API, nenhum doc de arquitetura.

---

## 4. Frontend

| Página | Rota | Permissão | Estado |
|--------|------|-----------|--------|
| IntegrationHubPage | `/integrations` | integrations:read | ✅ Funcional |
| ConnectorDetailPage | `/integrations/:connectorId` | integrations:read | ✅ Funcional |
| IngestionExecutionsPage | `/integrations/executions` | integrations:read | ✅ Funcional |
| IngestionFreshnessPage | `/integrations/freshness` | integrations:read | ✅ Funcional |

**Nota:** Apenas 1 item no menu (Integration Hub). As outras 3 páginas são sub-rotas acessíveis internamente.

---

## 5. Entidades (no módulo Governance)

| Entidade | Propósito |
|----------|-----------|
| IntegrationConnector | Definição de conector |
| IngestionSource | Fonte de ingestão |
| IngestionExecution | Registo de execução |

---

## 6. Ações Recomendadas

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | Criar documentação dedicada | P1 | 3h |
| 2 | Validar hub de conectores e execuções de ingestão | P1 | 2h |
| 3 | Promover sub-rotas ao menu (executions, freshness) | P2 | 30min |
| 4 | Avaliar extração como módulo backend independente | P3 | 4h |
| 5 | Criar API client dedicado (integrations.ts) | P2 | 2h |

---

## 7. Dependências

| Módulo | Relação |
|--------|---------|
| Governance | **Forte** — Backend reside neste módulo (acoplamento) |
| Configuration | **Média** — Conectores configuráveis |
| Notifications | **Fraca** — IntegrationFailureNotificationHandler |

---

## 8. Estado do Consolidado

| Aspeto | Valor |
|--------|-------|
| Consolidado | `CONSOLIDATED_OK` |
| Razão | Module review substantivo com análise de acoplamento real |
| Próximo passo | Criar documentação dedicada e avaliar extração como módulo independente |
