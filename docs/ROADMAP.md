# NexTraceOne — Roadmap

> **Última atualização:** Abril 2026
> **Fonte:** Auditoria Forense Março 2026 + Phase 7 Abril 2026
> **Referência de estado real:** `docs/IMPLEMENTATION-STATUS.md`
> **Status de implementação:** `docs/IMPLEMENTATION-STATUS.md`

---

## Visão do Produto

**NexTraceOne** — Plataforma enterprise unificada para governança de serviços, contratos, mudanças, operação e conhecimento operacional.

**Posicionamento:** Source of Truth para serviços, contratos, mudanças, operação e conhecimento operacional. Combina governance-first com change intelligence, service reliability e AI governada.

---

## Estado Atual (Abril 2026)

| Dimensão | Estado Real |
|---|---|
| Visão de produto | Bem definida, documentada, alinhada |
| Fundação arquitetural | Sólida — Clean Architecture, DDD, CQRS, bounded contexts |
| Módulos core (Catalog, Change, Identity, Audit) | READY para produção |
| Módulos operacionais (Incidents, Automation, Reliability) | READY — Incidents 85%, Automation 10/10 real, Reliability 15/15 real |
| AI Knowledge | REAL — LLM real E2E, governance real, 13 páginas frontend integradas |
| Governance | READY — 44+ handlers reais, FinOps cross-module, executive dashboard real, 25/26 frontend pages |
| Cross-module integration | COMPLETE — 15/15 interfaces implementadas |
| Frontend | REAL — 96%+ conectado ao backend real, design tokens 100% migrado |
| Segurança | Enterprise-grade — AES-256-GCM, isolamento de tenant em 3 camadas |
| Observabilidade | Estrutura configurada; ingestão E2E não validada |
| FinOps | REAL — via ICostIntelligenceModule e CostIntelligenceDbContext |
| Contract Studio | COMPLETE — 10/10 tipos de contrato com visual builders |
| Testes E2E | 8 specs Playwright confirmados + 5 testes real-environment separados |
| CI/CD | 5 workflows + e2e-smoke gate; E2E @smoke bloqueia PRs para main |

---

## Os Quatro Fluxos Centrais de Valor

### Fluxo 1 — Source of Truth / Contract Governance
**Estado: 75% funcional**

- ✅ Catalogação de serviços, contratos REST/SOAP/Kafka/background services: real
- ✅ Versionamento, diff semântico, compatibilidade: real
- ✅ Ownership via Graph: real
- ✅ Contract Studio: backend real, 10/10 contract types com visual builders
- ⚠️ Busca: GlobalSearch existe; SearchCatalog é stub intencional
- ⚠️ Documentação operacional: parcial (Knowledge Hub sem migrations)

**Evidência:** `src/modules/catalog/`, `docs/audit-forensic-2026-03/backend-state-report.md §Catalog`

---

### Fluxo 2 — Change Confidence
**Estado: 95% funcional — fluxo mais maduro**

- ✅ Submissão de mudança, blast radius, advisory, evidence pack: reais
- ✅ Approval/reject/conditional, rollback assessment, freeze windows: reais
- ✅ Promotion com gate evaluations: real
- ✅ Trilha de decisão + audit: real

**Evidência:** `src/modules/changegovernance/`, `docs/audit-forensic-2026-03/backend-state-report.md §ChangeGovernance`

---

### Fluxo 3 — Incident Correlation & Mitigation
**Estado: 85% funcional**

- ✅ `EfIncidentStore` (678 linhas): persistência real com `IncidentDbContext` e migração
- ✅ Frontend totalmente conectado — `IncidentsPage.tsx`, `IncidentDetailPage.tsx`, `RunbooksPage.tsx` usam API real
- ✅ CreateMitigationWorkflow — persiste via `IMitigationWorkflowRepository`
- ✅ GetMitigationHistory — consulta dados reais da base de dados
- ✅ RecordMitigationValidation — persiste logs de validação
- ✅ Correlação dinâmica incident↔change — `IIncidentCorrelationRepository`, `IChangeIntelligenceReader`, `LegacyEventCorrelator`
- ✅ IIncidentModule — interface cross-module implementada por `IncidentModuleService`
- ✅ Runbooks — `IRunbookRepository` com `EfRunbookRepository` registado
- ⚠️ Heurísticas de correlação são básicas (timestamp+service matching)
- ⚠️ Runbook templates — sem builder visual (backend CRUD é real)

**Evidência:** `src/modules/operationalintelligence/`, `src/frontend/src/features/operations/`

---

### Fluxo 4 — AI Assistant útil
**Estado: LLM real E2E; governance real; grounding cross-module parcial**

- ✅ Infraestrutura AI Governance funcional: modelos, políticas, budgets (EF Core real)
- ✅ Model registry, access policies, audit trail
- ✅ AI tool execution: 3 ferramentas reais (`list_services`, `get_service_health`, `list_recent_changes`)
- ✅ `SendAssistantMessage` invoca `IChatCompletionProvider.CompleteAsync()` via LLM real (Ollama/OpenAI)
- ✅ `AiAssistantPage` usa API real: `aiGovernanceApi.listConversations`, `sendMessage`, `getMessages` (7 chamadas)
- ✅ `IAiOrchestrationModule` implementado por `AiOrchestrationModule`; DbContext com migrações
- ✅ `IExternalAiModule` implementado por `ExternalAiModule`; DbContext com migrações
- ✅ `IAiGovernanceModule` implementado por `AiGovernanceModuleService`
- ✅ 13 páginas AI frontend totalmente integradas com APIs reais
- ⚠️ Cross-module grounding parcial — entidades de outros módulos acessíveis via grounding readers
- ⚠️ AI Source health check — conectores para fontes Database/ExternalMemory retornam estado persistido

**Evidência:** `src/modules/aiknowledge/`, `src/frontend/src/features/ai/`

---

## Testes & Qualidade

| Tipo | Quantidade | Estado |
|---|---|---|
| Testes unitários backend (.NET) | ~1.447 | Passando |
| Testes unitários frontend (Vitest) | ~264 | Passando |
| **Testes E2E (Playwright)** | **8 specs confirmados** | ✅ E2E @smoke testes agora bloqueiam PRs via CI |
| Testes E2E real-environment | 5 arquivos (`e2e-real/`) | Configuração separada, não são specs Playwright CI padrão |
| Testes de carga (k6) | 5 cenários | Thresholds não documentados |

> **⚠️ Correção de Auditoria (Março 2026):** Versões anteriores deste documento afirmavam "13 novos testes E2E". Apenas 8 Playwright specs existem confirmados. Os 5 testes real-environment (`e2e-real/`) são uma configuração separada e não integram o CI padrão.

> **✅ Atualização Abril 2026:** E2E @smoke testes agora são obrigatórios como gate de merge via `e2e-smoke` job no CI. `ci-status` job agrega todos os resultados para branch protection.

**~~Gap crítico:~~ Testes E2E agora bloqueiam PRs** via `e2e-smoke` job no CI.

**Evidência:** `.github/workflows/ci.yml` (e2e-smoke + ci-status jobs), `docs/audit-forensic-2026-03/tests-quality-pipelines-report.md`

---

## Prioridades de Desenvolvimento

### ~~Prioridade Máxima — Fecha fluxos core~~ ✅ CONCLUÍDO

> Os 4 fluxos core estão todos funcionais com dados reais. Cross-module interfaces (15/15) todas implementadas. Outbox activo para todos os 22 DbContexts.

### Prioridade Alta — Completar governança e compliance

1. ~~**Governance compliance pages** — 19 páginas frontend de compliance/policy/evidence precisam de integração com APIs reais~~ ✅ CONCLUÍDO (25/26 páginas conectadas)
2. ~~**Tornar testes E2E obrigatórios** como gate de merge para main~~ ✅ CONCLUÍDO — `e2e-smoke` job + `ci-status` aggregator
3. **CI/CD integration** — ingestão de deploy events reais de GitLab/Jenkins/GitHub Actions

### Prioridade Média — Qualidade e confiança

4. Eliminar warnings CS8632 nullable
5. ~~Padronizar loading, error e empty states no frontend~~ ✅ CONCLUÍDO — 7 governance pages migradas para TanStack Query + PageLoadingState/PageErrorState
6. Completar Product Analytics (pipeline de event tracking real)
7. AI cross-module grounding — enriquecer contexto de IA com entidades de todos os módulos
8. Correlação incident↔change avançada — ML/NLP-based correlation heuristics

---

## Módulos Removidos

| Módulo | Status | Referência |
|---|---|---|
| Commercial Governance | REMOVIDO (Removed in PR-17, module no longer exists) | PR-17 — módulo não alinhado ao núcleo do produto; sem `DbContext` de licensing ativo |

---

## Arquitetura Alvo

- **Estilo:** Modular monolith, DDD, Clean Architecture, SOLID, CQRS
- **Backend:** .NET 10 / ASP.NET Core 10, EF Core 10, PostgreSQL 16, MediatR, FluentValidation, Quartz.NET, Serilog, OpenTelemetry
- **Frontend:** React 18, TypeScript, Vite, TanStack Router, TanStack Query, Zustand, Tailwind CSS, Radix UI, Apache ECharts, Playwright
- **Infraestrutura:** PostgreSQL 16 (base central MVP), Docker Compose (POC), IIS (suporte explícito), evolução para Kubernetes
- **Observabilidade analítica:** ClickHouse como direção para workloads analíticos e de observabilidade

---

*Última atualização: Março 2026 — corrigido contra os achados da auditoria forense*
*Ver: `docs/audit-forensic-2026-03/final-project-state-assessment.md`*
