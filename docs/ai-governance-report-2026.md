# NexTraceOne — Relatório de Governança de IA
**Data:** 28 de Maio de 2026  
**Versão:** 1.0  
**Classificação:** Interno — Arquitetura

---

## Sumário Executivo

Este relatório apresenta o estado real da implementação de IA no NexTraceOne, identifica riscos críticos de conformidade geopolítica, falhas de implementação e lacunas arquiteturais. São propostas mudanças concretas com localização exata nos arquivos de código.

**Achados críticos:**
- **3 modelos de origem chinesa** estão configurados como padrão e recomendados no sistema
- **Nenhum toggle de ativação/desativação de IA** existe ao nível de tenant ou plataforma
- **O provider Anthropic está implementado mas não configurado** no `appsettings.json`
- **O controle de quota de tokens não cobre todos os pontos de consumo externo**
- **Rate limiting de IA particiona por IP**, não por tenant — facilmente contornável

---

## Parte 1 — Inventário do Stack de IA

### 1.1 Provedores Implementados

| Provider | Tipo | Arquivos | Estado |
|----------|------|---------|--------|
| **Ollama** | Interno / Local | `Runtime/Providers/Ollama/OllamaProvider.cs` | ✅ Operacional |
| **OpenAI** | Externo | `Runtime/Providers/OpenAI/OpenAiProvider.cs` | ✅ Configurável |
| **Anthropic** | Externo | `Runtime/Providers/Anthropic/AnthropicProvider.cs` | ⚠️ Implementado, sem config |
| **LM Studio** | Interno / Local | `Runtime/Providers/LmStudio/LmStudioProvider.cs` | ⚠️ Opcional |

### 1.2 Modelos no Catálogo Padrão (`DefaultModelCatalog.cs`)

| Modelo | Provider | Origem | IsDefault | Risco |
|--------|----------|--------|-----------|-------|
| `deepseek-r1:1.5b` | Ollama | 🔴 China (DeepSeek) | Reasoning | **CRÍTICO** |
| `llama3.2:3b` | Ollama | 🟢 EUA (Meta) | — | OK |
| `nomic-embed-text` | Ollama | 🟢 EUA (Nomic) | Embeddings | OK |
| `codellama:7b` | Ollama | 🟢 EUA (Meta) | — | OK |
| `gpt-4o` | OpenAI | 🟢 EUA (OpenAI) | — | OK |
| `gpt-4o-mini` | OpenAI | 🟢 EUA (OpenAI) | — | OK |
| `claude-3-5-sonnet` | Anthropic | 🟢 EUA (Anthropic) | — | OK |
| `claude-opus-4-7` | Anthropic | 🟢 EUA (Anthropic) | Reasoning | OK |
| `claude-sonnet-4-6` | Anthropic | 🟢 EUA (Anthropic) | Chat | OK |
| `claude-haiku-4-5` | Anthropic | 🟢 EUA (Anthropic) | — | OK |

### 1.3 Configuração Ativa (`appsettings.json`)

```json
"AiRuntime": {
  "Ollama": {
    "DefaultChatModel": "qwen3.5:9b",   // 🔴 ORIGEM CHINESA — Alibaba
    "Enabled": true
  },
  "OpenAI": {
    "DefaultChatModel": "gpt-4o-mini",
    "Enabled": false
  },
  "Routing": {
    "PreferredProvider": "ollama",
    "PreferredChatModel": "qwen3.5:9b"  // 🔴 ORIGEM CHINESA — Alibaba
  }
}
```

### 1.4 Componentes de Infraestrutura de IA

| Componente | Arquivo | Estado |
|-----------|---------|--------|
| Vector Store (Qdrant) | `Runtime/Services/VectorStore/QdrantVectorStoreRepository.cs` | ✅ Com fallback Null |
| Token Counter | `Runtime/Services/TokenCounterService.cs` | ⚠️ Usa cl100k_base para todos os modelos |
| RAG / Grounding | `Runtime/Services/RagGroundingService.cs` | ✅ Operacional |
| Semantic Kernel | `Runtime/Services/SemanticKernel/` | ✅ Operacional |
| AI Token Quota | `Application/Runtime/Abstractions/IAiTokenQuotaService.cs` | ⚠️ Parcialmente wired |
| Routing Port | `Infrastructure/Runtime/Services/ExternalAiRoutingPortAdapter.cs` | ✅ Com policy check |

### 1.5 Background Jobs de IA

| Job | Frequência | Função |
|----|-----------|--------|
| `EmbeddingIndexJob` | 30 min | Gera embeddings para fontes sem índice |
| `QdrantIndexJob` | 30 min | Sincroniza embeddings com Qdrant |
| `ExternalDataSourceSyncJob` | Configurável | Sincroniza fontes externas |
| `ProactiveArchitectureGuardianJob` | Configurável | Guardian proativo de arquitetura |
| `FeedbackThresholdJob` | Configurável | Avalia limiares de feedback |
| `TrajectoryExporterJob` | Configurável | Exporta trajetórias de agentes |
| `AiDataRetentionJob` | Configurável | Retenção e limpeza de dados de IA |

### 1.6 Frontend — AI Hub

19 páginas/componentes em `src/frontend/src/features/ai-hub/`:
`AiCopilotPage`, `ModelRegistryPage`, `AiAgentsPage`, `AgentDetailPage`,
`AiIntegrationsConfigurationPage`, `AiRoutingPage`, `AiPoliciesPage`,
`AiAuditPage`, `TokenBudgetPage`, `AiAnalysisPage`, `AiMemoryIntelligencePage`,
`AgentMarketplacePage`, `McpServerPage`, `AssistantPanel`, `ChatSidebar`, etc.

---

## Parte 2 — Problemas Críticos: Modelos de Origem Chinesa

### 2.1 Inventário Completo de Modelos Chineses no Codebase

#### 🔴 CRÍTICO — `appsettings.json`
**Arquivo:** `src/platform/NexTraceOne.ApiHost/appsettings.json`

```json
"DefaultChatModel": "qwen3.5:9b"   // linhas ~18 e ~27
"PreferredChatModel": "qwen3.5:9b" // linha ~32
```

**Qwen** é desenvolvido pela **Alibaba Cloud** (企业阿里巴巴集团), empresa chinesa com sede em Hangzhou, China. O modelo `qwen3.5:9b` está registrado como o modelo padrão de chat e o modelo preferido de roteamento.

#### 🔴 CRÍTICO — `OllamaOptions.cs`
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Configuration/OllamaOptions.cs:21`

```csharp
public string DefaultChatModel { get; set; } = "deepseek-r1:1.5b";
```

**DeepSeek** é desenvolvido pela **DeepSeek AI** (深度求索), empresa chinesa fundada pelo High-Flyer Quant, com sede em Hangzhou, China.

#### 🔴 CRÍTICO — `DefaultModelCatalog.cs`
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/DefaultModelCatalog.cs:49-67`

```csharp
new ModelDefinition(
    Name: "deepseek-r1:1.5b",
    DisplayName: "DeepSeek R1 1.5B",
    Provider: "Ollama",
    ...
    LicenseName: "MIT"),
```

O modelo `deepseek-r1:1.5b` está no catálogo de seed com `IsDefaultForReasoning: false`, mas ainda é propagado para o Model Registry no primeiro startup.

#### 🟠 ALTO — `HardwareAssessmentService.cs`
**Arquivo:** `src/platform/NexTraceOne.ApiHost/OnPrem/HardwareAssessmentService.cs:14-17`

```csharp
new("deepseek-r1:1.5b", "DeepSeek R1 1.5B", 1.1, 2.0, "Padrão recomendado — ..."),
new("deepseek-r1:7b",   "DeepSeek R1 7B",   4.7, 7.0, "Excelente relação ..."),
new("qwen2.5:7b",       "Qwen 2.5 7B",      4.7, 7.0, "Alta qualidade em ..."),
```

Três modelos chineses são apresentados como recomendados no assessment de hardware on-prem. O `deepseek-r1:1.5b` é descrito como "Padrão recomendado".

#### 🟡 MÉDIO — Testes Unitários
**Arquivos:**
- `tests/modules/aiknowledge/.../SendAssistantMessageLlmTests.cs`: `private const string ModelId = "qwen3.5:9b";`
- `tests/modules/aiknowledge/.../ConversationQueryTests.cs`: `AiMessage.AssistantMessage(..., "qwen3.5:9b", "ollama", ...)`
- `tests/modules/aiknowledge/.../TokenCounterServiceTests.cs`: `_sut.CountTokens(text, "qwen2.5-coder-32b")`

Testes usam modelos chineses como constantes. Devem ser atualizados para usar modelos aprovados.

### 2.2 Classificação de Origem dos Modelos

| Modelo | Empresa | País | Uso atual | Ação |
|--------|---------|------|-----------|------|
| `qwen3.5:9b` | Alibaba Cloud | 🇨🇳 China | **Default chat + routing** | 🔴 Remover imediatamente |
| `qwen2.5:7b` | Alibaba Cloud | 🇨🇳 China | Recomendado on-prem | 🔴 Remover |
| `deepseek-r1:1.5b` | DeepSeek AI | 🇨🇳 China | **Default Ollama + seed** | 🔴 Remover imediatamente |
| `deepseek-r1:7b` | DeepSeek AI | 🇨🇳 China | Recomendado on-prem | 🔴 Remover |
| `llama3.2:3b` | Meta | 🇺🇸 EUA | Catalog | ✅ Manter |
| `llama3.1:8b` | Meta | 🇺🇸 EUA | On-prem option | ✅ Manter |
| `llama3.1:70b` | Meta | 🇺🇸 EUA | On-prem option | ✅ Manter |
| `codellama:7b` | Meta | 🇺🇸 EUA | Code generation | ✅ Manter |
| `mistral-nemo:12b` | Mistral AI | 🇫🇷 França | On-prem option | ✅ Manter |
| `nomic-embed-text` | Nomic AI | 🇺🇸 EUA | Embeddings default | ✅ Manter |
| `mxbai-embed-large` | Mixedbread | 🇺🇸 EUA | Embeddings option | ✅ Manter |
| `gpt-4o` | OpenAI | 🇺🇸 EUA | External | ✅ Manter |
| `gpt-4o-mini` | OpenAI | 🇺🇸 EUA | External default | ✅ Manter |
| `claude-opus-4-7` | Anthropic | 🇺🇸 EUA | External reasoning | ✅ Manter |
| `claude-sonnet-4-6` | Anthropic | 🇺🇸 EUA | External chat default | ✅ Manter |
| `claude-haiku-4-5` | Anthropic | 🇺🇸 EUA | External fast | ✅ Manter |

---

## Parte 3 — Modelos de Substituição Recomendados

### 3.1 Para IA Interna (Ollama) — Substituições

| Função | Modelo Atual (🇨🇳) | Substituto Recomendado | Origem | Justificativa |
|--------|-------------------|----------------------|--------|---------------|
| **Chat padrão (appsettings)** | `qwen3.5:9b` | `llama3.2:3b` | 🇺🇸 Meta | Menor consumo, tool calling, Apache |
| **Reasoning padrão (code)** | `deepseek-r1:1.5b` | `phi3.5:3.8b` | 🇺🇸 Microsoft | Excelente raciocínio, baixo consumo |
| **On-prem qualidade** | `qwen2.5:7b` | `llama3.1:8b` | 🇺🇸 Meta | Melhor qualidade geral, 131K contexto |
| **On-prem raciocínio** | `deepseek-r1:7b` | `mistral-nemo:12b` | 🇫🇷 Mistral AI | Excelente multilíngue, 128K contexto |

**Notas sobre `phi3.5:3.8b`:** Desenvolvido pela Microsoft Research. Excelente para raciocínio, baixo consumo de RAM (≈ 2.3 GB), suporta 128K contexto. Disponível via `ollama pull phi3.5`.

### 3.2 Para IA Interna — Catálogo Completo Pós-Migração

```
Ollama (Interno):
  - phi3.5:3.8b          → Reasoning padrão (substitui deepseek-r1:1.5b)
  - llama3.2:3b          → Chat geral + Tool Calling
  - llama3.1:8b          → Chat qualidade + longo contexto
  - codellama:7b          → Code generation
  - nomic-embed-text      → Embeddings padrão
  - mxbai-embed-large     → Embeddings alta qualidade
  - mistral-nemo:12b      → Multilíngue + análise complexa (on-prem premium)
```

### 3.3 Para IA Externa — Stack Completo

```
Anthropic (Principal — recomendado):
  - claude-opus-4-7       → Reasoning complexo, análise de risco, blast radius
  - claude-sonnet-4-6     → Chat geral, contratos, governança
  - claude-haiku-4-5      → Tarefas simples, alto volume, baixo custo

OpenAI (Alternativo):
  - gpt-4o                → Multi-modal, visão, análise de código
  - gpt-4o-mini           → Volume alto, baixo custo
  - text-embedding-3-small → Embeddings externos
```

---

## Parte 4 — Falhas e Gaps de Implementação

### 4.1 CRÍTICO — Provider Anthropic Sem Configuração

**Problema:** O `AnthropicProvider.cs` está implementado, e modelos Claude 4.x estão no `DefaultModelCatalog.cs` como modelos padrão (`IsDefaultForChat: true` para `claude-sonnet-4-6`, `IsDefaultForReasoning: true` para `claude-opus-4-7`). Porém, **não existe seção `Anthropic` no `appsettings.json`**.

**Impacto:** Os modelos Claude nunca serão usados porque o provider não é configurado/registrado, mesmo estando marcados como padrão no catálogo.

**Arquivo ausente de configuração:** `src/platform/NexTraceOne.ApiHost/appsettings.json`

**Solução:** Adicionar seção de configuração Anthropic:
```json
"AiRuntime": {
  "Anthropic": {
    "ApiKey": "",
    "DefaultChatModel": "claude-sonnet-4-6",
    "DefaultReasoningModel": "claude-opus-4-7",
    "TimeoutSeconds": 90,
    "DefaultTemperature": 0.3,
    "DefaultMaxTokens": 4096,
    "Enabled": false
  }
}
```

### 4.2 CRÍTICO — Ausência de Toggle de IA (On/Off)

**Problema:** Não existe nenhum mecanismo para desativar toda a IA da plataforma. A capability `ai_governance` (Professional+) controla o acesso às funcionalidades de *governança de IA*, mas não desativa a IA em si.

**Impacto:** Uma empresa não pode optar por não usar IA. Todos os módulos que invocam IA continuam ativos.

**Pontos de consumo de IA fora do módulo AIKnowledge:**
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/AiDraftGeneratorService.cs` — geração de draft de contrato por IA
- `src/modules/changegovernance/` — cálculo de blast radius com IA
- `src/modules/governance/` — análise proativa com guardian

**Solução necessária:** Ver Parte 5 deste relatório.

### 4.3 ALTO — Quota de Tokens Não Cobre Todos os Pontos de Consumo

**Problema:** O `IAiTokenQuotaService.ValidateQuotaAsync` é chamado em:
- ✅ `SendAssistantMessage.cs`
- ✅ `ExecuteAiChat.cs`
- ✅ `AiAgentRuntimeService.cs`

Mas **não é chamado** em:
- ❌ `AiDraftGeneratorService.cs` (Catalog) — chama `IChatCompletionProvider.CompleteAsync()` diretamente, sem quota
- ❌ `ExternalAiRoutingPortAdapter.cs` — roteia para IA externa sem verificar quota
- ❌ Quaisquer futuros módulos que usem `IChatCompletionProvider` diretamente

**Impacto:** Consumo ilimitado e não rastreado de tokens externos em pelo menos dois caminhos de código ativos.

### 4.4 ALTO — Rate Limiting por IP, não por Tenant

**Arquivo:** `src/platform/NexTraceOne.ApiHost/RateLimitingOptions.cs`

**Problema:** A política `"ai"` usa `remoteIpAddress` como chave de partição:
```csharp
partitionKey: $"ai:{remoteIp ?? "unresolved-ip"}"
```

**Impacto:** Em um ambiente multi-tenant, todos os tenants que compartilham o mesmo egress IP (típico em ambientes cloud) têm o mesmo bucket de 30 req/min. Um tenant pode esgotar o limite para todos os outros.

**Solução:** Usar `tenantId + userId` como chave de partição, extraído dos claims JWT.

### 4.5 MÉDIO — Token Counter Usa Esquema Errado para Modelos Ollama

**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/TokenCounterService.cs:23`

```csharp
return TiktokenTokenizer.CreateForModel("gpt-4"); // cl100k_base
```

**Problema:** Modelos Llama, Phi, Mistral usam o tokenizador **SentencePiece** ou **BPE com vocabulário diferente**. O cl100k_base (GPT-4) pode subestimar até 30% os tokens reais para esses modelos.

**Impacto:** Quotas de tokens calculadas incorretamente para modelos Ollama — uma política de `MaxTokensPerRequest = 4000` pode na prática permitir 5200+ tokens reais.

**Solução:** Mapear modelos para esquemas de tokenização corretos, ou usar contagem conservadora com fator de segurança (multiplicar por 1.3 para modelos não-GPT).

### 4.6 MÉDIO — Modelo Padrão e Modelo no Catálogo Divergem

**Problema:**
- `OllamaOptions.cs` tem default hardcoded: `"deepseek-r1:1.5b"` (origem chinesa)
- `appsettings.json` sobrescreve para: `"qwen3.5:9b"` (origem chinesa)
- `DefaultModelCatalog.cs` não tem `IsDefaultForChat: true` em nenhum modelo Ollama — `llama3.2:3b` tem `IsDefaultForChat: false`

**Impacto:** Confusão sobre qual modelo é realmente usado. O resolver `ResolveDefaultModelAsync` pode retornar um modelo diferente do configurado.

### 4.7 MÉDIO — Jobs de IA Sem Controle de IA Desabilitada

**Problema:** Os background jobs `EmbeddingIndexJob`, `QdrantIndexJob`, etc. não verificam se a IA está desabilitada para o tenant/plataforma antes de executar.

**Impacto:** Mesmo com IA desabilitada, embeddings continuam sendo gerados e sincronizados com Qdrant, consumindo recursos.

### 4.8 BAIXO — Testes com Modelos Chineses Hardcoded

**Problema:** Testes unitários usam `"qwen3.5:9b"` e `"qwen2.5-coder-32b"` como strings hardcoded.

**Impacto:** Quando os modelos forem removidos da configuração, alguns testes podem falhar ou produzir resultados enganosos.

---

## Parte 5 — Arquitetura: Toggle de IA (On/Off) e Modo Externo-Only

### 5.1 Modelo de Capabilities Proposto

Adicionar ao `TenantCapabilities.cs` três novas capabilities:

```csharp
// src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/TenantCapabilities.cs

public const string AiEnabled       = "ai_enabled";         // AI ligada (qualquer tipo)
public const string AiInternal      = "ai_internal";        // Pode usar IA interna (Ollama/LM Studio)
public const string AiExternal      = "ai_external";        // Pode usar IA externa (OpenAI/Anthropic)
public const string AiTokenBudget   = "ai_token_budget";    // Gestão de budget de tokens ativos
```

**Regras de combinação:**

| `ai_enabled` | `ai_internal` | `ai_external` | Comportamento |
|:---:|:---:|:---:|---|
| false | — | — | IA completamente desabilitada. UI de IA oculta. |
| true | true | false | Apenas IA interna (Ollama). Endpoints externos bloqueados. |
| true | false | true | Apenas IA externa. Ollama não é utilizado. |
| true | true | true | IA completa (comportamento atual). |
| true | false | false | Configuração inválida — tratar como `ai_enabled = false`. |

### 5.2 Pontos de Mudança no Backend

#### 5.2.1 `TenantCapabilities.cs` — Adicionar constants

```
Arquivo: src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/TenantCapabilities.cs

Adicionar ao grupo Core (todos os planos):
  "ai_enabled"    — IA ativa (toggle principal)
  "ai_internal"   — Uso de Ollama/LM Studio permitido
  "ai_external"   — Uso de OpenAI/Anthropic permitido
  "ai_token_budget" — Controle de budget ativo
```

#### 5.2.2 `ExternalAiRoutingPortAdapter.cs` — Guard de capability

```
Arquivo: src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/ExternalAiRoutingPortAdapter.cs

Adicionar injeção de ICurrentTenant.
No início de RouteQueryAsync:
  1. Se !currentTenant.HasCapability("ai_enabled") → retornar fallback ou lançar Error.Forbidden
  2. Se !currentTenant.HasCapability("ai_external") → bloquear roteamento externo
```

#### 5.2.3 `ExecuteAiChat.cs` — Guard de capability

```
Arquivo: src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/ExecuteAiChat/ExecuteAiChat.cs

Adicionar no Handler antes de qualquer chamada ao provider:
  if (!currentTenant.HasCapability("ai_enabled"))
      return Error.Forbidden("AiDisabled", "AI is disabled for this tenant.");
  
  if (isExternalProvider && !currentTenant.HasCapability("ai_external"))
      return Error.Forbidden("ExternalAiDisabled", "External AI is disabled for this tenant.");
  
  if (isInternalProvider && !currentTenant.HasCapability("ai_internal"))
      return Error.Forbidden("InternalAiDisabled", "Internal AI is disabled for this tenant.");
```

#### 5.2.4 `AiDraftGeneratorService.cs` — Corrigir bypass de quota e capability

```
Arquivo: src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/AiDraftGeneratorService.cs

Adicionar injeção de ICurrentTenant e IAiTokenQuotaService.
Adicionar verificação de capability antes de chamar o provider.
Adicionar RecordUsageAsync após chamada bem-sucedida.
```

#### 5.2.5 `IAiProviderFactory` — Filtro por tipo (interno/externo)

```
Arquivo: src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IAiProviderFactory.cs
Arquivo: src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiProviderFactory.cs

Adicionar método: GetAvailableProviders(ICurrentTenant tenant)
  Filtra por:
    - ai_internal = false → exclui Ollama, LM Studio
    - ai_external = false → exclui OpenAI, Anthropic
```

#### 5.2.6 Background Jobs — Skip quando IA desabilitada

```
Arquivos: EmbeddingIndexJob.cs, QdrantIndexJob.cs, ExternalDataSourceSyncJob.cs

Injetar IConfiguration ou opções de platform.
No início do Execute: verificar "Platform:Ai:Enabled" — se false, skip silencioso com log.
```

### 5.3 Pontos de Mudança no Frontend

#### 5.3.1 Ocultar AI Hub quando `ai_enabled = false`

```
Arquivo: src/frontend/src/routes/aiHubRoutes.tsx

Envolver todas as rotas de AI Hub com um guard de capability:
  <CapabilityGuard capability="ai_enabled" fallback={<Navigate to="/dashboard" />}>
    {aiHubRoutes}
  </CapabilityGuard>
```

#### 5.3.2 Ocultar features específicas por `ai_internal` / `ai_external`

```
Arquivos:
  - src/frontend/src/features/ai-hub/pages/AiIntegrationsConfigurationPage.tsx
    → Ocultar configurações de Ollama se !ai_internal
    → Ocultar configurações de OpenAI/Anthropic se !ai_external

  - src/frontend/src/features/ai-hub/pages/ModelRegistryPage.tsx
    → Filtrar modelos exibidos por IsInternal baseado em capabilities

  - src/frontend/src/features/ai-hub/components/AssistantPanel.tsx
    → Retornar null se !ai_enabled (oculta o painel completamente)
```

#### 5.3.3 Ocultar features AI em outros módulos

Qualquer componente em outros módulos que use IA (ex: botão "Gerar com IA" em contratos) deve verificar a capability:

```tsx
const { hasCapability } = useAuth(); // ou useTenant()

{hasCapability('ai_enabled') && (
  <Button onClick={handleGenerateWithAi}>Gerar com IA</Button>
)}
```

---

## Parte 6 — Controle de Consumo de Tokens Externos

### 6.1 Estado Atual

| Mecanismo | Implementado | Wired | Efetivo |
|-----------|:---:|:---:|:---:|
| `AiTokenQuotaPolicy` (entidade) | ✅ | ✅ | ⚠️ Parcial |
| `IAiTokenQuotaService.ValidateQuotaAsync` | ✅ | ⚠️ | ⚠️ 3/5 pontos |
| `IAiTokenQuotaService.RecordUsageAsync` | ✅ | ⚠️ | ⚠️ 3/5 pontos |
| Budget financeiro (`MaxCostPerRequestUsd`) | ✅ | ❌ | ❌ Não enforcement |
| Rate limiting por tenant | ❌ | ❌ | ❌ Não existe |
| Alertas de quota | ❌ | ❌ | ❌ Não existe |
| Dashboard de consumo em tempo real | ✅ (UI) | ⚠️ | Depende de RecordUsage |

### 6.2 Pontos de Consumo Externo e Estado do Controle

| Ponto de Consumo | Arquivo | ValidateQuota | RecordUsage | Capability Guard |
|-----------------|---------|:---:|:---:|:---:|
| Chat principal | `ExecuteAiChat.cs` | ✅ | ✅ | ❌ |
| Assistente | `SendAssistantMessage.cs` | ✅ | ✅ | ❌ |
| Agent Runtime | `AiAgentRuntimeService.cs` | ✅ | ✅ | ❌ |
| Draft de contrato | `AiDraftGeneratorService.cs` | ❌ | ❌ | ❌ |
| Routing externo | `ExternalAiRoutingPortAdapter.cs` | ❌ | ❌ | ❌ |

### 6.3 Arquitetura de Controle Recomendada

#### Camada 1 — Budget por Tenant (mensal/anual)

```
Entidade: AIBudget (já existe em src/modules/aiknowledge/.../AIBudget.cs)
Verificar: Integrar AIBudget ao fluxo de ValidateQuota
Adicionar: Alerta quando atingir 80% e bloqueio em 100% (IsHardLimit)
```

#### Camada 2 — Quota por Usuário/Tenant (diário/mensal)

```
Entidade: AiTokenQuotaPolicy (já existe, bem implementada)
Gap: Não está sendo chamada em todos os pontos de consumo
Solução: Criar MediatR Behavior (pipeline) para requests que implementem IAiRequest
  → PipelineBehavior chamaria ValidateQuota automaticamente antes de qualquer handler AI
```

#### Camada 3 — Rate Limiting por Tenant (por minuto)

```
Arquivo: src/platform/NexTraceOne.ApiHost/RateLimitingOptions.cs

Mudar particionamento de:
  $"ai:{remoteIp}"
Para:
  $"ai:{tenantId}:{userId}" extraído do JWT

Adicionar política separada "ai-tenant" com limites configuráveis por plano:
  Starter: N/A (sem IA)
  Professional: 60 req/min por tenant
  Enterprise: 200 req/min por tenant (configurável)
```

#### Camada 4 — Controle de Custo Financeiro

```
Entidade ModelRoutingPolicy já tem: MaxCostPerRequestUsd
Falta: Implementar enforcement no ExternalAiRoutingPortAdapter

Adicionar: tabela de preços por modelo (USD/1K tokens)
  → claude-opus-4-7:    $15/1M input, $75/1M output
  → claude-sonnet-4-6:  $3/1M input,  $15/1M output
  → claude-haiku-4-5:   $0.25/1M input, $1.25/1M output
  → gpt-4o:             $2.50/1M input, $10/1M output
  → gpt-4o-mini:        $0.15/1M input, $0.60/1M output

Calcular custo estimado antes de enviar request.
Bloquear se custo > MaxCostPerRequestUsd da policy ativa.
```

#### Camada 5 — Observabilidade e Alertas

```
Implementar:
  - Métrica OTel: ai.tokens.consumed{provider,model,tenant,type=input|output}
  - Métrica OTel: ai.cost.usd{provider,model,tenant}
  - Alerta: ai.quota.threshold_reached{tenant,scope,percent=80|90|100}
  
Integrar com AlertEvaluationJob (já existente em governance):
  Adicionar tipo de alerta: AiTokenQuotaThreshold
  Notificar via notifications module quando quota atingir limiares
```

---

## Parte 7 — Plano de Implementação

### 7.1 Sprint 1 — Remover Modelos Chineses (Urgente, 1-2 dias)

| # | Arquivo | Mudança |
|---|---------|---------|
| 1 | `appsettings.json` | `DefaultChatModel` e `PreferredChatModel`: `qwen3.5:9b` → `llama3.2:3b` |
| 2 | `OllamaOptions.cs:21` | Default hardcoded: `deepseek-r1:1.5b` → `llama3.2:3b` |
| 3 | `DefaultModelCatalog.cs:49-67` | Remover entrada `deepseek-r1:1.5b`, adicionar `phi3.5:3.8b` |
| 4 | `HardwareAssessmentService.cs:14-17` | Remover `deepseek-r1:1.5b`, `deepseek-r1:7b`, `qwen2.5:7b`; adicionar `phi3.5:3.8b`, `mistral:7b` |
| 5 | `SendAssistantMessageLlmTests.cs` | `ModelId = "qwen3.5:9b"` → `"llama3.2:3b"` |
| 6 | `ConversationQueryTests.cs` | Strings `"qwen3.5:9b"` → `"llama3.2:3b"` |
| 7 | `TokenCounterServiceTests.cs` | `"qwen2.5-coder-32b"` → `"codellama:7b"` ou `"phi3.5"` |

### 7.2 Sprint 2 — Configurar Anthropic + Atualizar DefaultModelCatalog (2-3 dias)

| # | Arquivo | Mudança |
|---|---------|---------|
| 1 | `appsettings.json` | Adicionar seção `"Anthropic"` com config Claude |
| 2 | `DefaultModelCatalog.cs` | Definir `IsDefaultForChat: true` em `llama3.2:3b` (interno); confirmar defaults Claude |
| 3 | `OllamaOptions.cs` | Adicionar modelo para reasoning: `phi3.5:3.8b` |
| 4 | `HardwareAssessmentService.cs` | Adicionar `phi3.5:3.8b`, `mistral:7b`, `mistral-nemo:12b` |

### 7.3 Sprint 3 — Capabilities AI + Toggle On/Off (3-5 dias)

| # | Arquivo | Mudança |
|---|---------|---------|
| 1 | `TenantCapabilities.cs` | Adicionar `ai_enabled`, `ai_internal`, `ai_external`, `ai_token_budget` |
| 2 | `TenantCapabilities.ForPlan()` | Definir capabilities por plano (ver tabela abaixo) |
| 3 | `ExecuteAiChat.cs` | Guard de capability no handler |
| 4 | `SendAssistantMessage.cs` | Guard de capability no handler |
| 5 | `AiDraftGeneratorService.cs` | Injetar ICurrentTenant + guard |
| 6 | `ExternalAiRoutingPortAdapter.cs` | Guard de capability + ValidateQuota |
| 7 | `AiProviderFactory.cs` | Método `GetAvailableProviders(tenant)` |
| 8 | `EmbeddingIndexJob.cs` / `QdrantIndexJob.cs` | Skip quando IA desabilitada |
| 9 | Frontend `aiHubRoutes.tsx` | Guard `ai_enabled` nas rotas |
| 10 | Frontend `AssistantPanel.tsx` | Return null quando !ai_enabled |

**Capabilities por plano (proposto):**

| Capability | Starter | Professional | Enterprise | Trial |
|-----------|:-------:|:------------:|:----------:|:-----:|
| `ai_enabled` | ❌ | ✅ | ✅ | ✅ |
| `ai_internal` | ❌ | ✅ | ✅ | ✅ |
| `ai_external` | ❌ | ❌ | ✅ | ✅ (teaser) |
| `ai_token_budget` | ❌ | ✅ | ✅ | ✅ |
| `ai_governance` | ❌ | ✅ | ✅ | ✅ |
| `custom_agents` | ❌ | ❌ | ✅ | ✅ (teaser) |

### 7.4 Sprint 4 — Controle de Tokens Externos (3-5 dias)

| # | Arquivo | Mudança |
|---|---------|---------|
| 1 | `AiDraftGeneratorService.cs` | Adicionar ValidateQuota + RecordUsage |
| 2 | `ExternalAiRoutingPortAdapter.cs` | Adicionar ValidateQuota + RecordUsage |
| 3 | `RateLimitingOptions.cs` | Mudar partição de IP para tenant+user |
| 4 | `AIBudget.cs` + handler | Integrar budget mensal ao fluxo de quota |
| 5 | `TokenCounterService.cs` | Adicionar fator de segurança 1.3 para modelos não-GPT |
| 6 | Novo: `AiTokenBudgetAlertJob.cs` | Job que emite alertas de quota em 80%/90%/100% |
| 7 | Novo: métricas OTel | `ai.tokens.consumed`, `ai.cost.usd` em `ExecuteAiChat` |

---

## Parte 8 — Configuração Alvo Pós-Migração

### 8.1 `appsettings.json` — Seção AiRuntime Completa

```json
"AiRuntime": {
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "TimeoutSeconds": 120,
    "MaxRetries": 2,
    "DefaultChatModel": "llama3.2:3b",
    "DefaultReasoningModel": "phi3.5:3.8b",
    "DefaultEmbeddingModel": "nomic-embed-text",
    "Enabled": true
  },
  "OpenAI": {
    "BaseUrl": "https://api.openai.com",
    "ApiKey": "",
    "DefaultChatModel": "gpt-4o-mini",
    "TimeoutSeconds": 60,
    "DefaultTemperature": 0.3,
    "DefaultMaxTokens": 4096,
    "Enabled": false
  },
  "Anthropic": {
    "ApiKey": "",
    "DefaultChatModel": "claude-sonnet-4-6",
    "DefaultReasoningModel": "claude-opus-4-7",
    "DefaultFastModel": "claude-haiku-4-5",
    "TimeoutSeconds": 90,
    "DefaultTemperature": 0.3,
    "DefaultMaxTokens": 4096,
    "Enabled": false
  },
  "Routing": {
    "PreferredProvider": "ollama",
    "PreferredChatModel": "llama3.2:3b",
    "EnableDeterministicFallback": true,
    "FallbackPrefix": "[FALLBACK_PROVIDER_UNAVAILABLE]"
  },
  "TokenQuota": {
    "DefaultMaxTokensPerRequestInput": 8000,
    "DefaultMaxTokensPerRequestOutput": 4096,
    "DefaultMaxTokensPerDayPerUser": 100000,
    "DefaultMaxTokensPerMonthPerTenant": 5000000,
    "IsHardLimit": true
  }
}
```

---

## Parte 9 — Resumo de Risco

| ID | Severidade | Categoria | Descrição | Sprint |
|----|:----------:|-----------|-----------|--------|
| R-01 | 🔴 CRÍTICO | Compliance | `qwen3.5:9b` como modelo padrão de chat (Alibaba/China) | Sprint 1 |
| R-02 | 🔴 CRÍTICO | Compliance | `deepseek-r1:1.5b` como padrão Ollama e no catálogo seed (DeepSeek/China) | Sprint 1 |
| R-03 | 🔴 CRÍTICO | Compliance | `qwen2.5:7b` e `deepseek-r1:7b` recomendados para on-prem | Sprint 1 |
| R-04 | 🟠 ALTO | Arquitetura | Sem toggle de IA on/off — não há como desabilitar IA para um tenant | Sprint 3 |
| R-05 | 🟠 ALTO | Arquitetura | Anthropic implementado mas sem configuração (modelos Claude inacessíveis) | Sprint 2 |
| R-06 | 🟠 ALTO | Financeiro | Dois pontos de consumo externo sem controle de quota de tokens | Sprint 4 |
| R-07 | 🟠 ALTO | Segurança | Rate limiting de IA por IP, não por tenant — inadequado para multi-tenant | Sprint 4 |
| R-08 | 🟡 MÉDIO | Arquitetura | Sem modo "apenas IA externa" — não há como desabilitar Ollama para um tenant | Sprint 3 |
| R-09 | 🟡 MÉDIO | Precisão | TokenCounter usa cl100k_base para todos os modelos — impreciso para Llama/Phi | Sprint 4 |
| R-10 | 🟡 MÉDIO | Consistência | Modelo padrão em `OllamaOptions` (DeepSeek) diverge do `appsettings` (Qwen) | Sprint 1 |
| R-11 | 🟡 MÉDIO | Observabilidade | Sem métricas de custo financeiro de tokens externos | Sprint 4 |
| R-12 | 🟡 MÉDIO | Manutenção | Testes unitários com modelos chineses hardcoded | Sprint 1 |
| R-13 | 🟢 BAIXO | Compliance | Background jobs de IA sem verificação de estado de ativação | Sprint 3 |
| R-14 | 🟢 BAIXO | Arquitetura | Budget financeiro (`AIBudget`) existe mas não tem enforcement | Sprint 4 |

---

*Relatório gerado em 28 de Maio de 2026 — NexTraceOne AI Governance*
