# CLASSIFICAÇÃO ATUALIZADA DE GAPS — NexTraceOne

> **Data:** 2026-03-23
> **Última atualização:** 2026-03-23 (Wave 5 — Final Consolidation)
> **Referência:** Onda 0 — Realinhamento de Baseline
> **Base:** NEXTRACEONE-CURRENT-STATE-AND-100-PERCENT-GAP-REPORT.md (24 gaps originais)
> **Contexto:** Classificação final pós-Wave 5 — todos os gaps têm decisão formal explícita

---

## LEGENDA

### Decisão

| Código | Significado |
|--------|-----------|
| **Corrigir** | O gap permanece válido e deve ser resolvido nesta jornada |
| **Substituir** | O gap original não reflete a realidade; foi substituído por um gap correto |
| **Descartar** | O gap já não faz sentido no contexto atual |
| **Adiar** | O gap é válido mas não bloqueia produção nem credibilidade enterprise |

### Camada

| Código | Significado |
|--------|-----------|
| **PB** | Production Blocker — impede ir para produção |
| **ECB** | Enterprise Credibility Blocker — impede chamar o produto de enterprise-ready |
| **HOM** | Hardening / Operational Maturity — melhoria importante mas não bloqueadora |
| **PGLI** | Post-Go-Live Improvement — pode ser feito após go-live |

---

## TABELA COMPLETA DE GAPS

| Gap | Título | Módulo | Tipo | Severidade Original | Decisão Onda 0 | Camada | Prioridade Final | Bloqueia Prod? | Bloqueia Enterprise? | Onda Atribuída |
|-----|--------|--------|------|---------------------|-----------------|--------|-------------------|----------------|---------------------|----------------|
| GAP-001 | Secrets de produção não configurados | Ops/Infra | Ops | Critical | **Corrigir** | **PB** | P0 | **SIM** | SIM | Onda 1 |
| GAP-002 | Backup automatizado não configurado | Ops/Infra | Ops | Critical | **Corrigir** | **PB** | P0 | **SIM** | SIM | Onda 1 |
| GAP-003 | GetEfficiencyIndicators retorna demo | Governance | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-004 | GetWasteSignals retorna demo | Governance | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-005 | GetFrictionIndicators retorna demo | Governance | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-006 | RunComplianceChecks retorna mock | Governance | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-007 | GenerateDraftFromAi usa template stub | Catalog | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-008 | DocumentRetrievalService é stub | AIKnowledge | Functional | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-009 | TelemetryRetrievalService é stub | AIKnowledge | Functional | Medium | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-010 | EncryptionInterceptor ausente | Security | Security | High | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 2 |
| GAP-011 | GetExecutiveDrillDown IsSimulated inconsistente | Governance | Functional | Medium | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-012 | ~~Grafana dashboards ausentes~~ | ~~Observability~~ | ~~Ops~~ | ~~Medium~~ | **Substituir** | — | — | — | — | — |
| GAP-012-R | Superfície de visualização operacional sem Grafana precisa estar validada e documentada | Observability / Operations | Doc + Validação | Medium | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-013 | EvidencePackages preview badge | Governance | UX | Medium | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-014 | GovernancePackDetail preview badge | Governance | UX | Medium | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-015 | Rate limiting limitado a auth | Security | Security | Medium | **Corrigir** | **ECB** | P1 | NÃO | **SIM** | Onda 3 |
| GAP-016 | GetPlatformHealth subsistemas hardcoded | Governance | Functional | Low | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-017 | Load testing formal ausente | Testing | Testing | Medium | **Adiar** | **PGLI** | P3 | NÃO | NÃO | Onda 5 |
| GAP-018 | Playwright E2E frontend ausente | Testing | Testing | Medium | **Adiar** | **PGLI** | P3 | NÃO | NÃO | Onda 5 |
| GAP-019 | Refresh token E2E ausente | Testing | Testing | Medium | **Adiar** | **PGLI** | P3 | NÃO | NÃO | Onda 5 |
| GAP-020 | AssistantPanel mock generator | AIKnowledge | Functional | Low | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-021 | CORS configuration por ambiente | Security | Security | Low | **Corrigir** | **HOM** | P2 | NÃO | NÃO | Onda 4 |
| GAP-022 | Alerting não integrado a incidents | Ops | Functional | Medium | **Corrigir** | **ECB** | P2 | NÃO | **SIM** | Onda 3 |
| GAP-023 | ProductStore não implementado | Observability | Architecture | Low | **Adiar** | **PGLI** | P3 | NÃO | NÃO | Onda 5 |
| GAP-024 | ESLint warnings no frontend | Quality | Quality | Low | **Adiar** | **PGLI** | P3 | NÃO | NÃO | Onda 5 |

---

## DETALHE POR GAP

### GAP-001 — Secrets de produção não configurados

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Production Blocker |
| **Status atual** | Pendente |
| **Descrição** | A aplicação não inicia em produção sem `JWT_SECRET ≥ 32 chars` e connection strings reais |
| **O que falta** | Configurar GitHub Environment `production` com todos os secrets obrigatórios |
| **Esforço** | Pequeno |
| **Onda** | 1 |
| **Ainda válido?** | ✅ Sim — confirmado que `StartupValidation.cs` bloqueia startup sem estas configurações |

### GAP-002 — Backup automatizado não configurado

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Production Blocker |
| **Status atual** | Pendente |
| **Descrição** | Scripts `backup.sh`/`restore.sh` existem mas não há cron/scheduling configurado |
| **O que falta** | Configurar cron/scheduled backup para 4 bases com retenção de 30 dias |
| **Esforço** | Pequeno |
| **Onda** | 1 |
| **Ainda válido?** | ✅ Sim — scripts existem, scheduling ausente |

### GAP-003 — GetEfficiencyIndicators retorna demo

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Demo (`IsSimulated: true, DataSource: "demo"`) |
| **Descrição** | Handler retorna dados hardcoded em vez de consultar `ICostIntelligenceModule` |
| **O que falta** | Implementar query real via módulo de FinOps/custos |
| **Esforço** | Médio |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-004 — GetWasteSignals retorna demo

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Demo (`IsSimulated: true, DataSource: "demo"`) |
| **Descrição** | Handler retorna sinais de desperdício hardcoded |
| **O que falta** | Implementar detecção real de waste signals a partir de dados operacionais |
| **Esforço** | Médio |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-005 — GetFrictionIndicators retorna demo

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Demo (`IsSimulated: true, DataSource: "demo"`) |
| **Descrição** | Handler retorna indicadores de fricção hardcoded |
| **O que falta** | Implementar detecção real via dados operacionais |
| **Esforço** | Médio |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-006 — RunComplianceChecks retorna mock

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Stub (15 compliance checks hardcoded) |
| **Descrição** | Compliance engine retorna checks estáticos sem avaliação real |
| **O que falta** | Implementar motor de compliance real com regras configuráveis |
| **Esforço** | Grande |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-007 — GenerateDraftFromAi usa template stub

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Stub (template estático por protocolo) |
| **Descrição** | Geração de contratos via IA usa templates fixos, não IA real |
| **O que falta** | Integrar provider real de IA (OpenAI/Azure/LLM local) |
| **Esforço** | Grande |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-008 — DocumentRetrievalService é stub

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Stub (retorna `Array.Empty<DocumentSearchHit>()`) |
| **Descrição** | Serviço de retrieval para RAG não implementado |
| **O que falta** | Implementar RAG com embeddings e busca semântica |
| **Esforço** | Grande |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-009 — TelemetryRetrievalService é stub

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Stub (retorna `Array.Empty<TelemetrySearchHit>()`) |
| **Descrição** | Serviço de retrieval de telemetria para IA não implementado |
| **O que falta** | Integrar com ClickHouse/OTel para consultas de traces/logs |
| **Esforço** | Médio |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim — agora com contexto correto (ClickHouse, não Grafana/Tempo) |

### GAP-010 — EncryptionInterceptor ausente

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Documentado em docstring mas sem implementação |
| **Descrição** | `AesGcmEncryptor` existe como serviço standalone; interceptor EF Core não existe |
| **O que falta** | Implementar EF Core interceptor para encriptação AES-256-GCM de campos sensíveis |
| **Esforço** | Grande |
| **Onda** | 2 |
| **Ainda válido?** | ✅ Sim |

### GAP-011 — GetExecutiveDrillDown IsSimulated inconsistente

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Misto (consulta dados reais mas marca `IsSimulated: true`) |
| **Descrição** | Flag inconsistente na linha 115 do handler |
| **O que falta** | Corrigir flag para `IsSimulated: false` |
| **Esforço** | Pequeno |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-012 — ~~Grafana dashboards ausentes~~ → SUBSTITUÍDO

| Campo | Valor |
|-------|-------|
| **Decisão** | **Substituir** por GAP-012-R |
| **Razão** | Grafana não faz mais parte da solução. O gap era baseado numa premissa arquitetural abandonada. |
| **Status** | O gap original deixa de existir; substituído por GAP-012-R |

### GAP-012-R — Superfície de visualização operacional sem Grafana

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Parcialmente resolvido (Onda 0 documentou a superfície; falta validação completa) |
| **Descrição** | A superfície de troubleshooting e operação precisa estar claramente definida, validada e documentada sem pressupor Grafana |
| **O que falta** | (1) Validar que as 6 páginas operacionais cobrem cenários críticos; (2) Documentar acesso a dados brutos via ClickHouse; (3) Atualizar runbooks sem referência a Grafana |
| **Esforço** | Pequeno-Médio |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-013 — EvidencePackages preview badge

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Badge `<Badge variant="warning">` presente na UI |
| **O que falta** | Remover badge após completar implementação de evidence |
| **Esforço** | Pequeno |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-014 — GovernancePackDetail preview badge

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Badge `<Badge variant="warning">` presente na UI |
| **O que falta** | Remover badge após completar simulação de packs |
| **Esforço** | Pequeno |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-015 — Rate limiting limitado a auth

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | Apenas políticas `"auth"` e `"auth-sensitive"` |
| **O que falta** | Adicionar rate limiting para endpoints de dados (API, busca, relatórios) |
| **Esforço** | Médio |
| **Onda** | 3 |
| **Ainda válido?** | ✅ Sim |

### GAP-016 — GetPlatformHealth subsistemas hardcoded

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | 5 subsistemas sempre retornam `Healthy` |
| **O que falta** | Integrar com health checks reais por subsistema |
| **Esforço** | Médio |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-017 — Load testing formal

| Campo | Valor |
|-------|-------|
| **Decisão** | Adiar |
| **Camada** | Post-Go-Live Improvement |
| **Status atual** | `smoke-performance.sh` existe como smoke test |
| **O que falta** | Load tests formais com k6, Artillery ou JMeter |
| **Razão do adiamento** | Smoke test existe; load formal não bloqueia produção |
| **Onda** | 5 |
| **Ainda válido?** | ✅ Sim, mas não prioritário |

### GAP-018 — Playwright E2E frontend

| Campo | Valor |
|-------|-------|
| **Decisão** | Adiar |
| **Camada** | Post-Go-Live Improvement |
| **Status atual** | Dependência instalada, sem testes visíveis |
| **O que falta** | Implementar E2E smoke com Playwright para fluxos críticos |
| **Razão do adiamento** | Testes unitários e integração existem; E2E é melhoria incremental |
| **Onda** | 5 |
| **Ainda válido?** | ✅ Sim, mas não prioritário |

### GAP-019 — Refresh token E2E

| Campo | Valor |
|-------|-------|
| **Decisão** | Adiar |
| **Camada** | Post-Go-Live Improvement |
| **Status atual** | Refresh token funcional mas sem teste E2E |
| **O que falta** | Teste E2E específico para refresh flow |
| **Razão do adiamento** | Funcionalidade testada unitariamente |
| **Onda** | 5 |
| **Ainda válido?** | ✅ Sim, mas não prioritário |

### GAP-020 — AssistantPanel mock generator

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | 175+ linhas de resposta mock no frontend |
| **O que falta** | Remover fallback mock quando provider real estiver configurado |
| **Esforço** | Pequeno |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-021 — CORS configuration por ambiente

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Hardening / Operational Maturity |
| **Status atual** | Default localhost-only |
| **O que falta** | Documentar e configurar CORS por ambiente de deploy |
| **Esforço** | Pequeno |
| **Onda** | 4 |
| **Ainda válido?** | ✅ Sim |

### GAP-022 — Alerting não integrado a incidents

| Campo | Valor |
|-------|-------|
| **Decisão** | Corrigir |
| **Camada** | Enterprise Credibility Blocker |
| **Status atual** | AlertGateway existe mas não está wired ao IncidentDbContext |
| **O que falta** | Integrar AlertGateway com criação/escalação de incidentes |
| **Esforço** | Médio |
| **Onda** | 3 |
| **Ainda válido?** | ✅ Sim |

### GAP-023 — ProductStore não implementado

| Campo | Valor |
|-------|-------|
| **Decisão** | Adiar |
| **Camada** | Post-Go-Live Improvement |
| **Status atual** | Referenciado em docs de observabilidade mas sem código |
| **O que falta** | Avaliar necessidade e implementar se justificado |
| **Razão do adiamento** | Sem necessidade imediata comprovada; ClickHouse serve como store analítico |
| **Onda** | 5 |
| **Ainda válido?** | ✅ Sim, mas necessidade precisa ser reavaliada |

### GAP-024 — ESLint warnings no frontend

| Campo | Valor |
|-------|-------|
| **Decisão** | Adiar |
| **Camada** | Post-Go-Live Improvement |
| **Status atual** | 108 erros ESLint pré-existentes (63 unused vars, 20 setState-in-effect) |
| **O que falta** | Corrigir erros de linting progressivamente |
| **Razão do adiamento** | Não afetam funcionalidade; são technical debt de qualidade |
| **Onda** | 5 |
| **Ainda válido?** | ✅ Sim, mas não bloqueia nada |

---

## RESUMO ESTATÍSTICO

### Por decisão

| Decisão | Total |
|---------|-------|
| Corrigir | 16 (inclui GAP-012-R) |
| Substituir | 1 (GAP-012 → GAP-012-R) |
| Descartar | 0 |
| Adiar | 5 |

### Por camada

| Camada | Total | Gaps |
|--------|-------|------|
| Production Blocker | 2 | GAP-001, GAP-002 |
| Enterprise Credibility Blocker | 9 | GAP-003-008, GAP-010, GAP-015, GAP-022 |
| Hardening / Operational Maturity | 8 | GAP-009, GAP-011, GAP-012-R, GAP-013-014, GAP-016, GAP-020-021 |
| Post-Go-Live Improvement | 5 | GAP-017-019, GAP-023-024 |

### Por prioridade

| Prioridade | Total |
|-----------|-------|
| P0 (Critical) | 2 |
| P1 (High) | 7 |
| P2 (Medium) | 9 |
| P3 (Low) | 5 |

---

## ESTADO FINAL PÓS-WAVE 5 (Atualização 2026-03-23)

> Seção adicionada na Wave 5 — Final Consolidation para refletir o estado final de todos os gaps.

### Estado de cada gap

| Gap | Estado Final | Onda de Resolução |
|-----|-------------|-------------------|
| GAP-001 | ✅ Resolvido | Wave 1 |
| GAP-002 | ✅ Resolvido | Wave 1 |
| GAP-003 | ✅ Resolvido | Wave 2 |
| GAP-004 | ✅ Resolvido | Wave 2 |
| GAP-005 | ✅ Resolvido | Wave 2 |
| GAP-006 | ✅ Resolvido | Wave 2 |
| GAP-007 | ✅ Resolvido | Wave 2 |
| GAP-008 | ✅ Resolvido | Wave 2 |
| GAP-009 | ✅ Resolvido | Wave 4 |
| GAP-010 | ✅ Resolvido | Wave 2 + Wave 3 |
| GAP-011 | ✅ Resolvido | Wave 4 |
| GAP-012 | 🗑️ Substituído por GAP-012-R | Wave 0 |
| GAP-012-R | ✅ Resolvido | Wave 5 |
| GAP-013 | ✅ Resolvido (já limpo) | Wave 5 |
| GAP-014 | ✅ Resolvido (badge removido) | Wave 5 |
| GAP-015 | ✅ Resolvido | Wave 3 |
| GAP-016 | ✅ Resolvido | Wave 3 |
| GAP-017 | 📋 PGLI confirmado | Wave 5 |
| GAP-018 | 📋 PGLI confirmado | Wave 5 |
| GAP-019 | 📋 PGLI confirmado | Wave 5 |
| GAP-020 | ✅ Resolvido | Wave 4 |
| GAP-021 | ✅ Resolvido | Wave 3 |
| GAP-022 | ✅ Resolvido | Wave 3 |
| GAP-023 | 🗑️ Descartado oficialmente | Wave 5 |
| GAP-024 | 📋 PGLI confirmado | Wave 5 |

### Resumo final

| Classificação | Total |
|--------------|-------|
| ✅ Resolvido | 20 |
| 🗑️ Descartado | 2 (GAP-012, GAP-023) |
| 📋 PGLI confirmado | 4 (GAP-017, GAP-018, GAP-019, GAP-024) |
| ❌ Em aberto | 0 |

---

> **Este documento é a classificação oficial de gaps do NexTraceOne, atualizado após a Wave 5 — Final Consolidation.**
> **Todos os 24 gaps originais + 1 substituto (GAP-012-R) possuem decisão formal explícita.**
> **Ver também:** `NEXTRACEONE-WAVE-0-BASELINE-REALIGNMENT.md`, `NEXTRACEONE-UPDATED-WAVES-PLAN.md` e `WAVE-5-FINAL-CONSOLIDATION-REPORT.md`
