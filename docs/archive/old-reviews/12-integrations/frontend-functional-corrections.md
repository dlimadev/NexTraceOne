# Integrations — Frontend Functional Corrections

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Backlog de correcções gerado

---

## 1. Páginas do módulo

| # | Página | Ficheiro | Rota | Estado |
|---|--------|---------|------|--------|
| 1 | IntegrationHubPage | `pages/IntegrationHubPage.tsx` (~182 LOC) | `/integrations` | ✅ Funcional |
| 2 | ConnectorDetailPage | `pages/ConnectorDetailPage.tsx` (~300+ LOC) | `/integrations/:connectorId` | ✅ Funcional |
| 3 | IngestionExecutionsPage | `pages/IngestionExecutionsPage.tsx` | `/integrations/executions` | ✅ Funcional |
| 4 | IngestionFreshnessPage | `pages/IngestionFreshnessPage.tsx` | `/integrations/freshness` | ✅ Funcional |

**API Client:** `api/integrations.ts` (59 LOC) — 8 métodos tipados  
**Barrel Export:** `index.ts`

---

## 2. Revisão de rotas

| Rota | Registada | No menu | Acessível | Gap |
|------|-----------|---------|-----------|-----|
| `/integrations` | ✅ | ✅ (1 item) | ✅ | — |
| `/integrations/:connectorId` | ✅ | ❌ | ✅ (via link) | — |
| `/integrations/executions` | ✅ | ❌ | ✅ (via nav interno) | ⚠️ Não está no menu lateral |
| `/integrations/freshness` | ✅ | ❌ | ✅ (via nav interno) | ⚠️ Não está no menu lateral |

---

## 3. Revisão de menu

| Item no menu | Estado | Correcção |
|-------------|--------|-----------|
| "Integration Hub" | ✅ Presente | — |
| "Ingestion Executions" | ❌ Ausente | F-01: Adicionar como sub-item |
| "Data Freshness" | ❌ Ausente | F-02: Adicionar como sub-item |
| "Connectors" (gestão) | ❌ Ausente | F-03: Adicionar quando CRUD existir |

---

## 4. Revisão de formulários

| Formulário | Página | Estado | Gap |
|-----------|--------|--------|-----|
| Criar conector | — | ❌ Não existe | F-04: Necessário quando POST API existir |
| Editar conector | ConnectorDetail | ❌ Não existe | F-05: Necessário quando PUT API existir |
| Criar fonte de ingestão | — | ❌ Não existe | F-06: Necessário quando POST API existir |
| Editar fonte | — | ❌ Não existe | F-07: Necessário quando PUT API existir |
| Configurar retry policy | — | ❌ Não existe | F-08: Campo no form de edição de conector |
| Configurar webhook | — | ❌ Não existe | F-09: Necessário para setup de webhooks |

---

## 5. Revisão de telas existentes

### IntegrationHubPage ✅
- ✅ Cards de summary (total, healthy, degraded, stale)
- ✅ Tabela de conectores com filtros (type, status, environment)
- ✅ Search por nome
- ✅ Badges de status e health com cores
- ✅ Responsive (mobile layout)
- ✅ Link para detalhe
- ⚠️ Sem acção de "criar conector" (botão não existe)

### ConnectorDetailPage ✅
- ✅ Tabs: Overview, Configuration, Executions, Health
- ✅ Métricas: total/success/failed executions
- ✅ Timeline de execuções recentes
- ✅ Botão retry (com permissão `integrations:write`)
- ✅ Badge de health com feedback visual
- ⚠️ Tab "Configuration" é read-only — sem form de edição
- ⚠️ Sem botão de "edit", "activate", "disable", "delete"

### IngestionExecutionsPage ✅
- ✅ Tabela com filtros por connector, source, result, date range
- ✅ Paginação
- ✅ Botão reprocess (com permissão)
- ✅ Exibição de correlation ID, duration, items processed
- ⚠️ Sem filtro por retry attempt

### IngestionFreshnessPage ✅
- ✅ Tabela por domínio com freshness status
- ✅ Badges de freshness (Fresh, Stale, Outdated, Expired)
- ✅ Lag em minutos formatado
- ✅ Trust level exibido
- ⚠️ Sem gráfico de freshness ao longo do tempo (futuro ClickHouse)

---

## 6. Revisão de telas de status/health/histórico

| Tela/Componente | Estado | Gap |
|----------------|--------|-----|
| Health badges nos connectors | ✅ | — |
| Health summary no hub | ✅ | — |
| Freshness badges | ✅ | — |
| Freshness page dedicada | ✅ | — |
| Health history (timeline) | ❌ | F-10: Sem histórico de transições de health |
| Execution success rate chart | ❌ | F-11: Sem gráfico de taxa de sucesso |
| Connector uptime dashboard | ❌ | F-12: Sem dashboard de uptime |

---

## 7. Revisão de integração com API real

| Endpoint | API client | Chamada | Funcional |
|----------|-----------|---------|-----------|
| GET connectors | ✅ `getConnectors()` | ✅ React Query | ✅ |
| GET connector/{id} | ✅ `getConnectorDetail()` | ✅ React Query | ✅ |
| GET sources | ✅ `getSources()` | ✅ React Query | ✅ |
| GET executions | ✅ `getExecutions()` | ✅ React Query | ✅ |
| GET health | ✅ `getHealth()` | ✅ React Query | ✅ |
| GET freshness | ✅ `getFreshness()` | ✅ React Query | ✅ |
| POST retry | ✅ `retryConnector()` | ✅ useMutation | ✅ |
| POST reprocess | ✅ `reprocessExecution()` | ✅ useMutation | ✅ |

**Nota:** API client está em `features/integrations/api/integrations.ts` — já é dedicado ao módulo. ✅

---

## 8. Revisão de i18n

| Aspecto | Estado |
|---------|--------|
| Chaves em `en.json` | ✅ 60+ keys no namespace `integrations` |
| Chaves em `pt.json` | ✅ Traduzidas |
| Títulos | ✅ `hubTitle`, `hubSubtitle` |
| Colunas de tabela | ✅ Todas as column headers |
| Status labels | ✅ active, disabled, paused, healthy, degraded, etc. |
| Freshness labels | ✅ fresh, stale, outdated, expired, unknown |
| Empty states | ⚠️ `noConnectors` existe, mas faltam empty states para sources e executions |
| Error states | ⚠️ Genérico — não há keys específicas para erros de integração |
| Loading states | ✅ Padrão do React Query |

---

## 9. Revisão de botões sem acção

| Botão | Página | Estado |
|-------|--------|--------|
| Retry | ConnectorDetail | ⚠️ Funciona mas sem efeito real (backend não processa) |
| Reprocess | IngestionExecutions | ⚠️ Funciona mas sem efeito real (backend não processa) |
| Criar conector | — | ❌ Não existe |
| Editar conector | — | ❌ Não existe |
| Activar/Desactivar | — | ❌ Não existe |
| Eliminar conector | — | ❌ Não existe |
| Testar conexão | — | ❌ Não existe |

---

## 10. Revisão de placeholders e campos técnicos

| Problema | Localização | Correcção |
|----------|------------|-----------|
| F-13 | CorrelationId exibido como `exec-...` truncado | Exibir completo com tooltip |
| F-14 | AuthenticationMode "Not configured" como string raw | Usar label i18n |
| F-15 | PollingMode "Not configured" como string raw | Usar label i18n |
| F-16 | AllowedTeams como array JSON raw | Usar chips/badges formatados |

---

## 11. Backlog de correcções frontend

| # | Item | Prioridade | Tipo | Esforço |
|---|------|-----------|------|---------|
| F-01 | Adicionar "Executions" ao menu lateral | 🟢 P3_MEDIUM | QUICK_WIN | 30min |
| F-02 | Adicionar "Data Freshness" ao menu lateral | 🟢 P3_MEDIUM | QUICK_WIN | 30min |
| F-03 | Botão "New Connector" no hub (quando POST existir) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| F-04 | Formulário de criação de conector (modal/page) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 8h |
| F-05 | Formulário de edição de conector | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 6h |
| F-06 | Formulário de criação de fonte de ingestão | 🟡 P2_HIGH | FUNCTIONAL_FIX | 4h |
| F-07 | Formulário de edição de fonte | 🟡 P2_HIGH | FUNCTIONAL_FIX | 4h |
| F-08 | Campos de retry policy no form de edição | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| F-09 | Tela de configuração de webhook por conector | 🟡 P2_HIGH | FUNCTIONAL_FIX | 6h |
| F-10 | Timeline de histórico de health de conector | 🟢 P3_MEDIUM | FUNCTIONAL_FIX | 4h |
| F-11 | Gráfico de taxa de sucesso de execuções | 🟢 P3_MEDIUM | FUNCTIONAL_FIX | 4h |
| F-12 | Botões activate/disable/delete no detalhe | 🟡 P2_HIGH | FUNCTIONAL_FIX | 3h |
| F-13 | CorrelationId com tooltip completo | 🟢 P4_LOW | QUICK_WIN | 30min |
| F-14 | AuthenticationMode com label i18n | 🟢 P3_MEDIUM | QUICK_WIN | 30min |
| F-15 | PollingMode com label i18n | 🟢 P3_MEDIUM | QUICK_WIN | 30min |
| F-16 | AllowedTeams como chips formatados | 🟢 P3_MEDIUM | QUICK_WIN | 1h |
| F-17 | Empty states para sources e executions | 🟢 P3_MEDIUM | QUICK_WIN | 1h |
| F-18 | Keys i18n específicas para erros de integração | 🟢 P3_MEDIUM | QUICK_WIN | 1h |

**Total estimado: ~48h**
