# E7 — Notifications Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E7 para o módulo Notifications.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em Notification + NotificationDelivery | Domain + Persistence | ✅ Concluído |
| 7 check constraints (Status, Category, Severity, Channel) | Persistence | ✅ Concluído |
| FK Delivery→Notification (cascade) | Persistence | ✅ Concluído |
| ntf_ prefix em todas as tabelas | Persistence | ✅ Já existente |
| Permissões notifications:* registadas em RolePermissionCatalog | Security | ✅ Concluído |
| 4 permissões × 7 roles (23 entradas) | Security | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 412/412 Notifications + 290/290 Identity | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Gerar baseline migration (InitialCreate) | Persistência | Requer prefix decision final | Wave 3 | 1 sprint |
| DbUpdateConcurrencyException handling in handlers → 409 | Backend (NTF-01) | Handler updates | E7+ | 2h |
| Domain events for delivery completion/failure | Backend (NTF-02) | New event types | E7+ | 2h |
| Notification archival scheduled job | Backend (NTF-03) | Background worker | E7+ | 4h |
| Notification expiration scheduled job | Backend (NTF-04) | Background worker | E7+ | 4h |
| RowVersion on NotificationPreference | Persistence (NTF-05) | Lower priority | E7+ | 1h |
| Filtered indexes WHERE Status = 'Pending' | Persistence (NTF-06) | Config update | E7+ | 1h |
| Admin endpoints (manage templates, channels) | Backend (NTF-07) | Product decision | E7+ | 8h |
| Real SMTP integration (currently stub) | Infrastructure (NTF-08) | SMTP config | E7+ | 4h |
| Real Teams webhook integration | Infrastructure (NTF-09) | Teams config | E7+ | 4h |
| i18n completeness (pt-BR, es) for templates | Frontend (NTF-10) | Translation | E7+ | 2h |
| NotificationConfigurationPage full implementation | Frontend (NTF-11) | Admin endpoints | E7+ | 4h |
| Notification bell real-time (WebSocket/SSE) | Frontend (NTF-12) | Infrastructure | Future | 8h |
| ClickHouse analytics for notification metrics | Analytics | Wave 5 | Future | 8h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **E8+** — Próximos módulos da trilha E
2. **Wave 3** — Baseline generation (ChangeGov+Notifications+OpIntel)
3. **Próximas releases** do produto

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E7 | Após E7 | Target |
|----------|-------------|---------|--------|
| Backend | 70% | 73% | 90% |
| Frontend | 65% | 67% | 85% |
| Persistência | 55% | 80% | 100% |
| Segurança | 30% | 75% | 90% |
| Documentação | 25% | 60% | 85% |
| Domínio | 75% | 82% | 95% |
| Delivery/Retry/Status | 65% | 72% | 90% |
| **Global** | **55%** | **73%** | **91%** |

A maturidade global subiu 18 pontos percentuais (55% → 73%), com os maiores ganhos
na segurança (30% → 75%) pela resolução do bloqueio P0 das permissões não registadas,
e persistência (55% → 80%) pelos check constraints e concorrência otimista.

---

## Decisões Tomadas Durante E7

1. **ntf_ prefix já correcto**: O módulo já utilizava ntf_ em todas as 3 tabelas,
   não necessitando de renomeação. Consistente com o padrão dos outros módulos E1-E6.

2. **xmin via IsRowVersion()**: Aplicado em Notification e NotificationDelivery
   (as 2 entidades com ciclo de vida mutável). NotificationPreference é apenas
   toggle (enable/disable), ficando para E7+ por ser baixa prioridade.

3. **FK Delivery→Notification com CASCADE**: A entrega está fortemente acoplada
   à notificação. CASCADE garante limpeza automática se a notificação for eliminada.

4. **Permissões em TODOS os 7 roles**: Decisão de dar inbox:read a todos os roles
   (incluindo Viewer/Auditor/SecurityReview) porque notificações são transversais.
   Write-only para roles que interagem activamente (PlatformAdmin, TechLead,
   Developer, ApprovalOnly).

5. **Check constraints em string-stored enums**: Consistente com o padrão usado
   em WorkflowInstance (E6) e PromotionRequest (E6). Garante integridade mesmo
   sem baseline migration.
