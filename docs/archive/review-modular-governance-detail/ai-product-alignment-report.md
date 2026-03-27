# Auditoria — Alinhamento do Módulo de IA com a Visão do Produto NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** BEM_ALINHADO — módulo de IA respeita a visão do produto com lacunas parciais em contexto e tools.

---

## 1. Resumo

O módulo de IA do NexTraceOne está **fortemente alinhado** com a visão oficial do produto como fonte de verdade governada. A IA interna é o padrão (Ollama local), a IA externa é opcional e governada (OpenAI com controlo de política), os agentes são especializados nos domínios do produto e a auditoria é completa. As lacunas são de completude, não de direcção.

---

## 2. Avaliação por capacidade do produto

### 2.1 Capacidades de IA definidas no produto

| Capacidade | Estado | Evidência | Classificação |
|---|---|---|---|
| Chat operacional | ✅ SIM | `AiAssistantPage.tsx`, `ExecuteAiChat`, inferência real Ollama/OpenAI | FUNCIONAL |
| Assistência contextual | ⚠️ PARCIAL | Toggles de contexto existem (Services, Contracts, Incidents, Changes, Runbooks), grounding pode ser parcial | PARCIAL |
| Geração de contratos | ✅ SIM | 4 agentes de geração: API Contract Draft Generator (OpenAPI), Kafka Schema (Avro), SOAP Contract (WSDL), API Test Scenario | FUNCIONAL |
| Agentes especializados | ✅ SIM | 10 agentes oficiais cobrindo 6 domínios | FUNCIONAL |
| Agentes criados pelo utilizador | ✅ SIM | `createAgent` API, formulário na UI, OwnershipType=User | FUNCIONAL |
| Escolha de modelos | ✅ SIM | Selector na UI, modelos agrupados por interno/externo, política de acesso | FUNCIONAL |
| Separação interna/externa | ✅ SIM | IsInternal/IsExternal em modelos e providers, controlo por política | FUNCIONAL |
| Human-in-the-loop | ⚠️ PARCIAL | `AiAgentArtifact.ReviewStatus` (PendingReview/Approved/Rejected), sem workflow formal | PARCIAL |
| Reutilização de conhecimento | ⚠️ PARCIAL | `KnowledgeCaptureEntry` existe, `AIKnowledgeSource` configurável, workflow pouco claro | PARCIAL |
| Governança de IA | ✅ SIM | Políticas, quotas, auditoria, routing, model registry | FUNCIONAL |
| IDE integrations | ⚠️ PARCIAL | DB + UI existem (`IdeIntegrationsPage.tsx`), extensão real ausente do repositório | PARCIAL |

---

## 3. Alinhamento com os pilares oficiais

### 3.1 Pilar: AI-assisted Operations

| Requisito do pilar | Estado | Detalhe |
|---|---|---|
| Investigar problemas em produção | ✅ | Agentes: Service Health Analyzer, Incident Root Cause Investigator |
| Correlacionar incidentes | ⚠️ | Agente existe mas correlação depende de contexto real (tools/retrieval) |
| Sugerir causa provável | ✅ | Incident Root Cause Investigator |
| Recomendar mitigação | ⚠️ | Agente pode recomendar mas sem acesso real a dados de produção (tools) |
| Consultar telemetry, incidents, changes, contracts, topology, runbooks | ⚠️ | Toggles de contexto existem; retrieval possivelmente parcial |

### 3.2 Pilar: AI Governance & Developer Acceleration

| Requisito do pilar | Estado | Detalhe |
|---|---|---|
| IA interna local como padrão | ✅ | Ollama local activo por defeito |
| Integração com IAs externas | ✅ | OpenAI/Azure/Gemini configuráveis (inactivos por defeito) |
| Controle por utilizador, grupo, perfil | ✅ | AIAccessPolicy com scope Role/User/Team/Tenant |
| Quotas e budgets de tokens | ✅ | AiTokenQuotaPolicy + AiTokenUsageLedger |
| Model registry | ✅ | AIModel com 40+ propriedades, ModelRegistryPage |
| Permitir adicionar/remover modelos | ⚠️ | Backend suporta, UI de registo desactivada |
| Bloquear modelos por política | ✅ | BlockedModelIds na AIAccessPolicy |
| Auditar uso completo | ✅ | AIUsageEntry, AiExternalInferenceRecord, AiAuditPage |

### 3.3 Pilar: Contract Governance (via IA)

| Requisito | Estado | Detalhe |
|---|---|---|
| Criação assistida por IA | ✅ | API Contract Draft Generator, Kafka Schema Designer, SOAP Contract Author |
| Geração de artefactos reais | ✅ | OpenAPI YAML, Avro/JSON Schema, WSDL |
| Validação por IA | ⚠️ | Agentes podem sugerir mas sem integração com pipeline de validação |
| Sugerir schemas | ✅ | Kafka Schema Contract Designer |
| Explicar APIs, tópicos, serviços | ⚠️ | Chat pode explicar mas grounding depende de contexto real |

### 3.4 Pilar: Source of Truth & Operational Knowledge

| Requisito | Estado | Detalhe |
|---|---|---|
| IA com contexto do produto | ⚠️ | Toggles existem, grounding real possivelmente parcial |
| Knowledge capture | ⚠️ | KnowledgeCaptureEntry existe, workflow incerto |
| Search/command palette | ⚠️ | Não confirmado se IA está integrada com busca global |

---

## 4. Alinhamento com personas oficiais

| Persona | Experiência de IA | Estado |
|---|---|---|
| Engineer | Chat, agentes, geração de contratos | ✅ Coberto |
| Tech Lead | Governança de modelos, políticas, agentes | ✅ Coberto |
| Architect | Geração de contratos, análise de impacto | ✅ Coberto |
| Product | Visão de uso de IA, agentes de negócio | ⚠️ Sem vista diferenciada |
| Executive | Dashboards de uso, custo, tendências | ⚠️ Sem vista executiva de IA |
| Platform Admin | Configuração de providers, modelos, políticas | ✅ Coberto |
| Auditor | Auditoria completa de uso de IA | ✅ Coberto |

---

## 5. Regras obrigatórias do produto — verificação

### 5.1 Regra: IA interna como padrão

| Verificação | Resultado |
|---|---|
| Ollama local activo por defeito | ✅ CONFORME |
| OpenAI requer configuração explícita | ✅ CONFORME |
| InternalOnly como opção de política | ✅ CONFORME |

### 5.2 Regra: IA externa governada

| Verificação | Resultado |
|---|---|
| AllowExternalAI controlado por política | ✅ CONFORME |
| Auditoria de chamadas externas | ✅ CONFORME |
| Custo rastreado por chamada | ✅ CONFORME |

### 5.3 Regra: Sem chat genérico sem contexto/segurança/auditoria

| Verificação | Resultado |
|---|---|
| Contexto do produto (toggles) | ✅ CONFORME |
| Segurança (políticas de acesso) | ✅ CONFORME |
| Auditoria | ✅ CONFORME |
| Governança de acesso | ✅ CONFORME |
| Política de modelo | ✅ CONFORME |
| i18n na UI | ✅ CONFORME (100+ chaves) |

### 5.4 Regra: i18n obrigatório

| Verificação | Resultado |
|---|---|
| Chaves i18n no módulo de IA | ✅ 100+ chaves |
| Textos hardcoded identificados | ⚠️ Possíveis textos hardcoded em componentes menores |

---

## 6. Pontuação de alinhamento

| Dimensão | Peso | Pontuação | Resultado |
|---|---|---|---|
| Chat operacional | 15% | 9/10 | 1.35 |
| Assistência contextual | 15% | 5/10 | 0.75 |
| Geração de contratos | 10% | 8/10 | 0.80 |
| Agentes especializados | 10% | 9/10 | 0.90 |
| Governança de modelos | 10% | 8/10 | 0.80 |
| Separação interna/externa | 10% | 9/10 | 0.90 |
| Auditoria | 10% | 9/10 | 0.90 |
| Human-in-the-loop | 5% | 4/10 | 0.20 |
| Knowledge reuse | 5% | 3/10 | 0.15 |
| IDE integrations | 5% | 3/10 | 0.15 |
| Vistas por persona | 5% | 4/10 | 0.20 |
| **TOTAL** | **100%** | | **7.10/10** |

**Alinhamento geral: 71%** — BEM_ALINHADO com lacunas parciais.

---

## 7. Lacunas de alinhamento

| # | Lacuna | Impacto no produto | Prioridade |
|---|---|---|---|
| 1 | Contexto real parcial | Assistência contextual limitada — agentes geram texto sem dados reais | Alta |
| 2 | Tools não executados | Agentes não podem consultar dados reais (telemetria, incidentes, serviços) | Alta |
| 3 | Human-in-the-loop incompleto | Artefactos gerados sem workflow formal de aprovação | Média |
| 4 | IDE integrations ausentes | Extensões para VS Code/Visual Studio não existem no repositório | Média |
| 5 | Sem vistas por persona | Experiência de IA não diferenciada por persona | Média |
| 6 | Knowledge capture incerto | Reutilização de conhecimento operacional não claramente funcional | Média |
| 7 | Registo de modelos desactivado | Não é possível adicionar modelos pela UI | Baixa |

---

## 8. Recomendações para alinhamento

1. **Completar grounding de contexto** — garantir que toggles injectam dados reais no prompt dos agentes
2. **Implementar tools** — permitir agentes consultar telemetria, serviços, incidentes, contratos em tempo real
3. **Workflow de aprovação formal** — integrar revisão de artefactos com notificações e SLAs
4. **Desenvolver extensões IDE** — criar extensões reais para VS Code e Visual Studio
5. **Diferenciar experiência por persona** — dashboards de IA específicos por persona
6. **Clarificar knowledge capture** — documentar e completar workflow de captura de conhecimento

---

> **Veredicto:** O módulo de IA está **bem alinhado com a visão do NexTraceOne** como plataforma de IA governada, não genérica. A IA interna é o padrão, a externa é controlada, os agentes são especializados nos domínios do produto e a auditoria é completa. As lacunas são de **profundidade de integração** (contexto real, tools, IDE) e não de direcção estratégica.
