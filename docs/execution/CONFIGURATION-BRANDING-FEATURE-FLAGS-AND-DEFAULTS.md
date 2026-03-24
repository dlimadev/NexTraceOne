# Configuration — Branding, Feature Flags & Defaults

## Branding Settings

Branding definitions allow customization of the platform visual identity at instance and tenant levels.

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `branding.logo_url` | String | — | System, Tenant | Logo URL (light variant) |
| `branding.logo_dark_url` | String | — | System, Tenant | Logo URL (dark variant) |
| `branding.accent_color` | String | #3B82F6 | System, Tenant | Primary accent color (hex) |
| `branding.favicon_url` | String | — | System, Tenant | Custom favicon URL |
| `branding.welcome_message` | String | — | System, Tenant | Dashboard welcome message |
| `branding.footer_text` | String | — | System, Tenant | Custom footer text |

### Validation
- `branding.accent_color` must match pattern `^#[0-9a-fA-F]{6}$`
- URL fields accept any valid URL string

## Feature Flags

Feature flags control module availability and preview features per tenant.

### Module Flags

| Key | Default | Description |
|-----|---------|-------------|
| `feature.module.catalog.enabled` | true | Service Catalog module |
| `feature.module.contracts.enabled` | true | Contract Governance module |
| `feature.module.changes.enabled` | true | Change Intelligence module |
| `feature.module.operations.enabled` | true | Operations module |
| `feature.module.ai.enabled` | true | AI Hub module |
| `feature.module.governance.enabled` | true | Governance module |
| `feature.module.finops.enabled` | true | FinOps module |
| `feature.module.integrations.enabled` | true | Integration Hub module |
| `feature.module.analytics.enabled` | true | Product Analytics module |

### Preview Feature Flags

| Key | Default | Description |
|-----|---------|-------------|
| `feature.preview.ai_agents.enabled` | false | AI Agents (beta) |
| `feature.preview.environment_comparison.enabled` | true | Environment Comparison |

### Rules
- All feature flags are Boolean type with `toggle` UI editor
- Scoped to System and Tenant (inheritable)
- Changes are audited
- Module disabling affects sidebar visibility and API access

## Experience Defaults

Experience defaults are inherited through the scope hierarchy:

```
System (instance.default_language = "en")
  └── Tenant (tenant.default_language = "pt-BR")  ← override
        └── User (not yet in Phase 1)
```

Future phases will extend defaults to user-level preferences.
