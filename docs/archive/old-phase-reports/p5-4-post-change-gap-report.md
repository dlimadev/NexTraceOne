# P5.4 — Post-Change Gap Report: Evidence Pack CI/CD Integration

**Data:** 2026-03-26  
**Fase:** P5.4 — Evidence Pack End-to-End CI/CD Integration

---

## 1. O que foi resolvido nesta fase

| Gap | Estado |
|-----|--------|
| EvidencePack não tinha campos CI/CD | ✅ Resolvido — 4 campos adicionados (PipelineSource, BuildId, CommitSha, CiChecksResult) |
| InitiateWorkflow não criava EvidencePack automaticamente | ✅ Resolvido — EvidencePack auto-criado em InitiateWorkflow.Handler |
| Não existia `AttachCiCdEvidence` command | ✅ Resolvido — Command + Handler + Validator + Endpoint criados |
| GetEvidencePack não retornava dados CI/CD | ✅ Resolvido — Response inclui campos CI/CD |
| ExportEvidencePackPdf não incluía CI/CD | ✅ Resolvido — Response inclui campos CI/CD |
| CompletenessPercentage não considerava pipeline | ✅ Resolvido — PipelineSource contribui para completeness (6 campos) |
| 224 testes passando | ✅ Validado |

---

## 2. O que ainda fica pendente após P5.4

### Ligação automática NotifyDeployment → WorkflowInstance → EvidencePack

| Item pendente | Descrição |
|---------------|-----------|
| Ligação deploy → workflow automática | Quando `NotifyDeployment` é chamado (P5.1), o sistema não sabe automaticamente a qual `WorkflowInstance` associar os dados de CI/CD. O operador/CI ainda precisa chamar `POST /evidence-pack/cicd` explicitamente. |
| WorkflowInstance lookup por ReleaseId | Não existe query que retorne o WorkflowInstance ativo para um dado ReleaseId, o que seria necessário para a ligação automática. |

### Approval history automático

| Item pendente | Descrição |
|---------------|-----------|
| `ApprovalHistory` ainda manual | O campo `ApprovalHistory` é um JSON string. Não é atualizado automaticamente quando `ApproveStage` ou `RejectWorkflow` é chamado. Fica para P5.5 a sincronização automática. |

### Campos CI/CD avançados

| Item pendente | Descrição |
|---------------|-----------|
| CiChecksPassedCount / CiChecksTotalCount | Contadores de checks individuais não foram adicionados nesta fase para manter o escopo mínimo. Podem ser adicionados em P5.5. |
| Log URL do pipeline | URL para o log do job/pipeline não é armazenada ainda. |

---

## 3. O que fica explicitamente para P5.5

- **Ligação automática deploy → workflow → EvidencePack**: query `GetWorkflowInstanceByReleaseId` + wiring em `NotifyDeployment` para auto-dispatchar `AttachCiCdEvidence`
- **`ApprovalHistory` auto-updated**: quando `ApproveStage`/`RejectWorkflow` chamado, atualizar automaticamente o campo `ApprovalHistory` do EvidencePack com o histórico JSON serializado
- **Post-change verification**: usar `ObservationWindow` + métricas de runtime para adicionar evidência de saúde pós-deploy ao EvidencePack
- **Rollback intelligence**: se `RegisterRollback` é chamado, adicionar evidência de rollback ao EvidencePack

---

## 4. Limitações residuais

1. **`AttachCiCdEvidence` ainda requer chamada explícita**: o pipeline CI/CD deve chamar `POST /{instanceId}/evidence-pack/cicd` manualmente (ou via webhook). A automação total fica para P5.5.

2. **Cross-context reference**: `EvidencePack.ReleaseId` é um `Guid` sem FK no banco porque Release está no `ChangeIntelligenceDbContext` e EvidencePack no `WorkflowDbContext`. Esta é uma restrição arquitetural intencional (bounded context isolation).

3. **`WorkflowInstance.ReleaseId` sem índice de lookup inverso**: não existe método `GetByReleaseIdAsync` em `IWorkflowInstanceRepository`, o que impede lookup direto de WorkflowInstance a partir de um ReleaseId.
