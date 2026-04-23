# ADR-007: Data Contracts como Cidadão de Primeira Classe

## Status

Partially Implemented — Wave AQ.1 (Abril 2026)

> **Nota de revisão (Abril 2026):** `DataContractRecord` (entidade), `RegisterDataContract` (Command), `GetDataContractComplianceReport` (Query), `IDataContractRepository` e métricas `DataContractTier`/`FieldDefinitionCompleteness`/`TeamDataGovernanceScore` foram implementados na Wave AQ.1. A extensão para streams analíticos, pipelines BI e modelos ML mencionada neste ADR permanece como evolução futura.

## Data

2026-04-20

## Contexto

O NexTraceOne trata hoje como contratos de primeira classe: REST (OpenAPI), SOAP (WSDL), eventos (AsyncAPI, Kafka), background services, shared schemas, webhooks e diversos formatos legacy (Copybook, CICS Commarea, MQ, FixedLayout). Esta cobertura é forte para **contratos de interação** entre serviços.

Existe, no entanto, uma classe distinta de contratos que é crítica em ambientes enterprise 2025–2026 e que hoje não é modelada explicitamente: **contratos de dados** — tabelas, vistas, streams analíticos e datasets que são consumidos por dashboards, pipelines de BI, modelos de ML e integrações externas.

Os sintomas operacionais que motivam este ADR:

- Alterações de schema em tabelas/vistas produzem incidentes silenciosos em consumidores downstream (BI, ML, relatórios regulatórios).
- Não existe ownership claro sobre datasets partilhados.
- Não existe SLA explícito de frescura (freshness) ou qualidade (completeness, validity).
- PII e classificação de sensibilidade ficam implícitas em documentação dispersa.
- Change Intelligence perde blast radius quando a mudança é num *data product*, não numa API.

O mercado consolidou em 2025 a noção de **Data Contracts** (cf. Open Data Contract Standard, bitol-io, PayPal/Meta) como forma de aplicar a disciplina de contratos de API ao mundo de dados. Encaixa naturalmente no pilar oficial do NexTraceOne de **Contract Governance** (capítulos 3.2 e 7.3 das Copilot Instructions).

## Decisão

Adicionar **Data Contracts** como novo tipo de contrato first-class dentro do bounded context `Catalog`, seguindo o mesmo desenho do domínio dos contratos existentes e sem criar módulo novo.

### Escopo

1. **Novo valor de enum** `ContractType.DataContract` em `Catalog.Domain`.
2. **Novo agregado `DataContract`** em `Catalog.Domain.Contracts.Data`, reutilizando:
   - identidade tipada (`ContractId`),
   - versionamento e lifecycle (`Draft`/`Review`/`Approved`/`Deprecated`),
   - ownership via `ServiceAsset` / `Team`,
   - rulesets e linting (Spectral pipeline),
   - publication workflow e approval workflow existentes.
3. **Campos específicos** do Data Contract (mínimos, parametrizáveis via `ConfigurationDefinitionSeeder`):
   - `DataAssetReference` — identificador canónico do asset físico (ex.: `postgres://tenant/schema/table`, `kafka://cluster/topic`, `s3://bucket/path`).
   - `Schema` — JSON Schema / Avro / SQL DDL / Delta schema, armazenado como documento versionado.
   - `FreshnessSla` — `MaxStalenessSeconds` + janela de avaliação.
   - `QualityChecks` — lista de checks (completeness, uniqueness, referential integrity) com severidade.
   - `Classification` — `PiiLevel` (None/Internal/Pii/SensitivePii) + labels regulatórios (GDPR, LGPD, HIPAA).
   - `RetentionPolicy` — duração lógica, não infra.
   - `UpstreamLineage` / `DownstreamLineage` — lista de `DataContractId` / `ServiceAssetId` (hidratada incrementalmente; não obrigatória no `Create`).
4. **Fronteira com os contratos existentes**:
   - Data Contracts **não** substituem Event Contracts (AsyncAPI). Um tópico Kafka pode ter ambos: o Event Contract descreve a forma da mensagem; o Data Contract pode descrever o *stream agregado* consumido para analítica.
   - Data Contracts **não** substituem REST contracts. Uma vista exposta por API continua modelada como REST; o Data Contract modela a tabela/vista física.
   - Regra: se o consumidor primário é **outro sistema de dados** (warehouse, lakehouse, feature store, dashboard), é Data Contract. Se é **outro serviço aplicacional via protocolo**, é o contrato de protocolo.
5. **Persistência**: nova migração em `ContractsDbContext` (mesmo DbContext dos outros contratos, não separar) — `ct_data_contracts`, `ct_data_contract_versions`, `ct_data_contract_quality_checks`, `ct_data_contract_lineage_edges`.
6. **Features CQRS** espelhando o padrão existente: `CreateDataContract`, `PublishDataContractVersion`, `DeprecateDataContract`, `ListDataContracts`, `GetDataContractHealth`, `ComputeDataContractDiff`, `EvaluateDataContractCompatibility`.
7. **Integração com outros módulos** (sem quebrar fronteira):
   - `ChangeGovernance` passa a considerar Data Contracts em BlastRadius e PromotionGates.
   - `Knowledge` passa a relacionar documentos com `DataContractId`.
   - `OperationalIntelligence` pode alimentar `FreshnessSla` violations como incidents via provider opcional (`IDataQualityProvider`, seguindo o padrão `Null*Provider` + `ConfigurationResolutionService`).
   - `Governance` reports incluem Data Contract health e PII surface por domínio.
8. **Contract Studio**: nova superfície (visual builder) reutilizando o framework existente, com editor de schema JSON/Avro/SQL e validador de freshness/quality checks.
9. **AI Governance**: `ReviewContractDraft` estendido para Data Contracts com guardrails específicos (PII leakage warning quando classification cai de nível).

### Fora de escopo (explícito)

- **Execução real de quality checks** contra os dados físicos — isto é feito por tooling externo (Great Expectations, Soda, dbt tests); o NexTraceOne **governa** os checks, **não os executa**. Integração via `IDataQualityProvider` opcional.
- **Lineage automática** a partir de query parsing. A lineage é alimentada por ingestão (integrations) ou declaração; inferência automática fica para v2.x.
- **Catálogo físico de dados** (tipo DataHub/Atlas). O NexTraceOne governa **contratos sobre** datasets; a descoberta física é responsabilidade de tooling externo que pode alimentar via `DataAssetReference`.

## Consequências

### Positivas

- Fecha lacuna crítica do mercado enterprise 2026 (Data Mesh / Data Products).
- Reforça o pilar Source of Truth: contratos de dados passam a estar onde já estão os contratos de API/evento.
- Reutiliza workflow, approval, ruleset e lifecycle já maduros — impacto arquitetural localizado.
- Desbloqueia FinOps contextual sobre datasets (via cross-module com `CostIntelligence`).
- Amplia Change Intelligence: mudança num schema de tabela passa a ter blast radius sobre dashboards/pipelines declarados.

### Negativas

- Introduz nova superfície de UI (Contract Studio data editor) — custo não trivial.
- Risco de sobreposição com Event Contracts se a fronteira não for comunicada claramente (mitigado pela regra explícita na secção "Fronteira").
- Organizações sem maturidade de data ownership podem criar Data Contracts sem dono real — políticas de lint devem bloquear publicação sem `OwnerTeamId`.

### Neutras

- Não impacta módulos fora de Catalog em termos de obrigatoriedade — consumidores cross-module usam Data Contracts quando aplicável.
- Não requer novo módulo: mantém a regra arquitetural de preferir aprofundar em vez de criar.

## Roadmap de implementação

| Fase | Escopo | Depende de |
|------|--------|-----------|
| Fase 1 | Domínio + persistência + `Create/List/Get/Publish` | — |
| Fase 2 | Diff semântico + compatibilidade + `DeprecationPlan` | Fase 1 |
| Fase 3 | Quality checks + `IDataQualityProvider` opcional | Fase 1 |
| Fase 4 | Lineage declarativa + integração com BlastRadius | Fase 1 + ChangeGovernance existente |
| Fase 5 | Contract Studio visual builder + AI Review | Fase 1 + 2 |

## Critérios de aceite

- [ ] `DataContract` modelado respeitando Clean Architecture (Domain puro, Application com CQRS, Infrastructure com EF Core).
- [ ] Tenant isolation via RLS no novo schema.
- [ ] Configuração (thresholds de freshness, severidades, listas de labels regulatórios) via `IConfigurationResolutionService` — **nunca** `appsettings`.
- [ ] i18n obrigatório em toda a UI nova (pt-PT, pt-BR, en, es).
- [ ] Permissão dedicada `catalog:data-contracts:read|write|publish`.
- [ ] Audit trail de todas as mutações via `AuditCompliance`.
- [ ] Testes unitários + integração (mínimo 80% coverage no agregado).

## Referências

- [ADR-001: Modular Monolith](./001-modular-monolith.md)
- [Contract Studio Vision](../CONTRACT-STUDIO-VISION.md)
- [Service Contract Governance](../SERVICE-CONTRACT-GOVERNANCE.md)
- [FUTURE-ROADMAP — Wave A.2](../FUTURE-ROADMAP.md)
- Open Data Contract Standard (ODCS) — bitol-io
