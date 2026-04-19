# NexTraceOne — Gaps do Frontend

> **Parte da série de análise realista.** Ver [ESTADO-ATUAL-PRODUTO.md](./ESTADO-ATUAL-PRODUTO.md) para contexto.

---

## 1. Cobertura de Testes — Lacuna Crítica

### 1.1 Rácio Rotas/Testes

| Metric | Valor |
|---|---|
| Total de rotas no router | ~113 |
| Ficheiros de teste frontend | 27 |
| Páginas com teste unitário dedicado | ~26 (páginas testadas) |
| Cobertura estimada de páginas | **~23%** |

**O problema não é o número de testes — é a distribuição.** Os 27 ficheiros de teste cobrem principalmente:
- Páginas de AI Hub (copilot, assistant, integrations, etc.) — bem cobertas
- Alguns componentes base (Select, StatCard)
- Alguns contextos (EnvironmentContext)

**Módulos sem cobertura de teste:**
- `governance/` — sem ficheiro de teste dedicado em `__tests__/`
- `integrations/` — sem ficheiro de teste
- `knowledge/` — sem ficheiro de teste de página
- `notifications/` — sem ficheiro de teste
- `operations/incidents/` — sem ficheiro de teste de página
- `operations/reliability/` — sem ficheiro de teste
- `identity-access/` — sem ficheiro de teste
- `product-analytics/` — sem ficheiro de teste

### 1.2 Testes E2E (Playwright) — Cobertura Superficial

**Ficheiros E2E existentes:**
- `auth.spec.ts` — Login, forgot-password, MFA
- `navigation.spec.ts` — Sidebar, module switching
- `catalog.spec.ts` — Search, filter, create service
- `contracts.spec.ts` — Import, builder, diff
- `critical-flows.spec.ts` — Flows críticos genéricos

**Módulos sem E2E:**
- Change Governance (releases, workflows, promotion)
- Operational Intelligence (incidents, reliability)
- AI Hub (assistant, copilot, models)
- Governance (compliance, risk, FinOps)
- Knowledge Hub
- Admin (users, roles, tenants)
- Notifications
- Integrations

**Impacto:** Um refactoring no router, na autenticação, ou nos hooks base pode quebrar 8+ módulos sem que nenhum teste automatizado detete.

---

## 2. Duplicação de Código — AiAssistantPage e AiCopilotPage

**Ficheiros:**
- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/features/ai-hub/pages/AiCopilotPage.tsx`

**Problema:** Ambas as páginas implementam o mesmo padrão de chat com LLM, partilhando ~85% do código. Diferenças aparentes são cosméticas (título, persona padrão).

**Evidência:**
```typescript
// AiAssistantPage.tsx:72
const [conversations, setConversations] = useState<Conversation[]>([]);
const [messages, setMessages] = useState<ChatMessage[]>([]);
const [activeContexts, setActiveContexts] = useState<string[]>(...);

// AiCopilotPage.tsx:227 — código identico
const [conversations, setConversations] = useState<Conversation[]>([]);
const [messages, setMessages] = useState<ChatMessage[]>([]);
const [activeContexts, setActiveContexts] = useState<string[]>(...);
```

**Impacto real:** Qualquer bug encontrado numa página existe também na outra. Um fix aplicado numa não propaga automaticamente. Este padrão já aconteceu — as linhas 795 e 1022 no CopilotPage são cópias das linhas correspondentes no AssistantPage com a mesma lógica de `allModels`.

---

## 3. Estado de Conversações AI — Perda ao Navegar

**Ficheiros:**
- `AiAssistantPage.tsx:72,78`
- `AiCopilotPage.tsx:227,233`

**Problema:**
```typescript
const [conversations, setConversations] = useState<Conversation[]>([]);
const [messages, setMessages] = useState<ChatMessage[]>([]);
```

O estado de conversações e mensagens é gerido em React state local. Se o utilizador:
1. Abre o AI Assistant
2. Inicia uma conversa
3. Navega para outra página
4. Volta ao AI Assistant

**O histórico de mensagens é perdido** (exceto se a API recarregar as conversações ao montar o componente).

Verificando o código: há lógica de `fetchConversations` ao mount, mas se a API falhar ou se o utilizador tiver uma nova sessão de browser, as conversações não são restauradas a partir do estado local.

**Comparação:** O padrão correto seria usar React Query para sincronizar com a API, com cache persistente entre navegações.

---

## 4. Bug: PromotionPage — serviceName Incorreto

**Ficheiro:** `src/frontend/src/features/change-governance/pages/PromotionPage.tsx`

**Linhas 148 e 175:**
```typescript
serviceName: req.releaseId, // TODO: replace with actual service name once PromotionRequest carries service metadata
```

**Problema:** O campo `serviceName` exibido na UI de promoções está a usar `releaseId` (um UUID) como nome de serviço. O utilizador vê algo como `"3f7a8b2c-..."` onde deveria ver `"orders-api"`.

**Impacto:** A funcionalidade de promoção exibe identificadores técnicos em vez de nomes legíveis. Pode confundir utilizadores que tentam perceber qual serviço está a ser promovido.

---

## 5. Monaco Editor — Não Tem Lazy Loading

**Ficheiros que importam Monaco:**
- `ContractPipelinePage.tsx`
- Outros editores de contratos
- Possivelmente `KnowledgePage` (editor de documentação)

**Problema:** O Monaco Editor (~1.5MB de JavaScript) é carregado no bundle inicial em vez de ser lazy-loaded apenas quando o utilizador navega para uma página que usa o editor.

**Impacto:**
- Tempo de carregamento inicial aumentado para todos os utilizadores, mesmo os que nunca usam o editor
- Time-to-Interactive (TTI) aumentado
- Core Web Vitals afetados

**Solução:** Usar `React.lazy()` com `Suspense` para importar o componente Monaco.

---

## 6. Gestão de Estado — Inconsistências

### 6.1 Mix de useState e React Query

Algumas páginas usam React Query corretamente para sincronização com API:
```typescript
const { data, isLoading } = useQuery({
  queryKey: ['catalog', 'services'],
  queryFn: () => api.get('/catalog/services'),
});
```

Outras usam `useState` + `useEffect` + fetch manual, o que é mais propenso a race conditions, memory leaks, e cache stale:
```typescript
const [data, setData] = useState([]);
useEffect(() => {
  fetch('/api/...').then(r => r.json()).then(setData);
}, []);
```

**O padrão inconsistente** dificulta a manutenção e aumenta o risco de bugs de sincronização.

### 6.2 Cast `as Array<{...}>` Frequente

**Padrão encontrado em vários ficheiros AI Hub:**
```typescript
// AiPoliciesPage.tsx:52
const items = (data?.items ?? []) as Array<{
  id: string;
  name: string;
  ...
}>;

// AiAuditPage.tsx:78
const items = (data?.items ?? []) as Array<{...}>;
```

**Problema:** Cast direto com `as` ignora type safety. Se a API retornar uma estrutura diferente, o TypeScript não detecta em tempo de compilação — a aplicação lança erro em runtime.

**Correto:** Usar Zod ou interfaces TypeScript tipadas com guards, não casts diretos.

---

## 7. Testes Selenium — Autenticação Mockada

**Ficheiro:** `tests/platform/NexTraceOne.Selenium.Tests/Modules/AdminNavigationTests.cs`

**Problema:**
```csharp
public void UsersPage_Loads()
{
    MockAuthSessionWithProfileIntercept();  // <- mock de auth
    AssertPageLoadsSuccessfully("/users");
}
```

Os testes Selenium mockam a autenticação em vez de usar credenciais reais. Isto significa que:
- O flow de login nunca é testado
- A integração real entre frontend JWT e backend JWT não é validada
- Erros no ciclo de autenticação (refresh token, expiração) não são detetados

**Impacto:** Um bug no ciclo de autenticação real pode existir em produção sem ser detetado pelos testes automatizados.

---

## 8. Storybook — Ausente

**Estado:** O projeto tem um design system com 50+ componentes (`Button`, `Card`, `Modal`, `DataTable`, `StatCard`, `Combobox`, `TagEditor`, `Timeline`, etc.) mas **não tem Storybook**.

**Impacto:**
- Desenvolvedores não têm documentação viva dos componentes
- Não é possível testar componentes isoladamente no browser
- Não há forma de fazer visual regression testing automático (Chromatic, Percy, etc.)
- Novos membros da equipa não têm como explorar o design system sem ler o código

---

## 9. Visual Regression Testing — Ausente

**Estado:** Nenhum tool de visual regression (Chromatic, Percy, BackstopJS, Playwright Visual Comparisons) está configurado.

**Impacto:**
- Alterações em CSS/Tailwind podem quebrar visualmente componentes sem falhar nenhum teste
- Mudanças no design system podem ter regressões invisíveis
- Upgrades de versão do Tailwind ou Radix UI podem introduzir diferenças visuais não detetadas

---

## 10. Internacionalização (i18n) — Estado Incerto

**Configurado:** 4 idiomas (en, pt-BR, pt-PT, es)

**Problemas não verificados mas prováveis:**
- Qualidade da tradução não documentada (tradução automática vs. revisão profissional)
- Strings novas adicionadas em inglês frequentemente não são traduzidas imediatamente
- Não há validação automática de que todas as keys existem em todos os idiomas
- Não há CI check que falhe se uma tradução estiver em falta

**Risco:** Utilizadores em pt-BR ou es podem ver mistura de idiomas na interface.

---

## 11. Ficheiros de Componentes com Problemas Potenciais

### 11.1 Componentes que Importam `any`

**Ficheiro identificado por grep:** `src/frontend/src/components/...`

Vários componentes base usam `any` implícito ou explícito. Com TypeScript strict mode, isto pode ocultar erros que só aparecem em runtime.

### 11.2 CommandPalette — Funcionalidade Verificada?

**Ficheiro:** `src/frontend/src/components/CommandPalette.tsx`

A Command Palette é um componente complexo. Sem testes unitários dedicados, é difícil garantir que atalhos de teclado, navegação, e chamadas à API funcionam corretamente em todos os browsers.

---

## 12. Bundle Analysis — Estimativas

| Biblioteca | Tamanho Estimado | Carregamento |
|---|---|---|
| Monaco Editor | ~1.5MB | **Eager** (problema) |
| ECharts | ~800KB | Eager |
| React + React DOM | ~140KB | Eager (normal) |
| React Router | ~50KB | Eager (normal) |
| TanStack Query | ~40KB | Eager (normal) |

**Bundle total estimado:** 3-5MB (não minificado, não comprimido). Com compressão gzip: ~1-2MB.

**Benchmark:** Para uma SPA enterprise, <500KB gzipped é ideal. 1-2MB é aceitável mas impacta métricas de Core Web Vitals.

**Mitigação já implementada:**
- Code splitting ativo (vendor chunk separado)
- Source maps desativados em produção
- Minificação com terser ativa

**O que falta:** Lazy loading dos componentes pesados (Monaco, potencialmente ECharts).

---

## 13. Resumo de Prioridades Frontend

### Crítico

1. Corrigir `serviceName` bug na `PromotionPage.tsx:148,175`
2. Adicionar testes E2E para Change Governance (módulo flagship)
3. Lazy load do Monaco Editor

### Alto

4. Refatorar AiAssistantPage/AiCopilotPage em componente partilhado
5. Adicionar testes de páginas para Governance, Operations, Integrations
6. Corrigir casts `as Array<{...}>` por Zod schemas tipados
7. Resolver estado de conversações AI com React Query persistente

### Médio

8. Implementar Storybook para design system
9. Adicionar visual regression testing (pelo menos para componentes core)
10. Padronizar padrão de data fetching (só React Query, remover `useState` + `useEffect` manual)
11. Validação automática de keys i18n no CI
12. Corrigir Selenium tests para usar autenticação real
