# Fase 6 — Navegação e Contexto de UI

## WorkspaceSwitcher Dinâmico

### Antes da Fase 6

O `WorkspaceSwitcher` exibia uma lista estática e hardcoded de ambientes:

```typescript
const AVAILABLE_ENVIRONMENTS = ['Production', 'Staging', 'Development'] as const;
const DEFAULT_ENVIRONMENT = 'Production';
```

Esta abordagem apresentava os seguintes problemas:

- Ignorava a realidade de tenants com ambientes distintos (ex: UAT, QA, Sandbox)
- Não persistia a seleção de ambiente
- Não havia nenhum mecanismo para trocar de ambiente com efeito real

### Após a Fase 6

O `WorkspaceSwitcher` consome `useEnvironment()` e exibe:

1. **Ambiente ativo** — com badge colorido por `EnvironmentProfile`
2. **Lista dinâmica** — `availableEnvironments` vindo do `EnvironmentContext`
3. **Indicador de risco** — ícone `AlertTriangle` para ambientes não produtivos
4. **Seleção persistida** — `selectEnvironment(id)` persiste em `sessionStorage`

### Esquema de Cores por Perfil

| Profile | Cor |
|---|---|
| `production` | vermelho (`text-red-400`) |
| `staging` | laranja (`text-orange-400`) |
| `uat` | amarelo (`text-yellow-400`) |
| `qa` | azul (`text-blue-400`) |
| `development` | verde (`text-green-400`) |
| `sandbox` | roxo (`text-purple-400`) |
| `unknown` | padrão (`text-faded`) |

### Princípio de Design

O esquema de cores reflete o grau de risco operacional do ambiente:
tons quentes (vermelho/laranja) para ambientes próximos à produção,
tons frios (azul/verde) para ambientes de desenvolvimento e QA.

## EnvironmentBanner

O `EnvironmentBanner` é um componente não intrusivo que aparece entre o `AppTopbar`
e o `AppContentFrame` quando o ambiente ativo é não produtivo.

### Comportamento

- **Visível** quando `activeEnvironment.isProductionLike === false`
- **Oculto** quando o ambiente é produtivo ou nenhum ambiente está selecionado
- Exibe nome e perfil do ambiente via `t('environment.nonProductionBanner', ...)`
- Não bloqueia navegação — é puramente informativo
- Usa `role="status"` e `aria-live="polite"` para acessibilidade

### Localização no Layout

```
AppShell
├── AppTopbar
├── EnvironmentBanner  ← aqui
└── AppContentFrame
     └── <Outlet />
```

## Consciência de Ambiente no AssistantPanel

Quando `isNonProductionEnvironment === true`, o `AssistantPanel` exibe um badge
amarelo no header indicando que a IA está analisando um ambiente não produtivo.

Isso serve como grounding contextual: o utilizador sabe que as sugestões da IA
são baseadas em dados do ambiente selecionado, não de produção.

### Props adicionadas ao AssistantPanel

```typescript
activeEnvironmentId?: string;
activeEnvironmentName?: string;
isNonProductionEnvironment?: boolean;
```

Estas props são opcionais para preservar retrocompatibilidade.

## i18n — Chaves de Ambiente

As seguintes chaves foram adicionadas a todos os 4 locales (`en`, `pt-BR`, `pt-PT`, `es`):

```json
{
  "environment": {
    "nonProductionBanner": "...",
    "select": "...",
    "active": "...",
    "profile": {
      "production": "...",
      "staging": "...",
      "uat": "...",
      "qa": "...",
      "development": "...",
      "sandbox": "...",
      "unknown": "..."
    }
  },
  "assistantPanel": {
    "analyzingNonProd": "..."
  }
}
```

Nenhum texto de ambiente é hardcoded na UI — tudo passa por `useTranslation()`.

## Regras de UX por Ambiente

| Ambiente | Banner | Badge WorkspaceSwitcher | Aviso AssistantPanel |
|---|---|---|---|
| Production | ❌ | 🔴 vermelho | ❌ |
| Staging | ❌ | 🟠 laranja | ❌ |
| UAT | ✅ | 🟡 amarelo | ✅ |
| QA | ✅ | 🔵 azul | ✅ |
| Development | ✅ | 🟢 verde | ✅ |
| Sandbox | ✅ | 🟣 roxo | ✅ |
