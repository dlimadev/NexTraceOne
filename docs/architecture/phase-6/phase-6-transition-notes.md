# Fase 6 — Notas de Transição

## O que foi Implementado

### Novos arquivos criados

| Arquivo | Propósito |
|---|---|
| `src/frontend/src/contexts/EnvironmentContext.tsx` | Context, Provider e hooks de ambiente |
| `src/frontend/src/components/shell/EnvironmentBanner.tsx` | Banner contextual para ambientes não produtivos |
| `docs/architecture/phase-6/phase-6-frontend-context-architecture.md` | Arquitetura geral da Fase 6 |
| `docs/architecture/phase-6/phase-6-navigation-and-ui-context.md` | Navegação e contexto de UI |
| `docs/architecture/phase-6/phase-6-ai-surface-readiness.md` | Readiness da superfície de IA |
| `docs/architecture/phase-6/phase-6-transition-notes.md` | Este documento |

### Testes criados

| Arquivo | Propósito |
|---|---|
| `src/__tests__/contexts/EnvironmentContext.test.tsx` | Testes do EnvironmentContext e EnvironmentProvider |
| `src/__tests__/utils/tokenStorageEnvironment.test.ts` | Testes das funções de environment no tokenStorage |

### Arquivos modificados

| Arquivo | Mudança |
|---|---|
| `src/utils/tokenStorage.ts` | Adicionados `storeEnvironmentId`, `getEnvironmentId`, `clearEnvironmentId`; `clearAllTokens` agora limpa `nxt_eid` |
| `src/api/client.ts` | Request interceptor agora injeta `X-Environment-Id` |
| `src/App.tsx` | `EnvironmentProvider` adicionado entre `AuthProvider` e `PersonaProvider` |
| `src/components/shell/WorkspaceSwitcher.tsx` | Refatorado para usar `useEnvironment()` — lista dinâmica |
| `src/components/shell/AppShell.tsx` | Adicionado `<EnvironmentBanner />` entre TopBar e ContentFrame |
| `src/features/ai-hub/components/AssistantPanel.tsx` | Props `activeEnvironmentId`, `activeEnvironmentName`, `isNonProductionEnvironment` adicionadas |
| `src/features/operations/pages/IncidentDetailPage.tsx` | Passa props de ambiente ao `AssistantPanel` |
| `src/features/catalog/pages/ServiceDetailPage.tsx` | Passa props de ambiente ao `AssistantPanel` |
| `src/locales/en.json` | Adicionadas chaves `environment.*` e `assistantPanel.analyzingNonProd` |
| `src/locales/pt-BR.json` | Adicionadas chaves `environment.*` e `assistantPanel.analyzingNonProd` |
| `src/locales/pt-PT.json` | Adicionadas chaves `environment.*` e `assistantPanel.analyzingNonProd` |
| `src/locales/es.json` | Adicionadas chaves `environment.*` e `assistantPanel.analyzingNonProd` |

### Testes atualizados

| Arquivo | Motivo |
|---|---|
| `src/__tests__/components/shell/AppShell.test.tsx` | Mock de `EnvironmentContext` adicionado (EnvironmentBanner usa `useEnvironment()`) |
| `src/__tests__/pages/IncidentDetailPage.test.tsx` | Mock de `EnvironmentContext` adicionado |
| `src/__tests__/pages/ServiceDetailPage.test.tsx` | Mock de `EnvironmentContext` adicionado |

## Gaps Conhecidos e Trabalho Futuro

### TODO Fase 7

1. **API real de ambientes**: Substituir `loadEnvironmentsForTenant()` (mock) por chamada
   a `GET /api/v1/identity/environments?tenantId={tenantId}` no backend Identity.

2. **React Query para ambientes**: Usar `useQuery` com cache para carregar ambientes
   via API, incluindo invalidação ao trocar de tenant.

3. **CommandPalette**: Integrar seleção de ambiente no Command Palette (`⌘K`).

4. **Filtros contextuais por ambiente**: Páginas de Incidents, Services e Changes
   devem filtrar dados pelo ambiente ativo por padrão.

5. **Grounding de IA por ambiente**: O backend de IA deve filtrar fontes de grounding
   pelo `X-Environment-Id` recebido.

### Limitações da Implementação Atual

- A lista de ambientes é um mock (5 ambientes por tenant) até a Fase 7
- O ambiente ativo não influencia filtros de dados nas páginas de listagem ainda
- O `EnvironmentBanner` não tem botão de dismiss por sessão (pode ser adicionado na Fase 7)
- A CommandPalette não exibe o ambiente ativo na breadcrumb contextual ainda

## Considerações de Segurança

- Nenhuma decisão de autorização é feita no frontend com base no ambiente
- O `nxt_eid` em `sessionStorage` é apenas para UX e para propagar o header ao backend
- O backend valida o ambiente ativo via `EnvironmentResolutionMiddleware` (Fase 2)
- `clearAllTokens()` agora limpa o `nxt_eid` no logout, prevenindo vazamento de contexto

## Compatibilidade Retroativa

- As props adicionadas ao `AssistantPanel` são todas opcionais — nenhum uso existente quebrou
- O `WorkspaceSwitcher` agora depende de `EnvironmentProvider` — qualquer uso fora do
  `AuthProvider > EnvironmentProvider` precisará de mock ou wrapper
- Todos os testes afetados foram atualizados com mocks adequados
