# Recomendação de Priorização para Revisão Detalhada — NexTraceOne

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 6  
> **Fonte de verdade:** Resultados da auditoria estrutural

---

## Critérios de Priorização

A ordem recomendada considera:

1. **Criticidade para o produto** — pilares centrais primeiro
2. **Impacto transversal** — módulos que afetam outros módulos
3. **Dependências entre módulos** — fundações antes de funcionalidades
4. **Risco técnico** — áreas com maior divergência docs/código
5. **Exposição no menu** — funcionalidades visíveis aos utilizadores
6. **Dívida técnica acumulada** — rotas quebradas, páginas órfãs

---

## Ordem Recomendada de Revisão

### 🔴 Prioridade 1 — Fundação e Rotas Quebradas (Semana 1)

#### 1.1 Contracts — Corrigir Rotas Quebradas

| Item | Detalhe |
|------|---------|
| **Problema** | 3 itens de menu sem rota real; 4 páginas órfãs |
| **Impacto** | Utilizadores encontram menu não-funcional em área core do produto |
| **Ação** | Decidir: adicionar rotas no App.tsx ou remover itens do menu |
| **Ficheiros** | `App.tsx`, `AppSidebar.tsx`, `ContractGovernancePage.tsx`, `SpectralRulesetManagerPage.tsx`, `CanonicalEntityCatalogPage.tsx`, `ContractPortalPage.tsx` |
| **Esforço** | Baixo (2-4 horas) |
| **Risco** | Alto se não corrigido — credibilidade do produto |

#### 1.2 Identity & Access — Validação Fundacional

| Item | Detalhe |
|------|---------|
| **Problema** | Módulo fundacional — todos os outros dependem dele |
| **Impacto** | Auth, permissões, sessões, multi-tenancy |
| **Ação** | Validar que auth flow, permissions e ProtectedRoute funcionam end-to-end |
| **Módulo Backend** | `src/modules/identityaccess/` |
| **Módulo Frontend** | `src/frontend/src/features/identity-access/` (15 páginas) |
| **Esforço** | Médio (1-2 dias) |
| **Risco** | Fundacional — falha aqui impacta tudo |

---

### 🟠 Prioridade 2 — Pilares Core do Produto (Semana 2-3)

#### 2.1 Catalog — Módulo Mais Maduro

| Item | Detalhe |
|------|---------|
| **Por quê primeiro** | Maior maturidade (430+ testes), pilar Service Catalog e Source of Truth |
| **Ação** | Validar fluxos reais, consolidar páginas duplicadas (3 páginas órfãs em catalog/pages/), verificar integração com backend |
| **Módulo Backend** | `src/modules/catalog/` (3 subdomínios, 3 DbContexts) |
| **Módulo Frontend** | `src/frontend/src/features/catalog/` (12 páginas) |
| **Documentação** | Boa — atualizar MODULES-AND-PAGES.md |
| **Esforço** | Médio (2-3 dias) |

#### 2.2 Change Governance — Pilar Change Confidence

| Item | Detalhe |
|------|---------|
| **Por quê** | Pilar central — Change Confidence é narrativa central do produto |
| **Ação** | Validar fluxos de releases, workflows, promoções; verificar blast radius e correlation |
| **Módulo Backend** | `src/modules/changegovernance/` (4 subdomínios, 4 DbContexts) |
| **Módulo Frontend** | `src/frontend/src/features/change-governance/` (6 páginas) |
| **Documentação** | Boa |
| **Esforço** | Médio (2-3 dias) |

#### 2.3 Operational Intelligence — Incidents e Reliability

| Item | Detalhe |
|------|---------|
| **Por quê** | Incidentes e fiabilidade são operacionais e visíveis |
| **Ação** | Validar fluxos de incidentes, runbooks, automation; verificar persistência e API real |
| **Módulo Backend** | `src/modules/operationalintelligence/` (5 subdomínios, 5 DbContexts) |
| **Módulo Frontend** | `src/frontend/src/features/operations/` (10 páginas) |
| **Documentação** | Parcial — melhorar |
| **Esforço** | Alto (3-4 dias) — módulo grande |

---

### 🟡 Prioridade 3 — Governança e Organização (Semana 3-4)

#### 3.1 Governance — Módulo Mais Amplo

| Item | Detalhe |
|------|---------|
| **Por quê** | Módulo excessivamente amplo com 20+ endpoints e 22+ páginas frontend |
| **Ação** | Avaliar se deve ser dividido; validar Executive, Compliance, Risk, FinOps, Policies, Packs, Teams, Domains |
| **Módulo Backend** | `src/modules/governance/` — contém Integrations, Analytics, FinOps, Platform |
| **Módulo Frontend** | `src/frontend/src/features/governance/` (22 páginas) |
| **Risco** | Responsabilidades misturadas no backend; páginas sem documentação |
| **Esforço** | Alto (4-5 dias) — módulo muito grande |

#### 3.2 Configuration — 345 Definições sem Doc Unificada

| Item | Detalhe |
|------|---------|
| **Por quê** | Módulo transversal que configura todo o sistema |
| **Ação** | Criar documentação unificada; validar 8 fases de seed; confirmar todas as definições |
| **Módulo Backend** | `src/modules/configuration/` (251 testes) |
| **Módulo Frontend** | `src/frontend/src/features/configuration/` (2 páginas + 6 config pages distribuídas) |
| **Documentação** | Fragmentada em 35 ficheiros — precisa consolidação |
| **Esforço** | Médio (2-3 dias) |

---

### 🟢 Prioridade 4 — AI e Funcionalidades Avançadas (Semana 4-5)

#### 4.1 AI Knowledge — Calibrar Expectativas

| Item | Detalhe |
|------|---------|
| **Por quê** | 6+ documentos prometem sistema completo; backend está a 20-25% |
| **Ação** | Calibrar documentação com realidade; definir o que é MVP de AI; validar AiAssistant, ModelRegistry, Policies |
| **Módulo Backend** | `src/modules/aiknowledge/` (4 subdomínios, 3 DbContexts) |
| **Módulo Frontend** | `src/frontend/src/features/ai-hub/` (11 páginas) |
| **Risco** | Documentação muito otimista; 9 itens no menu para módulo parcial |
| **Esforço** | Alto (3-4 dias) |

#### 4.2 Audit & Compliance

| Item | Detalhe |
|------|---------|
| **Por quê** | Funcionalidade de auditoria com hash chain — essencial para compliance |
| **Ação** | Validar AuditPage, hash chain integrity, compliance policies |
| **Módulo Backend** | `src/modules/auditcompliance/` |
| **Módulo Frontend** | `src/frontend/src/features/audit-compliance/` (1 página) |
| **Esforço** | Baixo (1 dia) |

---

### 🔵 Prioridade 5 — Suporte e Complementos (Semana 5-6)

#### 5.1 Notifications

| Item | Detalhe |
|------|---------|
| **Ação** | Validar NotificationCenter, Preferences, delivery; criar doc unificada |
| **Módulo** | Backend: `notifications/`, Frontend: `notifications/` (3 páginas) |
| **Esforço** | Baixo (1 dia) |

#### 5.2 Integrations

| Item | Detalhe |
|------|---------|
| **Ação** | Validar IntegrationHub, conectores, execuções; criar documentação |
| **Frontend** | `integrations/` (4 páginas) |
| **Backend** | Endpoints no módulo Governance |
| **Esforço** | Baixo-Médio (1-2 dias) |

#### 5.3 Product Analytics

| Item | Detalhe |
|------|---------|
| **Ação** | Avaliar se deve ser mantido; criar documentação; validar integração com dados reais |
| **Frontend** | `product-analytics/` (5 páginas) |
| **Backend** | Endpoints analytics no módulo Governance |
| **Esforço** | Baixo (1 dia) |

---

### ⚪ Prioridade 6 — Documentação e Consolidação (Semana 6-7)

#### 6.1 Consolidação Documental

| Ação | Documentos Envolvidos |
|------|----------------------|
| Consolidar DESIGN*.md + GUIDELINE.md | 3 → 1 documento |
| Consolidar SECURITY*.md | 2 → 1 documento |
| Consolidar PERSONA*.md | 2 → 1 documento |
| Resolver ADRs duplicados | 3 duplicatas |
| Eliminar ANALISE-CRITICA duplicada | reviews/ vs raiz |
| Arquivar phase reports (0-8) | ~40 ficheiros → archive/ |

#### 6.2 Atualização de Docs Críticos

| Documento | Ação |
|-----------|------|
| ARCHITECTURE-OVERVIEW.md | Expandir de 19 para ~200+ linhas |
| FRONTEND-ARCHITECTURE.md | Expandir de 16 para ~150+ linhas |
| DATA-ARCHITECTURE.md | Documentar 16+ DbContexts |
| MODULES-AND-PAGES.md | Atualizar para 105 páginas |
| I18N-STRATEGY.md | Documentar 4 locales e ~4300+ chaves |

#### 6.3 Criação de Docs Ausentes

| Documento a Criar | Módulo |
|-------------------|--------|
| Configuration Reference | configuration |
| Product Analytics Overview | product-analytics |
| Integrations Architecture | integrations |
| Building Blocks Reference | building-blocks |
| Platform Hosts Guide | platform |

---

## Diagrama de Dependências para Revisão

```
Identity & Access  ←── TUDO depende disto
       │
       ├── Catalog  ←── Source of Truth, Service Catalog
       │      │
       │      ├── Contracts  ←── Contract Governance, Studio
       │      │
       │      └── Change Governance  ←── Releases, Workflows, Promotions
       │                │
       │                └── Operational Intelligence  ←── Incidents, Reliability
       │
       ├── Configuration  ←── Todas as features usam configuração
       │
       ├── Governance  ←── Executive, Compliance, FinOps, Teams, Domains
       │      │
       │      ├── Integrations  (endpoints dentro de Governance)
       │      └── Product Analytics  (endpoints dentro de Governance)
       │
       ├── AI Knowledge  ←── Assistant, Agents, Governance
       │
       ├── Notifications  ←── Centro de notificações
       │
       └── Audit & Compliance  ←── Hash chain, compliance
```

---

## Estimativa de Esforço Total

| Prioridade | Semana(s) | Esforço Estimado |
|-----------|-----------|-----------------|
| P1 — Fundação e Rotas Quebradas | Semana 1 | 3-6 dias |
| P2 — Pilares Core | Semana 2-3 | 7-10 dias |
| P3 — Governança e Config | Semana 3-4 | 6-8 dias |
| P4 — AI e Avançados | Semana 4-5 | 4-5 dias |
| P5 — Suporte e Complementos | Semana 5-6 | 3-4 dias |
| P6 — Documentação | Semana 6-7 | 5-7 dias |
| **Total** | **~7 semanas** | **~28-40 dias** |

---

## Critérios de Sucesso por Fase

### P1 — Fundação
- [ ] Nenhum item de menu aponta para rota inexistente
- [ ] Auth flow validado end-to-end
- [ ] Permissões funcionam conforme esperado

### P2 — Pilares Core
- [ ] Catalog: páginas órfãs resolvidas, fluxos validados
- [ ] Changes: releases, workflows e promoções testados
- [ ] Operations: incidentes e runbooks funcionais

### P3 — Governança
- [ ] Governance: responsabilidades clarificadas
- [ ] Configuration: documentação unificada criada

### P4 — AI
- [ ] AI: documentação calibrada com realidade
- [ ] AI: decidido o que é MVP vs roadmap

### P5 — Complementos
- [ ] Todos os módulos com documentação mínima
- [ ] Integrations e Analytics validados

### P6 — Documentação
- [ ] Zero documentos duplicados
- [ ] Docs core atualizados com realidade do código
- [ ] Phase reports arquivados
