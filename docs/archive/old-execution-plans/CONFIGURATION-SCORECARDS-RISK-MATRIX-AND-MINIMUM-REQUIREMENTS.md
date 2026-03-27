# Configuration — Scorecards, Risk Matrix and Minimum Requirements

## Overview

This document describes the parameterization of scorecards, risk matrix, and minimum requirements by system/API type delivered in Phase 4.

## Scorecards

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.scorecard.enabled` | Boolean | true | System, Tenant | Scorecard calculation active |
| `governance.scorecard.thresholds` | Json | Excellent≥90, Good≥70, Fair≥50, Poor≥0 | System, Tenant | Score classification thresholds |
| `governance.scorecard.weights` | Json | Security:30, Quality:25, Operational:25, Documentation:20 | System, Tenant | Category weights (sum to 100) |
| `governance.scorecard.thresholds_by_environment` | Json | Production: stricter thresholds | System, Tenant | Per-environment thresholds |

### Default Thresholds

| Classification | Score |
|---------------|-------|
| Excellent | ≥ 90 |
| Good | ≥ 70 |
| Fair | ≥ 50 |
| Poor | < 50 |

### Category Weights

| Category | Weight |
|----------|--------|
| Security | 30% |
| Quality | 25% |
| Operational | 25% |
| Documentation | 20% |

### Production Thresholds

Production environments have stricter thresholds by default:
- Excellent ≥ 95, Good ≥ 80, Fair ≥ 60, Poor < 60

## Risk Matrix

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.risk.matrix` | Json | 3×3 likelihood×impact matrix | System, Tenant | Risk matrix definition |
| `governance.risk.thresholds` | Json | Critical≥90, High≥70, Medium≥40, Low≥0 | System, Tenant, Environment | Risk thresholds |
| `governance.risk.labels` | Json | Labels with colors per level | System, Tenant | Display labels and colors |
| `governance.risk.thresholds_by_criticality` | Json | Per-service-criticality thresholds | System, Tenant | Thresholds by service criticality |

### Risk Matrix (Likelihood × Impact)

| | High Impact | Medium Impact | Low Impact |
|--|-------------|---------------|------------|
| **High Likelihood** | Critical | High | Medium |
| **Medium Likelihood** | High | Medium | Low |
| **Low Likelihood** | Medium | Low | Low |

### Risk Classification Colors

| Level | Color | Hex |
|-------|-------|-----|
| Critical | Red | #DC2626 |
| High | Amber | #F59E0B |
| Medium | Blue | #3B82F6 |
| Low | Green | #10B981 |

### Service Criticality Override

Critical services have lower thresholds (more sensitive risk detection):
- Critical services: Critical≥80, High≥60, Medium≥30, Low<30
- Standard services: Critical≥90, High≥70, Medium≥40, Low<40

## Minimum Requirements by System/API Type

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.requirements.by_system_type` | Json | Per-system-type requirements | System, Tenant | Requirements by system type |
| `governance.requirements.by_api_type` | Json | Per-API-classification requirements | System, Tenant | Requirements by API type |
| `governance.requirements.mandatory_evidence_by_classification` | Json | Per-classification evidence | System, Tenant | Evidence by classification |
| `governance.requirements.min_compliance_profile` | Json | Profile by classification | System, Tenant | Minimum profile |
| `governance.requirements.promotion_gates` | Json | Gates per environment | System, Tenant, Environment | Promotion governance gates |

### Requirements by System Type

| System Type | Mandatory Policies | Mandatory Pack | Min Score |
|-------------|--------------------|----------------|-----------|
| REST | SecurityBaseline, ApiVersioning | ApiGovernance | 70 |
| SOAP | SecurityBaseline, ApiVersioning | ApiGovernance | 70 |
| Event | SecurityBaseline | CoreGovernance | 60 |
| Background | SecurityBaseline | CoreGovernance | 50 |

### Requirements by API Type

| API Type | Mandatory Policies | Min Score |
|----------|--------------------|-----------|
| Public | SecurityBaseline, ApiVersioning, DocumentationCoverage | 80 |
| Internal | SecurityBaseline, ApiVersioning | 60 |
| Partner | SecurityBaseline, ApiVersioning, DocumentationCoverage | 75 |

### Promotion Gates

| Environment | Requirements |
|-------------|-------------|
| Production | minScore: 70, requiredProfile: Enhanced, allBlockingPoliciesMet: true |
| PreProduction | minScore: 50, allBlockingPoliciesMet: true |

These gates integrate with Phase 3 workflow gates to enforce governance requirements during promotion.

## Effective Settings

The effective settings explorer shows:
- Active scorecard thresholds and weights per tenant
- Risk matrix and classification per environment
- Minimum requirements per system/API type
- Promotion gates per environment
- Origin of each value (system default, tenant override, environment override)
