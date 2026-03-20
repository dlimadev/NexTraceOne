# Fase 6 — Arquitetura de Contexto Frontend

## Visão Geral

A Fase 6 implementa o contexto contextual de tenant e ambiente no frontend React/TypeScript do NexTraceOne.
O objetivo é garantir que toda interação da UI seja consciente do ambiente ativo, sem comprometer
a separação de responsabilidades: UX é responsabilidade do frontend, autorização e isolamento
são responsabilidade do backend.

## Problema Resolvido

Antes da Fase 6, o frontend do NexTraceOne tinha as seguintes limitações:

1. **WorkspaceSwitcher** com lista de ambientes hardcoded (`['Production', 'Staging', 'Development']`)
2. **API client** enviando `X-Tenant-Id` mas não `X-Environment-Id`
3. **Sem EnvironmentContext** — nenhuma forma de propagar o ambiente ativo entre componentes
4. **AssistantPanel** sem consciência do ambiente sendo analisado
5. **Sem persistência de ambiente** entre reloads dentro da mesma aba

## Arquitetura Implementada

### Fluxo de contexto

```
AuthProvider
  └─ EnvironmentProvider
       └─ PersonaProvider
            └─ App (BrowserRouter, Routes)
                  └─ AppShell
                       ├─ AppTopbar
                       │    └─ WorkspaceSwitcher (dinâmico, usa useEnvironment())
                       ├─ EnvironmentBanner (exibido para ambientes não produtivos)
                       └─ AppContentFrame
                            └─ Outlet
                                 ├─ IncidentDetailPage → AssistantPanel (env-aware)
                                 └─ ServiceDetailPage  → AssistantPanel (env-aware)
```

### Camadas de responsabilidade

| Camada | Responsabilidade |
|---|---|
| `tokenStorage.ts` | Persistência de `nxt_eid` em sessionStorage |
| `EnvironmentContext.tsx` | Estado global do ambiente ativo e ambientes disponíveis |
| `api/client.ts` | Injeção automática do header `X-Environment-Id` em todas as requests |
| `WorkspaceSwitcher.tsx` | UX de seleção dinâmica de ambiente |
| `EnvironmentBanner.tsx` | Aviso visual para ambientes não produtivos |
| `AssistantPanel.tsx` | Grounding contextual com indicação do ambiente analisado |

## Decisões de Design

### sessionStorage vs localStorage

O `nxt_eid` é armazenado em `sessionStorage` (não `localStorage`) pelo mesmo motivo que
os outros dados de sessão: o escopo de aba garante que ambientes diferentes podem ser
abertos em abas diferentes sem interferência.

### Profile como enum tipado, não nome de ambiente

O `EnvironmentProfile` é um enum TypeScript que classifica o tipo operacional do ambiente
(`production`, `staging`, `uat`, `qa`, `development`, `sandbox`, `unknown`).
Isto permite adaptar a UX sem depender de nomes literais de ambientes, que são configurados
por tenant e podem variar.

### isProductionLike como flag explícita

O campo `isProductionLike: boolean` foi adicionado à interface `EnvironmentOption` para
que componentes como `EnvironmentBanner` e `WorkspaceSwitcher` possam adaptar a UX
sem precisar mapear nomes de perfis para categorias de risco.

### Sem segurança no frontend

Nenhuma decisão de segurança é feita no cliente. O header `X-Environment-Id` enviado
pelo API client é apenas informativo para o backend, que possui sua própria
`EnvironmentResolutionMiddleware` (implementada na Fase 2) para validar e resolver o
ambiente. O frontend apenas reflete o que o backend autoriza.

## Estrutura de Arquivos

```
src/frontend/src/
├── contexts/
│   └── EnvironmentContext.tsx          # Novo — context + provider + hooks
├── components/shell/
│   ├── WorkspaceSwitcher.tsx           # Refatorado — dinâmico
│   └── EnvironmentBanner.tsx          # Novo — banner para não-produção
├── features/ai-hub/components/
│   └── AssistantPanel.tsx             # Atualizado — props env-aware
├── features/operations/pages/
│   └── IncidentDetailPage.tsx         # Atualizado — passa env ao AssistantPanel
├── features/catalog/pages/
│   └── ServiceDetailPage.tsx          # Atualizado — passa env ao AssistantPanel
├── utils/
│   └── tokenStorage.ts                # Atualizado — storeEnvironmentId/getEnvironmentId/clearEnvironmentId
├── api/
│   └── client.ts                      # Atualizado — X-Environment-Id header
└── locales/
    ├── en.json                        # Atualizado — chaves environment.*
    ├── pt-BR.json                     # Atualizado — chaves environment.*
    ├── pt-PT.json                     # Atualizado — chaves environment.*
    └── es.json                        # Atualizado — chaves environment.*
```

## Integração com Fases Anteriores

| Fase | Contribuição |
|---|---|
| Fase 2 | `EnvironmentResolutionMiddleware` no backend; header `X-Environment-Id` nos CORS |
| Fase 5 | `X-Environment-Id` adicionado ao CORS em `ApiHost` |
| Fase 6 | Frontend envia `X-Environment-Id` e adapta UX ao ambiente ativo |

## Próximos Passos (Fase 7)

- Substituir `loadEnvironmentsForTenant()` por chamada real a `GET /api/v1/identity/environments?tenantId=X`
- Implementar cache de ambientes via React Query
- Adicionar indicadores de health e readiness por ambiente vindos do backend
- Exibir informações de ambiente na CommandPalette e nos filtros de dados
