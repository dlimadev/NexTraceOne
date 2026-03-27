# Configuration — Minimum Requirements, Publication & Import/Export

## Minimum Requirements

### Requisitos de Catálogo/Contrato

| Key | Descrição | Default |
|-----|-----------|---------|
| `catalog.requirements.owner_required` | Owner obrigatório | true |
| `catalog.requirements.changelog_required` | Changelog obrigatório | true |
| `catalog.requirements.glossary_required` | Glossary obrigatório | false |
| `catalog.requirements.use_cases_required` | Use cases obrigatórios | false |
| `catalog.requirements.min_documentation` | Requisitos mínimos de documentação | descriptionMinLength:20, operationDescriptions:true |
| `catalog.requirements.min_catalog_fields` | Campos mínimos do catálogo | name, description, owner, team, lifecycle |
| `catalog.requirements.min_contract_fields` | Campos mínimos do contrato | title, version, description, servers, securityScheme |

### Requisitos por Contexto

| Key | Descrição |
|-----|-----------|
| `catalog.requirements.by_contract_type` | Requisitos adicionais por tipo de contrato |
| `catalog.requirements.by_environment` | Requisitos por ambiente (Production mais restritivo) |
| `catalog.requirements.by_criticality` | Requisitos por criticidade do serviço |

## Publication Policy

| Key | Descrição | Default |
|-----|-----------|---------|
| `catalog.publication.pre_publish_review` | Revisão pré-publicação obrigatória | true |
| `catalog.publication.visibility_defaults` | Visibilidade padrão por tipo de API | Internal→team, Public→org, Partner→restricted |
| `catalog.publication.portal_defaults` | Defaults de publicação no Developer Portal | autoPublish:true, examples:true, changelog:true |
| `catalog.publication.promotion_readiness` | Critérios de readiness para promoção | validations pass, owner assigned, changelog updated, minScore:60 |
| `catalog.publication.gating_by_environment` | Gating de publicação por ambiente | Production requer approval + all gates |

## Import/Export Policy

| Key | Descrição | Default |
|-----|-----------|---------|
| `catalog.import.types_allowed` | Tipos de import permitidos | fileUpload, urlImport, gitSync por formato |
| `catalog.export.types_allowed` | Formatos de export permitidos | OpenAPI-JSON/YAML, WSDL, GraphQL-SDL, Protobuf, AsyncAPI-YAML, Markdown, HTML |
| `catalog.import.overwrite_policy` | Política de overwrite | AskUser (opções: Merge, Overwrite, Block, AskUser) |
| `catalog.import.validation_on_import` | Validar automaticamente ao importar | true |

## Critérios por Ambiente/Tipo

A configuração por ambiente garante que Production tem requisitos mais rigorosos enquanto Development é mais flexível. A configuração por criticidade garante que serviços críticos têm exigências adicionais (glossary, use cases, etc.).
