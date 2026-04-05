# ADR-004: Local/Internal AI as Default Provider

## Status

Accepted

## Date

2026-02-15

## Context

NexTraceOne provides AI-assisted operations and engineering capabilities:

- Incident triage and root cause analysis
- Contract generation (REST, SOAP, AsyncAPI)
- Change impact assessment
- Compliance evidence summarization
- Developer acceleration via IDE extensions

Enterprise customers have strict data governance requirements:

- Sensitive operational data must not leave the enterprise network.
- Model usage must be auditable with full prompt/response logging.
- Token budgets and cost controls must be enforceable.
- Different teams/personas may have different model access policies.

## Decision

**Local/internal AI is the default architectural choice.** External AI providers (OpenAI, Azure OpenAI, Anthropic) are optional, governed, and auditable:

- **Model Registry**: All AI models (local and external) are registered with metadata, capabilities, and access policies.
- **AI Access Policies**: Control which users, teams, and personas can use which models.
- **Token & Budget Governance**: Per-tenant, per-team, per-model token budgets with enforcement.
- **Full Audit Trail**: Every AI interaction (prompt, context, response, cost) is logged.
- **Knowledge Sources**: AI can access service catalog, contracts, changes, incidents, and runbooks as context — governed by policies.
- **Data Classification**: Policies can prevent sensitive data from being sent to external models.

## Consequences

### Positive

- Enterprise customers can run AI fully on-premises.
- Complete audit trail for regulatory compliance.
- No dependency on external API availability.
- Cost predictability with local models.

### Negative

- Local models may have lower capability than frontier external models.
- Infrastructure requirements for running local models (GPU, memory).
- Dual-model support increases complexity.

### Mitigations

- External providers available as opt-in with full governance.
- Specialized agents per use case (contract creation, incident investigation) reduce model capability requirements.
- Knowledge source injection provides domain context that improves results regardless of model capability.
