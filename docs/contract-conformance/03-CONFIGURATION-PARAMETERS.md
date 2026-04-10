# Contract Conformance — Parametrização e Configuração

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. Princípio de design

Toda a parametrização de conformance deve ser **persistida no módulo `Configuration`** existente (`cfg_*`), e não em `appsettings.json`. Isto garante que:

- Administradores funcionais alteram políticas sem redeploy
- Cada tenant, equipa e serviço pode ter política própria
- Alterações de política são auditadas automaticamente
- A hierarquia de herança (`System → Tenant → Team → Service`) já está implementada

A implementação usa o padrão `ConfigurationDefinition` + `ConfigurationEntry` já existente.

---

## 2. Novos `ConfigurationDefinition` a registar

### Grupo: `contracts.conformance`

#### `contracts.conformance.resolution_strategy`

```
Key:           contracts.conformance.resolution_strategy
DisplayName:   Contract Resolution Strategy for CI
Category:      Contract Conformance
ValueType:     String (enum)
DefaultValue:  auto
AllowedValues: auto | slug_plus_environment | ci_token_bound | explicit_id
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

**Comportamento por valor:**

| Valor | Descrição |
|-------|-----------|
| `auto` | Tenta resolução por token → slug → explicit_id (hierarquia automática) |
| `slug_plus_environment` | Exige `serviceSlug` + `environmentName` no request |
| `ci_token_bound` | Exige token CI com binding; rejeita requests sem token |
| `explicit_id` | Exige `contractVersionId` ou `apiAssetId` explícito |

---

#### `contracts.conformance.blocking_policy`

```
Key:           contracts.conformance.blocking_policy
DisplayName:   CI Blocking Policy
Category:      Contract Conformance
ValueType:     String (enum)
DefaultValue:  breaking_only
AllowedValues: breaking_only | any_drift | score_below_threshold | warn_only | disabled
AllowedScopes: System, Tenant, Team, Environment
IsEditable:    true
IsInheritable: true
```

**Comportamento por valor:**

| Valor | Descrição | Uso recomendado |
|-------|-----------|-----------------|
| `breaking_only` | Bloqueia apenas se existirem desvios breaking | **Padrão — equilibrado** |
| `any_drift` | Bloqueia se qualquer desvio for detectado | Ambientes produtivos com contratos críticos |
| `score_below_threshold` | Bloqueia se score de conformance < threshold | Controlo por qualidade |
| `warn_only` | Nunca bloqueia — apenas regista e avisa | Onboarding / migração |
| `disabled` | Não valida — apenas regista a spec recebida | Desactivação temporária |

---

#### `contracts.conformance.score_threshold`

```
Key:           contracts.conformance.score_threshold
DisplayName:   Minimum Conformance Score to Pass
Category:      Contract Conformance
ValueType:     Decimal
DefaultValue:  80
AllowedScopes: System, Tenant, Team, Environment
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 0, "max": 100 }
```

Usado quando `blocking_policy = score_below_threshold`. Um score de conformance abaixo deste valor bloqueia o pipeline.

---

#### `contracts.conformance.required_environments`

```
Key:           contracts.conformance.required_environments
DisplayName:   Environments Where Conformance Check Is Mandatory
Category:      Contract Conformance
ValueType:     JsonArray
DefaultValue:  ["pre-production", "production"]
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

Em ambientes não listados, o check é executado mas o resultado nunca bloqueia (equivalente a `warn_only` local).

---

#### `contracts.conformance.allow_additional_endpoints`

```
Key:           contracts.conformance.allow_additional_endpoints
DisplayName:   Allow Implementation to Have Extra Endpoints
Category:      Contract Conformance
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

Quando `true`: endpoints implementados mas não presentes no contrato desenhado são reportados como `drift` mas **não como breaking**. Quando `false`: endpoints extra são tratados como violação de política.

---

#### `contracts.conformance.allow_additional_fields`

```
Key:           contracts.conformance.allow_additional_fields
DisplayName:   Allow Extra Fields in Response Bodies
Category:      Contract Conformance
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.conformance.ignored_paths`

```
Key:           contracts.conformance.ignored_paths
DisplayName:   Paths to Ignore in Conformance Check
Category:      Contract Conformance
ValueType:     JsonArray
DefaultValue:  ["/health", "/metrics", "/swagger"]
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

Paths que existem na implementação mas não no contrato desenhado e que **nunca devem ser reportados** (endpoints de infraestrutura, health checks, etc.).

---

#### `contracts.conformance.auto_register_deployment`

```
Key:           contracts.conformance.auto_register_deployment
DisplayName:   Auto-Register ContractDeployment on Successful Check
Category:      Contract Conformance
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
```

Quando `true`: um check bem-sucedido regista automaticamente um `ContractDeployment` para o ambiente, mantendo o histórico de deploys sincronizado.

---

### Grupo: `contracts.changelog`

#### `contracts.changelog.auto_generate`

```
Key:           contracts.changelog.auto_generate
DisplayName:   Auto-Generate Changelog on Contract Events
Category:      Contract Changelog
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.changelog.retention_days`

```
Key:           contracts.changelog.retention_days
DisplayName:   Changelog Retention in Days
Category:      Contract Changelog
ValueType:     Integer
DefaultValue:  365
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 30, "max": 3650 }
```

---

#### `contracts.changelog.include_conformance_events`

```
Key:           contracts.changelog.include_conformance_events
DisplayName:   Include CI Conformance Events in Changelog
Category:      Contract Changelog
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.changelog.include_runtime_drift`

```
Key:           contracts.changelog.include_runtime_drift
DisplayName:   Include Runtime Drift Events in Changelog
Category:      Contract Changelog
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
```

---

### Grupo: `contracts.notifications`

#### `contracts.notifications.breaking_change_alert`

```
Key:           contracts.notifications.breaking_change_alert
DisplayName:   Alert Consumers on Breaking Contract Change
Category:      Contract Notifications
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.notifications.channels`

```
Key:           contracts.notifications.channels
DisplayName:   Notification Channels for Contract Events
Category:      Contract Notifications
ValueType:     JsonArray
DefaultValue:  ["email"]
AllowedValues: email | slack | teams | webhook
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.notifications.notify_on_drift`

```
Key:           contracts.notifications.notify_on_drift
DisplayName:   Notify Team on Runtime Contract Drift
Category:      Contract Notifications
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.notifications.notify_on_deprecation`

```
Key:           contracts.notifications.notify_on_deprecation
DisplayName:   Notify Consumers When Contract Is Deprecated
Category:      Contract Notifications
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
```

---

#### `contracts.notifications.deprecation_advance_days`

```
Key:           contracts.notifications.deprecation_advance_days
DisplayName:   Days Before Sunset to Notify Consumers
Category:      Contract Notifications
ValueType:     Integer
DefaultValue:  30
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 7, "max": 365 }
```

---

### Grupo: `contracts.ci_token`

#### `contracts.ci_token.max_per_service`

```
Key:           contracts.ci_token.max_per_service
DisplayName:   Maximum Active CI Tokens per Service
Category:      Contract CI Tokens
ValueType:     Integer
DefaultValue:  5
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 1, "max": 20 }
```

---

#### `contracts.ci_token.default_expiry_days`

```
Key:           contracts.ci_token.default_expiry_days
DisplayName:   Default Expiry Days for CI Tokens
Category:      Contract CI Tokens
ValueType:     Integer
DefaultValue:  365
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 1, "max": 730 }
```

---

#### `contracts.ci_token.require_expiry`

```
Key:           contracts.ci_token.require_expiry
DisplayName:   Require Expiry Date on All CI Tokens
Category:      Contract CI Tokens
ValueType:     Boolean
DefaultValue:  false
AllowedScopes: System, Tenant
IsEditable:    true
IsInheritable: true
```

---

### Grupo: `contracts.promotion_gate`

#### `contracts.promotion_gate.require_conformance_check`

```
Key:           contracts.promotion_gate.require_conformance_check
DisplayName:   Require Conformance Check Before Environment Promotion
Category:      Contract Promotion Gates
ValueType:     Boolean
DefaultValue:  true
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
```

Quando `true`: o gate de promoção entre ambientes (ex: PRE → PROD) verifica se existe pelo menos um `ConformanceCheck` com status `Compliant` para o ambiente de origem.

---

#### `contracts.promotion_gate.conformance_max_age_hours`

```
Key:           contracts.promotion_gate.conformance_max_age_hours
DisplayName:   Max Age of Conformance Check to Accept in Promotion Gate
Category:      Contract Promotion Gates
ValueType:     Integer
DefaultValue:  24
AllowedScopes: System, Tenant, Team
IsEditable:    true
IsInheritable: true
ValidationRules: { "min": 1, "max": 168 }
```

Se o último `ConformanceCheck` tiver mais de N horas, o gate de promoção considera-o inválido e exige novo check.

---

## 3. Hierarquia de herança das configurações

```
System (defaults globais)
  └── Tenant (override por organização)
        └── Team (override por equipa)
              └── Environment (override por ambiente — ex: produção mais restrito)
```

O `ContractConformancePolicyService` resolve a política efectiva seguindo a hierarquia, usando o `IConfigurationModule` já existente.

---

## 4. Exemplo de configuração por cenário

### Cenário A — Onboarding (equipas novas)
```json
{
  "contracts.conformance.blocking_policy": "warn_only",
  "contracts.conformance.required_environments": [],
  "contracts.changelog.auto_generate": true
}
```

### Cenário B — Ambiente PRE-PROD padrão
```json
{
  "contracts.conformance.blocking_policy": "breaking_only",
  "contracts.conformance.required_environments": ["pre-production"],
  "contracts.conformance.score_threshold": 75,
  "contracts.promotion_gate.require_conformance_check": true
}
```

### Cenário C — Ambiente PROD com contratos críticos
```json
{
  "contracts.conformance.blocking_policy": "any_drift",
  "contracts.conformance.required_environments": ["pre-production", "production"],
  "contracts.conformance.score_threshold": 90,
  "contracts.conformance.allow_additional_endpoints": false,
  "contracts.promotion_gate.require_conformance_check": true,
  "contracts.promotion_gate.conformance_max_age_hours": 8,
  "contracts.notifications.breaking_change_alert": true
}
```

---

## 5. Implementação do seed de definições

As definições devem ser registadas via migration de dados, não de schema. Exemplo:

```csharp
// Infrastructure/Contracts/Persistence/Seeders/ContractConformanceConfigurationSeeder.cs
public static class ContractConformanceConfigurationSeeder
{
    public static IEnumerable<ConfigurationDefinition> GetDefinitions()
    {
        yield return new ConfigurationDefinition(
            key: "contracts.conformance.blocking_policy",
            displayName: "CI Blocking Policy",
            category: "Contract Conformance",
            valueType: ConfigurationValueType.String,
            defaultValue: "breaking_only",
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant,
                            ConfigurationScope.Team, ConfigurationScope.Environment],
            isEditable: true,
            isInheritable: true
        );
        // ... demais definições
    }
}
```

Este seeder é chamado no startup do módulo (ou via migration data) garantindo que as definições existem antes de serem usadas.
