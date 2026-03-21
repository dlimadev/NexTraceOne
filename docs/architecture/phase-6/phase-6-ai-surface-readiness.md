# Fase 6 — Readiness da Superfície de IA

## Consciência de Ambiente na IA

A Fase 6 prepara a superfície de IA do NexTraceOne para operar com consciência de ambiente,
sem criar dependências diretas do `EnvironmentContext` dentro do `AssistantPanel`.

### Abordagem

O `AssistantPanel` não consome `useEnvironment()` diretamente. Em vez disso, as páginas
que o utilizam (como `IncidentDetailPage` e `ServiceDetailPage`) passam os dados de
ambiente como props:

```typescript
<AssistantPanel
  contextType="incident"
  contextId={incidentId}
  contextSummary={...}
  contextData={...}
  // Props de ambiente — passadas pela página host
  activeEnvironmentId={activeEnvironment?.id}
  activeEnvironmentName={activeEnvironment?.name}
  isNonProductionEnvironment={activeEnvironment ? !activeEnvironment.isProductionLike : false}
/>
```

### Por que esta abordagem?

1. **Compatibilidade retroativa** — O `AssistantPanel` pode ser usado em contextos sem
   `EnvironmentProvider` (ex: testes, módulos independentes)
2. **Separação de responsabilidades** — O painel de IA não precisa conhecer a topologia
   de providers do app
3. **Extensibilidade** — Em fases futuras, o ambiente pode ser passado como parte do
   `contextData` para enriquecer o grounding da IA

## Impacto no Grounding da IA

Quando `isNonProductionEnvironment === true`, o `AssistantPanel` exibe:

```
⚠️ Analyzing {environmentName} (non-production)
```

Este aviso serve como sinal para o utilizador de que:

- Os dados analisados são do ambiente não produtivo selecionado
- Recomendações podem diferir das que seriam geradas para produção
- Incidentes, contratos e serviços listados são do escopo do ambiente ativo

## Roadmap de IA Contextual (Fase 7+)

| Capacidade | Status | Fase Prevista |
|---|---|---|
| Exibição do ambiente no AssistantPanel | ✅ Implementado | Fase 6 |
| `X-Environment-Id` no header das requests de IA | ✅ Implementado | Fase 6 |
| Filtragem de dados de IA por ambiente no backend | 🔲 Pendente | Fase 7 |
| Grounding explícito de ambiente no prompt da IA | 🔲 Pendente | Fase 7 |
| Alertas de risco contextual por ambiente na IA | 🔲 Pendente | Fase 8 |
| Comparação de comportamento entre ambientes via IA | 🔲 Pendente | Fase 9 |

## Integração com AI Governance

O `X-Environment-Id` injetado pelo API client garante que o backend de IA receba
o ambiente ativo em cada request. A `EnvironmentResolutionMiddleware` (Fase 2) pode
usar este header para:

1. Filtrar fontes de dados de grounding por ambiente
2. Auditar quais ambientes foram consultados na sessão de IA
3. Aplicar políticas de acesso a dados por ambiente

Este fluxo respeita o princípio de **AI Governance** do NexTraceOne:
toda consulta de IA é rastreável por tenant, ambiente, utilizador e modelo.

## Auditabilidade

Toda request ao backend de IA carrega:

| Header | Origem |
|---|---|
| `Authorization: Bearer {token}` | `getAccessToken()` — memória |
| `X-Tenant-Id` | `getTenantId()` — sessionStorage |
| `X-Environment-Id` | `getEnvironmentId()` — sessionStorage |
| `X-Csrf-Token` | `getCsrfToken()` — memória (para POST/PUT/PATCH/DELETE) |

Isto permite ao backend construir um contexto de auditoria completo para cada
interação de IA: quem consultou, em qual tenant, em qual ambiente, com qual modelo.
