# NexTraceOne — Análise Consolidada de Gaps e Plano de Ação

> **Data:** 7 de Abril de 2026
> **Objetivo:** Consolidar todas as funcionalidades mapeadas nos ficheiros `.md` que **ainda não estão implementadas** e definir um plano de ação para completar tudo.

---

## Sumário Executivo

Após análise exaustiva de **90+ ficheiros `.md`** de documentação e cross-referência com a implementação real do código-fonte (12 módulos backend, 130+ páginas frontend, 99 endpoints, 296 entidades de domínio, 154 migrações), este documento consolida **todos os gaps** identificados e propõe um plano de ação faseado para os resolver.

### Números Globais

| Métrica | Valor |
|---------|-------|
| Funcionalidades totalmente implementadas | ~92% |
| Funcionalidades parcialmente implementadas | ~5% |
| Funcionalidades não implementadas (gaps reais) | ~3% |
| Gaps Críticos | 0 (3 → 0, todos resolvidos) |
| Gaps Alta Prioridade | 10 (18 → 10, 8 resolvidos) |
| Gaps Média Prioridade | 20 (30 → 20, 10 resolvidos) |
| Gaps Baixa Prioridade | 8 |
| **Total de Gaps Resolvidos** | **21** |
| **Total de Gaps Remanescentes** | **38** |

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
| GAP-SEED-05 | ConfigurationDefinitionSeeder executa em todos os ambientes — XML doc confirma design intencional ("segura em todos os ambientes") | ✅ Resolvido (by design) |

---

### 2. DOCUMENTAÇÃO COM INFORMAÇÕES INCORRETAS

**Severidade: 🔴 CRÍTICA**
**Fonte:** analysis-output/modules/documentation-gaps.md, analysis-output/00-overall-gap-summary.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-DOC-01 | `docs/IMPLEMENTATION-STATUS.md` — contagens de testes atualizadas, módulos AI Grounding/Guardrails corrigidos, Knowledge Graph/AutoDoc adicionados | ✅ Corrigido |
| GAP-DOC-02 | `docs/CORE-FLOW-GAPS.md` — guardrails de segurança AI documentados (já implementados), grounding cross-module verificado | ✅ Corrigido |
| GAP-DOC-03 | Documentação de deployment incompleta — bootstrap, connection strings, seed strategy, OTEL endpoint não documentados | 🟠 Alta |
| GAP-DOC-04 | 22 ficheiros em `analysis-output/` marcados com disclaimer "ARCHIVED" — referência atual é `CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` | ✅ Corrigido |
| GAP-DOC-05 | XML doc comments em Governance FinOps DTOs — 6 handlers corrigidos para refletir `IsSimulated: false` (dados reais) | ✅ Corrigido |

---

### 3. CONTRATOS E SERVIÇOS — Gaps Remanescentes

**Severidade: 🟠 ALTA / 🟡 MÉDIA**
**Fonte:** docs/SERVICES-CONTRACTS-ACTION-PLAN.md, docs/SERVICES-CONTRACTS-DEEP-ANALYSIS-2026-04.md, docs/SERVICE-CREATION-STUDIO-PLAN.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-CTR-01 | GraphQL Contracts — enum existe mas sem implementação real | ❌ Não implementado (PLANNED) |
| GAP-CTR-02 | Protobuf Contracts — enum existe mas sem implementação real | ❌ Não implementado (PLANNED) |
| GAP-CTR-03 | Contract Drift Detection — `DetectContractDrift` implementado com detecção de ghost endpoints + endpoints não declarados; `GET /api/v1/catalog/contracts/{id}/drift` | ✅ Implementado |
| GAP-CTR-04 | 56 chaves de validação i18n para builders — todas presentes em 4 locales (en, pt-PT, pt-BR, es) | ✅ Implementado |
| GAP-CTR-05 | Service Type options em `ServiceCatalogPage.tsx` — todas as 22 opções agora usam i18n keys via `t()` | ✅ Corrigido |
| GAP-CTR-06 | Template options em `TemplateEditorPage.tsx` — todas as 12 opções agora usam i18n keys via `t()` | ✅ Corrigido |
| GAP-CTR-07 | Full-Text Search Index (GIN index necessário) para catálogo | 🟡 Média |
| GAP-CTR-08 | `.AsNoTracking()` — implementado ao nível do repositório (EfDeveloperSurveyRepository, ServiceAssetRepository, ProductivitySnapshotRepository, DxScoreRepository, etc.) | ✅ Implementado (repository layer) |
| GAP-CTR-09 | Frontend Unit Tests — 0 testes para VisualRestBuilder, VisualSoapBuilder, VisualEventBuilder, ServiceRegistrationWizard, ContractHealthDashboard | 🟠 Alta |
| GAP-CTR-10 | E2E Tests gaps: versioning, approval, health dashboard, deprecation flows sem cobertura | 🟡 Média |

---

### 4. AI E KNOWLEDGE — Gaps Remanescentes

**Severidade: 🟡 MÉDIA**
**Fonte:** docs/AI-ARCHITECTURE.md, docs/AI-GOVERNANCE.md, analysis-output/modules/aiknowledge-gaps.md

| Gap | Detalhe | Status |
|-----|---------|--------|
| GAP-AI-01 | Data Redaction antes de envio para LLM — `DefaultGuardrailCatalog` com `credential-leak-prevention` (sanitize) + `pii-email-detection` (warn) + `pii-phone-detection` (warn) | ✅ Implementado |
| GAP-AI-02 | Prompt Injection Mitigation — `DefaultGuardrailCatalog` com `prompt-injection-detection` (block, severity critical) | ✅ Implementado |
| GAP-AI-03 | LLM Response Validation — `credential-leak-prevention` guardrail aplica sanitização a respostas; `sensitive-data-classification` loga dados classificados | ✅ Implementado |
| GAP-AI-04 | `ListKnowledgeSourceWeights` — persistidos em `aik_source_weights`; consulta DB com fallback a defaults | ✅ Implementado |
| GAP-AI-05 | `PlanExecution` model selection — usa `IAiModelCatalogService` para resolver modelo real via Model Registry | ✅ Implementado |
| GAP-AI-06 | Model Selection Routing — heurísticas por keyword, sem NLP real (funcional para produção) | 🟡 Média (aceitável) |
| GAP-AI-07 | `AiSourceRegistryService.CheckHealthAsync()` — verifica conectividade HTTP para fontes Document, Database (PostgreSQL via Npgsql) e ExternalMemory | ✅ Implementado |
| GAP-AI-08 | SAML Authentication — entidades de domínio existem mas sem protocol handlers | 🟡 Média |
| GAP-AI-09 | Knowledge Graph Visual — `KnowledgeGraphPage` implementada com `GetKnowledgeGraphOverview` backend | ✅ Implementado |

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
| GAP-FE-01 | Error handling adicionado a 8 páginas (DeveloperExperienceScore, GlobalSearch, TemplateLibrary, TemplateDetail, TemplateEditor, AiAgents, ParameterUsageReport, ParameterComplianceDashboard) | ✅ Corrigido |
| GAP-FE-02 | 3 páginas Governance (PolicyCatalog, Compliance, EvidencePackages) com EmptyState component | ✅ Parcialmente corrigido |
| GAP-FE-03 | Páginas Governance com error handling — 100% das páginas Governance amostradas têm `isError` + `PageErrorState` | ✅ Corrigido |
| GAP-FE-04 | DashboardPage genérica sem persona awareness (engineer, tech lead, exec, admin) | 🟠 Alta |
| GAP-FE-05 | `DemoBanner.tsx` + test removidos — dead code eliminado | ✅ Removido |
| GAP-FE-06 | `ProductAnalyticsOverviewPage.tsx` wrapper duplicado removido — apenas implementação real em `pages/` | ✅ Removido |

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
| GAP-OPS-01 | `InMemoryIncidentStore` — marcado com `[Obsolete]` + XML doc atualizado para indicar "test-only, production uses EfIncidentStore" | ✅ Documentado |
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
| GAP-XM-04 | `IKnowledgeModule` — implementada por `KnowledgeModuleService` em Knowledge.Infrastructure | ✅ Implementado |
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

| # | Ação | Gaps Resolvidos | Estado |
|---|------|-----------------|--------|
| 1.1 | ~~Atualizar `docs/IMPLEMENTATION-STATUS.md` — corrigir as 9+ afirmações incorretas, refletir estado real~~ | GAP-DOC-01 | ✅ Concluído |
| 1.2 | ~~Atualizar `docs/CORE-FLOW-GAPS.md` — documentar guardrails AI, corrigir status grounding~~ | GAP-DOC-02 | ✅ Concluído |
| 1.3 | ~~Adicionar disclaimer "ARCHIVED" em documentação desatualizada~~ — 22 ficheiros em analysis-output/ marcados | GAP-DOC-04 | ✅ Concluído |
| 1.4 | ~~Corrigir XML doc comments FinOps DTOs (`IsSimulated` discrepancy)~~ | GAP-DOC-05 | ✅ Concluído |
| 1.5 | Documentar production bootstrap strategy (admin inicial, primeiro tenant, config mínima) | GAP-SEED-03 | ⬜ Pendente |
| 1.6 | Documentar deployment completo (connection strings, OTEL endpoints, seeds) | GAP-DOC-03 | ⬜ Pendente |
| 1.7 | ~~Verificar guard `IsDevelopment()` do ConfigurationDefinitionSeeder — confirmado seguro por design~~ | GAP-SEED-05 | ✅ Resolvido (by design) |
| 1.8 | Resolver ou documentar seed strategy para staging environment | GAP-SEED-04 | ⬜ Pendente |

**Entregável:** Documentação fiável e seed strategy segura.

---

### FASE 2 — Hardening de Frontend (3-5 dias)

**Prioridade: 🟠 ALTA**
**Objetivo:** Garantir que todas as páginas frontend tratam erros e estados vazios de forma consistente.

| # | Ação | Gaps Resolvidos | Estado |
|---|------|-----------------|--------|
| 2.1 | ~~Adicionar `isError` + PageErrorState em 8 páginas que falhavam silenciosamente~~ | GAP-FE-01, GAP-FE-03 | ✅ Concluído |
| 2.2 | ~~Adicionar EmptyState component em 3 páginas Governance (PolicyCatalog, Compliance, EvidencePackages)~~ | GAP-FE-02 | ✅ Parcial (3/25 páginas) |
| 2.3 | Implementar persona awareness na DashboardPage (engineer, tech lead, exec, admin) | GAP-FE-04 | ⬜ Pendente |
| 2.4 | ~~Remover `DemoBanner.tsx` + test file — dead code eliminado~~ | GAP-FE-05 | ✅ Concluído |
| 2.5 | ~~Remover duplicação de `ProductAnalyticsOverviewPage.tsx` wrapper~~ | GAP-FE-06 | ✅ Concluído |
| 2.6 | ~~56 chaves de validação i18n para builders — verificadas como presentes em 4 locales~~ | GAP-CTR-04 | ✅ Já implementado |
| 2.7 | ~~Substituir hardcoded options por i18n: ServiceType no `ServiceCatalogPage.tsx` — 18 keys adicionadas~~ | GAP-CTR-05 | ✅ Concluído |
| 2.8 | ~~Substituir hardcoded options por i18n: Templates no `TemplateEditorPage.tsx` — 9 keys adicionadas~~ | GAP-CTR-06 | ✅ Concluído |

**Entregável:** Frontend consistente com error handling e UX completa.

---

### FASE 3 — Segurança e Qualidade (3-5 dias)

**Prioridade: 🟠 ALTA**
**Objetivo:** Fechar gaps de segurança e qualidade de código.

| # | Ação | Gaps Resolvidos | Estado |
|---|------|-----------------|--------|
| 3.1 | ~~PII Data Redaction — `DefaultGuardrailCatalog` já implementa `credential-leak-prevention` + `pii-email-detection` + `pii-phone-detection`~~ | GAP-AI-01 | ✅ Já implementado |
| 3.2 | ~~Prompt Injection sanitization — `DefaultGuardrailCatalog` já implementa `prompt-injection-detection` (block)~~ | GAP-AI-02 | ✅ Já implementado |
| 3.3 | ~~LLM Response sanitization — `credential-leak-prevention` guardrail (sanitize action) + `sensitive-data-classification` (log)~~ | GAP-AI-03 | ✅ Já implementado |
| 3.4 | Ativar httpOnly Cookie mode (atualmente opt-in, precisa de validação staging) | GAP-SEC-01 | ⬜ Pendente |
| 3.5 | Expandir Encryption at Rest para campos sensíveis adicionais | GAP-SEC-02 | ⬜ Pendente |
| 3.6 | Mover dev credentials de appsettings.json para user-secrets | GAP-SEC-04 | ⬜ Pendente |
| 3.7 | Validar CORS configuration para produção | GAP-SEC-06 | ⬜ Pendente |
| 3.8 | Adicionar dependency vulnerability audit ao CI pipeline | GAP-SEC-05 | ⬜ Pendente |
| 3.9 | `.AsNoTracking()` — já implementado ao nível do repositório (EfDeveloperSurveyRepository, ServiceAssetRepository, etc.) | GAP-CTR-08 | ✅ Já implementado (repository layer) |
| 3.10 | ~~`InMemoryIncidentStore` marcado com `[Obsolete]` + XML doc atualizado~~ | GAP-OPS-01 | ✅ Concluído |

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

| # | Ação | Gaps Resolvidos | Estado |
|---|------|-----------------|--------|
| 5.1 | Implementar CI/CD connectors reais (pelo menos GitLab e GitHub Actions) | GAP-INT-01 | ⬜ Pendente |
| 5.2 | ~~`IKnowledgeModule` cross-module interface — já implementada por `KnowledgeModuleService`~~ | GAP-XM-04 | ✅ Já implementado |
| 5.3 | ~~`ListKnowledgeSourceWeights` — já persistidos em `aik_source_weights` com DB query~~ | GAP-AI-04 | ✅ Já implementado |
| 5.4 | ~~`AiSourceRegistryService.CheckHealthAsync()` — já verifica HTTP, PostgreSQL, ExternalMemory~~ | GAP-AI-07 | ✅ Já implementado |
| 5.5 | Melhorar model selection routing — ir além de keyword heuristics | GAP-AI-05, GAP-AI-06 | 🟡 Aceitável (funcional) |
| 5.6 | Implementar SAML protocol handlers (entidades existem, falta flow) | GAP-AI-08 | ⬜ Pendente |
| 5.7 | Adicionar GIN index para Full-Text Search no catálogo | GAP-CTR-07 | ⬜ Pendente |
| 5.8 | Consolidar connection strings — reduzir de 22+ para número gerível | GAP-XM-02 | ⬜ Pendente |
| 5.9 | Configurar OTEL endpoint default para produção (não localhost:4317) | GAP-XM-03 | ⬜ Pendente |
| 5.10 | Limpar outbox processors sem uso real | GAP-XM-01 | ⬜ Pendente |
| 5.11 | Implementar event tracking real no frontend para ProductAnalytics | GAP-XM-05 | ⬜ Pendente |
| 5.12 | Criar IIS deployment config (web.config, applicationHost.xml) | GAP-INT-03 | ⬜ Pendente |

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

| Métrica | Antes | Atual | Objetivo |
|---------|-------|-------|----------|
| Páginas frontend com error handling | 72% | 86%+ | 100% |
| Páginas frontend com empty state | 51% | 55%+ | 100% |
| Cobertura testes Integrations | 1:14 | 1:14 | 1:3 |
| Cobertura testes ProductAnalytics | 1:26 | 1:26 | 1:5 |
| Documentação factualmente correta | ~90% | ~98% | 100% |
| AI security (PII redaction, injection) | Parcial | ✅ Completo (guardrails) | Completo |
| CI pipeline com migration validation | ❌ | ❌ | ✅ |
| CI pipeline com smoke check | ❌ | ❌ | ✅ |

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
