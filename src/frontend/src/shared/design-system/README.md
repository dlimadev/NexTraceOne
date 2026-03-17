# NexTraceOne Design System — Guia Interno

## Visão Geral

Este diretório contém a fundação do design system do NexTraceOne em código.
A fonte de verdade para decisões visuais está nos documentos:

- `docs/DESIGN-SYSTEM.md` — tokens, specs de componentes, regras
- `docs/GUIDELINE.md` — direção visual, princípios, personalidade
- `docs/DESIGN.md` — visão de produto e blueprints de página

---

## Estrutura

```
src/shared/design-system/
├── tokens.ts        # Constantes TS para tokens (z-index, motion, breakpoints, sizes)
├── foundations.ts    # Padrões de classes reutilizáveis (surface, focus, input, etc.)
├── index.ts         # Barrel export
└── README.md        # Este ficheiro

src/index.css        # Tokens CSS oficiais (@theme block) + typography scale + globals
src/lib/cn.ts        # Utility de composição de classes (clsx + tailwind-merge)
src/components/      # Componentes base do design system
src/shared/ui/       # Barrel re-exports dos componentes
```

---

## Tokens CSS (index.css)

Todos os tokens visuais são definidos no bloco `@theme` do `index.css` e ficam
automaticamente disponíveis como utilities Tailwind CSS v4.

### Cores
| Categoria | Exemplos de utility |
|---|---|
| Surfaces | `bg-canvas`, `bg-deep`, `bg-panel`, `bg-card`, `bg-elevated`, `bg-hover` |
| Borders | `border-edge`, `border-edge-strong`, `border-edge-focus`, `bg-divider` |
| Text | `text-heading`, `text-body`, `text-muted`, `text-faded`, `text-on-accent` |
| Accent | `text-accent`, `bg-accent`, `text-cyan`, `text-mint` |
| Semantic | `text-success`, `text-warning`, `text-critical`, `text-info`, `text-danger` |
| Data Viz | `bg-data-1` a `bg-data-6` |

### Radius
`rounded-xs` (6px) · `rounded-sm` (10px) · `rounded-md` (14px) · `rounded-lg` (18px) · `rounded-xl` (24px) · `rounded-pill` (999px)

### Shadows
`shadow-xs` · `shadow-sm` · `shadow-surface` · `shadow-elevated` · `shadow-floating` · `shadow-glow-cyan` · `shadow-glow-mint` · `shadow-glow-danger`

### Spacing (base 8pt)
`spacing-1` (4px) a `spacing-16` (64px)

---

## Typography Scale (index.css)

Classes compostas de tipografia baseadas em DESIGN-SYSTEM.md §2.3:

| Classe | Size | Weight | Uso |
|---|---:|---:|---|
| `.type-display-01` | 64px | 700 | Hero/login |
| `.type-display-02` | 48px | 700 | Telas institucionais |
| `.type-heading-01` | 32px | 700 | Título de página |
| `.type-heading-02` | 24px | 600 | Subtítulo de página |
| `.type-title-01` | 20px | 600 | Títulos de seção |
| `.type-title-02` | 18px | 600 | Títulos de card |
| `.type-body-lg` | 18px | 400 | Descrição principal |
| `.type-body-md` | 16px | 400 | Texto comum |
| `.type-body-sm` | 14px | 400 | Labels, apoio |
| `.type-caption` | 12px | 500 | Metadados |
| `.type-mono-sm` | 12px | 500 | IDs, eventos, versões (mono) |
| `.type-label` | 14px | 500 | Labels de formulário |
| `.type-overline` | 11px | 600 | Overlines, categorias (uppercase) |

---

## Foundations (foundations.ts)

Padrões de classes reutilizáveis para garantir consistência:

```typescript
import { focusRingClass, surfaceClass, inputBaseClass } from '@/shared/design-system';
```

- `focusRingClass` — anel de foco acessível para elementos interativos
- `surfaceClass.panel` / `.card` / `.elevated` — superfícies com layering correto
- `inputBaseClass` — base visual para todos os inputs
- `inputDefaultBorderClass` / `inputErrorBorderClass` — estados de borda
- `interactiveSurfaceClass` — superfícies clicáveis
- `semanticBadgeClass` — estilos de badge por status semântico
- `textToneClass` — cores de texto por tom
- `transitionBase` / `transitionFast` — objetos de style para duração de transição

---

## Componentes Base

Todos os componentes em `src/components/` seguem estas regras:

1. **Tokens centralizados** — sem cores hardcoded
2. **`cn()` para composição** — `import { cn } from '@/lib/cn'`
3. **`className` externo** — sempre aceitar e aplicar via `cn()`
4. **Estados completos** — default, hover, focus, active, disabled, loading (quando aplicável)
5. **Acessibilidade** — `aria-invalid`, `aria-describedby`, `role`, focus visible
6. **forwardRef** — em todos os inputs/campos para compatibilidade com react-hook-form
7. **Transições** — via `style={{ transitionDuration: 'var(--nto-motion-fast)' }}`

### Componentes disponíveis
- **Button** — primary, secondary, danger, ghost, subtle (5 variantes, 3 tamanhos)
- **IconButton** — ghost, subtle, outline (botão de ícone com hit area mínima)
- **TextField** — input padrão com label/error/helper/icons
- **PasswordInput** — campo de password com toggle mostrar/ocultar
- **TextArea** — textarea com label/error/helper
- **Select** — select nativo estilizado
- **SearchInput** — campo de busca com ícone
- **Checkbox** — checkbox nativo estilizado
- **Switch** (Toggle) — switch com thumb animado
- **Radio** — radio button com label/description
- **Badge** — badge semântico (neutral, success, info, warning, danger)
- **Tabs** — underline e pill variants
- **Divider** — separador subtle/strong
- **Skeleton** — placeholder com shimmer
- **EmptyState** — estado vazio contextual
- **ErrorState** — estado de erro com ação de recuperação
- **Loader** — spinner animado
- **Tooltip** — tooltip CSS com suporte a teclado
- **Modal** — dialog modal com overlay
- **Drawer** — drawer lateral

### Typography components
- **Heading** — h1-h6 com scale automática
- **Text** — parágrafo/span com tone e size
- **Label** — label de formulário
- **MonoText** — texto monospace para IDs, traces, versões

---

## Convenções de Naming

### Tokens CSS
```
--color-{surface}     →  bg-canvas, bg-panel, bg-elevated
--color-{text-role}   →  text-heading, text-body, text-muted
--color-{semantic}    →  text-success, text-warning, text-critical
--radius-{scale}      →  rounded-sm, rounded-lg, rounded-pill
--shadow-{level}      →  shadow-surface, shadow-elevated, shadow-floating
--shadow-glow-{color} →  shadow-glow-cyan, shadow-glow-mint
```

### Componentes
- PascalCase para nomes de componentes
- Props tipadas com interface dedicada
- Variantes como union type literal (`'primary' | 'secondary'`)
- Tamanhos como `'sm' | 'md' | 'lg'`

---

## Integração com Formulários

Todos os campos (TextField, PasswordInput, TextArea, Select, Checkbox, Radio, Switch)
são compatíveis com `react-hook-form` via `forwardRef`.

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { TextField, PasswordInput } from '@/shared/ui';

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
});

function LoginForm() {
  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: zodResolver(schema),
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <TextField
        label="Email"
        error={errors.email?.message}
        {...register('email')}
      />
      <PasswordInput
        label="Password"
        error={errors.password?.message}
        {...register('password')}
      />
    </form>
  );
}
```
