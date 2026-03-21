# Phase 7 — AI Frontend Surfaces

## AiAnalysisPage

### Localização

`src/features/ai-hub/pages/AiAnalysisPage.tsx`

### Rota

`/ai/analysis` — protegida com `permission="ai:runtime:write"`

### Importação (lazy)

```tsx
const AiAnalysisPage = lazy(() =>
  import('./features/ai-hub/pages/AiAnalysisPage').then(m => ({ default: m.AiAnalysisPage }))
);
```

---

## Estrutura de 3 abas

### Tab 1: Non-Prod Risk (`non-prod`)

**Propósito**: Analisar o ambiente ativo (não-produtivo) em busca de sinais de risco.

**Comportamento**:
- Se `activeEnvironment.isProductionLike === true`: mostra mensagem informativa
- Se não-produtivo: mostra botão "Run Analysis"
- Após análise: exibe `overallRiskLevel`, `recommendation`, lista de `findings`

**Contexto usado**: `tenantId`, `activeEnvironment.id/name/profile`

### Tab 2: Compare (`compare`)

**Propósito**: Comparar o ambiente ativo com um ambiente de referência (mesmo tenant).

**Comportamento**:
- Select com ambientes disponíveis excluindo o ativo
- Após comparação: exibe `promotionRecommendation`, `summary`, lista de `divergences`

**Cores de recomendação**:
- `BLOCK_PROMOTION` → `text-red-400`
- `REVIEW_REQUIRED` → `text-yellow-400`
- `SAFE_TO_PROMOTE` → `text-green-400`

### Tab 3: Readiness (`readiness`)

**Propósito**: Avaliar readiness para promoção de um serviço específico.

**Campos de input**:
- Service name (texto livre)
- Version (semver)
- Target environment (select)

**Após avaliação**: exibe score numérico, `readinessLevel`, blockers, warnings, `shouldBlock`

---

## Integração com contextos

### AuthContext

```tsx
const { tenantId } = useAuth();
```

`tenantId` é incluído em todos os requests à API. Se `null`, exibe mensagem
de "no context available" e não renderiza as abas.

### EnvironmentContext

```tsx
const { activeEnvironment, availableEnvironments } = useEnvironment();
```

- `activeEnvironment`: fornece `id`, `name`, `profile`, `isProductionLike`
- `availableEnvironments`: usado para popular os selects de referência/target
- Se `activeEnvironment === null`: exibe mensagem e não renderiza conteúdo

---

## i18n

Todas as strings visíveis ao utilizador usam `useTranslation()`:

```tsx
const { t } = useTranslation();
// ...
{t('aiAnalysis.nonProd.runAnalysis')}
```

**Locales suportados**: `en`, `pt-BR`, `pt-PT`, `es`

Namespace: chave raiz `aiAnalysis` em todos os ficheiros de locale.

---

## Considerações de persona

A página é acessível a utilizadores com `ai:runtime:write`. O conteúdo é
relevante principalmente para:

- **Engineer**: executa análises de risco para o seu serviço
- **Tech Lead**: compara ambientes antes de aprovar promoção
- **Architect**: avalia readiness de múltiplos serviços

A UI não diferencia conteúdo por persona nesta fase — a segmentação
por persona é aplicada no nível de menu/sidebar.

---

## Sidebar

```ts
{
  labelKey: 'sidebar.aiAnalysis',
  to: '/ai/analysis',
  icon: <BarChart3 size={18} />,
  permission: 'ai:runtime:write',
  section: 'aiHub'
}
```

Posicionado após `aiAudit` na secção AI Hub.
