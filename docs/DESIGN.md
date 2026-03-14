# DESIGN.md

## Objetivo

Definir a identidade visual oficial do NexTraceOne para garantir
consistência entre login, Home, módulos, fluxos administrativos,
experiências por persona e interfaces assistidas por IA.

------------------------------------------------------------------------

## Princípios visuais

A interface do NexTraceOne deve ser:

-   enterprise
-   sóbria
-   premium
-   clara
-   consistente
-   confiável
-   orientada à decisão
-   preparada para alta densidade de informação sem poluição visual

A experiência visual não deve parecer:

-   gamer
-   cyberpunk
-   sci-fi exagerada
-   marketing landing page
-   dashboard genérico de observabilidade

------------------------------------------------------------------------

## Direção estética

O NexTraceOne deve seguir uma identidade **dark enterprise** com:

-   fundo escuro elegante
-   gradientes suaves
-   grid discreto
-   brilho sutil em pontos de destaque
-   cards limpos
-   bordas discretas
-   hierarquia tipográfica forte
-   espaçamento generoso
-   sensação de produto corporativo real

------------------------------------------------------------------------

## Fundo oficial da aplicação

O fundo visual deve manter consistência com a tela de login aprovada.

### Características obrigatórias

-   base em azul marinho muito escuro / azul petróleo
-   gradientes suaves em azul, ciano e verde/teal
-   glow muito sutil
-   grid técnico discreto
-   profundidade visual leve
-   sem divisórias agressivas
-   sem texturas chamativas

### Regra obrigatória

Login, Home e módulos devem compartilhar a mesma família visual de
fundo.

------------------------------------------------------------------------

## Paleta principal

### Base

-   `#040D19`
-   `#061527`
-   `#0A1D33`
-   `#0D1F38`

### Superfícies / cards

-   `#0F1B2E`
-   `#132238`
-   `rgba(16, 29, 53, 0.76)`
-   `rgba(10, 21, 40, 0.82)`

### Texto

-   Primário: `#E7EEF8`
-   Secundário: `#B2BFD0`
-   Terciário: `#8FA0B7`

### Acentos

-   Cyan: `#22D3EE`
-   Teal: `#14B8A6`
-   Green: `#34D399`
-   Blue accent: `#60A5FA`

### Estados

-   Success: `#34D399`
-   Warning: `#F59E0B`
-   Error: `#F87171`
-   Info: `#93C5FD`

------------------------------------------------------------------------

## Tipografia

### Fontes recomendadas

-   Inter
-   Segoe UI
-   IBM Plex Sans

### Regras

-   títulos com peso forte e legibilidade alta
-   corpo com leitura confortável
-   evitar tipografia futurista
-   evitar excesso de contraste visual
-   priorizar clareza e contexto

------------------------------------------------------------------------

## Componentes base

### Cards

-   cantos arredondados
-   bordas discretas
-   fundo escuro translúcido/controlado
-   sombra suave
-   cabeçalho claro
-   rodapé opcional para ações

### Tabelas

-   alta legibilidade
-   linhas discretas
-   densidade controlada
-   filtros claros
-   paginação simples
-   sem visual "planilha antiga"

### Formulários

-   campos altos e legíveis
-   ícones discretos
-   foco visível
-   estados de erro consistentes
-   labels sempre via i18n

### Menu lateral

-   flat e corporativo
-   item ativo com destaque lateral sutil
-   sem glow exagerado
-   grupos bem definidos
-   ordem adaptável por persona

### Top bar

-   search contextual
-   ambiente
-   período
-   perfil
-   ações rápidas
-   design discreto

------------------------------------------------------------------------

## Design por persona

A identidade visual é a mesma para todos, mas o conteúdo muda por
persona.

### Engineer / Tech Lead

-   mais contexto operacional
-   mais densidade
-   ações rápidas visíveis

### Architect

-   mais visão estrutural
-   dependências e consistência em evidência

### Product / Executive

-   linguagem menos técnica
-   foco em risco, impacto e evolução
-   simplificação de ruído técnico

### Platform Admin / Auditor

-   layouts orientados a governança, políticas e evidência

------------------------------------------------------------------------

## Padrões para IA na UI

### Chat / Assistant

A experiência de IA deve lembrar interfaces modernas de copiloto, mas
com identidade própria.

#### Regras

-   layout limpo
-   conversas com forte contexto
-   destaque claro para fontes/artefatos usados
-   separação entre prompt, resposta, evidência e ação
-   escolha de modelo apenas para perfis autorizados
-   mostrar quando a resposta usa IA interna ou externa, se aplicável

### Contract Studio

O editor de contratos pode se inspirar na clareza do Swagger Editor, mas
deve manter a identidade do NexTraceOne.

#### Regras

-   modo assistido por IA
-   modo manual
-   boa leitura de schemas
-   diff claro
-   exemplos visíveis
-   versão e compatibilidade em destaque

------------------------------------------------------------------------

## Responsividade

### Desktop

Experiência principal.

### Tablet

Redução de densidade mantendo hierarquia.

### Mobile

Visual consistente, porém com simplificação forte. Nem todos os módulos
precisam ter a mesma profundidade funcional em mobile.

------------------------------------------------------------------------

## Regras obrigatórias

-   Toda UI deve usar i18n.
-   Nenhuma tela deve quebrar a identidade visual comum.
-   Não misturar estilos conflitantes entre módulos.
-   Toda nova tela deve parecer parte do mesmo produto.
-   Observabilidade visual nunca deve dominar a narrativa da plataforma.
-   O design deve reforçar governança, contratos, confiança em mudança e
    consistência operacional.

------------------------------------------------------------------------

## Critério de aceite visual

Uma tela do NexTraceOne está visualmente correta apenas se:

-   parecer enterprise real
-   estiver alinhada ao login aprovado
-   mantiver coerência com Home e módulos
-   usar a paleta oficial
-   preservar clareza e foco
-   não parecer cópia direta de Dynatrace, Datadog ou Grafana
