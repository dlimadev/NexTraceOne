# NexTraceOne — Roadmap

> **Última atualização:** Março 2026
> **Fonte:** Auditoria Forense Março 2026 — `docs/audit-forensic-2026-03/`
> **Referência de estado real:** `docs/audit-forensic-2026-03/final-project-state-assessment.md`
> **Status de implementação:** `docs/IMPLEMENTATION-STATUS.md`

---

## Visão do Produto

**NexTraceOne** — Plataforma enterprise unificada para governança de serviços, contratos, mudanças, operação e conhecimento operacional.

**Posicionamento:** Source of Truth para serviços, contratos, mudanças, operação e conhecimento operacional. Combina governance-first com change intelligence, service reliability e AI governada.

---

## Estado Atual (Março 2026)

| Dimensão | Estado Real |
|---|---|
| Visão de produto | Bem definida, documentada, alinhada |
| Fundação arquitetural | Sólida — Clean Architecture, DDD, CQRS, bounded contexts |
| Módulos core (Catalog, Change, Identity, Audit) | READY para produção |
| Módulos operacionais (Incidents, AI, Governance) | PARTIAL a MOCK |
| Cross-module integration | PLAN — 8 interfaces definidas, 0 implementadas |
| Frontend | PARTIAL — 89% conectado ao backend real, 11% mock inline |
| Segurança | Enterprise-grade — AES-256-GCM, isolamento de tenant em 3 camadas |
| Observabilidade | Estrutura configurada; ingestão E2E não validada |
| FinOps | 100% mock |
| Testes E2E | 8 specs Playwright confirmados + 5 testes real-environment separados |
| CI/CD | 5 workflows, aprovação manual para produção; E2E não bloqueia PRs |

---

## Os Quatro Fluxos Centrais de Valor

### Fluxo 1 — Source of Truth / Contract Governance
**Estado: 75% funcional**

- ✅ Catalogação de serviços, contratos REST/SOAP/Kafka/background services: real
- ✅ Versionamento, diff semântico, compatibilidade: real
- ✅ Ownership via Graph: real
- ✅ Contract Studio: backend real, UX precisa polish
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
**Estado: 0% funcional — correlation engine missing, frontend uses mockIncidents, runbooks hardcoded, mitigations not persisted**

> **⚠️ Correção de Auditoria (Março 2026):** Versões anteriores deste documento descreviam o Fluxo 3 como "em progresso / conectado". A auditoria forense confirma 0% de correlação dinâmica.
> **Evidência:** `docs/audit-forensic-2026-03/final-project-state-assessment.md §Fluxo 3`

- ❌ `IncidentsPage.tsx` usa `mockIncidents` hardcoded inline — frontend NÃO está conectado à API real
- ❌ Correlação incident↔change: seed data JSON estático, NÃO dinâmica
- ❌ Runbooks: 3 hardcoded no código, não configuráveis
- ❌ `CreateMitigationWorkflow` existe mas NÃO persiste registos de mitigação
- ⚠️ `EfIncidentStore` (678 linhas) é a implementação registada no backend com `IncidentDbContext` e migração — persistência backend existe mas frontend e correlation engine estão em falta

**Gaps críticos a resolver:**
1. Conectar `IncidentsPage.tsx` à API real de incidents
2. Implementar engine de correlação dinâmica (incident↔change via eventos reais)
3. Substituir runbooks hardcoded por runbooks database-driven
4. Ligar `CreateMitigationWorkflow` à persistência real

**Evidência:** `src/frontend/src/features/operations/`, `docs/audit-forensic-2026-03/final-project-state-assessment.md`

---

### Fluxo 4 — AI Assistant útil
**Estado: 50% funcional**

- ✅ Infraestrutura AI Governance funcional: modelos, políticas, budgets (EF Core real)
- ✅ Model registry, access policies, audit trail
- ✅ AI tool execution: 3 ferramentas reais (`list_services`, `get_service_health`, `list_recent_changes`)
- ⚠️ `SendAssistantMessage` retorna respostas hardcoded — sem LLM real integrado E2E
- ❌ `AiAssistantPage` usa `mockConversations` hardcoded (frontend 100% mock, não conectado)
- ❌ ExternalAI: 8 feature stubs com handlers TODO
- ⚠️ Grounding context assemblado mas não validado E2E

**Evidência:** `src/modules/aiknowledge/`, `docs/audit-forensic-2026-03/backend-state-report.md §AI`

---

## Testes & Qualidade

| Tipo | Quantidade | Estado |
|---|---|---|
| Testes unitários backend (.NET) | ~1.447 | Passando |
| Testes unitários frontend (Vitest) | ~264 | Passando |
| **Testes E2E (Playwright)** | **8 specs confirmados** | Cobertura parcial — `incidents.spec.ts` usa mock fixtures |
| Testes E2E real-environment | 5 arquivos (`e2e-real/`) | Configuração separada, não são specs Playwright CI padrão |
| Testes de carga (k6) | 5 cenários | Thresholds não documentados |

> **⚠️ Correção de Auditoria (Março 2026):** Versões anteriores deste documento afirmavam "13 novos testes E2E". Apenas 8 Playwright specs existem confirmados. Os 5 testes real-environment (`e2e-real/`) são uma configuração separada e não integram o CI padrão.

**Gap crítico:** Testes E2E não bloqueiam PRs. `incidents.spec.ts` valida mock data, não correlação dinâmica real.

**Evidência:** `docs/audit-forensic-2026-03/tests-quality-pipelines-report.md`

---

## Prioridades de Desenvolvimento

### Prioridade Máxima — Fecha fluxos core

1. **Fluxo 3 — Incident Correlation:**
   - Implementar engine de correlação dinâmica incident↔change (event-based)
   - Conectar `IncidentsPage.tsx` à API real de incidents
   - Substituir runbooks hardcoded por runbooks database-driven
   - Ligar `CreateMitigationWorkflow` à persistência real

2. **Fluxo 4 — AI Assistant:**
   - Conectar `AiAssistantPage` à API real de conversações
   - Integrar LLM via `IExternalAIRoutingPort` (Ollama/OpenAI)
   - Completar 8 handlers ExternalAI com stubs `TODO`

3. **Cross-module interfaces:**
   - Implementar as 8 interfaces cross-module prioritárias (`IContractsModule`, `IChangeIntelligenceModule`, etc.)
   - Habilitar fluxo de dados módulo-a-módulo para Governance, AI e Operational Intelligence

4. **Outbox processing:**
   - Ativar processador de outbox para os 23 DbContexts restantes

### Prioridade Alta — Produto honesto e completo

5. Persistência real para Governance (migrar de mock para real)
6. Gerar migrações EF para `KnowledgeDbContext`, `RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext`
7. Conectar FinOps a dados reais (`CostIntelligenceDbContext` existe)
8. Tornar testes E2E obrigatórios como gate de merge para main

### Prioridade Média — Qualidade e confiança

9. Eliminar 516 warnings CS8632 nullable
10. Padronizar loading, error e empty states no frontend
11. Completar Knowledge Hub (migrations `KnowledgeDbContext`)
12. Completar Product Analytics (pipeline de event tracking real)

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
