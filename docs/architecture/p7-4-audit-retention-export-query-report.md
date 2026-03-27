# P7.4 — Audit & Compliance: Retenção, Export e Consulta Auditável Mínima

**Data:** 2026-03-27  
**Módulo:** Audit & Compliance  
**Objetivo:** reforçar retenção mínima real, export auditável mínimo e consulta funcional de eventos.

---

## 1. Estado anterior (resumo)

- **Retenção**: `RetentionPolicy` existia, mas a aplicação era manual e sem job recorrente; endpoints não expostos.
- **Export**: `ExportAuditReport` retornava JSON, sem correlação explícita e sem UI mínima.
- **Consulta**: `SearchAuditLog` suportava filtros básicos (módulo, ação, período), mas não expunha correlação nem parâmetros de recurso no endpoint.
- **Hash chain**: validação via `VerifyChainIntegrity` existia, porém não lidava com truncamento provocado por retenção.

Fontes analisadas: `backend-state-report.md`, `database-state-report.md`, `capability-gap-matrix.md`, `final-project-state-assessment.md`, `module-consolidated-review.md` (Audit & Compliance), além do código real em `AuditDbContext`, `AuditEvent`, `AuditChainLink`, `RetentionPolicy`, `CompliancePolicy`, `AuditCampaign`, `ComplianceResult`.

---

## 2. Modelo de retenção adotado

- **Política aplicada**: Retenção utiliza a política activa mais restritiva (`RetentionDays` menor).
- **Execução**: `ApplyRetention` elimina eventos anteriores ao cutoff com `ExecuteDeleteAsync`.
- **Job mínimo**: `AuditRetentionJob` executa ciclos periódicos, configurável via `Audit:Retention`.
- **Truncamento**: `VerifyChainIntegrity` agora reporta `IsTruncated` quando o primeiro elo tem `PreviousHash` não vazio (retenção aplicada).

**Limitação residual**: não há re-hash completo da cadeia após truncamento. A integridade é verificada a partir do primeiro elo remanescente.

---

## 3. Export mínimo auditável

- **Handler**: `ExportAuditReport` mantém export JSON, agora com `CorrelationId` e `ChainHash` por evento.
- **Campos mínimos garantidos**:
  - `EventId`
  - `OccurredAt`
  - `SourceModule`
  - `PerformedBy`
  - `CorrelationId` (quando aplicável)
  - `ChainHash`
- **Frontend**: botão de export no `AuditPage` para exportar o período actual (ou últimos 7 dias).

---

## 4. Consulta auditável mínima

### Backend

- `SearchAuditLog` agora aceita **CorrelationId** e filtros de recurso (`resourceType`, `resourceId`) via endpoint.
- Resposta inclui `ChainHash`, `PreviousHash` e `SequenceNumber` para consulta mínima de cadeia ligada a um fluxo.

### Frontend

- `AuditPage` agora permite:
  - filtrar por tipo de evento
  - filtrar por módulo de origem
  - filtrar por correlationId
  - filtrar por período (from/to)

---

## 5. Integração com hash chain

Atualizado `VerifyChainIntegrity` para sinalizar truncamento devido a retenção:

- `IsTruncated = true` quando o primeiro elo remanescente tem `PreviousHash` não vazio.
- `TruncatedAtSequence` e `TruncatedPreviousHash` retornados para diagnóstico.

Isto evita falsos negativos após retenção, mantendo rastreabilidade mínima.

---

## 6. Wiring backend & endpoints

**Novos endpoints de retenção** (AuditEndpointModule):

- `POST /api/v1/audit/retention/policies` — configurar política  
- `GET /api/v1/audit/retention/policies` — listar políticas  
- `POST /api/v1/audit/retention/apply` — aplicar retenção

**Query de audit**:

- `GET /api/v1/audit/search` agora suporta `correlationId`, `resourceType`, `resourceId`.

---

## 7. Principais ficheiros alterados

### Backend

- `...AuditEvent.cs` (CorrelationId)
- `...AuditEventConfiguration.cs` (coluna + índice)
- `...AuditRepositories.cs` (filtro por correlation)
- `...SearchAuditLog.cs` (novo filtro + chain info)
- `...ExportAuditReport.cs` (CorrelationId)
- `...VerifyChainIntegrity.cs` (IsTruncated)
- `...ApplyRetention.cs` (nota de truncamento)
- `...AuditRetentionOptions.cs` (config do job)
- `...AuditRetentionJob.cs` (job recorrente)
- `...AuditEndpointModule.cs` (retention endpoints)
- `...Migrations/20260327103000_P7_4_AuditCorrelationId.cs`
- `...AuditDbContextModelSnapshot.cs`

### Frontend

- `src/frontend/src/features/audit-compliance/api/audit.ts`
- `src/frontend/src/features/audit-compliance/pages/AuditPage.tsx`
- `src/frontend/src/types/index.ts`
- `src/frontend/src/locales/en.json`
- `src/frontend/src/locales/pt-BR.json`
- `src/frontend/src/locales/pt-PT.json`
- `src/frontend/src/locales/es.json`

### Documentação

- `src/modules/auditcompliance/README.md`

---

## 8. Validação realizada

- `dotnet build` (sucesso)
- `dotnet test tests/modules/auditcompliance/NexTraceOne.AuditCompliance.Tests/NexTraceOne.AuditCompliance.Tests.csproj`  
  - warnings NU1510 / MSB3277 pré-existentes (dependências EF Core)
- `npm run test -- src/__tests__/pages/AuditPage.test.tsx` (sucesso)

---

## 9. Limitações conhecidas (mantidas para P7.5)

- Re-hash completo da cadeia após retenção ainda não implementado.
- Exportação mantém JSON único (sem CSV/PDF).
- Não há UI dedicada para configuração de retenção (apenas endpoint).
