# Relatório de Legibilidade e Comentários — Frontend NexTraceOne

> **Tipo:** Relatório detalhado de auditoria — documentação e legibilidade do frontend  
> **Escopo:** Todos os 432 ficheiros TypeScript/React do frontend  
> **Data de referência:** Junho 2025  
> **Classificação:** Interno — Governança de Código

---

## 1. Resumo Executivo

| Métrica | Valor | Avaliação |
|---|---|---|
| Ficheiros totais | 432 (323 .tsx + 109 .ts) | — |
| Linhas de código totais | 151.764 | — |
| Blocos JSDoc/TSDoc | 728 | ⚠️ Baixo para o volume |
| Linhas de comentário inline | 728 | ⚠️ Baixo |
| Total de linhas de comentário | 1.456 | ⚠️ |
| Densidade de comentários | 0.95% | ❌ Muito baixa |
| Instâncias ESLint disable | 30 | ⚠️ Sem justificação documentada |
| Pontuação de legibilidade | 61/100 | ⚠️ Abaixo do desejável |
| Compatibilidade júnior | 60/100 | ⚠️ Abaixo do desejável |
| Idioma dos comentários | ~60% Inglês / ~40% Português | ⚠️ Inconsistente |

O frontend apresenta uma **densidade de comentários muito baixa** (< 1%), com **mistura de idiomas** e **lacunas significativas** em ficheiros de API services e páginas complexas. A legibilidade global é adequada para programadores experientes mas **insuficiente para júniores**.

---

## 2. Cobertura de JSDoc/TSDoc por Categoria

### 2.1 Tabela de cobertura

| Categoria | Total | Com JSDoc | Sem JSDoc | Cobertura | Classificação |
|---|---|---|---|---|---|
| Page Components | 96 | 64 | 32 | 66.7% | ⚠️ PARTIALLY_DOCUMENTED |
| Custom Hooks | 17 | 13 | 4 | 76.5% | ⚠️ PARTIALLY_DOCUMENTED |
| API Services | 34 | 19 | 15 | 55.9% | ❌ UNDERDOCUMENTED |
| Guard/Auth | 3 | 1+ | — | Parcial | ⚠️ PARTIALLY_DOCUMENTED |

### 2.2 Visualização de cobertura

```
Page Components  ████████████████░░░░░░░░ 66.7%
Custom Hooks     ██████████████████░░░░░░ 76.5%
API Services     █████████████░░░░░░░░░░░ 55.9%
Guard/Auth       ██████████████████░░░░░░ ~70%
```

---

## 3. Ficheiros Sem Documentação — Inventário

### 3.1 Hooks sem JSDoc (4 ficheiros)

| Hook | Localização provável | Complexidade | Prioridade |
|---|---|---|---|
| `useContractDiff` | `src/frontend/src/features/contracts/hooks/` | 🟡 Média | 🔴 Alta — contrato é pilar central |
| `useContractExport` | `src/frontend/src/features/contracts/hooks/` | 🟡 Média | 🔴 Alta — contrato é pilar central |
| `useContractTransition` | `src/frontend/src/features/contracts/hooks/` | 🟡 Média | 🔴 Alta — contrato é pilar central |
| `useSpectralRulesets` | `src/frontend/src/features/contracts/hooks/` | 🟡 Média | 🟡 Média |

**Nota:** Três dos quatro hooks não documentados pertencem ao domínio de **contratos**, que é um pilar central do NexTraceOne. Esta lacuna é particularmente relevante.

### 3.2 API Services sem JSDoc (15 ficheiros)

Esta é a **lacuna mais crítica** do frontend. Os ficheiros de API service são o ponto de integração com o backend e devem ser documentados com:
- Endpoint que consomem
- Parâmetros esperados
- Tipo de resposta
- Tratamento de erros

| # | Ficheiro (estimado) | Módulo provável | Prioridade |
|---|---|---|---|
| 1–3 | Services de Catalog | Catalog | 🔴 Alta |
| 4–6 | Services de Contract | Contract Governance | 🔴 Alta |
| 7–9 | Services de Operations | Operations | 🟡 Média |
| 10–12 | Services de AI | AI Agents | 🟡 Média |
| 13–15 | Services diversos | Vários | 🟢 Baixa |

### 3.3 Páginas sem JSDoc (32 ficheiros)

| Categoria estimada | Quantidade | Prioridade |
|---|---|---|
| Páginas principais de módulo | ~8 | 🔴 Alta |
| Páginas de detalhe/edição | ~12 | 🟡 Média |
| Páginas auxiliares/settings | ~12 | 🟢 Baixa |

---

## 4. Análise de Páginas Complexas

### 4.1 AiAssistantPage.tsx — 🔴 Muito difícil para júniores

| Métrica | Valor |
|---|---|
| Linhas de código | 1.216 |
| Hooks useState | 19 |
| Hooks useEffect | 9 |
| Blocos JSDoc | 1 |
| Funções internas | Múltiplas (estimativa: 15+) |

**Problemas identificados:**

1. **Excesso de estado local** — 19 hooks `useState` numa única página indica necessidade de refatoração (custom hook ou reducer).
2. **9 useEffect** — múltiplos efeitos laterais difíceis de rastrear sem comentários.
3. **Apenas 1 JSDoc** — para 1.216 linhas, é insuficiente.
4. **Dificuldade para júniores:** Um programador júnior precisaria de **horas** para entender o fluxo desta página.

**Recomendações específicas:**
- Extrair lógica de estado para custom hooks (e.g., `useAiAssistantState`, `useAiConversation`).
- Adicionar comentários de secção (`// --- Conversation management ---`).
- Documentar cada `useEffect` com o seu propósito.
- Adicionar JSDoc ao componente principal.

### 4.2 ServiceCatalogPage.tsx — 🟡 Moderada

| Métrica | Valor |
|---|---|
| Linhas de código | 1.010 |
| Hooks useState | 12 |
| Blocos JSDoc | 9 |

**Avaliação:** Melhor documentada que `AiAssistantPage`, com 9 blocos JSDoc. Ainda assim, 1.010 linhas com 12 estados beneficiariam de extração de lógica. A presença de JSDoc torna a página **navegável** para programadores mid-level.

### 4.3 useConfiguration.ts — 🟢 Auto-explicativo

| Métrica | Valor |
|---|---|
| Linhas de código | 123 |
| Padrão | Self-documenting |

**Avaliação:** Exemplo de código que não necessita de comentários extensos — curto, bem nomeado e com propósito claro. **Modelo a seguir.**

---

## 5. Problema: Mistura de Idiomas

### Distribuição atual

| Idioma | % dos comentários | Contextos |
|---|---|---|
| Inglês | ~60% | JSDoc técnico, nomes de funções, TODOs |
| Português | ~40% | Comentários inline, explicações de negócio |

### Impacto

1. **Inconsistência cognitiva** — o leitor alterna entre dois idiomas.
2. **Busca textual dificultada** — grep por termos de negócio pode falhar se o idioma variar.
3. **Onboarding confuso** — novos membros não sabem qual idioma usar.

### Recomendação

Definir **um idioma oficial para comentários frontend** e aplicar gradualmente:

| Opção | Prós | Contras |
|---|---|---|
| **Português** (como no backend) | Consistência com backend; equipa nativa | JSDoc em pt é incomum na comunidade React |
| **Inglês** | Standard da comunidade; melhor para open-source | Inconsistência com backend |
| **Híbrido governado** | Pragmático | Precisa de regras claras |

**Sugestão:** JSDoc em inglês (standard técnico), comentários inline em português (explicações de negócio). Documentar esta decisão.

---

## 6. Documentação de i18n

### Estado atual

| Métrica | Valor |
|---|---|
| Uso de i18n no frontend | Intensivo (sistema maduro) |
| Comentários sobre i18n | 4 |
| Ratio comentários:uso | Extremamente baixo |

### Lacunas

1. **Sem documentação** sobre a estrutura de chaves i18n.
2. **Sem guia** sobre como adicionar novas traduções.
3. **Sem explicação** das convenções de naming para chaves.
4. **Sem inventário** de namespaces i18n por módulo.

### Impacto

Um novo programador que precisa adicionar texto à UI não tem orientação sobre:
- Onde criar a chave
- Que namespace usar
- Convenção de nomes
- Como testar traduções

---

## 7. ESLint Disable — Análise

### Estado atual

| Métrica | Valor |
|---|---|
| Instâncias `eslint-disable` | 30 |
| Com justificação documentada | 0 (estimativa) |

### Recomendação

Cada `eslint-disable` deveria ter um comentário explicando o motivo:

```typescript
// eslint-disable-next-line @typescript-eslint/no-explicit-any -- API response type is dynamic and validated at runtime
const response: any = await api.get(url);
```

---

## 8. Pontuação de Legibilidade — Metodologia

### Critérios de avaliação (total: 100 pontos)

| Critério | Peso | Score atual | Notas |
|---|---|---|---|
| Densidade de comentários | 15 | 5/15 | 0.95% é muito baixo |
| Cobertura JSDoc | 15 | 10/15 | 66.7% páginas, 55.9% API services |
| Consistência de idioma | 10 | 4/10 | Mistura 60/40 |
| Complexidade de componentes | 15 | 8/15 | Páginas com 1.000+ LOC e 19 useState |
| Nomeação/legibilidade | 15 | 13/15 | Excelente — PascalCase, use{Feature} |
| Organização de ficheiros | 15 | 12/15 | Feature-based, consistente |
| Documentação de integrações | 10 | 5/10 | API services sub-documentados |
| i18n documentação | 5 | 1/5 | Quase inexistente |
| **TOTAL** | **100** | **61/100** | ⚠️ Abaixo do desejável |

### Pontuação de compatibilidade júnior

| Critério | Peso | Score | Notas |
|---|---|---|---|
| Pode entender uma página sem ajuda? | 20 | 10/20 | Páginas complexas sem comentários |
| Pode adicionar uma feature seguindo padrões? | 20 | 14/20 | Padrões existem mas não estão documentados |
| Pode integrar com o backend? | 20 | 10/20 | API services sub-documentados |
| Pode fazer i18n corretamente? | 20 | 10/20 | Sem guia de i18n |
| Pode fazer code review eficaz? | 20 | 12/20 | Nomeação boa mas lógica não comentada |
| **TOTAL** | **100** | **60/100** | ⚠️ Abaixo do desejável |

---

## 9. Recomendações Prioritizadas

### Prioridade Imediata (Semana 1)

| # | Ação | Ficheiros | Esforço | Impacto |
|---|---|---|---|---|
| 1 | Documentar 15 API services sem JSDoc | 15 | 4–6h | 🔴 Alto — integração backend |
| 2 | Documentar 4 hooks de contratos sem JSDoc | 4 | 1–2h | 🔴 Alto — pilar central do produto |
| 3 | Definir idioma oficial para comentários frontend | — | 1h | 🟡 Médio — consistência |

### Prioridade Curta (Sprint 2)

| # | Ação | Ficheiros | Esforço | Impacto |
|---|---|---|---|---|
| 4 | Documentar 32 páginas sem JSDoc | 32 | 8–12h | 🟡 Médio |
| 5 | Adicionar comentários de secção a AiAssistantPage | 1 | 2h | 🟡 Médio |
| 6 | Criar guia de i18n para programadores | 1 doc | 2h | 🟡 Médio |
| 7 | Documentar motivos de ESLint disable | 30 | 2h | 🟢 Baixo |

### Prioridade Média (1–2 meses)

| # | Ação | Esforço | Impacto |
|---|---|---|---|
| 8 | Refatorar AiAssistantPage (extrair hooks) | 4–8h | 🟡 Médio |
| 9 | Atingir 80%+ cobertura JSDoc global | 20–30h | 🟡 Médio |
| 10 | Normalizar idioma dos comentários existentes | 10–15h | 🟢 Baixo |

### Prioridade Contínua

| # | Ação | Frequência |
|---|---|---|
| 11 | Exigir JSDoc em novos ficheiros (code review) | Cada PR |
| 12 | Exigir JSDoc em API services e hooks (obrigatório) | Cada PR |
| 13 | Manter ESLint disable com justificação | Cada PR |

---

## 10. Métricas de Sucesso

| Métrica | Atual | Objetivo Sprint 2 | Objetivo Trimestral |
|---|---|---|---|
| Densidade de comentários | 0.95% | 2.0% | 3.0% |
| Cobertura JSDoc — Páginas | 66.7% | 80% | 90% |
| Cobertura JSDoc — API Services | 55.9% | 90% | 100% |
| Cobertura JSDoc — Hooks | 76.5% | 100% | 100% |
| Pontuação de legibilidade | 61/100 | 70/100 | 80/100 |
| Compatibilidade júnior | 60/100 | 70/100 | 75/100 |
| Consistência de idioma | 60/40 | 80/20 ou regra definida | Regra aplicada |

---

> **Nota:** Este relatório complementa o relatório principal `documentation-and-onboarding-audit.md`.
