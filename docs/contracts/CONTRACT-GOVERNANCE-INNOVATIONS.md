# Contract Governance Innovations (Module 3)

## Scope
Implementation baseline for the following capabilities in NexTraceOne Contract Governance:

1. Semantic Contract Diff with AI classification
2. Contract Test Generation (consumer-driven)
3. Automated Contract Changelog
4. Contract Health Dashboard
5. AsyncAPI/Event Contract Linting extension

## Product Alignment
These capabilities reinforce:
- Contract Governance
- Change Intelligence
- Source of Truth
- AI Governance

## 1) Semantic Contract Diff with AI
### Goal
When a new contract version is uploaded, classify change impact as:
- `breaking`
- `additive`
- `deprecation`

### Minimal Architecture
- Reuse existing semantic diff output as deterministic source.
- Add AI classification as secondary layer with:
  - model policy resolution
  - prompt/context audit trail
  - cost/token logging
- Persist result linked to contract version and known consumers.

### Suggested API
- `POST /api/v1/contracts/{versionId}/diff/classify-ai`
- Response:
  - `classification`
  - `confidence`
  - `rationale`
  - `consumerImpact[]`

## 2) Contract Test Generation
### Goal
Generate pact-style test scenarios from contract definitions and provide CI-ready artifacts.

### Minimal Architecture
- Input: OpenAPI/AsyncAPI/WSDL normalized model.
- Output artifacts:
  - provider conformance tests
  - consumer contract stubs
  - negative validation scenarios
- Store generated artifact metadata + provenance in contract version timeline.

### Suggested API
- `POST /api/v1/contracts/{versionId}/tests/generate`
- `GET /api/v1/contracts/{versionId}/artifacts/tests`

## 3) Automated Contract Changelog
### Goal
Create human-readable changelog on every new version.

### Minimal Architecture
- Trigger on version creation/publish.
- Aggregate from semantic diff + lifecycle transition + impacted consumers.
- Output sections:
  - what changed
  - who is impacted
  - recommended migration plan

### Suggested API
- `POST /api/v1/contracts/{versionId}/changelog/generate`
- `GET /api/v1/contracts/{versionId}/changelog`

## 4) Contract Health Dashboard
### Goal
Consolidated health view per contract and environment.

### Core Indicators
- versions in use by environment
- outdated consumers
- missing examples
- missing schema validation
- deprecated contracts still in use

### Suggested API
- `GET /api/v1/contracts/health/dashboard`
- `GET /api/v1/contracts/health/{contractId}`

## 5) AsyncAPI/Event Contract Linting
### Goal
Extend governance linting for event contracts.

### Rules Baseline
- event/channel naming conventions
- mandatory payload schema
- dead-letter queue declaration
- compatibility/versioning checks

### Suggested API
- `POST /api/v1/contracts/{versionId}/lint/asyncapi`
- Reuse existing validation report shape for UI consistency.

## Delivery Phasing
### Phase A (MVP)
- Contract Health Dashboard
- AsyncAPI linting baseline
- Automated changelog (deterministic)

### Phase B
- AI diff classification with governance/audit
- test generation v1

### Phase C
- CI blocking policy integration per environment
- advanced migration recommendations

## Quality Gates
- i18n keys for all new UI labels
- audit entries for all AI-assisted operations
- environment-aware filtering for all health indicators
- explicit traceability from version -> diff -> changelog -> tests
