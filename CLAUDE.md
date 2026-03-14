# NexTraceOne — Sovereign Change Intelligence Platform

## Identidade

NexTraceOne é uma **Sovereign Change Intelligence Platform** — enterprise, self-hosted, soberana e auditável.
Conecta contrato → mudança → impacto → aprovação → runtime → custo em um único sistema auditável.
Segmento: Bancos, seguradoras, telecom, governo, grandes enterprises.

## Stack

- **Backend:** .NET 10, ASP.NET Core 10 (Minimal APIs, REPR Pattern), EF Core 10, PostgreSQL 16
- **Libs:** MediatR 14, FluentValidation 12, Quartz.NET 3, OpenTelemetry 1.x, Serilog 4, Ardalis.GuardClauses 5, Mapster (source-gen)
- **Arquitetura:** Modular Monolith + Vertical Slice (VSA) + DDD tático + CQRS + Outbox Pattern
- **Infra MVP1:** PostgreSQL apenas (sem Redis, sem OpenSearch, sem Temporal)

## Estrutura do Projeto (Modular Monolith Consolidado)

A arquitetura foi consolidada de 14 módulos × 5 camadas para **7 bounded contexts × 3 camadas** (Domain / Application / Infrastructure). Cada contexto é uma unidade coesa que agrupa subdomínios relacionados.

```
NexTraceOne/
├── src/
│   ├── building-blocks/              ← 6 projetos granulares (substituem SharedKernel)
│   │   ├── NexTraceOne.BuildingBlocks.Domain
│   │   ├── NexTraceOne.BuildingBlocks.Application
│   │   ├── NexTraceOne.BuildingBlocks.Infrastructure
│   │   ├── NexTraceOne.BuildingBlocks.EventBus
│   │   ├── NexTraceOne.BuildingBlocks.Observability
│   │   └── NexTraceOne.BuildingBlocks.Security
│   ├── modules/                      ← 7 bounded contexts × 3 camadas cada
│   │   ├── IdentityAccess/           ← Autenticação, autorização, sessões, SSO
│   │   │   ├── NexTraceOne.IdentityAccess.Domain
│   │   │   ├── NexTraceOne.IdentityAccess.Application
│   │   │   └── NexTraceOne.IdentityAccess.Infrastructure
│   │   ├── CommercialGovernance/     ← Licenciamento, capacidades, billing
│   │   │   ├── NexTraceOne.CommercialGovernance.Domain
│   │   │   ├── NexTraceOne.CommercialGovernance.Application
│   │   │   └── NexTraceOne.CommercialGovernance.Infrastructure
│   │   ├── Catalog/                  ← Catálogo de APIs, contratos, portal
│   │   │   ├── NexTraceOne.Catalog.Domain
│   │   │   ├── NexTraceOne.Catalog.Application
│   │   │   └── NexTraceOne.Catalog.Infrastructure
│   │   ├── ChangeGovernance/         ← Releases, workflows, promoção, regras
│   │   │   ├── NexTraceOne.ChangeGovernance.Domain
│   │   │   ├── NexTraceOne.ChangeGovernance.Application
│   │   │   └── NexTraceOne.ChangeGovernance.Infrastructure
│   │   ├── OperationalIntelligence/  ← Runtime, custos, correlação
│   │   │   ├── NexTraceOne.OperationalIntelligence.Domain
│   │   │   ├── NexTraceOne.OperationalIntelligence.Application
│   │   │   └── NexTraceOne.OperationalIntelligence.Infrastructure
│   │   ├── AIKnowledge/              ← IA interna, routing externo, knowledge
│   │   │   ├── NexTraceOne.AIKnowledge.Domain
│   │   │   ├── NexTraceOne.AIKnowledge.Application
│   │   │   └── NexTraceOne.AIKnowledge.Infrastructure
│   │   └── AuditCompliance/          ← Auditoria, integridade, compliance
│   │       ├── NexTraceOne.AuditCompliance.Domain
│   │       ├── NexTraceOne.AuditCompliance.Application
│   │       └── NexTraceOne.AuditCompliance.Infrastructure
│   └── platform/
│       ├── NexTraceOne.ApiHost           ← Compõe todos os módulos (UI interna, portal)
│       ├── NexTraceOne.Ingestion.Api     ← Entry point externo (webhooks CI/CD, runtime signals)
│       └── NexTraceOne.BackgroundWorkers ← Outbox, Jobs, SLA checks
├── tools/NexTraceOne.CLI                 ← CLI 'nex' (consome só Contracts)
├── tests/
│   ├── building-blocks/                  ← Testes unitários dos building blocks
│   ├── modules/                          ← Testes unitários por bounded context
│   └── platform/                         ← Testes de integração e E2E
└── docs/
```

### Os 7 Bounded Contexts

| Contexto | Responsabilidade | Subdomínios consolidados |
|---|---|---|
| **IdentityAccess** | Autenticação, autorização, sessões, SSO/OIDC | Identity, Roles, Sessions, FederatedLogin |
| **CommercialGovernance** | Licenciamento, capacidades, billing | Licensing, Entitlements, HardwareBinding |
| **Catalog** | Catálogo de APIs, contratos, portal | EngineeringGraph, Contracts, DeveloperPortal |
| **ChangeGovernance** | Releases, workflows, promoção, regras | ChangeIntelligence, Workflow, RulesetGovernance, Promotion |
| **OperationalIntelligence** | Runtime, custos, correlação | DeploymentTracking, RuntimeCost, Correlation |
| **AIKnowledge** | IA interna, routing externo, knowledge | AIConsultation, KnowledgeBase, Routing |
| **AuditCompliance** | Auditoria, integridade, compliance | AuditTrail, Integrity, EvidencePack |

## Referências Detalhadas

See @docs/ARCHITECTURE.md for arquitetura completa, Building Blocks, módulos e regras de dependência.
See @docs/CONVENTIONS.md for padrões de código, idioma, documentação XML e regras inegociáveis.
See @docs/ROADMAP.md for estado atual do projeto, fases e próximos passos.
See @docs/DOMAIN.md for taxonomia de mudanças, discovery de dependências e domínio de negócio.
See @docs/SECURITY.md for pilares de segurança, RLS, encryption, integrity e licensing.

## Regras Críticas (SEMPRE seguir)

1. **Idioma:** Código/logs/nomes em INGLÊS. Comentários XML (`<summary>`) e inline em PORTUGUÊS.
2. **Nunca** usar `DateTime.Now` — sempre `IDateTimeProvider`.
3. **Nunca** acessar DbContext de outro módulo — comunicação via Integration Events ou ServiceInterfaces.
4. **Nunca** publicar Integration Events sem Outbox Pattern.
5. **Um bounded context por vez**, uma camada por vez (Domain → Application → Infrastructure), um aggregate por vez.
6. **Toda classe pública** com `<summary>` XML em português.
7. **Todo método público** com `<summary>` XML em português.
8. **Result Pattern** para operações que podem falhar — sem exceções para controle de fluxo.
9. **Testes unitários** adjacentes a cada implementação.
10. **Máximo 3 projetos por contexto** (Domain / Application / Infrastructure) — sem projetos adicionais (Contracts, API, Validators, etc.).

## Comandos

```bash
# Build
dotnet build NexTraceOne.sln

# Testes
dotnet test NexTraceOne.sln

# Executar API (portal interno)
dotnet run --project src/platform/NexTraceOne.ApiHost

# Executar Ingestion API (webhooks externos)
dotnet run --project src/platform/NexTraceOne.Ingestion.Api

# Executar Workers
dotnet run --project src/platform/NexTraceOne.BackgroundWorkers

# Executar CLI
dotnet run --project tools/NexTraceOne.CLI
```

## Estado Atual (Pós-Consolidação Arquitetural)

### Consolidação ✅

A arquitetura foi consolidada de 14 módulos × 5 camadas (70 projetos de módulos) para **7 bounded contexts × 3 camadas (21 projetos de módulos)**. Os antigos módulos (Identity, Licensing, EngineeringGraph, Contracts, ChangeIntelligence, Workflow, RulesetGovernance, etc.) foram agrupados nos bounded contexts correspondentes como subdomínios internos.

### Concluído ✅

- ✅ **Building Blocks (6/6)** — Domain, Application, Infrastructure, EventBus, Observability, Security
  - `IntegrationEventBase` adicionado ao Domain como base para todos os Integration Events
  - `OutboxEventBus` corrigido — implementação funcional de dispatch via DI
  - `AuditInterceptor` — reflection segura substituindo `dynamic`
  - i18n: `SharedMessages.resx` + `SharedMessages.pt-BR.resx` com códigos Identity
- ✅ **IdentityAccess** — Login, FederatedLogin, RefreshToken, CreateUser, AssignRole, Sessions, RBAC
  - Consolida o antigo módulo Identity
  - Testes: `AssignRoleTests`, `RevokeSessionTests`
- ✅ **CommercialGovernance** — Licensing Domain + Application + Infrastructure completos
  - Consolida o antigo módulo Licensing
  - `LicenseRepository` + `HardwareBindingRepository`
  - Testes: `LicensingApplicationTests`
- ✅ **Catalog** — EngineeringGraph + Contracts completos
  - Consolida os antigos módulos EngineeringGraph, Contracts e DeveloperPortal
  - Features: `ImportContract`, `ComputeSemanticDiff`, `ClassifyBreakingChange`, `SuggestSemanticVersion`, `GetContractHistory`, `LockContractVersion`, `ExportContract`, `ValidateContractIntegrity`
  - `ApiAssetRepository` + `ServiceAssetRepository` + `ContractVersionRepository`
  - Testes: `EngineeringGraphApplicationTests` (12 testes), `ContractsApplicationTests` (21 testes)
- ✅ **Testes Building Blocks** — `ResultTests`, `ValueObjectTests`, `PagedListTests`
- ✅ **Platform** — ApiHost + Ingestion.Api + BackgroundWorkers configurados
- ✅ **Estrutura de testes** — Unitários por bounded context + integração + E2E

### Em Progresso 🟡

**ChangeGovernance (CORE DO PRODUTO):**
1. `Release` (Aggregate Root) com `ChangeEvent`
2. `NotifyDeployment` — webhook receiver do CI/CD (via Ingestion.Api)
3. `ClassifyChangeLevel` — integra com Catalog para diff semântico
4. `CalculateBlastRadius` — consulta Catalog/EngineeringGraph para consumidores transitivos
5. `ComputeChangeScore` — score de risco 0.0–1.0
6. Workflow + RulesetGovernance + Promotion como subdomínios do mesmo contexto

### Próximos Bounded Contexts 🔲

- **OperationalIntelligence** — runtime tracking, custos, correlação
- **AIKnowledge** — IA interna, routing externo, knowledge base
- **AuditCompliance** — auditoria completa, integridade criptográfica, evidence pack

Ver `docs/ROADMAP.md` para o cronograma completo.
Ver `docs/ARCHITECTURE.md` para regras de dependência entre contextos.
