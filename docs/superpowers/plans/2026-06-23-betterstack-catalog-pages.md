# Catalog (Serviços) — passe de jornada v5 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`).

**Goal:** Converter os controlos HTML crus de ~24 páginas/painéis secundários do módulo catalog (serviços) para componentes DS, e cores cruas/`dark:` → tokens semânticos, sem alterar comportamento — fechando o último bloco grande do redesign Betterstack.

**Architecture:** Uma tarefa por ficheiro, subagent-driven. Quase todas as páginas têm teste dedicado (rede de segurança) que tem de continuar verde. O `PageHeader` já existe na maioria; o trabalho é controlos→DS (+ cores→tokens onde houver).

**Tech Stack:** React 19, TS 5.9, TanStack Query, Tailwind v4 tokens, DS `shared/ui`, Vitest.

## Global Constraints

- `npm run lint` + `npm run build` 0 erros; suíte total verde.
- Comportamento idêntico: preservar `useQuery`/`useMutation`, `value`/`onChange`/`disabled`, chaves i18n existentes.
- Zero `<button>/<input>/<select>/<textarea>` crus no ficheiro no fim — todos via DS de `shared/ui`.
- Cores cruas/`dark:`/`text-white` → tokens semânticos (mapa abaixo). Taxonomias intencionais (ex.: cores por tipo/método) ficam — confirmar caso a caso, mas a maioria são status/accent legacy.
- Link-como-botão: `useNavigate()` + `<Button onClick>`. NUNCA `<Link><Button>` (`Button.asChild` não implementado). `<Link>` de texto normal pode ficar.
- **PRESERVAR TESTES:** ler o teste da página primeiro; manter texto/roles/`data-testid`. Atualizar o teste só se um selector tiver mesmo de mudar (mínimo, justificado).
- Mudança cirúrgica: `git add` só do(s) path(s) explícito(s) por tarefa.

## Conversion Reference (igual ciclos 14-15)

Imports (de `features/catalog/pages/` = 3 níveis): `import { Button, IconButton, TextField, TextArea, Select, SearchInput, Tabs } from '../../../shared/ui';` + `useNavigate` se houver link-nav.
DS APIs: `Button`(`variant?:'primary'|'secondary'|'outline'|'ghost'|'danger'`,`size?:'xs'|'sm'|'md'|'lg'`,`icon?`,`loading?`,`title?` + ...rest; faz `disabled={disabled||loading}` interno). `IconButton`(EXIGE `label`; forwarda `title`/`...rest`; ghost já tem `hover:text-heading`). `TextField`(`label?`,`size?`,`error?` + ...rest). `TextArea`(inner via `textareaClassName`). `Select`(`options:{value:string;label:string}[]`,`placeholder?` renderiza opção DISABLED — para opção vazia SELECIONÁVEL pôr `{value:'',label}` em `options`; emite string → manter coerção `Number(...)` se houver). `SearchInput`(traz ícone próprio). `Tabs`(`items:{id,label,icon?}[]`,`activeId`,`onChange`,`variant="underline"`,`size`).
Patterns: accent-filled→`primary`; text-only→`ghost`; bordered→`outline`; destrutivo→`danger`; icon-only→`IconButton label`; search→`SearchInput`; tab-loop→`Tabs`; link-nav→`useNavigate`+Button. Preservar onClick/stopPropagation/guards/disabled. Traduzir labels no call site. Remover imports órfãos (grep antes).

**Mapa de cores (status/accent legacy + dark:):** `text-red-X`→`text-critical`; `text-yellow/amber-X`→`text-warning`; `text-green/emerald-X`→`text-success`; `text-blue-X`(link)→`text-accent`; `bg-blue-X text-white`(botão)→`Button variant="primary"`; badge `bg-{red,yellow,green}-100 ... dark:bg-..-900/..`→`bg-{critical,warning,success}-muted text-{critical,warning,success}`; banner `bg-red-50 border-red-200 text-red-700 dark:...`→`bg-critical-muted border border-critical/25 text-critical`. Remover SEMPRE os pares `dark:` migrados (tokens já são theme-aware). Confirmar tokens em `index.css` se incerto.

## Por tarefa — cada uma segue o MESMO ciclo de passos

Para CADA ficheiro abaixo:
1. Ler o ficheiro inteiro E o seu teste (se existir).
2. Converter todos os controlos crus → DS (Reference) + cores cruas/`dark:` → tokens (mapa). Preservar comportamento/i18n/selectors do teste.
3. `cd src/frontend && npm run lint && npm run build` → 0 erros (remover imports órfãos).
4. Confirmar limpeza: `grep -nE "<(button|input|select|textarea)\b|(red|green|blue|yellow|orange|purple|pink|indigo|emerald|amber|teal|cyan|violet|sky|rose|lime|slate|gray|zinc)-(50|100|200|300|400|500|600|700|800|900)|text-white|dark:" <ficheiro>` → sem resultados em JSX (taxonomias intencionais confirmadas podem ficar, justificar).
5. Correr o teste da página (se existir) → PASS; depois `npx vitest run src/__tests__/pages src/__tests__/catalog src/__tests__/contracts` → pass.
6. `git add` SÓ o(s) ficheiro(s) da tarefa; commit `feat(catalog): <Page> jornada v5 (controlos->DS[, cores->tokens])`.

### Wave A — Templates
- Task 1: `pages/TemplateLibraryPage.tsx` (8 controlos, 3 cores) — teste: `pages/TemplateLibraryPage.test.tsx`
- Task 2: `pages/TemplateDetailPage.tsx` (6, 1) — teste: `pages/TemplateDetailPage.test.tsx`
- Task 3: `pages/TemplateEditorPage.tsx` (16) — teste: `pages/TemplateEditorPage.test.tsx`

### Wave B — Service ops
- Task 4: `pages/ServiceDiscoveryPage.tsx` (14, 2) — teste: `pages/ServiceDiscoveryPage.test.tsx`
- Task 5: `pages/DependencyDashboardPage.tsx` (7, 9) — teste: `pages/DependencyDashboardPage.test.tsx`
- Task 6: `pages/ServiceFeatureFlagsPage.tsx` (5) — (teste se existir)
- Task 7: `pages/ServiceMaturityPage.tsx` (4) — teste: `pages/ServiceMaturityPage.test.tsx`
- Task 8: `pages/ServiceScorecardPage.tsx` (4, 1) — teste: `pages/ServiceScorecardPage.test.tsx`

### Wave C — Catalog-contracts views
- Task 9: `pages/ContractPipelinePage.tsx` (9, 6) — teste: `pages/ContractPipelinePage.test.tsx`
- Task 10: `pages/ContractListPage.tsx` (2) — testes: `pages/ContractListPage.test.tsx`, `catalog/ContractListPage.test.tsx`
- Task 11: `pages/ContractsPage.tsx` (16) — teste: `pages/ContractsPage.test.tsx`
- Task 12: `pages/CatalogContractsConfigurationPage.tsx` (13, 2) — teste: `pages/CatalogContractsConfigurationPage.test.tsx`
- Task 13: `pages/CreateServiceInterfacePage.tsx` (11) — teste: `pages/CreateServiceInterfacePage.test.tsx`

### Wave D — Dashboards / misc
- Task 14: `pages/SecurityGateDashboardPage.tsx` (7, 6) — teste: `pages/SecurityGateDashboardPage.test.tsx`
- Task 15: `pages/LicenseCompliancePage.tsx` (4, 1) — teste: `pages/LicenseCompliancePage.test.tsx`
- Task 16: `pages/DeveloperExperienceScorePage.tsx` (9) — teste: `pages/DeveloperExperienceScorePage.test.tsx`
- Task 17: `pages/AiScaffoldWizardPage.tsx` (16, 1) — teste: `pages/AiScaffoldWizardPage.test.tsx`
- Task 18: `pages/GlobalSearchPage.tsx` (2) — teste: `pages/GlobalSearchPage.test.tsx`
- Task 19: `pages/SourceOfTruthExplorerPage.tsx` (2) — teste: `pages/SourceOfTruthExplorerPage.test.tsx`

### Wave E — Residuais + painéis
- Task 20: `pages/ServiceCatalogListPage.tsx` (1) — testes: `pages/ServiceCatalogListPage.test.tsx`, `catalog/ServiceCatalogListPage.test.tsx`
- Task 21: `pages/ServiceDetailPage.tsx` (2, 2) — teste: `pages/ServiceDetailPage.test.tsx`
- Task 22: `pages/ServiceDetailPanel.tsx` (1) — (coberto por teste do pai)
- Task 23: `pages/TemporalPanel.tsx` (2) — (coberto por teste do pai)
- Task 24: `pages/ImpactPanel.tsx` (2) — (coberto por teste do pai)

### Task 25: Smoke visual + suíte total (manual checkpoint)
- `npm run test` → suíte total verde.
- `npm run dev`; abrir as páginas; confirmar controlos DS + páginas com sweep de cores corretas em dark E light.

## Self-Review
- Cobertura: todos os 24 ficheiros com controlos crus têm tarefa (Tasks 1-24). ✅
- Cores: ficheiros com cores cruas (TemplateLibrary, TemplateDetail, ServiceDiscovery, DependencyDashboard, ServiceScorecard, ContractPipeline, CatalogContractsConfiguration, SecurityGate, LicenseCompliance, AiScaffold, ServiceDetail) → mapa de cores aplicado nessas tasks. ✅
- Testes preservados: cada task corre o teste da sua página. ✅
- useNavigate/asChild, diff hygiene → Global Constraints. ✅
- Tipos/Reference consistentes com ciclos 14-15. ✅
