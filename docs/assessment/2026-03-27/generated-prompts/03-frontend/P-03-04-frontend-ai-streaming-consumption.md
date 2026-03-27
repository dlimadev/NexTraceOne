# P-03-04 — Implementar consumo SSE no painel AI Assistant para respostas em streaming

## 1. Título

Implementar consumo de Server-Sent Events (SSE) no AssistantPanel do frontend para respostas de IA em streaming.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

O backend já expõe POST /api/v1/ai/chat/stream com resposta SSE em `src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/Runtime/Endpoints/Endpoints/AiRuntimeEndpointModule.cs`. O frontend possui o componente AssistantPanel em `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx` mas não consome o streaming. Este prompt implementa o consumo SSE para que o utilizador veja as respostas da IA a serem geradas em tempo real.

## 4. Problema atual

- O endpoint `/api/v1/ai/chat/stream` existe no backend (linha 72 do AiRuntimeEndpointModule.cs) e escreve SSE.
- O `AssistantPanel.tsx` em `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx` não usa EventSource nem fetch com ReadableStream.
- O ficheiro de API `src/frontend/src/features/ai-hub/api/index.ts` e `aiGovernance.ts` não têm funções de streaming.
- A página `AiAssistantPage.tsx` em `src/frontend/src/features/ai-hub/pages/` utiliza o painel mas sem streaming.
- Sem streaming, a experiência de IA é bloqueante — o utilizador espera toda a resposta antes de ver qualquer texto.

## 5. Escopo permitido

- `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`
- `src/frontend/src/features/ai-hub/api/index.ts` — adicionar função de streaming
- `src/frontend/src/features/ai-hub/api/aiGovernance.ts` — se necessário
- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx` — se necessário para integrar

## 6. Escopo proibido

- Não alterar o backend (endpoint já funciona).
- Não alterar páginas fora de `src/frontend/src/features/ai-hub/`.
- Não implementar websockets — usar SSE via fetch API com ReadableStream.
- Não alterar lógica de governance, políticas ou audit da IA.

## 7. Ficheiros principais candidatos a alteração

1. `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`
2. `src/frontend/src/features/ai-hub/api/index.ts`
3. `src/frontend/src/features/ai-hub/api/aiGovernance.ts`
4. `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`

## 8. Responsabilidades permitidas

- Criar função de streaming usando fetch + ReadableStream (não EventSource, pois é POST).
- Implementar parsing incremental de chunks SSE (data: ...\n\n).
- Atualizar estado do AssistantPanel progressivamente à medida que chunks chegam.
- Adicionar indicador visual de "a gerar resposta..." durante streaming.
- Implementar cancelamento de streaming via AbortController.
- Tratar erros de conexão com mensagem i18n amigável.
- Manter fallback para endpoint não-streaming (/chat) se streaming falhar.

## 9. Responsabilidades proibidas

- Não implementar markdown rendering avançado — texto simples é suficiente inicialmente.
- Não criar sistema de retry automático para streaming.
- Não armazenar histórico de conversação no frontend (será prompt separado).
- Não implementar multi-turn conversation neste prompt.

## 10. Critérios de aceite

- [ ] AssistantPanel mostra tokens da resposta de IA incrementalmente (streaming visual).
- [ ] Indicador de "a gerar..." visível durante streaming.
- [ ] Botão de cancelar streaming funcional (AbortController).
- [ ] Fallback para /chat (não-streaming) se streaming falhar.
- [ ] Erros de conexão mostram mensagem i18n.
- [ ] Build frontend sem erros.

## 11. Validações obrigatórias

- Build do frontend sem erros.
- Lint sem erros críticos.
- Verificar que o AssistantPanel renderiza sem erros quando não há backend disponível (graceful degradation).
- Verificar que AbortController cancela efetivamente o fetch.

## 12. Riscos e cuidados

- SSE via POST requer fetch com ReadableStream (não EventSource nativo que só suporta GET).
- Parsing de chunks SSE deve lidar com chunks parciais (buffer até \n\n completo).
- Memory leaks: garantir cleanup do ReadableStream reader no unmount do componente.
- CORS: verificar que o backend permite streaming responses no CORS config.
- O endpoint pode requerer autenticação — incluir token no header do fetch.

## 13. Dependências

- Nenhuma dependência de outros prompts — o backend já tem o endpoint funcional.
- Endpoint POST /api/v1/ai/chat/stream deve estar acessível e retornar SSE.

## 14. Próximos prompts sugeridos

- **P-XX-XX** — Markdown rendering para respostas da IA (formatação rica).
- **P-XX-XX** — Histórico de conversação persistido no backend.
- **P-XX-XX** — Multi-turn conversation com contexto cumulativo.
- **P-XX-XX** — Integração do AssistantPanel como sidebar global em toda a aplicação.
