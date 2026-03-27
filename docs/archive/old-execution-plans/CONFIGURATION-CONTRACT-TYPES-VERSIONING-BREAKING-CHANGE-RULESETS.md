# Configuration — Contract Types, Versioning, Breaking Change & Rulesets

## Contract Types

O NexTraceOne suporta os seguintes tipos de contrato, configuráveis por tenant:

| Key | Tipos Default |
|-----|---------------|
| `catalog.contract.types_enabled` | REST, SOAP, GraphQL, gRPC, AsyncAPI, Event, SharedSchema |
| `catalog.contract.api_types_enabled` | Public, Internal, Partner, ThirdParty |

Cada tipo pode ser habilitado/desabilitado por tenant via configuração.

## Versioning Policy

| Key | Descrição |
|-----|-----------|
| `catalog.contract.versioning_policy` | Estratégia de versionamento por tipo (SemVer, CalVer, Sequential, Header-based) |
| `catalog.contract.version_increment_rules` | Regras de incremento automático (breaking=major, feature=minor, fix=patch) |

Default: REST/GraphQL/gRPC/AsyncAPI usam SemVer, SOAP usa Sequential.

## Breaking Change Policy

| Key | Descrição |
|-----|-----------|
| `catalog.contract.breaking_change_policy` | Comportamento por tipo (Block, Warn, RequireApproval, Allow) |
| `catalog.contract.breaking_change_severity` | Severidade padrão para breaking changes detectados |
| `catalog.contract.breaking_promotion_restriction` | Bloquear promoção com breaking changes não resolvidos |

Defaults: SOAP/SharedSchema = Block, REST/GraphQL/gRPC = RequireApproval, AsyncAPI/Event = Warn.

## Rulesets & Templates

| Key | Descrição |
|-----|-----------|
| `catalog.validation.lint_severity_defaults` | Severidades de lint padrão (error, warn, info, off) |
| `catalog.validation.rulesets_by_contract_type` | Bindings de ruleset por tipo de contrato |
| `catalog.validation.blocking_vs_warning` | Quais regras bloqueiam publicação vs apenas alertam |
| `catalog.validation.min_validations_by_type` | Validações mínimas por tipo antes de publicação |
| `catalog.templates.by_contract_type` | Templates padrão para novos contratos |
| `catalog.templates.metadata_defaults` | Metadados pre-preenchidos em novos drafts |

## Effective Settings

Todas as configurações suportam herança System → Tenant → Environment (quando aplicável).
O administrador pode visualizar o valor efetivo, a origem e se é default, herdado ou override.
