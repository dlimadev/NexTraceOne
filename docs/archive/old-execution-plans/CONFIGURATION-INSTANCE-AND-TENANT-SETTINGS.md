# Configuration — Instance & Tenant Settings

## Instance Settings

Instance settings define the global identity and behavior of the NexTraceOne platform. They are scoped to `System` and serve as the baseline for all tenants.

### Definitions

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `instance.name` | String | NexTraceOne | Display name of the platform instance |
| `instance.commercial_name` | String | NexTraceOne Platform | Commercial/marketing name |
| `instance.default_language` | String | en | Default language (en, pt-BR, pt-PT, es) |
| `instance.default_timezone` | String | UTC | Default timezone |
| `instance.date_format` | String | yyyy-MM-dd | Default date format pattern |
| `instance.support_url` | String | — | URL for platform support |
| `instance.terms_url` | String | — | URL for terms of service |
| `instance.privacy_url` | String | — | URL for privacy policy |

### Rules
- Instance settings are `System` scope only
- They serve as defaults for all tenants
- Changes are audited with user attribution
- Settings are accessible via the effective settings API

## Tenant Settings

Tenant settings allow each organization to customize their structural behavior. They inherit from System and can be overridden at the Tenant level.

### Definitions

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `tenant.display_name` | String | — | Tenant | Custom display name |
| `tenant.default_language` | String | en | System, Tenant | Default language |
| `tenant.default_timezone` | String | UTC | System, Tenant | Default timezone |
| `tenant.contact_email` | String | — | Tenant | Primary contact email |
| `tenant.max_users` | Integer | 100 | System, Tenant | Maximum users (1-10000) |
| `tenant.max_environments` | Integer | 10 | System, Tenant | Maximum environments (1-50) |

### Precedence Example

```
Request: tenant.default_language for Tenant "acme-corp"

1. Check Tenant scope (ScopeReferenceId = acme-corp-id) → found "pt-BR" → return "pt-BR"
   OR
1. Check Tenant scope → not found
2. Check System scope → found "en" → return "en" (inherited)
   OR
1. Check Tenant scope → not found
2. Check System scope → not found
3. Use definition default → "en" (default)
```

### Multi-Tenancy Isolation
- Each tenant can only see and edit their own settings
- Tenant-scoped entries require a valid ScopeReferenceId
- Row-Level Security (RLS) enforced at the database level
- Audit trail includes tenant context
