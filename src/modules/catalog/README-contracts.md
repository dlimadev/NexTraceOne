# NexTraceOne — Contracts Module

## Visão Geral

O módulo Contracts gere a governança contratual da plataforma NexTraceOne.
É responsável pelo ciclo de vida completo de contratos de interoperabilidade:
importação, versionamento, validação, diff semântico, review/aprovação,
linting via Spectral, scorecards de qualidade e publicação.

Suporta multi-protocolo: OpenAPI, Swagger, WSDL, AsyncAPI e formatos futuros (Protobuf, GraphQL).

## Fronteira com o Catalog

O Contracts é um bounded context próprio, separado do Catalog:
- **Catalog** é owner dos **ativos** (APIs, serviços, dependências)
- **Contracts** é owner dos **contratos** (especificações, versões, diffs, reviews)

O Contracts referencia ativos do Catalog por `ApiAssetId` (Guid FK sem navigation property),
mas não é dono deles. A comunicação entre módulos segue via Integration Events.

> **Nota**: Fisicamente, o código ainda reside em `src/modules/catalog/*/Contracts/`.
> A extração para `src/modules/contracts/` está planeada como OI-01 (Wave 0).

## Arquitetura

```
NexTraceOne.Catalog.Domain/Contracts/        → Entidades, enums, VOs, serviços de domínio
NexTraceOne.Catalog.Application/Contracts/   → Features CQRS (Commands + Queries)
NexTraceOne.Catalog.Contracts/Contracts/     → DTOs, Integration Events, IContractsModule
NexTraceOne.Catalog.Infrastructure/Contracts/→ DbContext, Repositories, Services, Configs
NexTraceOne.Catalog.API/Contracts/           → Endpoints Minimal API
```

## Entidades (13)

### Aggregates
| Entidade           | Papel                                               |
|-------------------|-----------------------------------------------------|
| `ContractVersion`  | Versão publicada de contrato com lifecycle           |
| `ContractDraft`    | Rascunho em edição no Contract Studio                |
| `SpectralRuleset`  | Regras de linting customizadas                       |
| `CanonicalEntity`  | Schemas/modelos padrão reutilizáveis                 |

### Child Entities
| Entidade                 | Pai               |
|-------------------------|-------------------|
| `ContractDiff`           | ContractVersion   |
| `ContractReview`         | ContractDraft     |
| `ContractExample`        | Draft/Version     |
| `ContractArtifact`       | ContractVersion   |
| `ContractRuleViolation`  | ContractVersion   |
| `ContractScorecard`      | ContractVersion   |
| `ContractEvidencePack`   | ContractVersion   |

### Value Objects
| VO                        | Contexto                                  |
|--------------------------|------------------------------------------|
| `ContractSignature`       | Assinatura digital                        |
| `ContractProvenance`      | Lineage/origem do contrato               |
| `SemanticVersion`         | Versão semântica                          |
| `ContractOperation`       | Operação de API                           |
| `ContractSchemaElement`   | Elemento de schema                        |
| `CompatibilityAssessment` | Avaliação de compatibilidade             |
| `ValidationIssue`         | Issue de validação                        |
| `ValidationSummary`       | Resumo de validação                       |

## Lifecycle (State Machine)

```
Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired
                 ↘ Draft (rejection)
       Approved ↗ InReview (re-review)
```

## Endpoints REST

### ContractsEndpointModule (`/api/v1/contracts`)
| Método | Rota                           | Permissão            | Descrição                    |
|--------|-------------------------------|----------------------|------------------------------|
| GET    | `/list`                        | `contracts:read`     | Lista contratos com filtros  |
| GET    | `/summary`                     | `contracts:read`     | Resumo de contratos          |
| GET    | `/by-service/{serviceId}`      | `contracts:read`     | Contratos por serviço        |
| POST   | `/`                            | `contracts:import`   | Importa contrato             |
| GET    | `/{id}`                        | `contracts:read`     | Detalhe de contrato          |
| GET    | `/{id}/history`                | `contracts:read`     | Histórico de versões         |
| POST   | `/lifecycle-transition`        | `contracts:write`    | Transição de lifecycle       |
| POST   | `/diff`                        | `contracts:read`     | Compute semantic diff        |
| POST   | `/classify-breaking-change`    | `contracts:read`     | Classifica breaking change   |
| POST   | `/suggest-version`             | `contracts:read`     | Sugere versão semântica      |
| POST   | `/evaluate-rules`              | `contracts:read`     | Avalia regras Spectral       |
| POST   | `/validate`                    | `contracts:read`     | Valida integridade           |
| POST   | `/sign`                        | `contracts:write`    | Assina contrato              |
| POST   | `/lock`                        | `contracts:write`    | Bloqueia versão              |
| POST   | `/deprecate`                   | `contracts:write`    | Deprecia contrato            |
| POST   | `/scorecard`                   | `contracts:read`     | Gera scorecard               |
| POST   | `/evidence-pack`               | `contracts:read`     | Gera evidence pack           |
| POST   | `/export`                      | `contracts:read`     | Exporta contrato             |
| POST   | `/search`                      | `contracts:read`     | Pesquisa contratos           |

### ContractStudioEndpointModule (`/api/v1/contracts/drafts`)
| Método | Rota                           | Permissão            | Descrição                    |
|--------|-------------------------------|----------------------|------------------------------|
| POST   | `/`                            | `contracts:write`    | Cria draft                   |
| GET    | `/{id}`                        | `contracts:read`     | Detalhe de draft             |
| GET    | `/`                            | `contracts:read`     | Lista drafts                 |
| PUT    | `/{id}/metadata`               | `contracts:write`    | Atualiza metadata            |
| PUT    | `/{id}/content`                | `contracts:write`    | Atualiza conteúdo            |
| POST   | `/{id}/examples`               | `contracts:write`    | Adiciona exemplo             |
| POST   | `/{id}/submit`                 | `contracts:write`    | Submete para review          |
| POST   | `/{id}/approve`                | `contracts:write`    | Aprova draft                 |
| POST   | `/{id}/reject`                 | `contracts:write`    | Rejeita draft                |
| POST   | `/{id}/publish`                | `contracts:write`    | Publica draft                |
| POST   | `/ai-generate`                 | `contracts:write`    | Gera draft com IA            |
| GET    | `/{id}/reviews`                | `contracts:read`     | Lista reviews                |
| GET    | `/{id}/violations`             | `contracts:read`     | Lista violações              |

## Base de Dados

- **DbContext**: `ContractsDbContext`
- **Prefixo de tabelas**: `ctr_` (atualizado de `ct_`)
- **11 tabelas**: contract_versions, contract_diffs, contract_rule_violations, contract_artifacts, contract_drafts, contract_reviews, contract_examples, spectral_rulesets, canonical_entities, contract_scorecards, contract_evidence_packs
- **Outbox**: `ctr_outbox_messages`
- **Concorrência otimista**: PostgreSQL xmin via RowVersion em ContractVersion, ContractDraft, SpectralRuleset
- **Check constraints**: Protocol, LifecycleState, DraftStatus, SpectralRulesetOrigin, CanonicalEntityState
- **FK**: Scorecard→Version, EvidencePack→Version, Diff→Version, Artifact→Version, Violation→Version

## Frontend

### Páginas (8)
| Página                       | Rota                              | Permissão              |
|-----------------------------|-----------------------------------|------------------------|
| ContractCatalogPage          | `/contracts`                      | `contracts:read`       |
| CreateServicePage            | `/contracts/new`                  | `contracts:write`      |
| DraftStudioPage              | `/contracts/studio/:draftId`      | `contracts:write`      |
| ContractWorkspacePage        | `/contracts/:contractVersionId`   | `contracts:read`       |
| ContractGovernancePage       | `/contracts/governance`           | `contracts:read`       |
| SpectralRulesetManagerPage   | `/contracts/spectral`             | `contracts:write`      |
| CanonicalEntityCatalogPage   | `/contracts/canonical`            | `contracts:read`       |
| ContractPortalPage           | `/contracts/portal/:contractVersionId` | `developer-portal:read` |

### Hooks (12 React Query)
- `useContractList`, `useContractDetail`, `useContractHistory`, `useContractDiff`
- `useContractTransition`, `useContractExport`, `useContractViolations`
- `useDraftWorkflow`, `useSpectralRulesets`, `useCanonicalEntities`, `useValidation`

## Testes

468 testes unitários cobrindo:
- Entidades de domínio (lifecycle, signatures, scorecards, evidence packs)
- Serviços de domínio (diff calculators, rule engine, scorecard calculator)
- Features CQRS (importação, governança, studio, validação)

## Segurança

- Permissões: `contracts:read`, `contracts:write`, `contracts:import`
- Todas as rotas frontend protegidas com `ProtectedRoute`
- Todos os endpoints backend protegidos com `RequirePermission`
- Soft-delete em entidades (IsDeleted, filtro global)
- Auditoria: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy em todas as entidades auditable
