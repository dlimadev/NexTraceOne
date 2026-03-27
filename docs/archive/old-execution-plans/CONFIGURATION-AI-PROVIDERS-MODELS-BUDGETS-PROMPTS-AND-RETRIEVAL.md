# Configuration — AI Providers, Models, Budgets, Prompts & Retrieval

## AI Providers & Models

### Enabled Providers (`ai.providers.enabled`)
- Default: OpenAI, AzureOpenAI, Internal
- Scope: System, Tenant
- Editor: JSON

### Enabled Models (`ai.models.enabled`)
- Default: gpt-4o, gpt-4o-mini, gpt-3.5-turbo, internal-llm
- Scope: System, Tenant
- Editor: JSON

### Default Provider by Capability (`ai.providers.default_by_capability`)
Maps each capability (chat, analysis, classification, draftGeneration, retrievalAugmented, codeReview) to a default provider.

### Default Model by Capability (`ai.models.default_by_capability`)
Maps each capability to a default model.

## Fallback & Access Control

### Provider Fallback Order (`ai.providers.fallback_order`)
- Default: AzureOpenAI → OpenAI → Internal
- Scope: System, Tenant

### Allow External AI (`ai.usage.allow_external`)
- Default: true
- Scope: System, Tenant, **Environment** — allows blocking external AI in specific environments

### Blocked Environments (`ai.usage.blocked_environments`)
- System-only, non-inheritable
- Permanently blocks external AI in specified environments

### Internal-Only Capabilities (`ai.usage.internal_only_capabilities`)
- Default: classification
- Enforces internal-only AI for sensitive capabilities

## Budgets & Quotas

### Token Budget by User/Team/Tenant
Per-level monthly token budgets with alertOnExceed and hardLimit flags.

### Quota by Capability (`ai.quota.by_capability`)
Daily token limits per capability (chat, analysis, draftGeneration, retrievalAugmented).

### Usage Limits by Environment (`ai.usage.limits_by_environment`)
Per-environment daily token limits (Production = 500K, Development = 50K).

### Budget Exceed Policy (`ai.budget.exceed_policy`)
- Options: Warn, Block, Throttle
- Validated enum selection

### Budget Warning Thresholds (`ai.budget.warning_thresholds`)
Percentage thresholds: 70% → Low, 85% → Medium, 95% → High, 100% → Critical

## Retention & Audit

### Conversation Retention (`ai.retention.conversation_days`)
- Default: 90 days (range 1-365)

### Artifact Retention (`ai.retention.artifact_days`)
- Default: 180 days (range 1-730)

### Audit Level (`ai.audit.level`)
- Options: Minimal, Standard, Full
- Default: Standard

### Log Prompts/Responses
Toggle controls for full audit logging of prompts and responses.

## Prompts & Retrieval

### Base Prompts by Capability (`ai.prompts.base_by_capability`)
System prompts per capability with NexTraceOne context.

### Allow Tenant Prompt Override (`ai.prompts.allow_tenant_override`)
- System-only, non-inheritable
- Default: false

### Retrieval Settings
- **Top-K**: Default 5 (range 1-50)
- **Temperature**: Default 0.7 (range 0.0-2.0)
- **Max Tokens**: Default 4096 (range 100-128000)
- **Similarity Threshold**: Default 0.7 (range 0.0-1.0)

### Source Allow/Deny Lists
Control which sources are available for document retrieval.

### Context Sources by Environment (`ai.retrieval.context_by_environment`)
Per-environment control of telemetry, documents, and incidents context.

## Effective Settings

All definitions support effective settings explorer with:
- System → Tenant → Environment inheritance chain
- Override indicators
- Resolved scope display
- Mandatory rule indicators for non-inheritable settings
