# P-01-05 — Hardening dos handlers de Orchestration do AIKnowledge (Batch 1)

## Modo de operação

Implementation

## Objetivo

Reforçar e consolidar os 3 handlers de Orchestration do módulo AIKnowledge que envolvem chamadas
a providers de IA: GenerateRobotFrameworkDraft, GenerateTestScenarios e ValidateKnowledgeCapture.
Estes handlers já estão implementados mas necessitam de hardening para garantir robustez em
chamadas a modelos de IA, tratamento de falhas de provider, validação de quotas, auditoria
e qualidade de resposta.

## Problema atual

A análise da codebase confirma que os 3 handlers têm implementação funcional:

- **GenerateRobotFrameworkDraft** — faz routing para provider de IA via `IExternalAIRoutingPort`, constrói
  prompt a partir de spec/contrato/descrição, persiste como `GeneratedTestArtifact` com confidence score
- **GenerateTestScenarios** — gera cenários de teste estruturados via provider de IA, suporta múltiplos
  frameworks (xunit default), persiste artefactos
- **ValidateKnowledgeCapture** — valida completude, relevância e duplicados de capturas de conhecimento,
  retorna issues/warnings e recomendação

Áreas que necessitam de hardening:
1. **Resiliência a falhas de provider** — se Ollama ou OpenAI não responder, o handler deve degradar gracefully
2. **Validação de quotas** — AiTokenQuotaService deve ser verificado antes de chamadas dispendiosas
3. **Timeout e cancelamento** — chamadas a providers podem demorar — CancellationToken deve ser propagado
4. **Auditoria de uso** — cada chamada a provider deve ser registada para governança de IA
5. **Qualidade de parsing** — respostas de IA podem vir malformadas — tratar parsing errors
6. **Logging estruturado** — latência, tokens consumidos e modelo usado devem ser logados

## Escopo permitido

- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GenerateRobotFrameworkDraft/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GenerateTestScenarios/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/ValidateKnowledgeCapture/`
- Serviços de suporte: `AiTokenQuotaService`, `ExternalAiRoutingPortAdapter`

## Escopo proibido

- Handlers de ExternalAI (tratados em P-01-04)
- Handlers de Runtime (SendAssistantMessage, etc.)
- Providers de IA (Ollama, OpenAI) — apenas consumir
- Outros módulos
- Ficheiros de migração

## Ficheiros principais candidatos a alteração

- `Orchestration/Features/GenerateRobotFrameworkDraft/GenerateRobotFrameworkDraft.cs`
- `Orchestration/Features/GenerateTestScenarios/GenerateTestScenarios.cs`
- `Orchestration/Features/ValidateKnowledgeCapture/ValidateKnowledgeCapture.cs`
- Possíveis ajustes em `Infrastructure/Adapters/ExternalAiRoutingPortAdapter.cs` (retry/timeout)

## Responsabilidades permitidas

- Adicionar verificação de quota antes de chamadas a providers de IA
- Tratar falhas de provider com degradação graciosa (ex: Result.Failure com mensagem clara)
- Adicionar logging estruturado (latência, tokens, modelo, resultado)
- Tratar parsing errors em respostas de IA
- Adicionar guard clauses e validações de entrada
- Registar uso para auditoria via AiTokenQuotaService.RecordUsageAsync

## Responsabilidades proibidas

- Alterar providers de IA (Ollama, OpenAI)
- Adicionar novos endpoints
- Alterar schema de base de dados

## Critérios de aceite

1. Cada handler verifica quota antes de chamar provider de IA
2. Falhas de provider são tratadas com Result.Failure, sem exceções não controladas
3. Parsing errors em respostas de IA são tratados gracefully
4. Uso é registado para auditoria (tokens, modelo, latência)
5. Logging estruturado presente em todas as chamadas a providers
6. Módulo compila e testes passam

## Validações obrigatórias

- `dotnet build src/modules/aiknowledge/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Revisão manual dos 3 handlers para confirmar resiliência e auditoria

## Riscos e cuidados

- Adição de verificação de quota pode bloquear funcionalidade se quota não estiver configurada — tratar default
- Logging excessivo pode impactar performance — usar LogLevel adequado
- Validação de resposta de IA é inherentemente frágil — ser tolerante mas defensivo

## Dependências

- P-00-04 (CancellationToken no AIKnowledge) idealmente já aplicado
- P-01-04 (Hardening ExternalAI) pode ser executado em paralelo

## Próximos prompts sugeridos

- P-01-06 (Hardening dos handlers de Orchestration — batch 2)
- P-01-07 (Completar handlers de Governance)
