# GUIDELINE.md — NexTraceOne

## 1. Propósito

Este guideline define a linha mestra de design do **NexTraceOne** a partir da tela de login já aprovada e dos padrões visuais presentes no dashboard.  
O objetivo é garantir que qualquer nova tela, componente, fluxo ou microinteração mantenha a mesma identidade:

- **corporativa**
- **premium**
- **confiável**
- **técnica sem ser fria**
- **orientada a operação, governança e decisão**

O NexTraceOne não deve parecer um “tema genérico dark”.  
Ele precisa transmitir a sensação de uma **plataforma enterprise de missão crítica**, onde APIs, serviços, observabilidade, compliance e AIOps convivem em um mesmo ecossistema.

---

## 2. Essência da marca

### 2.1 Posicionamento visual
A interface do NexTraceOne deve comunicar:

- **controle**
- **clareza operacional**
- **segurança**
- **inteligência contextual**
- **sofisticação tecnológica**

### 2.2 Personalidade da interface
A personalidade visual e comportamental da interface deve ser:

- séria, mas não pesada
- tecnológica, mas não futurista demais
- elegante, mas não chamativa
- analítica, mas não árida
- corporativa, mas moderna

### 2.3 O que evitar
Evitar qualquer decisão que aproxime o produto de:

- dashboard “neon gamer”
- SaaS genérico de marketing
- visual excessivamente colorido
- excesso de gradientes agressivos
- componentes com aparência “toy-like”
- interfaces cheias de ruído e enfeites sem função

---

## 3. Princípios de design

### 3.1 Dark-first enterprise
O tema principal do produto é **dark**, com fundos profundos em azul petróleo / navy e superfícies elevadas em tons de azul escuro.

### 3.2 Luz como hierarquia
O contraste não deve ser construído por blocos brancos, e sim por:

- elevação de superfície
- bordas suaves
- brilhos discretos
- halos de foco
- texto de alta legibilidade

### 3.3 Accent colors com responsabilidade
O sistema visual usa poucos acentos bem controlados:

- **ciano** para foco, interação e dados ativos
- **mint/teal** para sucesso, disponibilidade e elementos de valor
- **âmbar** para atenção e risco moderado
- **coral/vermelho suave** para criticidade

A cor nunca deve ser decorativa.  
Toda cor precisa carregar intenção semântica.

### 3.4 Densidade premium
A interface deve suportar muita informação, mas com:

- respiro
- agrupamento claro
- alinhamento rigoroso
- tipografia consistente
- grid previsível

### 3.5 Operação acima de ornamentação
Toda decoração precisa reforçar a leitura do produto.  
Glows, linhas, grids, partículas e halos são permitidos apenas quando ajudam a comunicar:

- contexto técnico
- conectividade
- profundidade
- estado da plataforma

---

## 4. Direção visual base

## 4.1 Fundo
Usar fundos escuros com profundidade, preferencialmente compostos por:

- base sólida azul-marinho escuro
- gradientes lineares suaves
- halos radiais em baixa opacidade
- grid técnico muito sutil
- ruído visual mínimo

### 4.2 Superfícies
Cards, painéis, sidebars e modais devem ter:

- fundo entre 4% e 10% mais claro que o background principal
- borda translúcida suave
- sombra difusa e escura
- leve brilho interno opcional
- cantos arredondados consistentes

### 4.3 Brilho e glow
Glow deve existir, mas com disciplina:

**permitido**
- em foco de input
- em CTA primário
- em indicadores de saúde
- em gráficos e nós ativos
- em badges relevantes

**não permitido**
- glow forte em todos os cards
- bordas neon permanentes
- sombras coloridas excessivas
- competição entre vários pontos brilhantes na mesma área

---

## 5. Arquitetura visual das páginas

## 5.1 Estrutura mestre
A estrutura padrão do produto deve seguir este raciocínio:

1. **App Shell**
   - sidebar
   - topbar
   - área de contexto
   - conteúdo principal

2. **Page Header**
   - título
   - descrição curta
   - ações globais
   - filtros/contexto temporal

3. **Painéis de leitura rápida**
   - KPIs
   - status cards
   - alertas
   - indicadores operacionais

4. **Blocos analíticos**
   - tabelas
   - timelines
   - topologias
   - gráficos
   - listas priorizadas

5. **Ações e drill-down**
   - botões de abrir detalhes
   - links contextuais
   - filtros
   - navegação por módulo

## 5.2 Tela de login como origem do sistema
A tela de login define as bases de identidade do NexTraceOne:

- fundo escuro institucional
- hero forte com mensagem clara
- tipografia grande e confiante
- poucos elementos cromáticos
- CTA primário evidente
- formulário limpo, seguro e corporativo

Tudo o que vier depois no sistema deve parecer uma evolução natural dessa tela.

---

## 6. Guidelines de layout

## 6.1 Grid
Usar grid consistente em múltiplos de 8.

- 4 px para microajustes
- 8 px como unidade base
- 16 px, 24 px, 32 px, 40 px, 48 px como espaçamentos principais

## 6.2 Largura de conteúdo
Páginas analíticas devem trabalhar com containers largos, mas controlados.  
Evitar blocos excessivamente esticados que prejudiquem leitura de tabelas, gráficos e KPIs.

## 6.3 Alinhamento
O produto deve parecer preciso.  
Logo:

- alinhar títulos, cards e ações por eixos consistentes
- evitar margens improvisadas
- manter alturas de componentes compatíveis
- não misturar padrões de padding na mesma seção

## 6.4 Densidade
A densidade ideal do NexTraceOne é **média para alta**, porém nunca claustrofóbica.  
Sempre usar agrupamento visual por proximidade e contraste de superfície.

---

## 7. Guidelines de tipografia

## 7.1 Tom tipográfico
A tipografia deve transmitir:

- seriedade
- clareza
- velocidade de leitura
- precisão técnica

## 7.2 Uso por nível
- **Display/Hero**: mensagens estratégicas e login
- **Heading**: títulos de página e módulos
- **Title**: títulos de cards, painéis e blocos
- **Body**: descrições, labels, texto explicativo
- **Caption**: metadados, timestamps, hints
- **Mono**: IDs técnicos, versionamento, eventos, métricas, traces

## 7.3 Regras
- nunca usar tipografia ornamental
- preferir pesos 400, 500, 600 e 700
- evitar texto muito claro em grandes blocos longos
- usar line-height confortável para leitura em dark theme
- valores críticos e métricas podem ter peso maior, mas não devem competir com títulos

---

## 8. Guidelines de cor e semântica

## 8.1 Cor primária da experiência
O eixo cromático principal do produto é:

- navy profundo
- azul técnico
- ciano controlado
- mint sofisticado

## 8.2 Semântica obrigatória
- **success / healthy**: mint / teal
- **info / active**: cyan
- **warning / attention**: amber
- **critical / error**: coral-red
- **neutral / disabled**: slate / steel blue

## 8.3 Regras de aplicação
- nunca usar vermelho puro saturado em grandes áreas
- nunca usar verde limão
- nunca usar amarelo vibrante puro em fundos
- manter coerência de semântica em todo o sistema
- badges, linhas de gráfico e ícones devem repetir o mesmo dicionário de cor

---

## 9. Guidelines de componentes

## 9.1 Botões
### Primário
Usado em ações de avanço, entrada, confirmação e abertura de fluxo principal.

Deve ter:
- maior destaque visual
- fundo ciano/azul brilhante controlado
- texto escuro ou muito claro, conforme contraste
- hover com elevação sutil

### Secundário
Usado para ações de apoio.

Deve ter:
- fundo escuro elevado
- borda suave
- hover com brilho discreto

### Tertiário / Ghost
Usado em contexto denso, grids, toolbars e painéis.

## 9.2 Inputs
Inputs precisam parecer seguros, estáveis e técnicos.

Devem ter:
- fundo escuro mais profundo
- borda discreta
- foco com glow ciano suave
- placeholder moderadamente visível
- estados de erro e sucesso claros

Campos sensíveis, como senha, exigem:
- alternância mostrar/ocultar
- feedback de foco
- nunca exibir comportamento que comprometa segurança percebida

## 9.3 Cards
Todo card precisa ter papel claro:

- KPI
- status
- bloco analítico
- lista operacional
- painel de navegação
- painel de configuração

Evitar cards apenas decorativos.

## 9.4 Tabelas
Tabelas devem parecer enterprise:

- cabeçalho claro e estável
- linhas com zebra muito sutil, se necessário
- hover leve
- alinhamento preciso
- status e risco com chips/badges
- ações em overflow quando necessário

## 9.5 Gráficos
Gráficos devem priorizar leitura.  
Evitar visual “marketing dashboard”.

Usar:
- poucas cores
- legenda clara
- gridlines discretas
- destaque apenas no dado importante
- tooltips organizados

## 9.6 Sidebar
A sidebar é um elemento de identidade do produto.

Deve:
- reforçar orientação espacial
- suportar grupos de navegação
- mostrar item ativo com contraste claro
- permitir leitura rápida do módulo atual
- ser estável entre telas

## 9.7 Topbar
A topbar deve centralizar contexto operacional:

- ambiente/workspace atual
- busca global
- notificações
- perfil
- atalhos técnicos
- ações contextuais

---

## 10. Guidelines de motion

## 10.1 Comportamento
Motion deve comunicar:

- resposta
- continuidade
- foco
- mudança de contexto
- abertura/fechamento de camada

## 10.2 Regras
- duração curta a média
- easing suave
- sem exagero de bounce
- sem animação decorativa contínua
- sem transições que atrasem operação

## 10.3 Onde usar
- hover
- focus
- expand/collapse
- drawer
- modal
- troca de tabs
- entrada de toasts
- carregamento/skeleton
- destaque temporário em atualização de status

---

## 11. Guidelines de conteúdo

## 11.1 Tom de voz
O texto do produto deve ser:

- direto
- confiável
- técnico com clareza
- orientado à ação
- sem jargão desnecessário

## 11.2 Microcopy
Preferir textos como:
- “Abrir detalhes”
- “Ver topologia”
- “Analisar impacto”
- “Entrar com SSO/OIDC”
- “Workspace”
- “Status da plataforma”

Evitar:
- frases vagas
- mensagens promocionais dentro da aplicação
- labels genéricas como “Submit”, “Click here”, “Data”, “Info”

## 11.3 Internacionalização
A estrutura de componentes deve nascer preparada para i18n.  
Mesmo que a interface inicial esteja em português, o layout não deve quebrar com labels maiores.

---

## 12. Guidelines de acessibilidade

## 12.1 Contraste
Garantir contraste suficiente entre:

- fundo e texto
- fundo e borda
- foco e superfície
- estados críticos e neutros

## 12.2 Navegação por teclado
Todo fluxo crítico deve funcionar por teclado:

- login
- filtros
- tabelas
- modais
- menus
- abas
- ações contextuais

## 12.3 Foco visível
Foco nunca pode depender só de alteração de cor discreta.  
Usar anel, glow ou outline compatível com o sistema visual.

## 12.4 Não depender apenas de cor
Risco, erro, saúde e status devem combinar:
- cor
- ícone
- texto
- posição/contexto

---

## 13. Guidelines responsivos

## 13.1 Desktop first, mas não desktop only
O núcleo do NexTraceOne é desktop, porém o sistema deve ser responsivo com inteligência.

## 13.2 Mobile e tablet
Nem toda densidade analítica precisa ser idêntica no mobile.  
Adotar reordenação de blocos e colapso progressivo.

## 13.3 Comportamentos recomendados
- sidebar colapsável
- filtros em drawer
- cards empilháveis
- tabelas com horizontal scroll controlado ou modo lista
- topbar simplificada
- busca priorizada

---

## 14. Guidelines de consistência entre módulos

Todos os módulos do NexTraceOne devem parecer da mesma família visual:

- Overview
- Change Intelligence
- Service Catalog
- API Governance
- Dependencies / Topology
- Observability
- Incidents
- Risk Center
- Knowledge
- Administration
- Identity & Access
- Integrations
- Policies
- Settings

Cada módulo pode ter sua ênfase funcional, mas não pode reinventar:

- espaçamento
- botões
- header
- filtros
- estilos de card
- status
- navegação

---

## 15. Checklist de revisão visual

Antes de aprovar qualquer tela, validar:

### Identidade
- parece NexTraceOne?
- deriva naturalmente da tela de login?
- mantém tom enterprise premium?

### Hierarquia
- está claro o que é principal, secundário e auxiliar?
- há excesso de informação competindo?
- o CTA principal está evidente?

### Consistência
- segue grid e spacing?
- usa tokens corretos?
- repete padrões já definidos?
- evita componentes improvisados?

### Legibilidade
- títulos e labels estão claros?
- métricas estão fáceis de ler?
- contraste está adequado?

### Semântica
- as cores comunicam o estado certo?
- riscos, incidentes e saúde estão consistentes?

### Operação
- a tela ajuda a decidir e agir?
- há excesso de ornamentação?
- o caminho para drill-down está claro?

---

## 16. Regras finais

1. O login é a referência visual fundadora do produto.
2. O dashboard deve ser a expansão natural dessa linguagem.
3. Toda nova tela deve ser reconhecível como NexTraceOne à primeira vista.
4. O sistema deve priorizar clareza operacional sobre estética superficial.
5. Sofisticação visual só é válida quando aumenta percepção de qualidade, confiança e controle.

---

## 17. Definição curta

**NexTraceOne é uma plataforma enterprise de governança, observabilidade e inteligência operacional para APIs, serviços e mudanças.**  
Seu design deve comunicar exatamente isso em cada pixel.
