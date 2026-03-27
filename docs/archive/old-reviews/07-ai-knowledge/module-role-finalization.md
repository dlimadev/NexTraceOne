# PARTE 1 — Papel Final do Módulo AI & Knowledge

> **Módulo:** AI & Knowledge (07)
> **Prefixo:** `aik_`
> **Estado atual:** ~25% backend, ~70% frontend
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Papel do módulo no NexTraceOne

O módulo **AI & Knowledge** é o **núcleo real de inteligência artificial, conhecimento e agents do NexTraceOne**. É responsável por:

- Toda a interação de IA do produto (chat, análise, geração)
- Gestão de modelos e providers de IA
- Execução governada e auditável de agents
- Captura e reutilização de conhecimento operacional
- Retrieval/grounding de contexto a partir de dados do produto
- Controlo de acesso, orçamento e políticas de uso de IA
- Integração com IDEs (VS Code, Visual Studio)
- Rastreabilidade completa de uso e custo de IA

---

## 2. Ownership confirmado

| Capacidade | Dono | Estado |
|------------|------|--------|
| Chat de IA com utilizador | ✅ AI & Knowledge | ⚠️ Funcional parcial — streaming não implementado |
| Registo e gestão de modelos LLM | ✅ AI & Knowledge | ⚠️ Schema completo, registo desativado no frontend |
| Registo e gestão de providers | ✅ AI & Knowledge | ⚠️ Schema completo, health check parcial |
| Políticas de acesso a IA | ✅ AI & Knowledge | ✅ Funcional — AIAccessPolicy com scope |
| Orçamentos e quotas de tokens | ✅ AI & Knowledge | ⚠️ Schema completo, enforcement parcial |
| Agents (definição, execução, artefactos) | ✅ AI & Knowledge | ⚠️ Definição OK, tools NÃO executam realmente |
| Retrieval/knowledge grounding | ✅ AI & Knowledge | ❌ Serviços de retrieval possivelmente stubs |
| Memória/histórico de conversas | ✅ AI & Knowledge | ✅ Persistência em AiAssistantConversation + AiMessage |
| Captura de conhecimento externo | ✅ AI & Knowledge | ⚠️ ExternalAI subdomain — parcial |
| Auditoria de uso de IA | ✅ AI & Knowledge | ⚠️ AIUsageEntry + AiTokenUsageLedger — parcial |
| Routing de modelos | ✅ AI & Knowledge | ⚠️ Schema de estratégias — execução incerta |
| Integração com IDEs | ✅ AI & Knowledge | ❌ UI-only — sem extensões reais |
| Análise assistida (change, catalog, incidents) | ✅ AI & Knowledge | ⚠️ Orchestration features — maioria stubs |
| KPIs de uso de IA por persona/tenant | ✅ AI & Knowledge | ❌ Não implementado |

---

## 3. O que o módulo NÃO deve ser dono

| Capacidade | Dono correto | Motivo |
|------------|--------------|--------|
| Métricas de adoção do produto | Product Analytics | Métricas de uso do produto ≠ métricas de uso de IA |
| Conformidade/compliance de políticas | Governance | Conformidade organizacional ≠ políticas de IA |
| Auditoria de segurança geral | Audit & Compliance | Audit trail de IA vive aqui, mas auditoria central é de Audit |
| Notificações de alerta de IA | Notifications | AI & Knowledge emite eventos, Notifications entrega |
| Gestão de identidades e permissões base | Identity & Access | AI usa permissões do IAM |
| Catálogo de serviços | Catalog | AI consulta o catálogo, não o gere |
| Gestão de contratos | Contracts | AI pode analisar contratos, não os gere |
| Gestão de incidentes | Operational Intelligence | AI assiste na investigação, não gere incidentes |
| Gestão de changes | Change Governance | AI classifica changes, não as gere |

---

## 4. Relação com Governance

| Aspecto | AI & Knowledge | Governance |
|---------|---------------|------------|
| Políticas de acesso a modelos | ✅ AIAccessPolicy | ❌ Não intervém |
| Orçamentos de tokens | ✅ AIBudget, AiTokenQuotaPolicy | ❌ Não intervém |
| Conformidade organizacional | ❌ Não intervém | ✅ Dono |
| Reports de compliance | ❌ Não intervém | ✅ Dono |
| Métricas de uso de IA | ✅ AIUsageEntry | ❌ Pode consumir via eventos |
| Auditoria de execução de agents | ✅ AiAgentExecution | ⚠️ Pode replicar para audit trail central |

**Regra clara:** AI & Knowledge é autónomo em governance de IA. Governance (módulo 08) NÃO intervém nas políticas de modelos, agents ou providers. Apenas consome eventos de uso de IA para reports de compliance organizacional.

---

## 5. Dependências principais

### 5.1 Módulos que AI & Knowledge consome

| Módulo | O que consome | Interface |
|--------|--------------|-----------|
| Identity & Access | UserId, TenantId, Roles, Permissions | JWT Claims + Permission checks |
| Catalog | ServiceDefinition (para grounding/contexto) | Integration Events / Query |
| Contracts | ContractDefinition (para análise de contratos) | Integration Events / Query |
| Change Governance | ChangeRequest (para classificação AI) | Integration Events |
| Operational Intelligence | IncidentRecord (para investigação AI) | Integration Events |
| Configuration | Feature flags de IA | Configuration API |

### 5.2 Módulos que consomem AI & Knowledge

| Módulo | O que consome | Interface |
|--------|--------------|-----------|
| Catalog | Análise AI de serviços | Orchestration Features |
| Change Governance | Classificação AI de changes | Orchestration Features |
| Operational Intelligence | Investigação AI de incidentes | Orchestration Features |
| Audit & Compliance | Eventos de uso de IA | Integration Events |
| Product Analytics | Métricas de uso de IA | Integration Events (futuro) |
| Notifications | Alertas de orçamento/quota | Integration Events (futuro) |

---

## 6. Estrutura interna do módulo

O módulo organiza-se em **4 subdomínios** (detalhados na PARTE 2):

```
AI & Knowledge
├── Governance    → Modelos, providers, políticas, orçamentos, agents, audit
├── Runtime       → Execução de chat, health check, token usage
├── Orchestration → Análise assistida, geração de artefactos, contexto
└── ExternalAI    → Providers externos, consultas externas, knowledge capture
```

---

## 7. Resumo

| Dimensão | Estado |
|----------|--------|
| Papel definido | ✅ Claro — núcleo de IA do NexTraceOne |
| Ownership confirmado | ✅ 14 capacidades confirmadas |
| Fronteira com Governance | ✅ Clara — AI autónomo em governance de IA |
| Dependências mapeadas | ✅ 6 consumos, 6 consumidores |
| Maturidade real | 🔴 ~25% backend vs 70% frontend |
| Risco principal | 🔴 Frontend sugere capacidades que o backend não entrega |
