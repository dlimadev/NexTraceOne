# Configuration — Change Types, Blast Radius, Release Scoring & Rollback

## Change Types & Criticality

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.types_enabled` | Tipos de mudança suportados | Feature, Bugfix, Hotfix, Refactor, Config, Infrastructure, Rollback |
| `change.criticality_defaults` | Criticidade padrão por tipo | Hotfix→Critical, Infrastructure→High, Feature→Medium, Refactor→Low |
| `change.risk_classification` | Classificação de risco por tipo | Hotfix/Infrastructure/Rollback→High+approval, Feature→Medium |
| `change.severity_criteria` | Critérios de severidade da mudança | affectedServices≥5, affectedDeps≥10, crossDomain, dataSchemaChange |

## Blast Radius

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.blast_radius.thresholds` | Score thresholds de classificação | Critical≥90, High≥70, Medium≥40, Low≥0 |
| `change.blast_radius.categories` | Categorias com labels, cores e ações | Critical→RequireApproval, High→RequireReview, Medium→Notify, Low→AutoApprove |
| `change.blast_radius.environment_weights` | Peso de impacto por ambiente | Production:1.0, PreProduction:0.6, Staging:0.4, Development:0.2 |

Blast radius thresholds suportam scope Environment, permitindo thresholds diferentes para Production vs outros ambientes.

## Release Scoring

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.release_score.weights` | Pesos do confidence score (soma=100) | testCoverage:20, codeReview:15, blastRadius:20, historicalSuccess:15, docs:10, governance:10, evidence:10 |
| `change.release_score.thresholds` | Classificação do score | HighConfidence≥80, Moderate≥60, LowConfidence≥40, Block≥0 |

## Evidence Pack

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.evidence_pack.required` | Evidence pack obrigatório | true |
| `change.evidence_pack.requirements` | Requisitos por ambiente | Production: testReport+securityScan+approval+rollbackPlan |
| `change.evidence_pack.by_criticality` | Requisitos por criticidade | Critical: securityScan+approval+rollback+impactAnalysis |

## Rollback Policy

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.rollback.recommendation_policy` | Política de recomendação de rollback | autoRecommend se score<40, autoRecommend se incidente correlacionado, requirePlan para Production e Critical |

## Release Calendar

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.release_calendar.window_policy` | Constraints de window por tipo de mudança | Hotfix: allow outside window (with approval), Feature: window obrigatório |
| `change.release_calendar.by_environment` | Calendar por ambiente | Production: seg-qui, 08h-18h, window obrigatório |

## Incident Correlation

| Key | Descrição | Default |
|-----|-----------|---------|
| `change.incident_correlation.enabled` | Correlação release↔incidente ativa | true |
| `change.incident_correlation.window_hours` | Janela de correlação em horas | 24 (min:1, max:168) |
