# Notifications — Contracts, Security, FinOps, AI & Integrations

## Catálogo e Contratos

| Tipo | Severidade | Requer Ação | Rationale |
|------|-----------|-------------|-----------|
| ContractPublished | Info | Não | Informativo — contrato publicado com sucesso |
| BreakingChangeDetected | Critical | Sim | Impacto direto em consumidores — ação imediata |
| ContractValidationFailed | Warning | Sim | Bloqueio de publicação — necessita correção |

### Regras
- Breaking changes recebem severidade Critical e deep link para `/contracts/{id}/changes`
- Publicações bem-sucedidas são informativas — não geram alertas externos por padrão
- Validações falhadas apontam para a página de validação específica do contrato
- Destinatário: owner do serviço ou publisher do contrato

## Segurança e Acesso

| Tipo | Severidade | Requer Ação | Rationale |
|------|-----------|-------------|-----------|
| BreakGlassActivated | Critical | Sim | Acesso de emergência — obrigatório e não-opt-out |
| UserRoleChanged | Info | Não | Informativo — mudança de papel |
| JitAccessGranted | Info | Não | Confirmação de acesso temporário |
| AccessReviewPending | ActionRequired | Sim | Revisão de acesso pendente com prazo |

### Regras
- BreakGlass é notificação mandatory (MandatoryNotificationPolicy)
- UserRoleChanged notifica o próprio utilizador afetado
- JitAccessGranted inclui data de expiração na mensagem
- AccessReviewPending inclui prazo e requer ação do assignee
- Nenhuma informação sensível no payload das mensagens

## FinOps, Governance e Compliance

| Tipo | Severidade | Requer Ação | Rationale |
|------|-----------|-------------|-----------|
| ComplianceCheckFailed | Warning | Sim | Gaps de compliance detetados |
| PolicyViolated | Warning | Sim | Violação de política de governança |
| EvidenceExpiring | ActionRequired | Sim | Evidência perto da expiração |
| BudgetExceeded | Warning | Sim | Custo excedeu orçamento |
| BudgetThresholdReached | Dinâmica | Dinâmica | Limiar progressivo (80/90/100%) |

### Severidade Dinâmica — BudgetThresholdReached
| Limiar | Severidade | Requer Ação |
|--------|-----------|-------------|
| 80% | ActionRequired | Não |
| 90% | Warning | Sim |
| 100%+ | Critical | Sim |

### Regras
- Budget threshold usa severidade dinâmica baseada na percentagem atingida
- Evidence expiring inclui data de expiração no template
- Policy violated inclui descrição da violação para contexto
- Deep links apontam para governance/compliance/finops conforme domínio
- Mensagens incluem valores numéricos contextuais (gaps, custos, limiares)

## IA e Governança de IA

| Tipo | Severidade | Requer Ação | Rationale |
|------|-----------|-------------|-----------|
| AiProviderUnavailable | Critical | Sim | Indisponibilidade impacta features de IA |
| TokenBudgetExceeded | Warning | Não | Budget de tokens excedido — informativo |
| AiGenerationFailed | Warning | Não | Falha de geração — retry possível |
| AiActionBlockedByPolicy | Info | Não | Bloqueio por política — informativo |

### Regras
- AiProviderUnavailable notifica roles (AiAdmin, PlatformAdmin) em vez de utilizadores individuais
- TokenBudgetExceeded notifica o utilizador que excedeu com contexto de uso
- AiGenerationFailed notifica o solicitante com provider e erro
- AiActionBlockedByPolicy informa o utilizador e aponta para políticas
- Provider e modelo são sempre identificados na mensagem

## Integrações e Ingestion

| Tipo | Severidade | Requer Ação | Rationale |
|------|-----------|-------------|-----------|
| IntegrationFailed | Warning | Sim | Falha de integração — necessita investigação |
| SyncFailed | Warning | Sim | Sincronização falhada — dados desatualizados |
| ConnectorAuthFailed | Critical | Sim | Autenticação falhada — fluxo interrompido |

### Regras
- ConnectorAuthFailed recebe Critical — autenticação falhada bloqueia o fluxo
- SyncFailed e IntegrationFailed recebem Warning — falha que necessita atenção
- Todas as mensagens incluem: nome do conector/integração, resumo do erro, impacto
- Deep links apontam para a entidade específica (integração ou conector)
- Deduplicação existente evita spam por falhas repetitivas sem mudança de estado
