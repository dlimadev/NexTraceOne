# P-01-04 — Hardening dos handlers de ExternalAI do módulo AIKnowledge

## Modo de operação

Implementation

## Objetivo

Reforçar e consolidar os 6 handlers de ExternalAI do módulo AIKnowledge (CaptureExternalAIResponse,
ConfigureExternalAIPolicy, ApproveKnowledgeCapture, ReuseKnowledgeCapture, GetExternalAIUsage,
ListKnowledgeCaptures) que já possuem implementação funcional, mas necessitam de hardening para
garantir robustez enterprise: validação completa, auditoria, tratamento de erros, edge cases
e coerência com o modelo de governança de IA do NexTraceOne.

## Problema atual

A análise da codebase confirma que os 6 handlers de ExternalAI estão implementados com lógica
real e funcional:

- **CaptureExternalAIResponse** — cria `ExternalAiConsultation` e `KnowledgeCapture`, regista tokens e confidence
- **ConfigureExternalAIPolicy** — cria/atualiza `ExternalAiPolicy`, valida constraints
- **ApproveKnowledgeCapture** — aprova capturas com reviewer e timestamp
- **ReuseKnowledgeCapture** — incrementa contagem de reuse, valida estado de aprovação
- **GetExternalAIUsage** — agrega métricas de consultações e capturas
- **ListKnowledgeCaptures** — lista capturas com filtros (status, categoria, tags, período) e paginação

No entanto, estes handlers precisam de hardening porque:
1. Validações de entrada podem estar incompletas (ex: strings vazias, GUIDs inválidos)
2. Cenários de concorrência não estão tratados (ex: dupla aprovação, reuse simultâneo)
3. Auditoria de ações sensíveis (configuração de políticas de IA) pode estar insuficiente
4. Tratamento de erros pode não seguir o padrão `Result<T>` consistentemente
5. Faltam guard clauses e verificações de ownership/tenant

## Escopo permitido

- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/` — 6 handlers
- Repositórios correspondentes em Infrastructure/Persistence/
- DTOs e validações associadas

## Escopo proibido

- Handlers de Orchestration, Runtime ou Governance do AIKnowledge
- Providers de IA (Ollama, OpenAI)
- Outros módulos
- Ficheiros de migração

## Ficheiros principais candidatos a alteração

- `ExternalAI/Features/CaptureExternalAIResponse/CaptureExternalAIResponse.cs`
- `ExternalAI/Features/ConfigureExternalAIPolicy/ConfigureExternalAIPolicy.cs`
- `ExternalAI/Features/ApproveKnowledgeCapture/ApproveKnowledgeCapture.cs`
- `ExternalAI/Features/ReuseKnowledgeCapture/ReuseKnowledgeCapture.cs`
- `ExternalAI/Features/GetExternalAIUsage/GetExternalAIUsage.cs`
- `ExternalAI/Features/ListKnowledgeCaptures/ListKnowledgeCaptures.cs`
- `Infrastructure/Persistence/ExternalAiRepositories.cs` (se necessário ajustar queries)

## Responsabilidades permitidas

- Adicionar guard clauses e validações de entrada completas
- Garantir uso consistente de `Result<T>` para erros controlados
- Adicionar verificações de tenant e ownership
- Tratar cenários de concorrência (ex: optimistic concurrency para aprovações)
- Melhorar logging estruturado com correlationId
- Garantir que ConfigureExternalAIPolicy gera evento de auditoria

## Responsabilidades proibidas

- Alterar a lógica de negócio fundamental (captura, aprovação, reuse)
- Adicionar novas features ou endpoints
- Alterar migrações ou schema
- Alterar providers de IA

## Critérios de aceite

1. Todos os 6 handlers têm guard clauses para parâmetros obrigatórios
2. Uso consistente de `Result<T>` — sem exceções para fluxos de negócio previsíveis
3. ConfigureExternalAIPolicy gera evento de auditoria
4. Cenários de concorrência tratados (ex: aprovação duplicada retorna erro amigável)
5. Logging estruturado em operações sensíveis
6. Módulo compila e testes passam

## Validações obrigatórias

- `dotnet build src/modules/aiknowledge/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Revisão manual dos 6 handlers para confirmar guard clauses e Result<T>

## Riscos e cuidados

- Alterações nos DTOs de resposta podem afetar o frontend — manter retrocompatibilidade
- Adição de validações pode rejeitar requests que antes eram aceites — verificar impacto
- Auditoria adicional gera mais escrita na base — monitorar performance
- Tenant isolation é crítico para IA governada — garantir que queries filtram por tenant

## Dependências

- P-00-04 (CancellationToken no AIKnowledge) idealmente já aplicado
- Não depende de outros prompts da fase 01

## Próximos prompts sugeridos

- P-01-05 (Hardening dos handlers de Orchestration — batch 1)
- P-01-06 (Hardening dos handlers de Orchestration — batch 2)
