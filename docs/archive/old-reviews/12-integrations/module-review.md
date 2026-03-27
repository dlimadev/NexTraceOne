# Revisão Modular — Integrations

> **Data:** 2026-03-24  
> **Prioridade:** P5 (Suporte)  
> **Módulo Backend:** Endpoints no módulo `governance/` (IntegrationHubEndpointModule)  
> **Módulo Frontend:** `src/frontend/src/features/integrations/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Integrations** é o hub de integrações do NexTraceOne, responsável por:

- Gestão de conectores de integração
- Execuções de ingestão de dados
- Monitorização de freshness dos dados ingeridos
- Detalhe de cada conector

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento | ✅ Forte | Integrações são essenciais para plataforma enterprise |
| Frontend | ✅ Funcional | 4 páginas completas |
| Backend | ⚠️ **Misturado** | Endpoints residem no módulo Governance (IntegrationHubEndpointModule) |
| Documentação | ❌ **Zero** | Não existe documentação dedicada |
| Autonomia | ❌ **Não é módulo independente** | Depende do Governance backend |

---

## 3. Páginas Frontend

| Página | Rota | Permissão | Estado | Funcionalidade |
|--------|------|-----------|--------|----------------|
| IntegrationHubPage | `/integrations` | integrations:read | ✅ Funcional | Hub com lista de conectores |
| ConnectorDetailPage | `/integrations/:connectorId` | integrations:read | ✅ Funcional | Detalhe de conector |
| IngestionExecutionsPage | `/integrations/executions` | integrations:read | ✅ Funcional | Lista de execuções |
| IngestionFreshnessPage | `/integrations/freshness` | integrations:read | ✅ Funcional | Freshness dos dados |

### Nota de Menu

Apenas 1 item no menu (Integration Hub). As outras 3 páginas são sub-rotas acessíveis internamente.

---

## 4. Backend

### Entidades (no módulo Governance)

| Entidade | Propósito |
|----------|-----------|
| IntegrationConnector | Definição de conector |
| IngestionSource | Fonte de ingestão |
| IngestionExecution | Registo de execução |

---

## 5. Problema Arquitetural

O backend de Integrations reside dentro do módulo Governance, criando acoplamento:

- IntegrationHubEndpointModule → em GovernanceAPI
- IntegrationConnector entity → em GovernanceDomain
- Deveria ser bounded context separado

---

## 6. Resumo de Ações

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | **Criar documentação** — zero documentação existe | P1 | 3h |
| 2 | Validar hub de conectores e execuções de ingestão | P1 | 2h |
| 3 | Promover sub-rotas ao menu (executions, freshness) | P2 | 30 min |
| 4 | Avaliar extração como módulo backend independente | P3 | 4h |
| 5 | Criar API client dedicado (integrations.ts) | P2 | 2h |
