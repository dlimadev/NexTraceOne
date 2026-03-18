# NexTraceOne — Baseline Estável (Fase 9)

> **Data de congelamento:** Fase 9 — Correções pós-aceite e baseline estável
> **Documento de escopo:** `docs/acceptance/NexTraceOne_Escopo_Homologavel.md`
> **Relatório de aceite:** `docs/acceptance/NexTraceOne_Relatorio_Teste_Aceitacao.md`
> **Plano operacional:** `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`

---

## 1. Declaração de baseline

Esta versão do NexTraceOne é declarada como **baseline estável** após conclusão das Fases 1–9 do plano operacional de finalização.

A baseline foi validada por:

- Execução completa do teste de aceitação (Fase 8)
- Correções pós-aceite (Fase 9 — nenhum bug P0/P1 encontrado)
- Regressão mínima executada com sucesso (backend + frontend)
- Documentação de escopo atualizada e congelada

---

## 2. Resultado do aceite

| Métrica | Valor |
|---------|-------|
| Bugs P0 (bloqueadores) | **0** |
| Bugs P1 (funcionalidade crítica) | **0** |
| Observações P2 (UX/completude) | **4** |
| Módulos aprovados | **8 de 12** (aprovação limpa) |
| Módulos aprovados com ressalvas | **4 de 12** (P2 — não impeditivos) |
| Módulos reprovados | **0** |
| Resultado global | **APROVADO COM RESSALVAS** |

---

## 3. Módulos homologados na baseline

| Módulo | Estado | Observação |
|--------|--------|------------|
| Login / Auth | ✅ Aprovado | — |
| Shell / Navegação | ✅ Aprovado | — |
| Dashboard | ✅ Aprovado | — |
| Service Catalog | ✅ Aprovado | — |
| Source of Truth | ✅ Aprovado | — |
| Contracts | ✅ Aprovado com ressalvas | P2-001: mock enrichment campos secundários |
| Change Governance | ✅ Aprovado | — |
| Incidents | ✅ Aprovado com ressalvas | P2-002: RunbooksPage standalone é empty state |
| Audit | ✅ Aprovado | — |
| Identity Admin | ✅ Aprovado | — |
| AI Assistant | ✅ Aprovado com ressalvas | P2-003: mock conversations para demonstração |
| Platform Operations | ✅ Aprovado com ressalvas | P2-004: dados mock de demonstração |

---

## 4. Regressão mínima da Fase 9

| Verificação | Resultado |
|------------|-----------|
| Backend `run_build` (.NET 10) | ✅ Compilação bem-sucedida |
| Frontend `npx tsc --noEmit` | ✅ Zero erros TypeScript |
| Migrations | ✅ 11 InitialCreate migrations verificadas (Fase 8) |
| Seed data | ✅ 6 SQL files, 4 utilizadores, 2 tenants (Fase 8) |
| Rotas homologáveis | ✅ 38 rotas verificadas em `App.tsx` (Fase 8) |
| Rotas preview | ✅ 40+ rotas com `<PreviewGate>` (Fase 8) |
| APIs reais | ✅ Todos os módulos core usam endpoints reais (Fase 8) |

---

## 5. Bugs corrigidos na Fase 9

Nenhum bug P0 ou P1 foi encontrado na Fase 8, portanto nenhuma correção foi necessária na Fase 9.

### Histórico de correções (Fases 1–6)

| Fase | Correção | Ficheiro |
|------|----------|---------|
| 1 | JWT audience mismatch | `JwtTokenService.cs` |
| 1 | Middleware order (auth before routing) | `Program.cs` |
| 1 | Missing roleId na seleção de papel | `UsersPage.tsx` |
| 1 | minLength mismatch no break-glass | `BreakGlassPage.tsx` |
| 2 | Race condition em ProtectedRoute (isLoadingUser) | `AuthContext.tsx`, `ProtectedRoute.tsx`, `AppShell.tsx` |
| 3 | 6 rotas sem ProtectedRoute wrapper | `App.tsx` |
| 4 | 2 data bugs no detalhe de incidente | `IncidentDetailPage.tsx` |
| 4 | ProtectedRoute em /operations/runbooks | `App.tsx` |
| 4 | Link Source of Truth em ServiceDetailPage | `ServiceDetailPage.tsx` |
| 5 | 40+ rotas marcadas como Preview | `App.tsx`, `AppSidebar.tsx`, `AppSidebarItem.tsx` |
| 5 | PreviewBanner + PreviewGate criados | `PreviewBanner.tsx`, `PreviewGate.tsx` |
| 5 | i18n keys para Preview | `en.json`, `pt-BR.json`, `pt-PT.json` |
| 6 | Loading/Error states padronizados | `PageLoadingState.tsx`, `PageErrorState.tsx`, 12+ páginas |

---

## 6. Backlog pós-baseline (Fase 10)

### Prioridade Média

| Item | Origem | Trilha Fase 10 |
|------|--------|----------------|
| Remover mock enrichment do catálogo de contratos (domain, owner, compliance, technology) | P2-001 | Trilha 1 — Contracts avançado |
| Integrar AiAssistantPage com backend real de conversas/mensagens | P2-003 | Trilha 5 — AI Hub real |

### Prioridade Baixa

| Item | Origem | Trilha Fase 10 |
|------|--------|----------------|
| Implementar listagem real de runbooks standalone | P2-002 | Evolução Operations |
| Integrar PlatformOperationsPage com health checks e métricas reais | P2-004 | Evolução Platform |

### Gaps de produto identificados

| Gap | Descrição | Prioridade | Trilha Fase 10 |
|-----|-----------|-----------|----------------|
| GAP-001 | Backend não fornece campos domain/owner/technology para contratos | Média | Trilha 1 |
| GAP-002 | Runbooks standalone sem listagem real | Baixa | Operations |
| GAP-003 | AI Assistant chat não integrado com backend real | Média | Trilha 5 |
| GAP-004 | Platform Operations não integrado com health checks reais | Baixa | Platform |

---

## 7. Infraestrutura da baseline

### Backend

- **Runtime:** .NET 10
- **Arquitetura:** Modular Monolith
- **Base de dados:** PostgreSQL multi-database
- **Módulos registados:** 16+ no `Program.cs`
- **Migrations:** 11 InitialCreate
- **Seed:** 6 ficheiros SQL idempotentes
- **Health checks:** `/health`, `/ready`, `/live`

### Frontend

- **Runtime:** React 18 + Vite + TypeScript
- **Internacionalização:** react-i18next (en, pt-BR, pt-PT)
- **Data fetching:** @tanstack/react-query
- **Lazy loading:** todas as páginas protegidas
- **Estado compartilhado:** PageLoadingState, PageErrorState, PreviewBanner, PreviewGate

### Utilizadores de teste

| Email | Papel |
|-------|-------|
| admin@nextraceone.dev | PlatformAdmin |
| techlead@nextraceone.dev | TechLead |
| dev@nextraceone.dev | Developer |
| auditor@nextraceone.dev | Auditor |

### Tenants de teste

| Nome | Slug |
|------|------|
| NexTrace Corp | nexttrace-corp |
| Acme Fintech | acme-fintech |

---

## 8. Declaração de congelamento

A baseline acima está **congelada**. A evolução do produto deve seguir exclusivamente as trilhas definidas na **Fase 10** do plano operacional.

Qualquer alteração na baseline requer:

1. Justificativa técnica documentada
2. Verificação de regressão (build backend + frontend)
3. Atualização deste documento

---

## 9. Critérios de aceite da Fase 9

| Critério | Estado |
|---------|--------|
| Bugs P0 corrigidos | ✅ N/A — nenhum P0 encontrado |
| Bugs P1 impeditivos corrigidos | ✅ N/A — nenhum P1 encontrado |
| Regressão mínima executada | ✅ Backend + Frontend compilam sem erros |
| Documentação de escopo atualizada | ✅ Baseline estável documentada |
| Baseline funcional congelada | ✅ Declaração de congelamento emitida |
| Backlog pós-baseline organizado | ✅ 4 itens P2 mapeados para trilhas da Fase 10 |

### **Resultado: Critérios de aceite da Fase 9 ATINGIDOS.**

### **Estado da baseline: ESTÁVEL — pronta para Fase 10 (Evolução do produto).**
