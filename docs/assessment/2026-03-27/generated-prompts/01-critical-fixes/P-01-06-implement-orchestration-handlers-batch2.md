# P-01-06 — Hardening dos handlers de Orchestration do AIKnowledge (Batch 2)

## Modo de operação

Implementation

## Objetivo

Reforçar e consolidar os 2 handlers de Orchestration do módulo AIKnowledge que lidam com
dados históricos e sumarização: GetAiConversationHistory e SummarizeReleaseForApproval.
Estes handlers já possuem implementação funcional mas necessitam de hardening para garantir
qualidade de paginação, tratamento de períodos vazios, completude de dados para sumarização
e robustez nas chamadas ao provider de IA para geração de resumos.

## Problema atual

A análise da codebase confirma que ambos os handlers estão implementados:

- **GetAiConversationHistory** — lista conversações de IA persistidas com filtros (release, serviço,
  tópico, estado, período), paginação funcional. Utiliza `IAiOrchestrationConversationRepository`
  que implementa queries reais com `ListHistoryAsync` no `AiOrchestrationRepositories.cs`.

- **SummarizeReleaseForApproval** — recolhe dados reais de conversações e artefactos via repositórios
  (`GetRecentByReleaseAsync` em conversações e artefactos), gera sumarização via provider de IA,
  retorna indicadores de confidence. Utiliza `IExternalAIRoutingPort` para chamada ao modelo.

Áreas que necessitam de hardening:

1. **GetAiConversationHistory**:
   - Paginação pode não ter proteção contra page sizes excessivos
   - Filtro por período pode não validar datas inválidas ou intervalos absurdos
   - Resposta pode não indicar claramente "sem conversações" vs "filtros sem resultados"

2. **SummarizeReleaseForApproval**:
   - Se não existirem conversações ou artefactos para a release, o handler pode chamar IA com contexto vazio
   - Falha do provider de IA deve resultar em degradação graciosa, não exceção
   - Confidence score deve ser baseado em dados reais, não em heurística fixa
   - Auditoria da sumarização deve registar contexto, modelo usado e tokens consumidos
   - Release ID inválido ou inexistente deve ser tratado com Result.Failure

## Escopo permitido

- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GetAiConversationHistory/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/SummarizeReleaseForApproval/`
- Repositórios correspondentes em Infrastructure/Persistence/AiOrchestrationRepositories.cs

## Escopo proibido

- Handlers de ExternalAI (P-01-04)
- Handlers de geração (GenerateRobotFrameworkDraft, GenerateTestScenarios — P-01-05)
- Providers de IA
- Outros módulos
- Ficheiros de migração

## Ficheiros principais candidatos a alteração

- `Orchestration/Features/GetAiConversationHistory/GetAiConversationHistory.cs`
- `Orchestration/Features/SummarizeReleaseForApproval/SummarizeReleaseForApproval.cs`
- `Infrastructure/Persistence/AiOrchestrationRepositories.cs` (se necessário ajustar queries)

## Responsabilidades permitidas

- Adicionar validação de page size máximo no GetAiConversationHistory
- Validar datas e intervalos no filtro de período
- Tratar cenário de "release sem conversações" no SummarizeReleaseForApproval (retornar Result informativo)
- Adicionar verificação de quota e auditoria de uso no SummarizeReleaseForApproval
- Tratar falhas de provider de IA com degradação graciosa
- Adicionar logging estruturado (latência, tokens, modelo)
- Adicionar guard clauses para parâmetros obrigatórios

## Responsabilidades proibidas

- Alterar providers de IA
- Alterar lógica de persistência de conversações
- Adicionar novos endpoints
- Alterar schema de base de dados

## Critérios de aceite

1. GetAiConversationHistory valida page size e datas
2. SummarizeReleaseForApproval verifica quota antes de chamar IA
3. Cenário de release sem dados é tratado sem chamar IA desnecessariamente
4. Falhas de provider resultam em Result.Failure, não exceções
5. Auditoria de uso registada para sumarização
6. Módulo compila e testes passam

## Validações obrigatórias

- `dotnet build src/modules/aiknowledge/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Revisão manual dos 2 handlers para confirmar validações e resiliência

## Riscos e cuidados

- SummarizeReleaseForApproval depende de dados de múltiplos repositórios — falha parcial deve ser tratada
- A sumarização de IA pode gerar conteúdo de baixa qualidade com contexto insuficiente — sinalizar confidence
- Limitar page size pode quebrar consumidores que pedem páginas grandes — escolher limite razoável (ex: 100)

## Dependências

- P-00-04 (CancellationToken) idealmente já aplicado
- P-01-05 (Orchestration batch 1) pode ser executado em paralelo

## Próximos prompts sugeridos

- P-01-07 (Completar handlers de Governance com DeferredFields)
