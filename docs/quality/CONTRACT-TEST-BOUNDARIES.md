# Contract Test Boundaries — NexTraceOne

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22

---

## Propósito

Este documento define as fronteiras inter-módulo do NexTraceOne que requerem contract tests explícitos.  
O objetivo é detectar quebras de contrato entre módulos antes que cheguem a produção — especialmente em um sistema modular onde múltiplos DbContexts compartilham databases e estruturas de dados.

---

## Contexto Arquitetural

O NexTraceOne é um **monolito modular** com:
- 16 DbContexts consolidados em 4 databases (ADR-001)
- Comunicação cross-module via MediatR (in-process events)
- Contracts explícitos em `*.Domain.*` por módulo
- Sem chamadas HTTP cross-module (all in-process)

O risco de quebra de contrato existe quando:
1. Módulo A persiste dados que Módulo B lê
2. Um evento publicado por A é consumido por B (via OutboxMessage)
3. Um handler de A depende de entidades de B no mesmo banco

### Por que não Pact/CDC externo?

Para um monolito modular in-process, consumer-driven contract testing (Pact) seria complexidade desnecessária. A abordagem adotada usa a mesma infraestrutura de Testcontainers: os módulos são testados na mesma fixture real, validando que dados persistidos por A são legíveis por B sem ambiguidade.

---

## Fronteiras Mapeadas

### Fronteira 1 — Reliability (OI.Runtime) ↔ Catalog

| Atributo | Detalhe |
|----------|---------|
| **Risco** | Runtime health data referencia ServiceId de Catalog |
| **Banco compartilhado** | `operations` (runtime) + `catalog` (catalog graph) — databases separados |
| **Contrato crítico** | ServiceId (GUID) deve ser consistente entre módulos |
| **Teste** | `ContractBoundaryTests.Catalog_ServiceId_Referenced_By_Runtime_Should_Be_Consistent` |
| **Critério** | ServiceAsset criado em Catalog com ID X; RuntimeServiceHealth criado com ServiceId=X; ambos persistidos sem conflito |

### Fronteira 2 — ChangeGovernance ↔ OperationalIntelligence.Incidents

| Atributo | Detalhe |
|----------|---------|
| **Risco** | Incidentes são correlacionados a releases via ReleaseId/ApiAssetId |
| **Banco compartilhado** | `operations` (partilhado entre ChangeGovernance e OI) |
| **Contrato crítico** | ReleaseCandidate e Incident podem coexistir no mesmo banco |
| **Teste** | `ContractBoundaryTests.ChangeGovernance_And_Incidents_Should_Coexist_In_Operations_Database` |
| **Critério** | ReleaseCandidate + Incident persistidos no operations DB sem conflito de schema |

### Fronteira 3 — AIKnowledge ↔ Catalog

| Atributo | Detalhe |
|----------|---------|
| **Risco** | AI conversations referenciam ServiceId e ApiAssetId de Catalog |
| **Banco compartilhado** | `ai` (AIKnowledge) + `catalog` (Catalog) — databases separados |
| **Contrato crítico** | ServiceId como GUID é o identificador externo esperado pelo AI |
| **Teste** | `ContractBoundaryTests.AIKnowledge_Conversation_Can_Reference_Catalog_ServiceId` |
| **Critério** | Conversa AI com serviceId de serviço Catalog existente persiste sem violação |

### Fronteira 4 — AuditCompliance ↔ Operations

| Atributo | Detalhe |
|----------|---------|
| **Risco** | Audit trail deve persistir no mesmo banco identity sem conflito com Identity |
| **Banco compartilhado** | `identity` (IdentityAccess + AuditCompliance) |
| **Contrato crítico** | AuditEntry pode ser persistida junto com Identity no mesmo banco |
| **Teste** | `ContractBoundaryTests.AuditCompliance_Tables_Should_Coexist_With_Identity_Tables` |
| **Critério** | Tabelas de audit e identity no mesmo banco sem conflito |

### Fronteira 5 — Governance ↔ Catalog (Teams e Serviços)

| Atributo | Detalhe |
|----------|---------|
| **Risco** | Teams de Governance referenciam ownership de serviços em Catalog |
| **Banco compartilhado** | `operations` (Governance) + `catalog` (Catalog) — databases separados |
| **Contrato crítico** | TeamId e ownedByTeam são strings/slugs compartilhados como identificadores externos |
| **Teste** | `ContractBoundaryTests.Governance_Team_Slug_Matches_Catalog_OwnerSlug_Convention` |
| **Critério** | ServiceAsset criado com owner slug "team-payments"; Team criado com name "team-payments"; slugs são compatíveis por convenção |

---

## Implementação

Os contract tests estão implementados em:
```
tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/ContractBoundaryTests.cs
```

Usam a mesma `PostgreSqlIntegrationFixture` dos demais integration tests, garantindo:
- PostgreSQL 16 real via Testcontainers
- Migrations aplicadas
- Reset de estado via Respawn entre cenários

---

## Decisões de Design

### O que validamos
- Que entidades de módulos diferentes podem coexistir no mesmo banco sem conflito de schema
- Que identificadores compartilhados (GUIDs, slugs) seguem a mesma convenção entre módulos
- Que tables de módulos diferentes não têm conflito de nome (graças aos prefixos únicos por módulo)

### O que NÃO validamos aqui
- Lógica de negócio cross-module (cobertura de unit tests)
- Fluxos HTTP completos (cobertura de E2E tests)
- Performance de queries cross-database (cobertura de performance baseline)

---

## Fronteiras Futuras (Fase 9+)

| Fronteira | Motivo de diferimento |
|-----------|----------------------|
| Automation ↔ ChangeGovernance | Módulo em desenvolvimento ativo |
| ExternalAI ↔ AIKnowledge.Runtime | Depende de provider real; mockado em CI |
| FinOps ↔ Operations | Feature não implementada em ZR-6 |

---

*Documento mantido pelo Release Readiness Lead. Atualizar quando novas dependências cross-module forem introduzidas.*
