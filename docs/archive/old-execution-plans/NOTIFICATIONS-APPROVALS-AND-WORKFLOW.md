# Notifications — Approvals and Workflow

## Eventos de Aprovação

| Tipo | Severidade | Requer Ação | Destinatário |
|------|-----------|-------------|-------------|
| ApprovalPending | ActionRequired | Sim | Aprovador designado |
| ApprovalRejected | Warning | Não | Solicitante (owner) |
| ApprovalApproved | Info | Não | Solicitante (owner) |
| ApprovalExpiring | Warning | Sim | Aprovador designado |

## Templates

### ApprovalPending
- **Título**: `Approval required — {WorkflowName}`
- **Mensagem**: `A new approval has been requested by {RequestedBy} for {WorkflowName}. Review and decide.`
- **Deep link**: `/workflows/{WorkflowId}/stages/{StageId}`

### ApprovalRejected
- **Título**: `Rejected — {WorkflowName}`
- **Mensagem**: `The approval for {WorkflowName} was rejected by {RejectedBy}. Reason: {Reason}`
- **Deep link**: `/workflows/{WorkflowId}`

### ApprovalApproved
- **Título**: `Approved — {WorkflowName}`
- **Mensagem**: `The approval for {WorkflowName} was granted by {ApprovedBy}.`
- **Deep link**: `/workflows/{WorkflowId}/stages/{StageId}`

### ApprovalExpiring
- **Título**: `Approval expiring — {WorkflowName}`
- **Mensagem**: `The approval for {WorkflowName} is expiring at {ExpiresAt}. Act before the deadline.`
- **Deep link**: `/workflows/{WorkflowId}/stages/{StageId}`

## Regras de Destinatário

- **ApprovalPending** e **ApprovalExpiring** → Aprovador designado (ApproverUserId)
- **ApprovalRejected** e **ApprovalApproved** → Solicitante/Owner (OwnerUserId)

## Severidade e Deep Links

- Aprovações pendentes e expirando usam severidade alta (ActionRequired/Warning) com RequiresAction=true
- Aprovações concluídas (aprovadas/rejeitadas) usam severidade baixa (Info/Warning) sem RequiresAction
- Todos os deep links apontam para a fase/stage específica do workflow

## Canais

- **InApp**: Todos os eventos
- **Email**: Pendentes, rejeitados e expirando
- **Teams**: Apenas casos críticos de aprovação
