# Estratégia de Testes e Camadas — NexTraceOne

> **Fase 8 — Go-Live Readiness**  
> Versão: 1.0 | Data: 2026-03-22

---

## Visão Geral

O NexTraceOne adota uma estratégia de testes em camadas, onde cada tipo de teste tem um papel bem definido e complementar. A suíte não deve ser avaliada isoladamente — a saúde do produto é medida pelo conjunto.

**Princípio central**: testes que validam mocks sem comportamento real não têm valor para go-live. Cada camada deve provar comportamento, não código.

---

## Pirâmide de Testes

```
        ┌─────────────┐
        │  E2E Tests  │  ← Jornadas de negócio críticas (poucas, confiáveis)
        └─────────────┘
       ┌───────────────┐
       │ Contract Tests│  ← Fronteiras entre módulos (cross-module contracts)
       └───────────────┘
      ┌─────────────────┐
      │Integration Tests│  ← Camadas reais: API→Handler→DB via Testcontainers
      └─────────────────┘
     ┌───────────────────┐
     │    Unit Tests     │  ← Domain logic, validators, calculators (rápidos)
     └───────────────────┘
    ┌─────────────────────┐
    │  Smoke Tests        │  ← Pós-deploy: sistema vivo e respondendo
    └─────────────────────┘
   ┌───────────────────────┐
   │ Performance/Resilience│  ← Baseline mínima de carga e estabilidade
   └───────────────────────┘
```

---

## 1. Unit Tests

### Objetivo
Validar lógica de domínio, regras de negócio, validators e serviços de aplicação de forma isolada e rápida.

### Política
- **Escopo**: domain entities, value objects, validators, calculators, feature handlers com dependências mockadas
- **Ferramenta**: xUnit + FluentAssertions + NSubstitute/Bogus
- **Velocidade**: < 1ms por teste; toda a suíte < 60s
- **Isolamento**: zero dependências externas (DB, HTTP, filesystem)
- **Cobertura mínima esperada**: lógica de domínio crítica (P0/P1) com 100% de ramos

### O que NÃO deve ser unit test
- Testes que apenas instanciam o handler e retornam void
- Testes que validam que um mock foi chamado sem verificar comportamento real
- Testes que "freeze" um comportamento fake por não ter implementação

### Estado atual (por módulo)
| Módulo | Contagem estimada | Classificação |
|--------|-----------------|---------------|
| IdentityAccess | 280+ | ✅ Alta confiança |
| AIKnowledge | 266+ | ✅ Alta confiança |
| Catalog | 200+ | ✅ Alta confiança |
| ChangeGovernance | ~80 | ⚠️ Cobertura parcial |
| Governance | 23 | ⚠️ Cobertura básica |
| OperationalIntelligence | 283+ | ✅ Alta confiança |
| AuditCompliance | ~30 | ⚠️ Cobertura básica |
| BuildingBlocks.Application | 34 | ✅ |
| BuildingBlocks.Core | 30 | ✅ |
| BuildingBlocks.Observability | 56 | ✅ |

---

## 2. Integration Tests

### Objetivo
Validar que as camadas da aplicação funcionam corretamente juntas: Handler → Repository → EF Core → PostgreSQL real.

### Política
- **Escopo**: persistência real com Testcontainers (PostgreSQL 16-alpine), migrations aplicadas, leitura e escrita real
- **Ferramenta**: xUnit + Testcontainers.PostgreSql + Respawn + FluentAssertions
- **Estrutura**: `PostgreSqlIntegrationFixture` gerencia 4 databases consolidados (ADR-001)
- **Reset de estado**: `ResetDatabasesAsync()` via Respawn entre testes
- **Velocidade**: < 5 min para toda a suíte de integration tests

### Projetos
- `NexTraceOne.IntegrationTests` — testes de integração por module/domain
- `NexTraceOne.E2E.Tests` — API HTTP real via `WebApplicationFactory<Program>`

### O que DEVE ser integration test
- Persistência e consulta de entidades de domínio
- Migrations aplicadas corretamente
- Queries com joins e JSONB
- Ciclo de vida de entidades (create/update/delete)

### O que NÃO deve ser integration test
- Regras de negócio puras (→ unit test)
- Jornadas completas de usuário (→ E2E test)

### Estado atual
| Classe de teste | DB testado | Cobertura |
|----------------|-----------|-----------|
| `CriticalFlowsPostgreSqlTests` | identity, catalog, operations | ✅ |
| `DeepCoveragePostgreSqlTests` | catalog, operations | ✅ |
| `GovernanceWorkflowPostgreSqlTests` | operations | ✅ |
| `AiGovernancePostgreSqlTests` | ai | ✅ |
| `ExtendedDbContextsPostgreSqlTests` | all | ✅ |
| `CoreApiHostIntegrationTests` | all (via ApiHost) | ✅ |
| `ContractBoundaryTests` | catalog + operations + ai | ✅ (novo — Fase 8) |

---

## 3. Contract Tests

### Objetivo
Detectar quebras de contrato entre módulos antes que cheguem a produção. Validar que as fronteiras cross-module respeitam os shapes esperados.

### Política
- **Escopo**: integrações entre módulos que compartilham dados ou se chamam via mediator/events
- **Abordagem**: testes que persistem dados em módulo A e consultam via módulo B usando os mesmos DbContexts reais
- **NÃO usa**: Pact ou consumer-driven contracts externos (complexidade desnecessária para este monolito modular)
- **Ferramenta**: xUnit + Testcontainers (mesma infraestrutura dos integration tests)

### Fronteiras cobertas (Fase 8)
Ver `/docs/quality/CONTRACT-TEST-BOUNDARIES.md` para detalhes completos.

| Fronteira | Status |
|-----------|--------|
| Reliability ↔ Catalog | ✅ `ContractBoundaryTests.cs` |
| ChangeGovernance ↔ Incidents | ✅ `ContractBoundaryTests.cs` |
| AIKnowledge ↔ Catalog | ✅ `ContractBoundaryTests.cs` |
| Audit ↔ Operations | ✅ `ContractBoundaryTests.cs` |

---

## 4. E2E Tests

### Objetivo
Validar jornadas completas de negócio do ponto de vista do consumidor da API, do início ao fim.

### Política
- **Escopo**: fluxos business-critical conforme `E2E-GO-LIVE-SUITE.md`
- **Ferramenta backend**: xUnit + `WebApplicationFactory<Program>` + `ApiE2EFixture` (PostgreSQL real)
- **Ferramenta frontend**: Playwright (`src/frontend/e2e/`, `src/frontend/e2e-real/`)
- **Dados**: usuários e dados seedados pela fixture (`e2e.admin@nextraceone.test`)
- **Ambiente**: Development com `NEXTRACE_SKIP_INTEGRITY=true`
- **Velocidade**: suíte mínima < 10 min

### Suíte mínima de go-live
Ver `/docs/quality/E2E-GO-LIVE-SUITE.md` para detalhes.

### Estado atual
| Classe de teste E2E | Fluxo | Status |
|--------------------|-------|--------|
| `SystemHealthFlowTests` | Health, liveness, readiness | ✅ |
| `AuthApiFlowTests` | Login, JWT, rota protegida | ✅ |
| `ReleaseCandidateSmokeFlowTests` | Smoke mínimo pós-deploy | ✅ |
| `CatalogAndIncidentApiFlowTests` | Catalog + incidents | ✅ |
| `RealBusinessApiFlowTests` | Runtime, governance, AI | ✅ |

---

## 5. Smoke Tests

### Objetivo
Confirmar em minutos que o sistema está vivo e funcional após deploy.

### Política
- **Quando executar**: após qualquer deploy em staging ou produção
- **Duração máxima**: < 3 minutos
- **Falha**: bloqueia promoção do deploy

### Cobertura mínima
1. `/live` retorna 200
2. `/ready` retorna 200
3. `/health` retorna JSON com status
4. Frontend responde HTTP 200
5. Login com usuário de teste retorna JWT
6. GET /catalog/services retorna dados (ou lista vazia estruturada)

### Scripts
- `scripts/quality/check-no-demo-artifacts.sh` — validação de artefatos
- `docs/runbooks/POST-DEPLOY-VALIDATION.md` — checklist manual expandido

---

## 6. Performance e Resiliência

### Objetivo
Estabelecer baseline mínima de performance e detectar gargalos óbvios antes de produção.

### Política
- **Ferramenta primária**: scripts shell com `curl` + análise de tempo de resposta
- **Evolução**: k6 para cenários de carga progressiva (documentado como próximo passo)
- **Metas mínimas**: ver `/docs/quality/PERFORMANCE-AND-RESILIENCE-BASELINE.md`

---

## Execução por Pipeline

| Pipeline | Testes executados | Trigger |
|----------|------------------|---------|
| `ci.yml` | Unit + Integration + E2E (backend) | PR + push main |
| `e2e.yml` | E2E Playwright (frontend) | PR + push main |
| `staging.yml` | Build + smoke-check | Push main (deploy staging) |
| `security.yml` | SAST + dependency scan | PR + push main |

---

## Critérios de Gate de Go-Live

Para aprovar go-live:

- [ ] 100% de unit tests passando (sem skip)
- [ ] 100% de integration tests passando
- [ ] 100% de contract tests passando
- [ ] 100% dos E2E da suíte mínima passando
- [ ] Smoke checks pós-deploy passando em staging
- [ ] Zero vulnerabilidades críticas no security scan
- [ ] Performance baseline dentro das metas documentadas

---

*Documento mantido pelo Release Readiness Lead. Revisar a cada fase do produto.*
