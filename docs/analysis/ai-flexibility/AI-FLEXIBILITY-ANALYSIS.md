# Análise de Arquitetura: Flexibilidade de IA no NexTraceOne

> **Data:** 2026-06-09
> **Escopo:** Análise do estado atual da arquitetura de IA e proposta para suportar: (1) Desligar IA, (2) Usar IA Externa (ChatGPT, Claude Code, Gemini, GitHub Copilot), (3) Usar IA Interna, com escolha granular por funcionalidade, contexto e usuário.

---

## 1. RESUMO EXECUTIVO

O NexTraceOne já possui uma das arquiteturas de IA mais maduras e bem estruturadas entre plataformas enterprise similares. O módulo `AIKnowledge` implementa:

- **Multi-provider interno** (Ollama, OpenAI, Anthropic, LM Studio)
- **14 agentes especializados** com Semantic Kernel
- **Governança multicamada** (Model Registry, Access Policies, Token Quotas, Budgets, Guardrails)
- **Roteamento inteligente** (Routing Strategies + Feature-Model Bindings)
- **RAG com Vector Search** (Qdrant)
- **MCP Server** para integração com IDEs
- **ExternalAI** para consulta a LLMs externos com auditoria

**No entanto, existem gaps arquiteturais que impedem o usuário de escolher, por funcionalidade, se quer usar IA interna, IA externa (produtos), ou nenhuma IA.** Este documento identifica esses gaps e propõe uma evolução arquitetural mínima e não-destrutiva.

---

## 2. ESTADO ATUAL — DIAGNÓSTICO DETALHADO

### 2.1 O que JÁ EXISTE e Funciona Bem

| Componente | Status | Descrição |
|------------|--------|-----------|
| **Multi-Provider Interno** | ✅ Operacional | Ollama (local), OpenAI (API), Anthropic (API), LM Studio (local). Cada um implementa `IAiProvider`, `IChatCompletionProvider`, etc. |
| **Model Registry** | ✅ Operacional | Entidade `AIModel` com 50+ campos (capabilities, flags de default, compliance, hardware). Seed com modelos internos e externos. |
| **Feature-Model Bindings** | ✅ Operacional | `AiFeatureModelBinding` permite definir, por tenant, qual modelo usar para cada `FeatureKey` (ex: `catalog.contract-draft`). Suporta fallback. |
| **Routing Strategies** | ✅ Operacional | `AIRoutingStrategy` + `AIRoutingDecision` permitem roteamento por persona, use case, custo, qualidade, sensibilidade. Com fallback automático por health check. |
| **Access Policies** | ✅ Operacional | `AIAccessPolicy` controla quem pode usar quais modelos, com scope por user/role/persona/team. |
| **Token Quotas & Budgets** | ✅ Operacional | `AiTokenQuotaPolicy` + `AIBudget` com limites por request/dia/mês/tenant. Hard e soft limits. |
| **Guardrails** | ✅ Operacional | `AiGuardrailEnforcementService` com detecção de prompt injection, PII, e guardrails configuráveis por regex/keyword. |
| **Semantic Kernel Adapter** | ✅ Operacional | `NexTraceOneChatCompletionService` adapta providers customizados para `IChatCompletionService` do SK. |
| **ExternalAI Module** | ✅ Operacional | `ExternalAiProvider`, `ExternalAiPolicy`, `ExternalAiConsultation` — permite consultar LLMs externos com auditoria completa e captura de conhecimento. |
| **Feature Flags** | ✅ Operacional | 6 flags de IA seedadas (`ai.assistant.enabled`, `ai.agents.enabled`, `ai.external-providers.enabled`, etc.), todas desabilitadas por default. |
| **MCP Server** | ✅ Operacional | Servidor JSON-RPC 2.0 que expõe tools para clientes externos (VS Code, Claude Desktop). |
| **IDE Extensions** | ✅ Operacional | VS (ToolWindow WebView2) e VS Code (TypeScript + MCP) consomem `/api/v1/ai/ide/query`. |

### 2.2 O que JÁ EXISTE mas é Insuficiente

| Componente | Problema |
|------------|----------|
| **ExternalAI Module** | É um módulo de **consulta pontual** (fire-and-forget com auditoria), não um **provider de runtime**. Não se integra ao `AiAgentRuntimeService`, `KernelService`, ou Feature-Model Bindings. |
| **Feature Flags** | São binárias (on/off) mas não controlam **qual** IA usar, apenas **se** o módulo está disponível. Não há flag global "desligar todas as IAs". |
| **AiProviderFactory** | Só resolve providers **internos** registrados no DI. Não tem conceito de "provider externo" (produto) ou "provider nulo". |
| **Feature-Model Bindings** | Ligam feature → modelo interno. Não suportam: (a) "nenhum modelo", (b) "provider externo/produto", (c) override por usuário. |
| **AI Agents (14)** | Têm provider hardcoded (`ollama ?? openai`) em seus handlers. Não respeitam Feature-Model Bindings nem preferências do usuário. |
| **Operational Intelligence** | Features com "nomes de IA" (anomaly narrative, healing recommendation) são **template-based**, não consomem LLM. Não há alternativa de IA real configurável. |

### 2.3 Gaps Arquiteturais Críticos

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  GAP 1: Não há "Null/No-Op Provider"                                         │
│  → Não existe uma implementação de IA que simplesmente retorne               │
│    "IA desabilitada" ou execute lógica determinística sem LLM.               │
│  → Feature flags desligam endpoints, mas não graceful degradation.            │
├─────────────────────────────────────────────────────────────────────────────┤
│  GAP 2: "IA Externa" é consulta, não provider                                │
│  → ExternalAiProvider é uma entidade de configuração de endpoint API.        │
│  → Não existe adapter para ChatGPT-web, Claude-Code-CLI, Gemini-app,         │
│    GitHub-Copilot-API como "backend de execução" de features.                │
│  → O usuário quer ESCOLHER usar ChatGPT/Claude/Gemini/Copilot como           │
│    alternativa ao runtime interno — não apenas consultar APIs.               │
├─────────────────────────────────────────────────────────────────────────────┤
│  GAP 3: Ausência de "User AI Preference"                                     │
│  → Não existe entidade/perfil que armazene, por usuário:                     │
│    - "Prefiro não usar IA"                                                   │
│    - "Prefiro IA externa X para feature Y"                                   │
│    - "Prefiro modelo Z da IA interna para feature W"                         │
│  → Access Policies dizem o que é PERMITIDO, não o que é PREFERIDO.           │
├─────────────────────────────────────────────────────────────────────────────┤
│  GAP 4: AI Agents não respeitam configuração dinâmica                        │
│  → 14 agents têm provider/modelo hardcoded ou fallback estático.             │
│  → Não consultam AiModelCatalogService.ResolveModelForFeatureAsync().        │
├─────────────────────────────────────────────────────────────────────────────┤
│  GAP 5: Módulos consumidores não são "AI-aware"                              │
│  → catalog, governance, operationalintelligence usam IA diretamente           │
│    (ex: AiDraftGeneratorService) sem passar por uma fábrica/abstração        │
│    que respeite preferências do usuário/tenant.                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. REQUISITOS DO USUÁRIO — MAPEAMENTO

| Requisito do Usuário | Gap Atual | Solução Proposta |
|----------------------|-----------|------------------|
| **"Não usar IA"** | GAP 1 + 3 | `NullAiProvider` + `UserAiPreference.Disabled` + graceful degradation em todos os módulos consumidores |
| **"Usar IA externa (ChatGPT, Claude Code, Gemini, GitHub Copilot)"** | GAP 2 + 3 | `ExternalAiProductProvider` (adapter pattern) + `UserAiPreference.ExternalProduct` + integrações específicas por produto |
| **"Usar IA interna"** | ✅ Já existe | Expandir para respeitar preferências do usuário |
| **"Escolher qual IA quer usar"** | GAP 3 | `UserAiPreference` (entidade + API + UI) com escopo por feature e contexto |
| **"Aonde quer usar"** | GAP 4 + 5 | Feature-Model Bindings expandidos + AI Execution Gateway que consulta preferências antes de executar |
| **"Qual modelo/IA para cada funcionalidade"** | GAP 4 + 5 | `AiExecutionContext` resolvido dinamicamente por: feature → tenant binding → user preference → system default |

---

## 4. PROPOSTA ARQUITETURAL

### 4.1 Visão Geral — AI Execution Gateway

Introduzir uma **camada de orquestração central** — o **AI Execution Gateway** — que é o ÚNICO ponto pelo qual qualquer módulo do NexTraceOne consome IA. Esse gateway resolve, para cada requisição:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AI EXECUTION GATEWAY                                 │
│  (único ponto de entrada para IA em toda a plataforma)                       │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Input: (FeatureKey, UserId, TenantId, ContextData, RequestType)            │
│                                                                              │
│  1. FEATURE FLAG CHECK                                                      │
│     → Feature desabilitada? Retorna "IA não disponível para esta feature"   │
│                                                                              │
│  2. USER PREFERENCE RESOLUTION                                              │
│     → Busca UserAiPreference para (FeatureKey, UserId)                      │
│     → Preferência: Disabled | Internal | ExternalProduct                    │
│                                                                              │
│  3. TENANT BINDING RESOLUTION                                               │
│     → Se user não tem preferência, busca AiFeatureModelBinding do tenant    │
│                                                                              │
│  4. SYSTEM DEFAULT                                                          │
│     → Fallback para configuração global (appsettings)                       │
│                                                                              │
│  5. PROVIDER RESOLUTION                                                     │
│     ┌─────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐   │
│     │  Disabled       │  │  Internal           │  │  External Product   │   │
│     │  → NullProvider │  │  → AiProviderFactory│  │  → ExternalProduct  │   │
│     │  → No-op /      │  │  → Resolve modelo   │  │     Provider        │   │
│     │    deterministic│  │    interno          │  │  → ChatGPT/Claude/  │   │
│     │                 │  │                     │  │    Gemini/Copilot   │   │
│     └─────────────────┘  └─────────────────────┘  └─────────────────────┘   │
│                                                                              │
│  6. GOVERNANCE & GUARDRAILS (sempre aplicados)                              │
│     → Access Policy check                                                   │
│     → Token Quota check                                                     │
│     → Guardrail evaluation                                                  │
│     → Budget check                                                          │
│                                                                              │
│  7. EXECUTION                                                               │
│     → Delega ao provider resolvido                                          │
│     → Registra AIUsageEntry + AIRoutingDecision                             │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Novas Entidades e Interfaces

#### 4.2.1 `UserAiPreference` (Domínio)

```csharp
public sealed class UserAiPreference : Entity, ITenantScoped
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    
    // Escopo da preferência
    public string FeatureKey { get; private set; }  // ex: "catalog.contract-draft"
                                                      // "*" = global para o usuário
    public AiPreferenceType PreferenceType { get; private set; }
    
    // Se PreferenceType = Internal
    public string? PreferredModelId { get; private set; }
    public string? PreferredProviderId { get; private set; }
    
    // Se PreferenceType = ExternalProduct
    public ExternalAiProductType? ExternalProduct { get; private set; }
    public string? ExternalProductModel { get; private set; }  // ex: "gpt-4o", "claude-sonnet-4"
    
    // Se PreferenceType = Disabled
    public string? DisableReason { get; private set; }  // opcional, para analytics
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
}

public enum AiPreferenceType
{
    Disabled = 0,       // Não usar IA para esta feature
    Internal = 1,       // Usar IA interna (provider/modelo configurável)
    ExternalProduct = 2 // Usar produto externo (ChatGPT, Claude, etc.)
}

public enum ExternalAiProductType
{
    ChatGPT = 0,        // OpenAI ChatGPT (web/api)
    ClaudeCode = 1,     // Anthropic Claude Code
    Gemini = 2,         // Google Gemini
    GitHubCopilot = 3,  // GitHub Copilot
    Custom = 99         // Outro produto configurável
}
```

#### 4.2.2 `IAiExecutionGateway` (Porta — Domínio)

```csharp
public interface IAiExecutionGateway
{
    /// <summary>
    /// Executa uma requisição de IA resolvendo provider, modelo e governança automaticamente.
    /// </summary>
    Task<AiExecutionResult> ExecuteAsync(
        AiExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve qual provider/modelo seria usado sem executar (para preview/UI).
    /// </summary>
    Task<AiExecutionPlan> PreviewExecutionAsync(
        AiExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica se IA está disponível para uma feature específica para o usuário atual.
    /// </summary>
    Task<AiAvailabilityStatus> CheckAvailabilityAsync(
        string featureKey,
        CancellationToken ct = default);
}

public sealed record AiExecutionRequest(
    string FeatureKey,                    // ex: "catalog.contract-draft"
    string RequestType,                   // "chat", "completion", "embedding", "agent"
    string? UserPrompt,                   // prompt do usuário
    IReadOnlyList<ChatMessage>? Messages, // histórico de mensagens
    string? SystemPrompt,                 // system prompt override
    IReadOnlyList<FunctionDefinition>? Tools, // tools disponíveis
    Guid? TargetModelId,                  // override explícito (admin)
    Dictionary<string, object>? ContextData,  // dados contextuais para grounding
    bool AllowExternalProduct = true,     // permite resolver para produto externo
    bool AllowFallback = true             // permite fallback se preferência falhar
);

public sealed record AiExecutionResult(
    string Content,
    AiProviderType ProviderType,          // Internal | ExternalProduct | Null
    string ResolvedProviderId,
    string ResolvedModelId,
    int TokensUsed,
    TimeSpan Duration,
    Guid? RoutingDecisionId,
    bool WasFallbackUsed
);

public sealed record AiExecutionPlan(
    AiProviderType ProviderType,
    string ProviderId,
    string ModelId,
    string ModelDisplayName,
    bool IsAvailable,
    string? UnavailabilityReason,
    decimal? EstimatedCost,
    IReadOnlyList<string> AppliedPolicies
);

public enum AiAvailabilityStatus
{
    Available = 0,
    DisabledByUser = 1,
    DisabledByFeatureFlag = 2,
    DisabledByPolicy = 3,
    QuotaExceeded = 4,
    NoProviderAvailable = 5,
    GuardrailBlocked = 6
}
```

#### 4.2.3 `NullAiProvider` — Implementação No-Op

```csharp
/// <summary>
/// Provider que não executa LLM. Usado quando o usuário desabilita IA ou
/// quando a feature deve operar em modo determinístico/template-only.
/// </summary>
public sealed class NullAiProvider : IAiProvider, IChatCompletionProvider
{
    public string ProviderId => "null";
    public string DisplayName => "IA Desabilitada";
    public bool IsLocal => true;

    public Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new AiProviderHealthResult(true, "No-op provider is always healthy"));

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AiProviderModelInfo>>(new List<AiProviderModelInfo>());

    public Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken ct = default)
        => Task.FromResult(new ChatCompletionResult(
            Content: null,
            FinishReason: "disabled",
            Usage: new TokenUsage(0, 0, 0),
            Error: new AiError("AI_DISABLED", "IA está desabilitada para esta funcionalidade.", 
                resolutionHint: "Habilite a IA nas preferências do usuário ou contate o administrador.")
        ));

    public IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(ChatCompletionRequest request, CancellationToken ct = default)
    {
        yield return new ChatStreamChunk("IA está desabilitada para esta funcionalidade.", isLast: true);
    }
}
```

#### 4.2.4 `ExternalAiProductProvider` — Adapter para Produtos Externos

```csharp
/// <summary>
/// Adapter que permite usar produtos de IA externos (ChatGPT, Claude Code, Gemini, Copilot)
/// como "providers" dentro do ecossistema NexTraceOne.
/// 
/// IMPORTANTE: Produtos externos têm modelos de integração diferentes:
/// - ChatGPT: via OpenAI API (já suportado) OU via Browser Automation (não recomendado)
/// - Claude Code: via Anthropic API (já suportado) OU via MCP/CLI integration
/// - Gemini: via Google Generative Language API (novo provider necessário)
/// - GitHub Copilot: via GitHub Copilot API (limitado, requer OAuth GitHub)
/// </summary>
public sealed class ExternalAiProductProvider : IAiProvider, IChatCompletionProvider
{
    // Implementação delega para o adapter específico do produto
    // Cada produto tem seu próprio adapter interno
}
```

**Modelos de Integração por Produto Externo:**

| Produto | API Oficial | Status no NexTraceOne | Estratégia de Integração |
|---------|-------------|----------------------|--------------------------|
| **ChatGPT** | OpenAI API | ✅ Já suportado via `OpenAiProvider` | Tratar como provider interno com `ProviderId = "openai"`. O usuário "escolhe ChatGPT" mas tecnicamente usa a API. |
| **Claude Code** | Anthropic Messages API | ✅ Já suportado via `AnthropicProvider` | Mesma estratégia do ChatGPT. `ProviderId = "anthropic"`. |
| **Gemini** | Google Generative Language API | ❌ Não suportado | **Novo provider necessário:** `GeminiProvider` implementando `IAiProvider`, `IChatCompletionProvider`. |
| **GitHub Copilot** | GitHub Copilot API / Copilot Chat API | ❌ Não suportado | **Integração complexa:** requer OAuth GitHub App, escopos `copilot`, e uso da Copilot Chat API (preview/limitada). Pode ser deferido para fase 2. |

> **Nota importante:** "ChatGPT" e "Claude Code" como **produtos** (apps web/IDE) não têm APIs diretas para integração de terceiros. A integração "real" é via suas APIs respectivas (OpenAI e Anthropic), que já estão implementadas. O valor da proposta está em **permitir ao usuário escolher "quero usar GPT-4o" ou "quero usar Claude" como preferência**, e o sistema resolver para o provider correto.

### 4.3 Expansão de Feature-Model Bindings

A entidade `AiFeatureModelBinding` precisa suportar 3 modos:

```csharp
public enum AiBindingMode
{
    Disabled = 0,       // Feature opera sem IA (NullProvider)
    Internal = 1,       // Usa provider interno (como hoje)
    ExternalProduct = 2 // Usa produto externo (novo)
}

// Campos a adicionar em AiFeatureModelBinding:
public AiBindingMode Mode { get; private set; }
public ExternalAiProductType? ExternalProduct { get; private set; }
public string? ExternalProductModel { get; private set; }
```

**Resolução de execução (ordem de precedência):**

```
1. Override explícito de admin (TargetModelId no request)
2. UserAiPreference para (FeatureKey, UserId)
3. UserAiPreference global para UserId (FeatureKey = "*")
4. AiFeatureModelBinding do Tenant para FeatureKey
5. AIRoutingStrategy ativa (matching de persona/use case)
6. System default (appsettings → PreferredProvider/PreferredChatModel)
7. HARD FALLBACK: NullProvider (sempre disponível)
```

### 4.4 Refatoração dos AI Agents

Atualmente, os 14 AI Agents resolvem provider de forma hardcoded:

```csharp
// ANTES (atual)
var provider = providerFactory.GetChatProvider("ollama") 
            ?? providerFactory.GetChatProvider("openai");
```

```csharp
// DEPOIS (proposto)
var executionResult = await aiExecutionGateway.ExecuteAsync(new AiExecutionRequest(
    FeatureKey: "aiknowledge.agent.incident-responder",
    RequestType: "agent",
    UserPrompt: input,
    SystemPrompt: systemPrompt,
    Messages: messages,
    ContextData: new() { ["GroundingQuery"] = groundingQuery }
), ct);

// O gateway resolve provider, modelo, governança e fallback automaticamente
```

**Agents a refatorar:**
1. `IncidentResponder`
2. `ChangeAdvisor`
3. `SecurityReview`
4. `ArchitectureFitness`
5. `DocumentationQuality`
6. `ServiceAnalyst`
7. `DependencyAdvisor`
8. `DocAgent`
9. `PRAgent`
10. `ReflectionAgent`
11. `WebSearchAgent`
12. `ContractAssistant`
13. `TestGenerator`
14. `ServiceScaffoldAgent`

### 4.5 Refatoração dos Módulos Consumidores

Módulos que consomem IA diretamente precisam migrar para o `IAiExecutionGateway`:

| Módulo | Serviço Atual | FeatureKey Proposto |
|--------|---------------|---------------------|
| **catalog** | `AiDraftGeneratorService` | `catalog.contract-draft` |
| **catalog** | `OllamaCompletionClient` (schema analysis) | `catalog.schema-analysis` |
| **governance** | `AiDashboardComposerService` | `governance.ai-dashboard` |
| **operationalintelligence** | `GenerateAnomalyNarrative` (template) | `ops.anomaly-narrative` |
| **operationalintelligence** | `GenerateHealingRecommendation` (regras) | `ops.healing-recommendation` |
| **operationalintelligence** | `GenerateIncidentNarrative` (template) | `ops.incident-narrative` |
| **aiknowledge** | `AiAgentRuntimeService` (todos os agents) | `aiknowledge.agent.{agent-name}` |
| **aiknowledge** | `ExecuteAiChat` / `ExecuteAiChatStream` | `aiknowledge.assistant-chat` |
| **aiknowledge** | RAG / Embeddings | `aiknowledge.rag-retrieval` |

---

## 5. MODELO DE DADOS — ALTERAÇÕES NECESSÁRIAS

### 5.1 Novas Tabelas

```sql
-- Preferências de IA por usuário
CREATE TABLE user_ai_preferences (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    feature_key VARCHAR(128) NOT NULL,  -- "*" para global
    preference_type INTEGER NOT NULL,   -- 0=Disabled, 1=Internal, 2=ExternalProduct
    preferred_model_id UUID REFERENCES ai_models(id),
    preferred_provider_id VARCHAR(64),
    external_product_type INTEGER,      -- null se Internal/Disabled
    external_product_model VARCHAR(64), -- ex: "gpt-4o"
    disable_reason VARCHAR(512),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    
    CONSTRAINT uq_user_feature UNIQUE (user_id, feature_key, tenant_id),
    CONSTRAINT chk_preference_consistency CHECK (
        (preference_type = 0 AND preferred_model_id IS NULL AND external_product_type IS NULL) OR
        (preference_type = 1 AND preferred_model_id IS NOT NULL) OR
        (preference_type = 2 AND external_product_type IS NOT NULL)
    )
);

-- Habilitar RLS
ALTER TABLE user_ai_preferences ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_user_ai_prefs ON user_ai_preferences
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Índices
CREATE INDEX idx_user_ai_prefs_user ON user_ai_preferences(user_id, tenant_id, is_active);
CREATE INDEX idx_user_ai_prefs_feature ON user_ai_preferences(feature_key, tenant_id, is_active);
```

### 5.2 Tabelas a Alterar

```sql
-- AiFeatureModelBinding: adicionar modo e suporte a produto externo
ALTER TABLE ai_feature_model_bindings
    ADD COLUMN mode INTEGER NOT NULL DEFAULT 1,  -- 1=Internal (default atual)
    ADD COLUMN external_product_type INTEGER,
    ADD COLUMN external_product_model VARCHAR(64);

-- Constraint para consistência
ALTER TABLE ai_feature_model_bindings
    ADD CONSTRAINT chk_binding_mode CHECK (
        (mode = 0) OR  -- Disabled: nenhum modelo necessário
        (mode = 1 AND required_model_id IS NOT NULL) OR  -- Internal
        (mode = 2 AND external_product_type IS NOT NULL)  -- ExternalProduct
    );
```

### 5.3 Seed Data Atualizado

```csharp
// Novos modelos no DefaultModelCatalog:
// - gemini-1.5-pro (Google) → Provider: "gemini"
// - gemini-1.5-flash (Google) → Provider: "gemini"

// Novos providers no seeder:
// - GeminiProvider (novo)
// - NullProvider (sempre registrado)
// - ExternalAiProductProvider (sempre registrado)
```

---

## 6. API — NOVOS ENDPOINTS

### 6.1 User AI Preferences

```
GET    /api/v1/me/ai-preferences              → Lista preferências do usuário logado
GET    /api/v1/me/ai-preferences/{featureKey} → Preferência para feature específica
PUT    /api/v1/me/ai-preferences              → Cria/atualiza preferência
DELETE /api/v1/me/ai-preferences/{featureKey} → Remove preferência (volta ao default do tenant)
GET    /api/v1/me/ai-availability             → Lista features disponíveis e status
GET    /api/v1/me/ai-execution-preview        → Preview de qual provider seria usado
```

### 6.2 Admin AI Configuration (expansão)

```
GET    /api/v1/ai/user-preferences?userId={id}   → Lista preferências de um usuário (admin)
GET    /api/v1/ai/user-preferences/summary       → Dashboard de adoção de IA
GET    /api/v1/ai/execution-log                  → Log de execuções do gateway
```

### 6.3 Feature Model Bindings (já existe, expandir)

```
POST   /api/v1/ai/feature-model-bindings         → Suportar mode: disabled/external
```

---

## 7. IMPACTO POR MÓDULO

### 7.1 Módulo `aiknowledge` — Maior Impacto

| Área | Mudança | Esforço |
|------|---------|---------|
| Domain | Novas entidades `UserAiPreference`, enums | 🔵 Baixo |
| Application | Novo `IAiExecutionGateway` + implementação | 🟡 Médio |
| Application | Refatorar `AiAgentRuntimeService` para usar gateway | 🟡 Médio |
| Application | Refatorar 14 agents | 🟡 Médio |
| Application | Novo `NullAiProvider` | 🔵 Baixo |
| Application | Novo `GeminiProvider` | 🟡 Médio |
| Infrastructure | Migração EF Core + RLS | 🔵 Baixo |
| API | Novos endpoints de preferência | 🔵 Baixo |
| API | Expandir Feature-Model Bindings | 🔵 Baixo |

### 7.2 Módulos Consumidores — Impacto Médio

| Módulo | Serviço | Mudança |
|--------|---------|---------|
| catalog | `AiDraftGeneratorService` | Injetar `IAiExecutionGateway`, usar `FeatureKey = "catalog.contract-draft"` |
| catalog | `OllamaCompletionClient` | Renomear para `AiCompletionClient`, usar gateway |
| governance | `AiDashboardComposerService` | Usar gateway com `FeatureKey = "governance.ai-dashboard"` |
| operationalintelligence | Narrative generators | Adicionar opção de IA real via gateway, manter fallback template |

### 7.3 Módulo `configuration` — Impacto Baixo

- Nova feature flag: `ai.user-preferences.enabled`
- Atualizar seeder de flags

### 7.4 Frontend / IDE Extensions — Impacto Médio

- Nova tela: "Preferências de IA" por usuário
- Indicador visual de qual IA está sendo usada em cada feature
- Botão "Desabilitar IA" com confirmação
- Preview de provider/modelo antes de executar

---

## 8. PLANO DE IMPLEMENTAÇÃO FASEADO

### Fase 1 — Fundação (2-3 sprints)

1. **Criar `NullAiProvider`**
   - Implementar interface, registrar no DI
   - Testar graceful degradation em chat e agents

2. **Criar `IAiExecutionGateway` + implementação base**
   - Resolver por: feature flag → user preference → tenant binding → system default
   - Integrar com governança existente (access policy, quota, guardrails)
   - Suportar modo Internal e Disabled

3. **Criar entidade `UserAiPreference` + repositório + API**
   - Migration EF Core, RLS
   - Endpoints CRUD em `/api/v1/me/ai-preferences`
   - Seed de dados de exemplo

4. **Expandir `AiFeatureModelBinding` com modo Disabled**
   - Migration: adicionar `mode` column
   - Atualizar endpoints e lógica de resolução

5. **Refatorar 1-2 agents piloto**
   - Escolher agents menos críticos (ex: `DocAgent`, `ReflectionAgent`)
   - Migrar para `IAiExecutionGateway`
   - Validar pipeline completo

### Fase 2 — IA Externa via API (2 sprints)

6. **Criar `GeminiProvider`**
   - Implementar `IAiProvider`, `IChatCompletionProvider`
   - Integrar com Google Generative Language API
   - Adicionar ao Model Registry

7. **Expandid `ExternalAiProductProvider`**
   - Mapear produtos para providers internos:
     - ChatGPT → OpenAiProvider
     - Claude Code → AnthropicProvider
     - Gemini → GeminiProvider
   - Adicionar metadados de display (logo, descrição, URL)

8. **Refatorar todos os 14 agents**
   - Migrar todos para `IAiExecutionGateway`
   - Remover hardcodes de provider

9. **Refatorar módulos consumidores**
   - catalog, governance, operationalintelligence

### Fase 3 — GitHub Copilot & Polish (2 sprints)

10. **GitHub Copilot Integration** (se viável)
    - Research da Copilot Chat API
    - OAuth flow com GitHub App
    - Adapter `CopilotProvider`

11. **Frontend — Tela de Preferências**
    - Tela por usuário para escolher IA por feature
    - Preview de provider/modelo
    - Toggle global "Desabilitar todas as IAs"

12. **Analytics & Observability**
    - Dashboard de adoção (quantos usuários usam qual IA)
    - Métricas de fallback
    - Alertas de quota

### Fase 4 — Operational Intelligence Real (1-2 sprints)

13. **Migrar features template-based para IA real (opcional)**
    - `GenerateAnomalyNarrative`
    - `GenerateHealingRecommendation`
    - `GenerateIncidentNarrative`
    - Manter fallback template quando IA está desabilitada

---

## 9. DECISÕES ARQUITETURAIS (ADRs Propostos)

### ADR-0XX: AI Execution Gateway como Ponto Único de Entrada

**Contexto:** Múltiplos módulos consomem IA de formas dispersas.
**Decisão:** Criar `IAiExecutionGateway` como único ponto de entrada.
**Consequências:**
- ✅ Governança centralizada e consistente
- ✅ Fácil adicionar novos providers
- ✅ Preferências do usuário respeitadas em toda a plataforma
- ⚠️ Custo de refatorar módulos existentes
- ⚠️ Gateway se torna ponto único de falha (mitigar com circuit breaker)

### ADR-0XX: Null Object Pattern para IA Desabilitada

**Contexto:** Precisamos desabilitar IA sem quebrar features.
**Decisão:** `NullAiProvider` implementa as mesmas interfaces mas retorna resposta vazia/erro controlado.
**Consequências:**
- ✅ Módulos não precisam de `if (aiEnabled)` espalhado
- ✅ Fallback automático e previsível
- ✅ Fácil testar

### ADR-0XX: User Preference sobrepõe Tenant Binding

**Contexto:** Conflito entre preferência do usuário e configuração do tenant.
**Decisão:** User preference tem precedência SOBRE tenant binding, MAS ABAIXO de access policy.
**Consequências:**
- ✅ Usuário tem autonomia
- ✅ Admin ainda pode bloquear via Access Policy
- ⚠️ Pode gerar inconsistência se usuário escolher modelo caro

### ADR-0XX: Produtos Externos Mapeiam para Providers Internos

**Contexto:** ChatGPT, Claude, Gemini não têm APIs de integração direta como "produto".
**Decisão:** "Escolher ChatGPT" significa usar a API da OpenAI. "Escolher Claude" significa usar a API da Anthropic. A diferença é puramente de UX/percepção do usuário.
**Consequências:**
- ✅ Técnicamente viável imediatamente
- ✅ Mesmo contrato de governança (quotas, custos, auditoria)
- ⚠️ Usuário pode esperar experiência idêntica ao app web (limitação a documentar)

---

## 10. RISCOS E MITIGAÇÕES

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Refatorar 14 agents quebra funcionalidades | Média | Alto | Fasear refatoração; manter testes de integração; feature flags |
| GitHub Copilot API não permite integração adequada | Alta | Médio | Deferir para fase 3; documentar limitação; focar nos 3 primeiros |
| Usuários desabilitarem IA massivamente | Baixa | Médio | Analytics de adoção; entender motivos; melhorar qualidade das respostas |
| Performance do gateway como ponto central | Média | Médio | Cache de preferências (Redis); resolver provider async com timeout curto |
| Complexidade de UX (muitas opções) | Média | Médio | Defaults sensatos; interface simplificada ("Automático", "Rápido", "Preciso", "Desligado") |

---

## 11. CONCLUSÃO

O NexTraceOne já tem **~70% da infraestrutura necessária** para atender ao requisito de flexibilidade de IA. Os principais trabalhos são:

1. **Introduzir o AI Execution Gateway** (~30% do esforço)
2. **Criar User AI Preferences** (~20% do esforço)
3. **Refatorar agents e módulos consumidores** (~40% do esforço)
4. **Adicionar GeminiProvider** (~5% do esforço)
5. **Frontend de preferências** (~5% do esforço)

A arquitetura proposta é **incremental e não-destrutiva**. O sistema atual continua funcionando enquanto cada componente é migrado para o gateway. O uso de feature flags permite lançamento gradual.

**Recomendação:** Iniciar pela Fase 1 (NullProvider + Gateway + User Preferences) para entregar valor imediato ("desligar IA") e estabelecer a fundação arquitetural. As fases subsequentes se beneficiam naturalmente dessa base.

---

## APÊNDICE A: FeatureKeys Propostos

```
aiknowledge.assistant-chat
aiknowledge.agent.incident-responder
aiknowledge.agent.change-advisor
aiknowledge.agent.security-review
aiknowledge.agent.architecture-fitness
aiknowledge.agent.documentation-quality
aiknowledge.agent.service-analyst
aiknowledge.agent.dependency-advisor
aiknowledge.agent.doc-agent
aiknowledge.agent.pr-agent
aiknowledge.agent.reflection
aiknowledge.agent.web-search
aiknowledge.agent.contract-assistant
aiknowledge.agent.test-generator
aiknowledge.agent.service-scaffold
aiknowledge.rag-retrieval
aiknowledge.embedding-generation

catalog.contract-draft
catalog.schema-analysis
catalog.service-description

governance.ai-dashboard
governance.policy-generation

ops.anomaly-narrative
ops.healing-recommendation
ops.incident-narrative
ops.failure-prediction
ops.capacity-forecast
```

## APÊNDICE B: Diagrama de Sequência — Resolução de Execução

```
Usuário → AIExecutionGateway.ExecuteAsync(request)
    → FeatureFlagService.IsEnabled(request.FeatureKey)?
        NÃO → retorna AI_DISABLED
    → UserPreferenceRepository.GetAsync(userId, featureKey)
        ENCONTRADA → usa PreferenceType
        NÃO → continua
    → TenantBindingRepository.GetAsync(tenantId, featureKey)
        ENCONTRADA → usa Mode
        NÃO → continua
    → AiRoutingResolver.ResolveAsync(persona, useCase)
        ENCONTRADA → usa path
        NÃO → continua
    → AiModelCatalogService.ResolveDefaultModelAsync("chat")
    → ProviderFactory.GetChatProvider(resolvedModel.ProviderId)
    → AiModelAuthorizationService.ValidateAsync(userId, modelId)
    → AiTokenQuotaService.CheckAsync(userId, tenantId, estimatedTokens)
    → AiGuardrailEnforcementService.EvaluateInputAsync(prompt)
    → EXECUTA provider.CompleteAsync()
    → AiGuardrailEnforcementService.EvaluateOutputAsync(response)
    → Persiste AIUsageEntry + AIRoutingDecision
    → RETORNA AiExecutionResult
```
