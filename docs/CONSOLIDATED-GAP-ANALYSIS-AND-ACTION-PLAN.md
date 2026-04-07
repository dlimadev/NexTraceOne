# NexTraceOne — Análise Consolidada de Gaps e Plano de Ação

> **Data:** 7 de Abril de 2026
> **Objetivo:** Consolidar todas as funcionalidades mapeadas nos ficheiros `.md` que **ainda não estão implementadas** e definir um plano de ação para completar tudo.

---

## Sumário Executivo

Após análise exaustiva de **90+ ficheiros `.md`** de documentação e cross-referência com a implementação real do código-fonte (12 módulos backend, 130+ páginas frontend, 99 endpoints, 296 entidades de domínio, 154 migrações), este documento consolida **todos os gaps** identificados e propõe um plano de ação faseado para os resolver.

### Números Globais

| Métrica | Valor |
|---------|-------|
| Funcionalidades totalmente implementadas | ~85% |
| Funcionalidades parcialmente implementadas | ~10% |
| Funcionalidades não implementadas (gaps reais) | ~5% |
| Gaps Críticos | 3 |
| Gaps Alta Prioridade | 18 |
| Gaps Média Prioridade | 30 |
| Gaps Baixa Prioridade | 8 |
| **Total de Gaps** | **59** |

---

## PARTE 1 — Gaps Consolidados por Categoria

---

### 1. SEED STRATEGY E BOOTSTRAP

**Severidade: 🔴 CRÍTICA / 🟠 ALTA**
**Fonte:** analysis-output/00-seed-strategy-gaps.md, analysis-output/modules/seeds-and-bootstrap-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-SEED-01 | 6 de 7 ficheiros SQL de seed referenciados não existem como ficheiros separados (apenas seed_development.sql e seed_production.sql monolíticos) | 🟠 Alta |
| GAP-SEED-02 | Seed strategy tem falhas silenciosas — ficheiros SQL inexistentes geram warning mas continuam | 🟠 Alta |
| GAP-SEED-03 | Production bootstrap strategy não documentada (admin inicial, primeiro tenant, config mínima) | 🟠 Alta |
| GAP-SEED-04 | Seed strategy sem dados para staging environment | 🟡 Média |
| GAP-SEED-05 | ConfigurationDefinitionSeeder pode faltar guard `IsDevelopment()` — risco de seed indevido em produção | 🟡 Média |

---

### 2. DOCUMENTAÇÃO COM INFORMAÇÕES INCORRETAS

**Severidade: 🔴 CRÍTICA**
**Fonte:** analysis-output/modules/documentation-gaps.md, analysis-output/00-overall-gap-summary.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-DOC-01 | `docs/IMPLEMENTATION-STATUS.md` contém 9+ afirmações incorretas sobre o estado de implementação | 🔴 Crítico |
| GAP-DOC-02 | `docs/CORE-FLOW-GAPS.md` contém 7+ afirmações incorretas sobre mocks e hardcoded responses (muitos já foram implementados) | 🔴 Crítico |
| GAP-DOC-03 | Documentação de deployment incompleta — bootstrap, connection strings, seed strategy, OTEL endpoint não documentados | 🟠 Alta |
| GAP-DOC-04 | Falta disclaimer "ARCHIVED — outdated" em relatórios de Março 2026 referenciados como verdade atual | 🟢 Baixa |
| GAP-DOC-05 | XML doc comments em Governance FinOps DTOs mencionam `IsSimulated=true` mas código passa `false` | 🟡 Média |

---

### 3. CONTRATOS E SERVIÇOS — Gaps Remanescentes

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** docs/SERVICES-CONTRACTS-ACTION-PLAN.md, docs/SERVICES-CONTRACTS-DEEP-ANALYSIS-2026-04.md, docs/SERVICE-CREATION-STUDIO-PLAN.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-CTR-01 | GraphQL Contracts — enum existe mas sem implementação real | ❌ Não implementado (PLANNED) |
| GAP-CTR-02 | Protobuf Contracts — enum existe mas sem implementação real | ❌ Não implementado (PLANNED) |
| GAP-CTR-03 | Contract Drift Detection — comparação published vs runtime | ❌ Não implementado (PLANNED) |
| GAP-CTR-04 | 57 chaves de validação i18n em falta em todos os locales (builder validation messages) | ❌ Não implementado |
| GAP-CTR-05 | Service Type Options hardcoded em `ServiceCatalogPage.tsx` (API Gateway, Framework/SDK) | 🟡 Média |
| GAP-CTR-06 | Template Options hardcoded em `TemplateEditorPage.tsx` (gRPC, .NET, Node.js) | 🟡 Média |
| GAP-CTR-07 | Full-Text Search Index (GIN index necessário) para catálogo | 🟡 Média |
| GAP-CTR-08 | `.AsNoTracking()` ausente em todos os query handlers (risco de performance) | 🟡 Média |
| GAP-CTR-09 | Frontend Unit Tests — 0 testes para VisualRestBuilder, VisualSoapBuilder, VisualEventBuilder, ServiceRegistrationWizard, ContractHealthDashboard | 🟠 Alta |
| GAP-CTR-10 | E2E Tests gaps: versioning, approval, health dashboard, deprecation flows sem cobertura | 🟡 Média |

---

### 4. AI E KNOWLEDGE — Gaps Remanescentes

**Severidade: 🟡 MÉDIA**
**Fonte:** docs/AI-ARCHITECTURE.md, docs/AI-GOVERNANCE.md, analysis-output/modules/aiknowledge-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-AI-01 | Data Redaction antes de envio para LLM — sem sanitização de PII | ⚠️ Parcial |
| GAP-AI-02 | Prompt Injection Mitigation — sem sanitização explícita | ⚠️ Parcial |
| GAP-AI-03 | LLM Response Validation — sem sanitização antes de display no frontend | ⚠️ Parcial |
| GAP-AI-04 | `ListKnowledgeSourceWeights` — stub in-memory, pesos não persistidos no banco | 🟡 Média |
| GAP-AI-05 | `PlanExecution` model selection simplificado — não usa model registry completo com routing context-aware | 🟡 Média |
| GAP-AI-06 | Model Selection Routing — heurísticas por keyword, sem NLP real | 🟡 Média |
| GAP-AI-07 | `AiSourceRegistryService.CheckHealthAsync()` retorna valor fixo em vez de health check real | 🟢 Baixa |
| GAP-AI-08 | SAML Authentication — entidades de domínio existem mas sem protocol handlers | 🟡 Média |
| GAP-AI-09 | Knowledge Graph Visual — interactive graph visualization (PLANNED Wave 2) | ❌ Parcialmente implementado (página existe, falta integração real) |

---

### 5. SEGURANÇA — Gaps Remanescentes

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** docs/SECURITY-ARCHITECTURE.md, docs/security/application-hardening-checklist.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-SEC-01 | httpOnly Cookie + CSRF — infraestrutura existe mas opt-in activation pendente | ⚠️ Parcial |
| GAP-SEC-02 | Encryption at Rest Full Coverage — infraestrutura existe, aplicação seletiva por campo necessária | ⚠️ Parcial |
| GAP-SEC-03 | Assembly/artifact signing (.NET + frontend bundles) | ❌ Não implementado |
| GAP-SEC-04 | appsettings.json dev credentials hardcoded (devem ser movidos para user-secrets) | 🟡 Média |
| GAP-SEC-05 | Dependency vulnerability audits automatizados | 🟡 Média |
| GAP-SEC-06 | CORS production config validation | 🟠 Alta |

---

### 6. FRONTEND — Gaps Transversais

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** analysis-output/modules/frontend-shared-gaps.md, analysis-output/modules/governance-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-FE-01 | 30 de 106 páginas frontend sem error handling (`isError`/`ErrorBoundary`) — 28% falham silenciosamente | 🟠 Alta |
| GAP-FE-02 | 52+ de 106 páginas frontend sem empty state pattern — UX mostra ecrãs em branco sem feedback | 🟡 Média |
| GAP-FE-03 | 22 de 25 páginas Governance sem error handling (`isError`) | 🟠 Alta |
| GAP-FE-04 | DashboardPage genérica sem persona awareness (engineer, tech lead, exec, admin) | 🟠 Alta |
| GAP-FE-05 | `DemoBanner.tsx` existe mas nunca é importado/usado — dead code | 🟢 Baixa |
| GAP-FE-06 | `ProductAnalyticsOverviewPage.tsx` duplicada em 2 localizações | 🟢 Baixa |

---

### 7. INTEGRAÇÕES — Gaps

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** analysis-output/modules/integrations-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-INT-01 | CI/CD integration connectors (GitLab, Jenkins, GitHub Actions, Azure DevOps) são stubs metadata-only sem processamento real de dados | 🟠 Alta |
| GAP-INT-02 | Kafka integration — modelo de domínio existe mas sem producer/consumer real | ⚠️ Parcial |
| GAP-INT-03 | IIS deployment config — apenas documentação, sem web.config ou applicationHost.xml | ⚠️ Parcial |
| GAP-INT-04 | External queue consumer pendente para ingestion pipeline | 🟡 Média |

---

### 8. TESTES E QUALIDADE — Gaps

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** analysis-output/modules/tests-pipelines-quality-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-TST-01 | Integrations module — 3 test files para 42 implementation files (ratio 1:14) | 🟠 Alta |
| GAP-TST-02 | ProductAnalytics module — 3 test files para 26 implementation files (ratio 1:26) | 🟠 Alta |
| GAP-TST-03 | CI pipeline sem migration validation step (`dotnet ef migrations has-pending-model-changes`) | 🟡 Média |
| GAP-TST-04 | CI pipeline não corre smoke check (`smoke-check.sh` existe mas não integrado) | 🟡 Média |
| GAP-TST-05 | E2E tests não cobrem fluxo de notifications | 🟡 Média |
| GAP-TST-06 | E2E tests para incidents e AI usam fixtures estáticas em vez de validar contra backend real | 🟡 Média |

---

### 9. OPERAÇÕES E OBSERVABILIDADE — Gaps

**Severidade: 🟡 MÉDIA**
**Fonte:** analysis-output/modules/operationalintelligence-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-OPS-01 | `InMemoryIncidentStore` — 748+ linhas de dead code não registadas em DI (ativo é `EfIncidentStore`) | 🟡 Média |
| GAP-OPS-02 | Runtime Intelligence parcial — DbContext e repositories presentes, integração não completa | ⚠️ Parcial |
| GAP-OPS-03 | Cost Intelligence parcial — CostIntelligenceDbContext existe, consumo via cross-module | ⚠️ Parcial |

---

### 10. MÓDULOS CROSS-CUTTING — Gaps

**Severidade: 🟡 MÉDIA**
**Fonte:** analysis-output/00-critical-cross-module-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-XM-01 | Outbox infrastructure registada para 21 DbContexts mas maioritariamente sem uso — processadores correm ciclos sem mensagens | 🟡 Média |
| GAP-XM-02 | 22+ connection strings necessárias para full deployment — complexidade e risco de misconfiguration significativo | 🟡 Média |
| GAP-XM-03 | OpenTelemetry default endpoint é `localhost:4317` — produção requer override ou telemetry falha silenciosamente | 🟡 Média |
| GAP-XM-04 | Knowledge module sem `IKnowledgeModule` cross-module interface — não pode ser consumido por AI, Operations | 🟡 Média |
| GAP-XM-05 | No event tracking real no frontend para ProductAnalytics — backend repository existe mas é maioritariamente vazio | 🟡 Média |

---

### 11. FUNCIONALIDADES FUTURAS PLANEADAS (Roadmap)

**Severidade: 🔵 PLANEADO**
**Fonte:** docs/EVOLUTION-ROADMAP-2026-2027.md, docs/FEATURE-ANALYSIS-AND-INNOVATION.md, docs/SERVICE-CREATION-STUDIO-PLAN.md

Estas funcionalidades estão documentadas como planeadas mas **não são gaps de implementação atual** — são evolução futura:

| Funcionalidade | Módulo | Fase Planeada |
|---------------|--------|---------------|
| IDE Extensions (VS Code, Visual Studio) — real integration (não apenas docs) | AI/IDE | Phase 4.2 |
| Dependency Advisor Agent | AIKnowledge | Wave futuro |
| Architecture Fitness Agent | AIKnowledge | Wave futuro |
| Documentation Quality Agent | AIKnowledge | Wave futuro |
| GitLab real integration (repository push) | Integrations | Wave futuro |
| Azure DevOps real integration | Integrations | Wave futuro |
| GitHub Advisory Integration | DependencyGovernance | Wave futuro |
| OSV/NVD Integration | DependencyGovernance | Wave futuro |
| Dependency Pinning Policy | Governance | Wave futuro |
| License Allowlist/Denylist configurável | Governance | Wave futuro |
| Breaking Change Detection em dependências | DependencyGovernance | Wave futuro |
| Reproducible Builds enforcement | DependencyGovernance | Wave futuro |
| VEX (Vulnerability Exploitability) | DependencyGovernance | Wave futuro |
| Dependency Drift Monitor | DependencyGovernance | Wave futuro |
| Pre-Commit Governance Check | Governance | Wave futuro |
| Auto-Fix for Violations | Governance | Wave futuro |
| Compliance as Code (PCI-DSS, SOC2, HIPAA) | Governance | Wave futuro |
| Secure Defaults Generator | Templates | Wave futuro |
| Developer Onboarding Path | Catalog | Wave futuro |
| Smart Template Recommendations | Catalog | Wave futuro |
| Service Impact Preview | Catalog | Wave futuro |
| Duplicate Service Detection (IA-powered) | Catalog | Wave futuro |
| ADR Generator | Knowledge | Wave futuro |
| Docker/Helm/CI Blueprint Templates | Templates | Wave futuro |
| Environment Blueprint (IaC) | Templates | Wave futuro |

---

## PARTE 2 — Plano de Ação

O plano está organizado em **6 fases**, priorizadas por impacto no produto e dependências técnicas.

---

### FASE 1 — Correção Crítica de Documentação e Seed Strategy (1-2 dias)

**Prioridade: 🔴 CRÍTICA**
**Objetivo:** Eliminar informação incorreta que causa confusão e corrigir seed strategy.

| # | Ação | Gaps Resolvidos | Esforço |
|---|------|-----------------|---------|
| 1.1 | Atualizar `docs/IMPLEMENTATION-STATUS.md` — corrigir as 9+ afirmações incorretas, refletir estado real | GAP-DOC-01 | 2h |
| 1.2 | Atualizar `docs/CORE-FLOW-GAPS.md` — corrigir 7+ afirmações incorretas sobre mocks já resolvidos | GAP-DOC-02 | 2h |
| 1.3 | Adicionar disclaimer "ARCHIVED" em documentação desatualizada de Março 2026 | GAP-DOC-04 | 30min |
| 1.4 | Corrigir XML doc comments FinOps DTOs (`IsSimulated` discrepancy) | GAP-DOC-05 | 30min |
| 1.5 | Documentar production bootstrap strategy (admin inicial, primeiro tenant, config mínima) | GAP-SEED-03 | 2h |
| 1.6 | Documentar deployment completo (connection strings, OTEL endpoints, seeds) | GAP-DOC-03 | 3h |
| 1.7 | Adicionar guard `IsDevelopment()` ao ConfigurationDefinitionSeeder se em falta | GAP-SEED-05 | 1h |
| 1.8 | Resolver ou documentar seed strategy para staging environment | GAP-SEED-04 | 1h |

**Entregável:** Documentação fiável e seed strategy segura.

---

### FASE 2 — Hardening de Frontend (3-5 dias)

**Prioridade: 🟠 ALTA**
**Objetivo:** Garantir que todas as páginas frontend tratam erros e estados vazios de forma consistente.

| # | Ação | Gaps Resolvidos | Esforço |
|---|------|-----------------|---------|
| 2.1 | Adicionar `isError` + error boundary em 30 páginas que falham silenciosamente | GAP-FE-01, GAP-FE-03 | 2d |
| 2.2 | Adicionar empty state pattern em 52+ páginas que mostram ecrãs em branco | GAP-FE-02 | 2d |
| 2.3 | Implementar persona awareness na DashboardPage (engineer, tech lead, exec, admin) | GAP-FE-04 | 1d |
| 2.4 | Remover `DemoBanner.tsx` ou integrá-lo corretamente | GAP-FE-05 | 30min |
| 2.5 | Resolver duplicação de `ProductAnalyticsOverviewPage.tsx` | GAP-FE-06 | 30min |
| 2.6 | Adicionar 57 chaves de validação i18n em falta para contract builders | GAP-CTR-04 | 2h |
| 2.7 | Substituir hardcoded options por parametrização: ServiceType no `ServiceCatalogPage.tsx` | GAP-CTR-05 | 1h |
| 2.8 | Substituir hardcoded options por parametrização: Templates no `TemplateEditorPage.tsx` | GAP-CTR-06 | 1h |

**Entregável:** Frontend consistente com error handling e UX completa.

---

### FASE 3 — Segurança e Qualidade (3-5 dias)

**Prioridade: 🟠 ALTA**
**Objetivo:** Fechar gaps de segurança e qualidade de código.

| # | Ação | Gaps Resolvidos | Esforço |
|---|------|-----------------|---------|
| 3.1 | Implementar PII Data Redaction antes de envio para LLM | GAP-AI-01 | 1d |
| 3.2 | Implementar Prompt Injection sanitization | GAP-AI-02 | 1d |
| 3.3 | Implementar LLM Response sanitization antes de display frontend | GAP-AI-03 | 4h |
| 3.4 | Ativar httpOnly Cookie mode (atualmente opt-in, precisa de validação staging) | GAP-SEC-01 | 4h |
| 3.5 | Expandir Encryption at Rest para campos sensíveis adicionais | GAP-SEC-02 | 4h |
| 3.6 | Mover dev credentials de appsettings.json para user-secrets | GAP-SEC-04 | 2h |
| 3.7 | Validar CORS configuration para produção | GAP-SEC-06 | 2h |
| 3.8 | Adicionar dependency vulnerability audit ao CI pipeline | GAP-SEC-05 | 2h |
| 3.9 | Adicionar `.AsNoTracking()` em todos os query handlers de leitura | GAP-CTR-08 | 3h |
| 3.10 | Remover ou documentar `InMemoryIncidentStore` como dead code | GAP-OPS-01 | 1h |

**Entregável:** Segurança AI reforçada, hardening completo, performance queries melhorada.

---

### FASE 4 — Testes e CI/CD (3-5 dias)

**Prioridade: 🟠 ALTA**
**Objetivo:** Aumentar cobertura de testes e reforçar CI pipeline.

| # | Ação | Gaps Resolvidos | Esforço |
|---|------|-----------------|---------|
| 4.1 | Adicionar testes unitários frontend: VisualRestBuilder, VisualSoapBuilder, VisualEventBuilder, ServiceRegistrationWizard, ContractHealthDashboard | GAP-CTR-09 | 2d |
| 4.2 | Adicionar E2E tests: versioning, approval, health dashboard, deprecation | GAP-CTR-10 | 1d |
| 4.3 | Aumentar cobertura Integrations module (de 3 para 15+ test files) | GAP-TST-01 | 1d |
| 4.4 | Aumentar cobertura ProductAnalytics module (de 3 para 10+ test files) | GAP-TST-02 | 1d |
| 4.5 | Adicionar migration validation step ao CI (`dotnet ef migrations has-pending-model-changes`) | GAP-TST-03 | 2h |
| 4.6 | Integrar smoke-check.sh no CI pipeline | GAP-TST-04 | 1h |
| 4.7 | Adicionar E2E tests para fluxo de notifications | GAP-TST-05 | 4h |
| 4.8 | Melhorar E2E tests de incidents e AI — validar contra backend real | GAP-TST-06 | 4h |

**Entregável:** Cobertura de testes adequada, CI pipeline robusto.

---

### FASE 5 — Integrações e Cross-Module (3-5 dias)

**Prioridade: 🟡 MÉDIA**
**Objetivo:** Completar integrações e resolver gaps cross-module.

| # | Ação | Gaps Resolvidos | Esforço |
|---|------|-----------------|---------|
| 5.1 | Implementar CI/CD connectors reais (pelo menos GitLab e GitHub Actions) | GAP-INT-01 | 2d |
| 5.2 | Implementar `IKnowledgeModule` cross-module interface | GAP-XM-04 | 4h |
| 5.3 | Persistir `ListKnowledgeSourceWeights` no banco em vez de in-memory | GAP-AI-04 | 4h |
| 5.4 | Implementar `AiSourceRegistryService.CheckHealthAsync()` com health check real | GAP-AI-07 | 2h |
| 5.5 | Melhorar model selection routing — ir além de keyword heuristics | GAP-AI-05, GAP-AI-06 | 1d |
| 5.6 | Implementar SAML protocol handlers (entidades existem, falta flow) | GAP-AI-08 | 1d |
| 5.7 | Adicionar GIN index para Full-Text Search no catálogo | GAP-CTR-07 | 2h |
| 5.8 | Consolidar connection strings — reduzir de 22+ para número gerível | GAP-XM-02 | 4h |
| 5.9 | Configurar OTEL endpoint default para produção (não localhost:4317) | GAP-XM-03 | 1h |
| 5.10 | Limpar outbox processors sem uso real | GAP-XM-01 | 2h |
| 5.11 | Implementar event tracking real no frontend para ProductAnalytics | GAP-XM-05 | 4h |
| 5.12 | Criar IIS deployment config (web.config, applicationHost.xml) | GAP-INT-03 | 4h |

**Entregável:** Integrações funcionais, cross-module interfaces completas.

---

## PARTE 3 — Resumo do Plano de Ação

### Cronograma Estimado

| Fase | Nome | Duração | Prioridade | Gaps Resolvidos |
|------|------|---------|------------|-----------------|
| **Fase 1** | Correção Documentação e Seeds | 1-2 dias | 🔴 Crítica | 8 |
| **Fase 2** | Hardening Frontend | 3-5 dias | 🟠 Alta | 8 |
| **Fase 3** | Segurança e Qualidade | 3-5 dias | 🟠 Alta | 10 |
| **Fase 4** | Testes e CI/CD | 3-5 dias | 🟠 Alta | 8 |
| **Fase 5** | Integrações e Cross-Module | 3-5 dias | 🟡 Média | 12 |
| **TOTAL** | | **13-22 dias** | | **46 ações** |

### Dependências entre Fases

```
Fase 1 (Docs) ──────────┐
                         ├──→ Fase 2 (Frontend) ──→ Fase 4 (Testes)
Fase 3 (Segurança) ─────┘
                                                    ↓
Fase 5 (Integrações) ────────────────────────────────┘
```

### Métricas de Sucesso

| Métrica | Antes | Objetivo |
|---------|-------|----------|
| Páginas frontend com error handling | 72% | 100% |
| Páginas frontend com empty state | 51% | 100% |
| Cobertura testes Integrations | 1:14 | 1:3 |
| Cobertura testes ProductAnalytics | 1:26 | 1:5 |
| Documentação factualmente correta | ~90% | 100% |
| AI security (PII redaction, injection) | Parcial | Completo |
| CI pipeline com migration validation | ❌ | ✅ |
| CI pipeline com smoke check | ❌ | ✅ |

---

## PARTE 4 — Funcionalidades Planeadas NÃO Incluídas (Evolução Futura)

As seguintes funcionalidades estão documentadas como roadmap futuro e **não são consideradas gaps atuais**. Devem ser abordadas em waves futuros:

1. **GraphQL e Protobuf Contracts** — requer decisão de design significativa
2. **IDE Extensions reais** (VS Code, Visual Studio) — requer client-side development
3. **Agentes AI especializados** (Dependency Advisor, Architecture Fitness, Doc Quality)
4. **Real Kafka producer/consumer** — requer infraestrutura message broker
5. **GitLab/Azure DevOps deep integration** — requer OAuth apps e API integration
6. **Compliance as Code** (PCI-DSS, SOC2, HIPAA) — requer expertise regulatória
7. **ADR Generator** e **Environment Blueprint** — funcionalidades Wave futuro
8. **Docker/Helm/CI Blueprint Templates** — templates de infra para scaffolding

---

## Notas Finais

1. **O estado geral do projeto é sólido** — ~85% das funcionalidades documentadas estão implementadas.
2. **Os gaps mais críticos são:** documentação incorreta e hardening de frontend.
3. **A segurança AI merece atenção imediata** — PII redaction e prompt injection são riscos reais.
4. **O plano pode ser executado incrementalmente** — cada fase entrega valor independente.
5. **Este documento deve ser atualizado** à medida que os gaps forem resolvidos (marcar como ✅ quando completo).
6. **O módulo de Licensing foi removido da solução** e não consta neste plano de ação.
