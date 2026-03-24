# Revisão Modular — Audit & Compliance

> **Data:** 2026-03-24  
> **Prioridade:** P4  
> **Módulo Backend:** `src/modules/auditcompliance/`  
> **Módulo Frontend:** `src/frontend/src/features/audit-compliance/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Audit & Compliance** é responsável por:

- Registo de eventos de auditoria (audit trail)
- Hash chain para integridade e não-repudiação
- Políticas de compliance
- Campanhas de auditoria
- Retenção de dados
- Exportação de relatórios

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento | ✅ Forte | Auditoria é requisito enterprise essencial |
| Completude | ✅ Funcional | Hash chain, compliance policies, campaigns |
| Frontend | ⚠️ Mínimo | Apenas 1 página (AuditPage) |

---

## 3. Páginas Frontend

| Página | Rota | Permissão | Estado |
|--------|------|-----------|--------|
| AuditPage | `/audit` | audit:read | ✅ Funcional |

---

## 4. Backend

### 4.1 Features (10 features)

| Feature | Propósito |
|---------|-----------|
| RecordAuditEvent | Registar evento de auditoria |
| GetAuditTrail | Obter trilha de auditoria |
| SearchAuditLog | Pesquisar logs |
| ExportAuditReport | Exportar relatório |
| VerifyChainIntegrity | Verificar integridade da hash chain |
| GetComplianceReport | Relatório de compliance |
| ListCompliancePolicies | Listar políticas |
| ListAuditCampaigns | Listar campanhas |
| CreateAuditCampaign | Criar campanha |
| ConfigureRetention | Configurar retenção |

### 4.2 Entidades

| Entidade | Propósito |
|----------|-----------|
| AuditEvent | Evento individual de auditoria |
| AuditChainLink | Elo da hash chain |
| AuditCampaign | Campanha de auditoria |
| CompliancePolicy | Política de compliance |
| ComplianceResult | Resultado de avaliação |
| RetentionPolicy | Política de retenção |

### 4.3 Banco de Dados

| DbContext | Propósito |
|-----------|-----------|
| AuditDbContext | Eventos, chain links, campanhas, políticas |

**Migrations:** InitialCreate, Phase3ComplianceDomain

---

## 5. Integrações

O módulo recebe eventos de todos os outros módulos via:
- SecurityAuditBridge (do Identity module)
- Integration events (Outbox pattern)
- Pipeline behaviors (SecurityEventAuditBehavior)

---

## 6. Resumo de Ações

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | Validar hash chain integrity verification end-to-end | P1 | 2h |
| 2 | Validar que todos os módulos publicam eventos de auditoria | P1 | 2h |
| 3 | Avaliar se AuditPage precisa de mais funcionalidades (filtros, exportação visual) | P2 | 1h |
| 4 | Documentar modelo de hash chain | P2 | 2h |
| 5 | Documentar políticas de compliance e retenção | P2 | 2h |
