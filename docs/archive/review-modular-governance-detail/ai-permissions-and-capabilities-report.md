# Auditoria — Permissões e Capacidades de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** PARCIAL — acesso a modelos bem controlado, mas sem permissões por ferramenta nem modelo de capacidades granular.

---

## 1. Resumo

O sistema de permissões de IA do NexTraceOne implementa **4 permissões distintas**, **políticas de acesso baseadas em scope**, **quotas de tokens** e **controlo de IA externa**. O acesso a modelos é verificado via `IAiModelAuthorizationService`. A limitação principal é a ausência de permissões granulares por ferramenta/acção e a incompletude do modelo de capacidades.

---

## 2. Permissões de IA — 4 permissões distintas

| Permissão | Escopo | Páginas/Funcionalidades protegidas |
|---|---|---|
| `ai:assistant:read` | Chat e agentes | AiAssistantPage, AiAgentsPage, AgentDetailPage |
| `ai:governance:read` | Governança de IA | ModelRegistryPage, AiPoliciesPage, AiRoutingPage, TokenBudgetPage, AiAuditPage, IdeIntegrationsPage |
| `ai:runtime:write` | Execução de IA | AiAnalysisPage (análise de ambiente) |
| `platform:admin:read` | Configuração | AiIntegrationsConfigurationPage |

### Mapeamento permissão → rota

| Rota | Permissão |
|---|---|
| `/ai/assistant` | `ai:assistant:read` |
| `/ai/agents` | `ai:assistant:read` |
| `/ai/agents/:agentId` | `ai:assistant:read` |
| `/ai/models` | `ai:governance:read` |
| `/ai/policies` | `ai:governance:read` |
| `/ai/routing` | `ai:governance:read` |
| `/ai/budgets` | `ai:governance:read` |
| `/ai/audit` | `ai:governance:read` |
| `/ai/ide-integrations` | `ai:governance:read` |
| `/ai/analysis` | `ai:runtime:write` |
| `/platform/configuration/ai-integrations` | `platform:admin:read` |

---

## 3. Políticas de acesso (AIAccessPolicy)

### 3.1 Estrutura da política

| Campo | Tipo | Propósito |
|---|---|---|
| Scope | enum | Role / User / Team / Tenant |
| ScopeValue | string | Valor do scope (ex: roleId, userId, teamId) |
| AllowedModelIds | Guid[] | Modelos explicitamente permitidos |
| BlockedModelIds | Guid[] | Modelos explicitamente bloqueados |
| AllowExternalAI | bool | Permite ou bloqueia IA externa |
| InternalOnly | bool | Restringe a modelos internos apenas |
| MaxTokensPerRequest | int | Limite de tokens por pedido individual |
| EnvironmentRestrictions | string[] | Ambientes onde a política se aplica |

### 3.2 Fluxo de autorização

```
1. Pedido de uso de modelo (userId, modelId)
2. IAiModelAuthorizationService.Authorize()
3. Resolve políticas aplicáveis:
   a. Políticas de role do utilizador
   b. Políticas de user específico
   c. Políticas de team do utilizador
   d. Políticas de tenant
4. Avalia AllowedModelIds → se modelo não está na lista → Blocked
5. Avalia BlockedModelIds → se modelo está na lista → Blocked
6. Avalia AllowExternalAI → se modelo é externo e flag é false → Blocked
7. Avalia InternalOnly → se modelo é externo → Blocked
8. Avalia MaxTokensPerRequest → se excede → QuotaExceeded
9. Resultado: Allowed / Blocked / QuotaExceeded
```

### 3.3 Registo de decisão

Cada decisão de autorização gera uma `AIUsageEntry` com:
- `result`: Allowed / Blocked / QuotaExceeded
- `policyName`: Nome da política que determinou a decisão
- `correlationId`: Para rastreabilidade

---

## 4. Quotas e budgets de tokens

### 4.1 Entidades

| Entidade | Propósito |
|---|---|
| `AiTokenQuotaPolicy` | Define limites de tokens por período (diário, semanal, mensal) |
| `AiTokenUsageLedger` | Regista consumo real de tokens por utilizador/equipa |

### 4.2 Funcionalidades

| Aspecto | Estado |
|---|---|
| Definição de quotas | ✅ Entidade e API existem |
| Rastreamento de consumo | ✅ Ledger de uso implementado |
| UI de gestão | ✅ `TokenBudgetPage.tsx` (196 linhas) |
| Bloqueio por excesso | ✅ QuotaExceeded como resultado de autorização |
| Alertas de proximidade de limite | ⚠️ Não confirmado |

---

## 5. Controlo de IA externa

| Aspecto | Estado | Mecanismo |
|---|---|---|
| Bloquear IA externa | ✅ | `AllowExternalAI = false` na política |
| Forçar apenas interna | ✅ | `InternalOnly = true` na política |
| Separação interna/externa | ✅ | Modelos e providers marcados como IsInternal/IsExternal |
| UI de selecção | ✅ | Modelos agrupados por interno/externo na UI do chat |
| Auditoria | ✅ | `isInternal` registado em cada `AIUsageEntry` |
| Provider externo dedicado | ✅ | `AiExternalInferenceRecord` para chamadas externas |

---

## 6. O que está implementado vs. o que falta

### ✅ Implementado

| Capacidade | Evidência |
|---|---|
| Acesso a modelos por política | `IAiModelAuthorizationService` |
| Scope multi-nível | Role / User / Team / Tenant |
| Modelos permitidos/bloqueados | AllowedModelIds / BlockedModelIds |
| Controlo de IA externa | AllowExternalAI / InternalOnly |
| Quotas de tokens | AiTokenQuotaPolicy + AiTokenUsageLedger |
| Tokens por pedido | MaxTokensPerRequest |
| Auditoria de decisões | AIUsageEntry com resultado |
| Rotas protegidas | 4 permissões distintas no frontend |

### ⚠️ Parcialmente implementado

| Capacidade | Estado | Detalhe |
|---|---|---|
| Restrições por ambiente | ⚠️ | Campo existe mas aplicação não confirmada |
| Alertas de quota | ⚠️ | Bloqueio existe mas alertas proactivos não confirmados |

### ❌ Não implementado

| Capacidade | Impacto |
|---|---|
| Permissões por ferramenta/tool | Qualquer utilizador com `ai:assistant:read` pode usar qualquer tool (quando tools forem implementados) |
| Capacidades granulares por acção | Não é possível permitir "chat" mas bloquear "gerar contrato" |
| Permissões por agente | Todos os agentes visíveis são executáveis por qualquer utilizador com permissão base |
| Rate limiting por utilizador | Quotas de tokens existem mas sem rate limiting temporal |
| Permissões de aprovação de artefactos | Qualquer pessoa pode aprovar/rejeitar (sem role específico) |

---

## 7. Mapeamento de permissões por persona

| Persona | Permissões esperadas | Estado |
|---|---|---|
| Engineer | `ai:assistant:read` | ✅ Básico coberto |
| Tech Lead | `ai:assistant:read`, `ai:governance:read` parcial | ⚠️ Governança não segmentada |
| Architect | `ai:assistant:read`, `ai:governance:read` | ⚠️ Sem diferenciação de Tech Lead |
| Platform Admin | Todas | ✅ `platform:admin:read` cobre configuração |
| Auditor | `ai:governance:read` | ⚠️ Sem permissão específica read-only de auditoria |
| Executive | Dashboards de uso | ❌ Sem vista executiva de IA |
| Product | Visão de agentes e uso | ⚠️ Partilha permissão com Engineer |

---

## 8. Lacunas identificadas

| # | Lacuna | Severidade | Impacto |
|---|---|---|---|
| 1 | Sem permissões por tool | Alta | Quando tools forem implementados, não haverá controlo granular |
| 2 | Sem permissões por agente | Média | Todos os agentes visíveis são executáveis |
| 3 | Sem capacidades granulares por acção | Média | Não é possível segmentar funcionalidades de IA |
| 4 | Sem permissão de aprovação de artefactos | Média | Revisão de artefactos sem role específico |
| 5 | Governança não segmentada por persona | Média | `ai:governance:read` é tudo-ou-nada |
| 6 | Sem rate limiting temporal | Baixa | Quotas de tokens sem limite por minuto/hora |
| 7 | Sem vista executiva | Baixa | Sem dashboards de uso de IA por persona Executive |

---

## 9. Recomendações

### Prioridade Alta

1. **Preparar permissões por tool** — antes de implementar execução de tools, definir modelo de permissões granular
2. **Permissões por agente** — permitir controlar quais agentes cada utilizador/equipa pode executar
3. **Permissão de aprovação de artefactos** — criar role específico para revisão de artefactos gerados

### Prioridade Média

4. **Segmentar `ai:governance:read`** — dividir em sub-permissões (modelos, políticas, routing, auditoria)
5. **Capacidades por acção** — permitir diferenciar "usar chat" de "executar agente" de "gerar contrato"
6. **Rate limiting temporal** — implementar limites por minuto/hora além de quotas de tokens

### Prioridade Baixa

7. **Vista executiva** — dashboards de uso de IA para persona Executive
8. **Auditoria read-only** — permissão específica para auditores verem logs sem acesso a governança

---

> **Veredicto:** O sistema de permissões é **sólido ao nível de acesso a modelos** com políticas scope-based e quotas de tokens. A lacuna principal é a **ausência de granularidade** — quando tools e capacidades avançadas forem implementados, o modelo de permissões precisará evoluir significativamente.
