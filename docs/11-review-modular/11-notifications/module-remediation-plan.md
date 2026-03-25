# Notifications — Plano Final de Remediação

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## Resumo executivo

| Métrica | Valor |
|---------|-------|
| **Maturidade actual** | ~65% |
| **Maturidade alvo** | 90%+ |
| **Total de itens de remediação** | 48 |
| **Esforço total estimado** | ~139h (~17.4 dias) |
| **Quick wins (< 4h cada)** | 12 itens |
| **Correcções funcionais obrigatórias** | 16 itens |
| **Ajustes estruturais** | 12 itens |
| **Pré-condições migrations** | 5 itens |
| **Critérios de aceite** | 8 critérios |

---

## A. Quick Wins (< 4h cada)

| # | Item | Esforço | Prioridade | Fonte |
|---|------|---------|-----------|-------|
| QW-01 | 🔴 **Registar `notifications:inbox:read/write`, `notifications:preferences:read/write` no RolePermissionCatalog** | 2h | P0 | S-01 |
| QW-02 | 🔴 **Registar permissões na PermissionConfiguration.cs** | 1h | P0 | S-02 |
| QW-03 | 🟠 Adicionar sidebar menu item para `/notifications` | 1h | P1 | F-01 |
| QW-04 | 🟠 Adicionar filtro por EnvironmentId em ListNotifications | 2h | P2 | B-07 |
| QW-05 | 🟠 Adicionar filtro por dateRange em ListNotifications | 2h | P2 | B-08 |
| QW-06 | 🟠 Adicionar filtro por sourceModule em ListNotifications | 1h | P2 | B-09 |
| QW-07 | 🟡 Adicionar rate limiting nos endpoints de write | 2h | P2 | S-04 |
| QW-08 | 🟡 Adicionar audit trail para mark-all-read | 2h | P2 | S-05 |
| QW-09 | 🟡 Validar que ErrorMessage não expõe dados sensíveis | 2h | P2 | S-06 |
| QW-10 | 🟡 Criar README.md no módulo backend | 4h | P0 | DOC-B01 |
| QW-11 | 🟡 Documentar pipeline de delivery | 3h | P0 | DOC-B02 |
| QW-12 | 🟡 Adicionar XML docs ao NotificationOrchestrator | 2h | P1 | DOC-B05 |

**Subtotal Quick Wins:** 24h

---

## B. Correcções Funcionais Obrigatórias

| # | Item | Esforço | Prioridade | Fonte |
|---|------|---------|-----------|-------|
| CF-01 | 🟠 Criar endpoint GET `/notifications/{id}` (detalhe) | 3h | P1 | B-02 |
| CF-02 | 🟠 Criar endpoint POST `/notifications/{id}/acknowledge` | 2h | P1 | B-03 |
| CF-03 | 🟠 Criar endpoint POST `/notifications/{id}/archive` | 2h | P1 | B-04 |
| CF-04 | 🟠 Criar endpoint GET `/notifications/deliveries/{notificationId}` | 3h | P1 | B-05 |
| CF-05 | 🟠 Criar endpoint POST `/notifications/deliveries/{id}/retry` | 4h | P1 | B-06 |
| CF-06 | 🟠 Adicionar botões Acknowledge/Archive no frontend | 3h | P1 | F-02 |
| CF-07 | 🟠 Criar página de delivery status/history no frontend | 8h | P1 | F-03 |
| CF-08 | 🟡 Criar endpoint POST `/notifications/{id}/dismiss` | 2h | P2 | B-10 |
| CF-09 | 🟡 Criar endpoint POST `/notifications/{id}/snooze` | 3h | P2 | B-11 |
| CF-10 | 🟡 Adicionar FluentValidation a todos os handlers | 4h | P2 | B-12 |
| CF-11 | 🟡 Adicionar filtro dateRange no frontend | 2h | P2 | F-04 |
| CF-12 | 🟡 Adicionar filtro sourceModule no frontend | 1h | P2 | F-05 |
| CF-13 | 🟡 Adicionar filtro EnvironmentId no frontend | 2h | P2 | F-06 |
| CF-14 | 🟡 Adicionar botão Snooze no frontend | 3h | P2 | F-07 |
| CF-15 | 🟡 Implementar toast notifications para mutations | 2h | P2 | F-09 |
| CF-16 | 🟡 Adicionar retry button na delivery history | 2h | P2 | F-10 |

**Subtotal Correcções Funcionais:** 46h

---

## C. Ajustes Estruturais

| # | Item | Esforço | Prioridade | Fonte |
|---|------|---------|-----------|-------|
| AE-01 | 🟠 **Implementar background retry scheduler** (HostedService/Hangfire) | 8h | P2 | B-13 |
| AE-02 | 🟠 **Implementar background escalation scheduler** | 6h | P2 | B-14 |
| AE-03 | 🟡 Implementar background digest scheduler | 6h | P3 | B-15 |
| AE-04 | 🟡 Implementar cleanup de notificações expiradas | 4h | P3 | B-16 |
| AE-05 | 🟡 Completar NotificationConfigurationPage com endpoints reais | 8h | P3 | F-11 |
| AE-06 | 🟡 Validar i18n em todas as páginas/componentes | 4h | P2 | F-08 |
| AE-07 | 🟡 Avaliar filtro por EnvironmentAccess | 4h | P2 | S-03 |
| AE-08 | 🟡 Adicionar permissão `notifications:admin:write` para config | 3h | P3 | S-07 |
| AE-09 | 🟡 Documentar 13 templates com exemplos | 3h | P1 | DOC-B03 |
| AE-10 | 🟡 Documentar 8 event handlers com mapeamento | 2h | P1 | DOC-B04 |
| AE-11 | 🟡 Consolidar ficheiros NOTIFICATIONS-* fragmentados | 4h | P2 | DOC-B08 |
| AE-12 | 🟡 Adicionar XML docs a todas as entidades | 2h | P2 | DOC-B09 |

**Subtotal Ajustes Estruturais:** 54h

---

## D. Pré-condições para Recriar Migrations

| # | Pré-condição | Estado | Acção necessária |
|---|-------------|--------|-----------------|
| PM-01 | Prefixo ntf_ aplicado em todas as configs | ✅ Já correcto | Nenhuma |
| PM-02 | Phase 6 fields (CorrelationKey, GroupId, etc.) mapeados no EF Config | ⚠️ Verificar | Auditar NotificationConfiguration.cs e adicionar mapeamentos em falta |
| PM-03 | RowVersion (UseXminAsConcurrencyToken) adicionado | ❌ Ausente | Adicionar a todas as configs |
| PM-04 | CHECK constraints definidos (Status, Channel enums) | ❌ Ausente | Definir na migration |
| PM-05 | Filtered index para notificações expiradas | ❌ Ausente | Adicionar na migration |

**Dependência:** As pré-condições PM-02 a PM-05 devem ser resolvidas antes de criar a primeira migration.

---

## E. Critérios de Aceite do Módulo

O módulo Notifications será considerado **completo para produção** quando:

| # | Critério | Estado actual |
|---|---------|--------------|
| CA-01 | Todas as 7+ permissões registadas no RolePermissionCatalog | 🔴 Falha |
| CA-02 | Migration inicial criada e testada | 🔴 Falha |
| CA-03 | Fluxo E2E funcional: Evento → Notificação → Email/Teams → Status Updated | ⚠️ Parcial |
| CA-04 | Retry automático implementado (background scheduler) | 🔴 Falha |
| CA-05 | Endpoints de acknowledge, archive e delivery history existem | 🔴 Falha |
| CA-06 | Frontend com sidebar menu item e delivery dashboard | 🔴 Falha |
| CA-07 | Documentação consolidada (README, pipeline docs, template reference) | 🔴 Falha |
| CA-08 | i18n validado em todas as 4 locales | ⚠️ Parcial |

---

## F. Plano de execução por sprint

### Sprint 1 — Quick Wins e Blocker Fix (~24h)
- QW-01 a QW-12
- **Objectivo:** Desbloquear acesso a notificações para todos os utilizadores

### Sprint 2 — Correcções Funcionais Core (~25h)
- CF-01 a CF-07
- **Objectivo:** Endpoints completos + delivery dashboard frontend

### Sprint 3 — Correcções Funcionais Complementares (~21h)
- CF-08 a CF-16
- **Objectivo:** Funcionalidades avançadas (dismiss, snooze, validação)

### Sprint 4 — Estruturais e Migrations (~54h)
- AE-01 a AE-12
- PM-02 a PM-05
- **Objectivo:** Background schedulers, documentation, primeira migration

---

## G. Riscos

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|-------|-------------|---------|----------|
| R-01 | Permissões não registadas impede UAT | 🔴 Alta | 🔴 Blocker | Executar QW-01 e QW-02 primeiro |
| R-02 | Background scheduler introduz complexidade operacional | 🟠 Média | 🟠 Médio | Usar HostedService simples antes de Hangfire |
| R-03 | Templates in-code dificultam customização por tenant | 🟡 Baixa | 🟡 Baixo | Planeado para Phase 2 |
| R-04 | 0 migrations pode causar problemas de schema | 🟠 Média | 🟠 Médio | Criar migration baseline no Sprint 4 |
