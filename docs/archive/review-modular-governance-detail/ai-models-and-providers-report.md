# Auditoria — Modelos e Providers de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** BEM_ESTRUTURADO com ACTIVAÇÃO PARCIAL — apenas Ollama activo por defeito.

---

## 1. Resumo

A camada de modelos e providers do NexTraceOne é **bem arquitectada** com entidades ricas (40+ propriedades por modelo), suporte a múltiplos modos de autenticação, políticas de acesso baseadas em scope, estratégias de routing e factory de providers. A limitação principal é a activação: apenas 1 de 4 providers e 1 de 3 modelos estão activos por defeito.

---

## 2. Entidade AIModel — análise detalhada

A entidade `AIModel` possui **40+ propriedades** cobrindo todos os aspectos de governança de modelos:

### Identificação e classificação

| Propriedade | Tipo | Propósito |
|---|---|---|
| Name | string | Nome do modelo |
| Provider | ref | Provider associado |
| ExternalModelId | string | ID no provider externo |
| ModelType | enum | Tipo (Chat, Completion, Embedding, etc.) |
| Category | string | Categoria funcional |
| IsInternal | bool | Modelo interno/local |
| IsExternal | bool | Modelo externo/cloud |
| Status | enum | Active / Inactive / Deprecated / Blocked |

### Capacidades

| Propriedade | Tipo | Propósito |
|---|---|---|
| Capabilities | string[] | Capacidades declaradas |
| DefaultUseCases | string[] | Casos de uso recomendados |
| SupportsStreaming | bool | Suporte a streaming |
| SupportsToolCalling | bool | Suporte a tool calling |
| SupportsVision | bool | Suporte a visão |
| SupportsStructuredOutput | bool | Suporte a saída estruturada |
| ContextWindow | int | Tamanho da janela de contexto |
| RequiresGpu | bool | Requisito de GPU |

### Governança

| Propriedade | Tipo | Propósito |
|---|---|---|
| SensitivityLevel | enum | Nível de sensibilidade |
| IsDefaultForChat | bool | Padrão para chat |
| IsDefaultForReasoning | bool | Padrão para raciocínio |
| IsDefaultForEmbeddings | bool | Padrão para embeddings |
| LicenseName | string | Nome da licença |
| ComplianceStatus | enum | Estado de conformidade |

---

## 3. Entidade AiProvider — análise detalhada

| Propriedade | Tipo | Propósito |
|---|---|---|
| ProviderType | enum | Tipo do provider |
| BaseUrl | string | URL base da API |
| IsLocal | bool | Provider local |
| IsExternal | bool | Provider externo |
| AuthenticationMode | enum | NoAuth / ApiKey / OAuth2 / MutualTLS |
| HealthStatus | enum | Estado de saúde |
| Priority | int | Prioridade de routing |
| TimeoutSeconds | int | Timeout de chamada |

### Modos de autenticação suportados

| Modo | Descrição | Utilização |
|---|---|---|
| NoAuth | Sem autenticação | Ollama local |
| ApiKey | Chave de API | OpenAI, Azure OpenAI |
| OAuth2 | OAuth 2.0 | Providers empresariais |
| MutualTLS | TLS mútuo | Ambientes de alta segurança |

---

## 4. Providers semeados

| Provider | Estado | Tipo | Notas |
|---|---|---|---|
| Ollama | ✅ Activo | Local | `http://localhost:11434`, sem autenticação |
| OpenAI | ❌ Inactivo | Externo | Requer API key |
| Azure OpenAI | ❌ Inactivo | Externo | Requer API key |
| Google Gemini | ❌ Inactivo | Externo | Requer API key |

---

## 5. Modelos semeados

| Modelo | Provider | Estado | Tipo | Detalhes |
|---|---|---|---|---|
| DeepSeek R1 1.5B | Ollama | ✅ Activo | Local/Interno | Modelo padrão, `deepseek-r1:1.5b` |
| GPT-4o-mini | OpenAI | ❌ Inactivo | Externo | Requer provider OpenAI activo |
| GPT-4o | OpenAI | ❌ Inactivo | Externo | Requer provider OpenAI activo |

---

## 6. Políticas de acesso (AIAccessPolicy)

| Aspecto | Detalhe |
|---|---|
| Scope | Role / User / Team / Tenant |
| AllowedModelIds | Lista de modelos permitidos |
| BlockedModelIds | Lista de modelos bloqueados |
| AllowExternalAI | Controlo de IA externa |
| InternalOnly | Restrição a modelos internos |
| MaxTokensPerRequest | Limite de tokens por pedido |
| EnvironmentRestrictions | Restrições por ambiente |

### Fluxo de verificação

```
Pedido do utilizador
  → IAiModelAuthorizationService.Authorize(userId, modelId)
    → Resolve políticas aplicáveis (por role, user, team, tenant)
    → Verifica AllowedModelIds / BlockedModelIds
    → Verifica AllowExternalAI / InternalOnly
    → Verifica MaxTokensPerRequest
    → Retorna Allowed / Blocked / QuotaExceeded
```

---

## 7. Routing de modelos

| Entidade | Propósito |
|---|---|
| `AIRoutingStrategy` | Define estratégias de selecção de modelo |
| `AIRoutingDecision` | Regista decisões de routing tomadas |

| Campo (Decision) | Propósito |
|---|---|
| SourceContext | Contexto de origem do pedido |
| SelectedProvider | Provider seleccionado |
| SelectedModel | Modelo seleccionado |
| Confidence | Nível de confiança da decisão |
| ExecutedAt | Timestamp da execução |
| Cost | Custo da chamada |
| TokensUsed | Tokens consumidos |

---

## 8. Provider Factory

| Aspecto | Detalhe |
|---|---|
| Interface | `IAiProviderFactory` |
| Resolução | Por ID do provider |
| OpenAI | Condicional à existência de API key |
| Ollama | Disponível por defeito |

---

## 9. Frontend — ModelRegistryPage.tsx

| Aspecto | Estado |
|---|---|
| Ficheiro | `src/frontend/…/ModelRegistryPage.tsx` (243 linhas) |
| Listagem de modelos | ✅ Funcional |
| Detalhes do modelo | ✅ Funcional |
| Botão "Register Model" | ❌ **Desactivado** (placeholder) |
| Routing (AiRoutingPage) | ✅ Funcional (462 linhas) |

---

## 10. Lacunas identificadas

| # | Lacuna | Severidade | Impacto |
|---|---|---|---|
| 1 | 3/4 providers inactivos | Alta | Apenas Ollama local disponível por defeito |
| 2 | Registo de modelos via UI desactivado | Média | Não é possível adicionar modelos pela interface |
| 3 | Sem execução de embeddings | Média | Flag SupportsEmbeddings existe mas endpoint não implementado |
| 4 | Sem streaming | Média | SupportsStreaming como propriedade mas não implementado |
| 5 | Apenas 1 modelo activo | Média | Experiência limitada para utilizadores |

---

## 11. Recomendações

1. **Documentar processo de activação de providers** — criar guia para configurar API keys de OpenAI, Azure OpenAI e Google Gemini
2. **Activar registo de modelos via UI** — implementar funcionalidade do botão "Register Model"
3. **Implementar endpoint de embeddings** — completar suporte a embeddings para RAG
4. **Adicionar modelos locais adicionais** — semear mais modelos Ollama (Llama 3, Mistral, etc.)
5. **Health check automático** — implementar verificação periódica de saúde dos providers

---

> **Veredicto:** A arquitectura de modelos e providers é **sólida e bem pensada**, com entidades ricas e governança completa. A limitação é de **activação**, não de estrutura — activar providers adicionais e habilitar o registo de modelos elevaria significativamente a maturidade.
