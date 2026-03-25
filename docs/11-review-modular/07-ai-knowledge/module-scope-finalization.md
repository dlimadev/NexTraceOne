# PARTE 3 — Escopo Funcional Final do Módulo

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Funcionalidades já existentes (funcionais)

| # | Funcionalidade | Ficheiros | Estado |
|---|---------------|-----------|--------|
| F-01 | Chat de IA (envio de mensagem, resposta) | `SendAssistantMessage`, `AiAssistantConversation`, `AiMessage` | ✅ Funcional (sem streaming) |
| F-02 | Gestão de conversas (criar, listar, arquivar) | `CreateConversation`, `ListConversations`, `GetConversation`, `UpdateConversation` | ✅ Funcional |
| F-03 | Histórico de mensagens | `ListMessages`, `AiMessage` persistido | ✅ Funcional |
| F-04 | Políticas de acesso a modelos | `AIAccessPolicy`, `CreatePolicy`, `ListPolicies` | ✅ Funcional |
| F-05 | Registo de uso de tokens | `AIUsageEntry`, `AiTokenUsageLedger` | ⚠️ Schema OK, registo parcial |
| F-06 | Auditoria de ações de IA | `ListAuditEntries` + AuditColumns via NexTraceDbContextBase | ⚠️ Parcial |
| F-07 | Definição de agents | `AiAgent`, `CreateAgent`, `UpdateAgent`, `ListAgents` | ✅ Schema e CRUD funcional |
| F-08 | Execução de agents (basic) | `ExecuteAgent`, `AiAgentExecution` | ⚠️ Execução registada, mas tools NÃO executam |

---

## 2. Funcionalidades parciais

| # | Funcionalidade | Estado | Lacuna |
|---|---------------|--------|--------|
| P-01 | Registo de modelos | Schema completo, CRUD backend existe | Frontend desativou registo; providers sem integração real |
| P-02 | Registo de providers | Schema completo | Health check parcial; sem teste de conectividade real |
| P-03 | Orçamentos e quotas | Schema `AIBudget`, `AiTokenQuotaPolicy` | Enforcement não verificado; alertas não emitidos |
| P-04 | Routing de modelos | `AIRoutingStrategy`, `AIRoutingDecision` | Schema existe mas execução de routing provavelmente stub |
| P-05 | Knowledge sources | `AIKnowledgeSource`, `AiSource` | Configuração existe; retrieval real incerto |
| P-06 | IDE clients | `AIIDEClientRegistration`, `AIIDECapabilityPolicy` | Schema e UI existem; sem extensões reais de IDE |
| P-07 | Context enrichment | `EnrichContext`, `AIEnrichmentResult` | Feature handler existe; grounding real incerto |
| P-08 | External AI providers | `ExternalAiProvider`, `ExternalAiPolicy` | Schema OK; integração real com APIs externas incerta |
| P-09 | Knowledge capture | `KnowledgeCapture`, `KnowledgeCaptureEntry` | Captura e aprovação existem; reutilização incerta |
| P-10 | Agent artifacts | `AiAgentArtifact`, `ReviewArtifact` | Schema e review existem; geração real de artefactos incerta |

---

## 3. Funcionalidades ausentes

| # | Funcionalidade | Criticidade | Notas |
|---|---------------|-------------|-------|
| A-01 | Streaming de respostas de chat | 🔴 ALTA | Fundamental para UX de chat — não implementado |
| A-02 | Execução real de tools por agents | 🔴 ALTA | AllowedTools declarados mas nunca executados em runtime |
| A-03 | Framework de tool calling | 🔴 ALTA | Não existe mecanismo de despacho de tools |
| A-04 | Retrieval real de conhecimento | 🟠 ALTA | Serviços de retrieval provavelmente stubs |
| A-05 | Rate limiting por utilizador/tenant | 🟠 MÉDIA | Quotas existem em schema, enforcement ausente |
| A-06 | Alertas de orçamento excedido | 🟠 MÉDIA | Sem integração com Notifications |
| A-07 | Teste de conectividade de providers | 🟠 MÉDIA | Health check parcial |
| A-08 | Extensões reais de IDE | 🟡 BAIXA (MVP) | Pode ser adiado para versão posterior |
| A-09 | KPIs de uso de IA por persona/tenant | 🟡 MÉDIA | Dados existem; dashboards/métricas não |
| A-10 | Human-in-the-loop para agents | 🟡 MÉDIA | Não implementado |

---

## 4. Classificação: obrigatório vs futuro

### 4.1 Obrigatório no produto final (MVP funcional)

| # | Funcionalidade | Prioridade |
|---|---------------|------------|
| F-01 | Chat de IA funcional | P0 |
| F-02 | Gestão de conversas | P0 |
| F-03 | Histórico de mensagens | P0 |
| F-04 | Políticas de acesso | P0 |
| F-07 | Definição de agents | P1 |
| A-01 | Streaming de chat | P1 |
| A-02 | Execução real de tools | P1 |
| A-03 | Framework de tool calling | P1 |
| P-01 | Registo de modelos (completo) | P1 |
| P-02 | Registo de providers (completo) | P1 |
| P-03 | Orçamentos com enforcement | P1 |
| F-05 | Registo de uso de tokens | P1 |
| F-06 | Auditoria de ações de IA | P1 |
| A-04 | Retrieval real de conhecimento | P2 |
| P-07 | Context enrichment real | P2 |
| A-05 | Rate limiting | P2 |

### 4.2 Importante mas pode ser faseado

| # | Funcionalidade | Prioridade |
|---|---------------|------------|
| P-04 | Routing inteligente de modelos | P2 |
| P-08 | External AI providers | P2 |
| P-09 | Knowledge capture e reutilização | P2 |
| P-10 | Agent artifacts com review | P2 |
| A-06 | Alertas de orçamento | P2 |
| A-07 | Health check de providers | P2 |
| A-09 | KPIs de uso de IA | P3 |
| A-10 | Human-in-the-loop | P3 |

### 4.3 Futuro (fora do MVP)

| # | Funcionalidade | Notas |
|---|---------------|-------|
| P-06/A-08 | IDE integrations (extensões reais) | Manter schema; esconder ou marcar como "coming soon" |

---

## 5. O que NÃO pertence ao módulo

| Funcionalidade | Dono correto |
|---------------|--------------|
| Dashboards genéricos de produto | Product Analytics |
| Conformidade organizacional | Governance |
| Notificações e alertas | Notifications (AI emite eventos) |
| Gestão de identidades | Identity & Access |
| Catálogo de serviços | Catalog |
| Contratos | Contracts |
| Incidentes | Operational Intelligence |
| Changes | Change Governance |

---

## 6. Conjunto mínimo completo do módulo final

### AI Core (mínimo)
- [x] Chat funcional com persistência
- [ ] Streaming de respostas
- [x] Gestão de conversas
- [x] Histórico de mensagens
- [x] Registo de modelos
- [x] Registo de providers
- [ ] Health check de providers
- [x] Políticas de acesso
- [x] Orçamentos e quotas
- [ ] Enforcement de quotas
- [x] Registo de token usage
- [x] Auditoria

### Agents (mínimo)
- [x] Definição de agents (CRUD)
- [x] Execução de agents (invocação)
- [ ] Framework de tool calling
- [ ] Execução real de tools
- [x] Artefactos de agents
- [ ] Review de artefactos (workflow completo)

### Knowledge (mínimo)
- [x] Knowledge sources configuráveis
- [ ] Retrieval real de conhecimento
- [ ] Context enrichment funcional
- [x] Knowledge capture
- [ ] Reutilização de knowledge

### Orchestration (mínimo)
- [ ] Pelo menos 3 análises cross-module funcionais (de 11 features)
- [ ] Integração verificada com Catalog, Change Governance, Ops Intel

---

## 7. Resumo

| Dimensão | Contagem |
|----------|----------|
| Funcionalidades existentes | 8 |
| Funcionalidades parciais | 10 |
| Funcionalidades ausentes | 10 |
| Obrigatórias no MVP | 16 |
| Faseáveis | 8 |
| Futuras (fora MVP) | 1 |
| **Maturidade funcional estimada** | **🔴 ~30%** |
