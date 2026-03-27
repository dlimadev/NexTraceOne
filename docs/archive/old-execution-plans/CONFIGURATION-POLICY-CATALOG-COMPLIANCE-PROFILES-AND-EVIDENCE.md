# Configuration — Policy Catalog, Compliance Profiles and Evidence

## Overview

This document describes the parameterization of governance policies, compliance profiles, and evidence requirements delivered in Phase 4 of the NexTraceOne configuration platform.

## Policy Catalog

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.policies.enabled` | Json | 5 default policies | System, Tenant | Enabled policy IDs |
| `governance.policies.severity` | Json | Critical/High/Medium map | System, Tenant | Severity per policy |
| `governance.policies.criticality` | Json | Blocking/NonBlocking/Advisory | System, Tenant | Criticality per policy |
| `governance.policies.category_map` | Json | Security/Quality/Operational/Docs | System, Tenant | Category per policy |
| `governance.policies.applicability` | Json | By system/API type | System, Tenant | Applicability rules |

### Default Policies

| Policy | Severity | Criticality | Category |
|--------|----------|-------------|----------|
| SecurityBaseline | Critical | Blocking | Security |
| ApiVersioning | High | NonBlocking | Quality |
| DocumentationCoverage | Medium | Advisory | Documentation |
| TestCoverage | High | NonBlocking | Quality |
| OwnershipRequired | Critical | Blocking | Operational |

## Compliance Profiles

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.compliance.profiles.enabled` | Json | Standard, Enhanced, Strict | System, Tenant | Active profiles |
| `governance.compliance.profiles.default` | String | Standard | System, Tenant, Environment | Default profile |
| `governance.compliance.profiles.policies_map` | Json | Profile→Policies mapping | System, Tenant | Policies per profile |
| `governance.compliance.profiles.by_environment` | Json | Production→Strict | System, Tenant | Profile per environment |

### Profile Policies

| Profile | Included Policies |
|---------|-------------------|
| Standard | SecurityBaseline, OwnershipRequired |
| Enhanced | Standard + ApiVersioning, TestCoverage |
| Strict | Enhanced + DocumentationCoverage |

### Environment Mapping

- **Production** → Strict
- **PreProduction** → Enhanced
- **Development** → Standard

## Evidence Requirements

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.evidence.types_accepted` | Json | 6 types | System, Tenant | Accepted evidence types |
| `governance.evidence.required_by_policy` | Json | Per-policy requirements | System, Tenant | Evidence per policy |
| `governance.evidence.expiry_days` | Integer | 90 | System, Tenant, Environment | Default expiry (1-730 days) |
| `governance.evidence.expiry_by_criticality` | Json | Critical: 30d, High: 60d | System, Tenant | Expiry per criticality |
| `governance.evidence.expired_action` | String | Notify | System, Tenant | Expiry action (Notify, Block, Degrade) |
| `governance.evidence.required_by_environment` | Json | Production mandatory | System, Tenant | Per-environment rules |

### Accepted Evidence Types

- Document, Screenshot, TestReport, ScanResult, AuditLog, Attestation

### Evidence Expiry by Criticality

| Criticality | Expiry Days |
|-------------|-------------|
| Critical | 30 |
| High | 60 |
| Medium | 90 |
| Low | 180 |

## Effective Settings

The effective settings explorer shows:
- Which policies are enabled (resolved from System → Tenant)
- The active compliance profile per environment
- Evidence requirements per policy
- Evidence expiry per criticality level
- The origin of each value (default, inherited, or overridden)
