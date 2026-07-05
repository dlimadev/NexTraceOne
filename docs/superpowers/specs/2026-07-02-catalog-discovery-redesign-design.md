# Redesign — Catálogo de Serviços: Jornada de Descoberta & Leitura

**Data:** 2026-07-02
**Módulo:** catalog
**Jornada:** 1 de N — Descoberta & Leitura (a criação/onboarding é a Jornada 2, num ciclo próprio)
**Estética:** Betterstack (ver [[project-betterstack-redesign]])

## 1. Contexto & problema

A `ServiceCatalogPage` atual serve várias personas ao mesmo nível e não otimiza para nenhuma:

- Aterra na aba **overview** (dashboard operacional), não numa lista navegável de serviços/APIs para usar.
- **6 abas** ao mesmo nível misturam personas: `overview / graph / impact / temporal` são ferramentas de *arquiteto/análise*; só `services` e `apis` servem o *consumidor* — e estão separadas.
- A CTA principal é **"Registar serviço"** (ação de *produtor*).
- Os **StatCards** de topo são métricas de arquiteto (edges, domains).
- Pesquisa básica (nome/equipa/domínio); **sem filtros facetados**.
- As linhas de resultado mostram pouco para decidir "é este que quero usar".

## 2. Objetivo & persona

**Persona dominante:** **Consumidor / integrador** — "preciso de encontrar um serviço/API para usar, ver os seus endpoints/contrato e saber quem é o dono para falar." A superfície continua a servir *donos* e *arquitetos* em segundo plano, mas otimiza por defeito para o consumidor.

**Dores a resolver (por prioridade do utilizador):**
- **A — Descoberta/navegação:** encontrar o serviço/contrato certo e perceber o seu estado.
- **B — Criação/onboarding:** *fora deste ciclo* (Jornada 2).
- **D — Consistência visual/densidade:** hierarquia, respiro, elegância.

Em descoberta, **A e D são a mesma coisa**: findability boa é hierarquia visual boa.

## 3. Desenho aprovado

### 3.1 Modelo de descoberta & estrutura (Abordagem 2 — browse unificado centrado no consumidor)

- **Vista por defeito = "Browse"** (encontrar algo para usar), não o dashboard operacional.
- **Unidade de descoberta = unificada, com o serviço como âncora:** uma pesquisa única devolve **serviços**; cada cartão mostra logo as **APIs/contratos que expõe**. Uma faceta **`Ver como: Serviços | APIs`** dá o atalho direto a quem já sabe o que quer.
  - *Racional:* dono, estabilidade e contacto vivem no serviço; a API sem esse contexto deixa o consumidor sem saber "posso confiar? a quem pergunto?". O serviço-âncora dá contexto; a faceta dá o atalho.
- **Ferramentas de análise realojadas:** `grafo / impacto / temporal / overview` passam para um segmento secundário **"Explorar/Analisar"** — deixam de competir com o default. O *interior* destas ferramentas **não é redesenhado** neste ciclo (só relocação).
- **CTA "Registar serviço"** desce a ação secundária (ghost).

### 3.2 Layout concreto (vista Browse, de cima para baixo)

1. **Header slim** — título + subtítulo; à direita o segmento `Browse | Explorar` e *Registar serviço* (ghost).
2. **Barra pesquisa-primeiro + facetas** — pesquisa grande/central; facetas por baixo: **Domínio · Tipo/Protocolo · Exposição (Público/Interno/Parceiro) · Ciclo (Estável/Beta/Deprecated) · Tem contrato · Equipa**. Toggle `Ver como: Serviços | APIs` + ordenação (relevância / nome / mais usados / recém-atualizados).
3. **Resultados** — grelha de cartões arejados (2–3 col em ecrã largo, lista em estreito) + **toggle de densidade** (arejado ↔ compacto; default arejado).

### 3.3 Anatomia do cartão (serviço-âncora)

```
┌────────────────────────────────────────────────────────────────┐
│ ▸ payment-service                    ● Estável    🔒 Interno     │  ← linha-scan: nome + go/no-go
│   Processa pagamentos e reembolsos                               │  ← o que faz (1 linha)
│                                                                  │
│   payments · Squad Billing · 👤 J. Silva            ⚡ Saúde OK   │  ← contexto: domínio, dono
│   ───────────────────────────────────────────────────────────  │
│   APIs:  REST /payments 📄   ·   gRPC PaymentSvc 📄   ·   +2      │  ← unidades consumíveis + badge contrato
└────────────────────────────────────────────────────────────────┘
```

**Hierarquia:**
- **Linha-scan:** nome + os 2 sinais go/no-go → **ciclo/estabilidade + exposição** (dots discretos, não badges gritantes).
- **Uma linha do que faz** (capability/descrição).
- **Contexto:** domínio + dono/equipa (*a quem pergunto*).
- **Linha consumível:** APIs/interfaces como chips, cada uma com badge de contrato (📄); `+N` colapsa o resto; afordância **"Ver contrato"** direta.

**Elegância (D):** padding generoso, bordas 1px, um só accent, secundário `muted`, mono só em tokens técnicos, dots de estado em vez de badges pesados.

### 3.4 Pesquisa & filtros (comportamento)

- Facetas **multi-seleção, combináveis, com contagem** + "limpar tudo".
- Estado dos filtros **sincronizado no URL** (link partilhável já filtrado).
- Pesquisa cobre nome/descrição/capability/rota/domínio/equipa.
- Toggle **Serviços ↔ APIs** troca a unidade do resultado mantendo as facetas aplicáveis.

### 3.5 Estados & handoff

- **Loading:** skeleton cards (mantêm layout), não spinner.
- **Sem resultados com filtros:** estado próprio — "Nada corresponde" + filtros ativos visíveis + *limpar filtros*.
- **Catálogo vazio:** EmptyState → *Registar primeiro serviço*.
- **Handoff:** cartão → detalhe do serviço existente (**não** redesenhado aqui); chip de API → deep-link para a interface/contrato; **"Ver contrato"** leva o consumidor direto ao spec.

## 4. Escopo

**Dentro deste ciclo:**
- `ServiceCatalogPage` reformulada (browse default + segmento "Explorar").
- Novos/reformulados: superfície de browse, cartão de serviço, barra de facetas, toggle Serviços/APIs, toggle densidade, estados.
- Relocação (sem redesign interno) das abas de análise sob "Explorar".

**Fora (ciclos seguintes):**
- `ContractCatalogPage` — herda a mesma linguagem no ciclo seguinte.
- Detalhe/workspace do serviço (dor C, despriorizada).
- Developer portal.
- Jornada 2 (criação/onboarding).

## 5. Honestidade de dados

Alguns sinais do cartão (**saúde**, **nº de contratos**, **exposição por interface**, **ciclo de vida**) dependem do que a API/grafo fornece hoje (`serviceCatalogApi.getGraph`, `getNodeHealth`, e a API de contratos). Regra: **onde o dado não existir, esconder o sinal (honest-null), nunca inventar**. Disponibilidade confirmada na fase de implementação; sinais em falta degradam com elegância.

## 6. Critérios de sucesso

1. Ao abrir o catálogo, o consumidor vê **por defeito** uma superfície navegável de serviços (não o dashboard operacional).
2. Um serviço pode ser encontrado por **pesquisa + facetas combináveis** (domínio/tipo/exposição/ciclo/tem-contrato/equipa), com estado no URL.
3. Cada cartão comunica de relance **go/no-go (ciclo + exposição)**, **o que faz**, **dono**, e as **APIs/contratos consumíveis** com acesso direto ao contrato.
4. As ferramentas de análise existem mas **não competem** com o default (segmento "Explorar").
5. Estados de loading/sem-resultados/vazio tratados; handoff limpo para detalhe/contrato.
6. Visualmente Betterstack: arejado, hierárquico, um accent, tokens semânticos — **zero controlos crus, zero cores hardcoded**.
7. `tsc` + `lint` + testes verdes; sinais sem dado degradam sem inventar.

## 7. Fora de âmbito (YAGNI)

- Não redesenhar o interior das ferramentas de análise.
- Não redesenhar a página de detalhe do serviço.
- Não construir criação/onboarding.
- Não inventar dados que a API não fornece.
