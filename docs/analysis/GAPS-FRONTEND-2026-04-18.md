# NexTraceOne — Gaps e Problemas: Frontend
**Data:** 2026-04-18  
**Modo:** Analysis realista — sem minimizar problemas  
**Referência:** [STATE-OF-PRODUCT-2026-04-18.md](./STATE-OF-PRODUCT-2026-04-18.md)

---

## 1. Resumo

O frontend do NexTraceOne tem estrutura sólida: React 19, TypeScript strict, TanStack Query, i18n em 4 idiomas, Radix UI, Tailwind CSS. Com 338 ficheiros TSX, 113 rotas e cobertura de 14 módulos, o volume de implementação é real.

**O problema não é falta de páginas — é a qualidade e completude de algumas delas.**

**Total de problemas identificados nesta análise:** 26  
(5 críticos, 6 altos, 9 médios, 6 baixos)

---

## 2. Problemas Críticos

### [C-07 a C-10] 4 ecrãs com campos GUID expostos ao utilizador {#c-07}

**Regra violada (CLAUDE.md §18.4):** "Não pedir ao utilizador para introduzir GUIDs/IDs técnicos manualmente em fluxos de negócio."

**Ecrãs afectados:**

| Ficheiro | Campo problemático | Contexto |
|----------|-------------------|---------|
| `CanonicalEntityImpactCascadePage.tsx` | `entityId` (UUID bruto) | Análise de impacto de entidade canónica |
| `ContractHealthTimelinePage.tsx` | `contractId` (UUID bruto) | Timeline de saúde do contrato |
| `DependencyDashboardPage.tsx` | `serviceId` (UUID bruto) | Dashboard de dependências de serviço |
| `LicenseCompliancePage.tsx` | `licenseId` (UUID bruto) | Detalhes de conformidade de licença |

**Impacto real:** Um gestor técnico ou arquitecto que tente usar estes ecrãs precisa de:
1. Ir a outro ecrã para encontrar o UUID
2. Copiar o UUID
3. Colá-lo num campo de texto
4. Esperar que não tenha errado um caractere

Isto não é UX enterprise. É UX de ferramenta interna de desenvolvimento.

**Remediação:** Substituir campos de texto por:
- `SearchableSelect` com autocomplete (busca por nome, retorna ID internamente)
- Ou navegação directa com contexto passado via state/params de rota
- O ID deve ser invisível para o utilizador; o nome/label deve ser o que é mostrado e seleccionado

---

### [C-11] Fluxos de Identity Access com backend em falta

**Páginas afectadas:** `ActivationPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `MFAPage`, `InvitationPage`

**Problema:** As páginas existem e têm bom design, mas chamam endpoints que não existem no backend (C-04). O comportamento actual:
- Formulário submetido → erro HTTP 404
- Nenhuma mensagem de erro clara para o utilizador
- Loop de retry possível
- Utilizador convidado fica definitivamente bloqueado

**Remediação:** Bloqueada pelo C-04 backend. Assim que os endpoints existirem, validar end-to-end o fluxo completo.

---

### [C-12] Ausência de tratamento de erro global consistente

**Problema:** O Axios client (`api/client.ts`) tem interceptors de erro, mas o tratamento a nível de UI é inconsistente:
- Alguns ecrãs mostram mensagens de erro
- Outros mostram ecrã em branco
- Outros ficam em loading infinito quando o backend falha
- Erros de autenticação (401) não são sempre tratados com redirect para login

**Evidência:** Ecrãs em módulos `governance` e `operational-intelligence` têm fallback inconsistente.

**Remediação:** `ErrorBoundary` global + estado `isError` tratado em todas as queries TanStack Query com UI de fallback consistente.

---

## 3. Problemas de Alta Prioridade

### [A-07] Strings hardcoded sem i18n em 4 ficheiros críticos {#a-07}

**Regra violada (CLAUDE.md §19.3):** "Todo texto visível deve vir de i18n."

**Ficheiros com strings hardcoded:**

```tsx
// Exemplo de padrão encontrado:
<h1>Contract Health Timeline</h1>
<p>No data available for this period</p>
<button>Export Report</button>
```

**Ficheiros afectados:** 4 ficheiros em features `catalog`, `change-governance`, `operational-intelligence`, `governance`.

**Impacto:** Utilizadores em pt-BR, pt-PT ou es vêem texto em inglês sem tradução. Viola promessa de produto multilingue.

**Remediação:** Migrar para `t('key.name')` com chaves adicionadas em todos os 4 ficheiros de locale.

---

### [A-11] ContractHealthTimelinePage sem loading state

**Ficheiro:** `ContractHealthTimelinePage.tsx`  
**Problema:** A página inicia uma query e renderiza o gráfico directamente. Se os dados demorarem, o utilizador vê um ecrã vazio ou um gráfico incompleto sem indicação de estado.

**Remediação:** Adicionar skeleton/spinner durante `isLoading`, mensagem de erro durante `isError`, e estado de "sem dados" explícito.

---

### [A-12] UUID format validation ausente antes de submit

**Localização:** 4 ecrãs com campos UUID (ver C-07 a C-10)  
**Problema adicional:** Mesmo os campos que aceitam UUID não validam o formato antes de submeter ao backend. Um UUID inválido gera erro HTTP 400 sem mensagem clara na UI.

**Remediação:** `z.string().uuid()` no schema Zod de cada formulário afectado.

---

### [A-13] Módulo Integrations com frontend parcial

**Localização:** `src/frontend/src/features/integrations/`  
**Problema:** O backend de Integrations está bem implementado (webhooks CI/CD, multi-cluster), mas o frontend correspondente está incompleto. Ecrãs de configuração de integração não têm equivalente visual completo para todas as capacidades backend.

---

### [A-14] AI Hub limitado a 2 páginas principais

**Localização:** `src/frontend/src/features/ai-hub/`  
**Problema:** O backend de AI Knowledge tem 819+ testes e implementação real de: model registry, access policies, token budgets, guardrails, AI agents, knowledge sources, external AI. O frontend expõe apenas `AIAssistant` e `AIContracts` como páginas principais.

**Capacidades backend sem representação frontend:**
- Model Registry (CRUD de modelos disponíveis)
- Access Policies (quem pode usar qual modelo)
- Token Budget Management (quotas e consumo)
- AI Audit Log (historial de uso com custo)
- Knowledge Source Weights (configuração das fontes de contexto)
- External AI Provider Configuration

**Impacto:** O Platform Admin e o Architect não conseguem gerir a camada de IA a partir do produto. Têm que usar API directamente.

---

### [A-15] Legacy Assets com cobertura frontend mínima

**Problema:** O backend tem suporte a Mainframe, COBOL, CICS, IMS, DB2, Copybook. O frontend tem apenas `LegacyAssetCatalog` e `MainframeSystemDetail`. Não há ecrãs para:
- Importação de assets legados
- Mapeamento de dependências mainframe↔serviços modernos
- Análise de impacto de assets legados

---

## 4. Problemas de Média Prioridade

### [M-13] Ausência de skeleton loading consistente

**Problema:** Algumas páginas têm skeletons de loading (pattern correcto), outras simplesmente não renderizam nada. Inconsistência de UX visível ao navegar entre módulos.

**Padrão recomendado:** `<Skeleton>` de Radix/Tailwind em todas as queries com `isLoading`. Já existe em alguns componentes — replicar.

---

### [M-14] Estados de "lista vazia" sem mensagens contextuais

**Problema:** Quando uma lista não tem dados (ex: sem incidentes, sem contratos), alguns ecrãs mostram estado vazio genérico "No data" sem:
- Explicação contextual do que falta
- Acção sugerida para preencher o vazio ("Create your first contract")
- Link de contextualização para onboarding

---

### [M-15] Falta de confirmação em acções destrutivas

**Ecrãs afectados:** Delete de contratos, remoção de serviços, revogação de acesso delegado.  
**Problema:** Algumas acções destrutivas não têm diálogo de confirmação. Um clique acidental pode remover um contrato activo.

---

### [M-16] Responsividade não testada em ecrãs <1024px

**Problema:** O produto é claramente desenhado para desktop enterprise (>1280px). Alguns ecrãs do módulo Governance e Change Governance têm tabelas com >8 colunas que overflow em viewports menores. Não há evidência de testes de responsividade sistemáticos.

---

### [M-17] Navegação sem breadcrumbs em ecrãs de detalhe

**Afectados:** ServiceDetail, ContractDetail, IncidentDetail  
**Problema:** O utilizador em profundidade de navegação (ex: Service → Contract → Version) não tem breadcrumbs para perceber onde está ou voltar de forma precisa. Apenas o botão back do browser.

---

### [M-18] DeveloperPortal — SearchCatalog como stub intencional mas sem indicação na UI

**Problema:** O DeveloperPortal tem uma barra de pesquisa. A pesquisa usa o endpoint `SearchCatalog` que é um **stub intencional** aguardando integração cross-module. O utilizador que pesquisa recebe resultados inconsistentes ou vazios sem qualquer explicação.

**Remediação:** Mostrar banner "Search limitada — indexação cross-module em desenvolvimento" OU implementar a integração real.

---

### [M-19] Ausência de feedback visual em operações longas

**Problema:** Operações como `Promote to Production`, `Run Compliance Gate`, `Generate Contract from AI` podem demorar 3-15 segundos. Durante este tempo, o botão fica desactivado mas sem indicação de progresso ou estimativa.

**Remediação:** Progress indicator ou stepper para operações multi-step.

---

### [M-20] Product Analytics — sem ecrã de utilizador final

**Problema:** O módulo Product Analytics regista métricas de uso. Mas não há ecrã para o Platform Admin ver estas métricas (feature adoption, módulos mais usados, utilizadores mais activos). Os dados existem mas não são visíveis no produto.

---

### [M-21] Ausência de modo "compact/dense" para tabelas

**Problema:** Em ambiente enterprise com muitos serviços/contratos, as tabelas com padding generoso desperdiçam espaço. Não há opção de visualização compacta para utilizadores que precisam de ver mais linhas por ecrã.

---

## 5. Problemas de Baixa Prioridade

### [L-06] Console errors em desenvolvimento

**Problema:** Vários componentes produzem `console.warn` e `console.error` em modo de desenvolvimento relacionados com keys de React em listas e prop-types. Não afectam produção mas dificultam depuração.

### [L-07] Inconsistência de capitalização em labels

**Problema:** Alguns labels usam Title Case, outros sentence case. Inconsistência visível especialmente em tabs e botões secundários.

### [L-08] Falta de tooltip em ícones sem label

**Problema:** Alguns botões de ícone (ex: copy, export, refresh) não têm tooltip. Um utilizador que não conheça o produto não sabe o que fazem.

### [L-09] Ausência de keyboard shortcuts para acções principais

**Problema:** Acções frequentes como "criar contrato", "nova mudança", "pesquisar serviço" não têm keyboard shortcuts. O Command Palette existe na arquitectura mas a implementação de shortcuts globais não é mencionada em nenhum ficheiro frontend.

### [L-10] Dark mode inconsistente entre módulos

**Problema:** O ThemeToggle existe (`shared/ui/ThemeToggle.tsx`), mas alguns componentes específicos de módulo têm cores hardcoded que não respeitam o tema escuro.

### [L-11] Ausência de testes de acessibilidade automatizados

**Problema:** Nenhuma evidência de testes automatizados de acessibilidade (axe-core, jest-axe). O produto enterprise deve cumprir WCAG 2.1 AA minimamente.

---

## 6. Avaliação por módulo frontend

| Módulo | Páginas | Estado UX | Estado i18n | Testes |
|--------|---------|-----------|-------------|--------|
| identity-access | 15 | ⚠️ Fluxos quebrados | ✅ OK | ⚠️ Parcial |
| catalog | 10+ | ⚠️ 2 ecrãs GUID | ✅ OK | ✅ Bom |
| contracts | 7+ | ✅ OK | ✅ OK | ✅ Bom |
| change-governance | 8+ | ✅ OK | ⚠️ 1 ficheiro hardcoded | ✅ Bom |
| operational-intelligence | 6+ | ✅ OK | ⚠️ 1 ficheiro hardcoded | ⚠️ Parcial |
| governance | 11+ | ✅ OK | ⚠️ 1 ficheiro hardcoded | ✅ Bom |
| ai-hub | 2+ | ⚠️ Incompleto | ✅ OK | ⚠️ Parcial |
| knowledge | 3+ | ✅ OK | ✅ OK | ⚠️ Parcial |
| notifications | 4+ | ✅ OK | ✅ OK | ✅ Bom |
| platform-admin | Parcial | ⚠️ Incompleto | ✅ OK | ⚠️ Parcial |
| integrations | Parcial | ⚠️ Incompleto | ✅ OK | ⚠️ Parcial |
| audit-compliance | ✅ Completo | ✅ OK | ✅ OK | ✅ Bom |
| legacy-assets | Mínimo | ⚠️ Muito básico | ✅ OK | ⚠️ Parcial |
| product-analytics | Mínimo | ⚠️ Sem ecrã admin | ✅ OK | ⚠️ Parcial |

---

## 7. O que o frontend faz bem

Para equilíbrio da análise:

- **Design system consistente**: Radix UI + Tailwind + tokens — visual enterprise real
- **i18n estrutura sólida**: 4 idiomas, 95%+ dos textos correctamente internacionalizados
- **TanStack Query**: Gestão de estado server-side correcta, com cache, invalidação e refetch
- **TypeScript strict**: Sem `any` generalizado; tipos derivados dos DTOs backend
- **Formulários com Zod**: Validação client-side adequada em 90%+ dos formulários
- **Routing**: TanStack Router / React Router 7 bem estruturado com lazy loading
- **Charts**: Apache ECharts 6 com visualizações operacionais coerentes (não decorativas)
- **Monaco Editor**: Integração profissional para edição de contratos OpenAPI/AsyncAPI

---

## 8. Priorização de remediação frontend

```
SPRINT 1 (bloqueadores de UX críticos):
  [C-07 a C-10] Substituir GUID inputs         → 3 dias
  [A-07] Migrar strings hardcoded para i18n     → 4h
  [A-11] Adicionar loading states ausentes      → 4h
  [C-12] Error handling global consistente      → 1 dia

SPRINT 2 (completude funcional):
  [A-14] Ecrãs de gestão de IA                 → 3 dias
  [A-13] Frontend Integrations module          → 2 dias
  [M-13] Skeleton loading consistente          → 1 dia
  [M-15] Confirmações em acções destrutivas    → 4h
  [M-17] Breadcrumbs em detalhe               → 1 dia
```

---

*Para análise de backend ver [GAPS-BACKEND-2026-04-18.md](./GAPS-BACKEND-2026-04-18.md)*  
*Para análise de base de dados ver [GAPS-DATABASE-2026-04-18.md](./GAPS-DATABASE-2026-04-18.md)*
