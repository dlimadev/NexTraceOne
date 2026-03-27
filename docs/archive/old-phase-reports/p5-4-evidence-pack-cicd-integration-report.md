# P5.4 — Evidence Pack End-to-End com Integração CI/CD: Relatório de Execução

**Data:** 2026-03-26  
**Fase:** P5.4 — Evidence Pack CI/CD Integration  
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Completar o fluxo de `EvidencePack` end-to-end com integração CI/CD, tornando o pacote de
evidências automaticamente alimentado quando um workflow é iniciado e quando dados de pipeline
chegam via CI/CD. Ligar `EvidencePack` a `Release`, `WorkflowInstance`, `ApprovalDecision`,
`ChangeEvent` e `ExternalMarker` de forma rastreável.

---

## 2. Estado Anterior

| Aspecto | Estado antes do P5.4 |
|---------|----------------------|
| `EvidencePack` entity | Existia com: WorkflowInstanceId, ReleaseId, ContractDiffSummary, BlastRadiusScore, SpectralScore, ChangeIntelligenceScore, ApprovalHistory, ContractHash, CompletenessPercentage |
| Criação automática de pack | **Inexistente** — `InitiateWorkflow` não criava EvidencePack |
| Campos CI/CD | **Inexistentes** — nenhum campo de pipeline no entity |
| `AttachCiCdEvidence` command | **Inexistente** |
| `CompletenessPercentage` | Calculado com 5 campos |
| `GetEvidencePack.Response` | Não incluía dados CI/CD |
| `ExportEvidencePackPdf.Response` | Não incluía dados CI/CD |

---

## 3. Modelo de Evidências CI/CD Adotado

Os seguintes campos foram adicionados à entidade `EvidencePack`:

| Campo | Tipo | Origem | Descrição |
|-------|------|--------|-----------|
| `PipelineSource` | `string?` | `ExternalMarker.SourceSystem` | Sistema de CI/CD (ex: "github-actions") |
| `BuildId` | `string?` | `ExternalMarker.ExternalId` | ID externo do run/build |
| `CommitSha` | `string?` | `Release.CommitSha` | SHA do commit do deploy |
| `CiChecksResult` | `string?` | CI/CD event | "passed", "failed", "partial", "unknown" |

Método `AttachCiCdEvidence(pipelineSource, buildId, commitSha, ciChecksResult)` adicionado ao
aggregate root, que também atualiza `CompletenessPercentage`.

### Completeness revisada (6 campos)
- `ContractDiffSummary` (1/6 = 16.67%)
- `BlastRadiusScore` (1/6 = 16.67%)
- `SpectralScore` (1/6 = 16.67%)
- `ChangeIntelligenceScore` (1/6 = 16.67%)
- `ApprovalHistory` (1/6 = 16.67%)
- `PipelineSource` (1/6 = 16.67%)

---

## 4. Pipeline Automático (pós-P5.4)

```
POST /api/v1/workflow/instances  (InitiateWorkflow)
    │
    └─► InitiateWorkflow.Handler
            ├─ WorkflowInstance.Create()
            ├─ WorkflowStage.Create() (N estágios)
            └─► EvidencePack.Create(instanceId, releaseId, now)  [AUTO-CRIADO]
                    └─► evidencePackRepository.Add()

POST /api/v1/workflow/{instanceId}/evidence-pack/cicd  (AttachCiCdEvidence)
    │
    └─► AttachCiCdEvidence.Handler
            ├─ Lookup WorkflowInstance (verifica existência)
            ├─ Lookup EvidencePack by WorkflowInstanceId
            └─► pack.AttachCiCdEvidence(pipelineSource, buildId, commitSha, ciChecksResult)
                    └─► evidencePackRepository.Update()  [SCORE ACTUALIZADO]

GET /api/v1/workflow/{instanceId}/evidence-pack
    └─► GetEvidencePack.Handler → Response { ...scores, PipelineSource, BuildId, CommitSha, CiChecksResult }

GET /api/v1/workflow/{instanceId}/evidence-pack/export
    └─► ExportEvidencePackPdf.Handler → Response { ...all fields including CI/CD }
```

---

## 5. Ficheiros Alterados

### Domínio

| Ficheiro | Alteração |
|----------|-----------|
| `EvidencePack.cs` | 4 campos CI/CD adicionados; método `AttachCiCdEvidence()` criado; `RecalculateCompleteness()` atualizado para 6 campos |

### Infrastructure

| Ficheiro | Alteração |
|----------|-----------|
| `EvidencePackConfiguration.cs` | 4 novas colunas mapeadas: `PipelineSource`, `BuildId`, `CommitSha`, `CiChecksResult` |

### Application

| Ficheiro | Alteração |
|----------|-----------|
| `AttachCiCdEvidence/AttachCiCdEvidence.cs` | **Novo** — Command + Validator + Handler + Response |
| `InitiateWorkflow/InitiateWorkflow.cs` | `IEvidencePackRepository` injectado; `EvidencePack.Create()` chamado na inicialização; `Response` inclui `EvidencePackId` |
| `GetEvidencePack/GetEvidencePack.cs` | `Response` inclui 4 campos CI/CD |
| `ExportEvidencePackPdf/ExportEvidencePackPdf.cs` | `Response` inclui 4 campos CI/CD |
| `Workflow/DependencyInjection.cs` | `AttachCiCdEvidence.Validator` registado |

### API

| Ficheiro | Alteração |
|----------|-----------|
| `EvidencePackEndpoints.cs` | Novo endpoint `POST /{instanceId}/evidence-pack/cicd` (AttachCiCdEvidence) |

### Testes

| Ficheiro | Alteração |
|----------|-----------|
| `WorkflowApplicationTests.cs` | 2 `InitiateWorkflow` tests atualizados (novo dep `IEvidencePackRepository`); 3 novos `AttachCiCdEvidence` tests |

---

## 6. Ligação EvidencePack → Release/ChangeEvent/ExternalMarker

| Ligação | Como está feita |
|---------|-----------------|
| EvidencePack ↔ WorkflowInstance | `WorkflowInstanceId` (FK direta) |
| EvidencePack ↔ Release | `ReleaseId` (Guid, cross-context reference) |
| EvidencePack ↔ PipelineSource | `PipelineSource` campo (vem de `ExternalMarker.SourceSystem`) |
| EvidencePack ↔ BuildId | `BuildId` campo (vem de `ExternalMarker.ExternalId`) |
| EvidencePack ↔ CommitSha | `CommitSha` campo (vem de `Release.CommitSha`) |
| WorkflowInstance ↔ Release | `ReleaseId` (Guid, cross-context reference) |

---

## 7. Validação

- ✅ 224/224 testes ChangeGovernance passam (incluindo 3 novos `AttachCiCdEvidence` tests)
- ✅ Compilação sem erros em todos os projetos alterados
- ✅ EvidencePack auto-criado em `InitiateWorkflow`
- ✅ `AttachCiCdEvidence` endpoint disponível em `POST /{instanceId}/evidence-pack/cicd`
- ✅ `GetEvidencePack` e `ExportEvidencePackPdf` retornam campos CI/CD
- ✅ `CompletenessPercentage` reflecte presença de `PipelineSource`
