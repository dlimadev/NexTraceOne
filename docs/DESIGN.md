# DESIGN.md — NexTraceOne

## 1. Visão de design do produto

O NexTraceOne deve ser percebido como uma **plataforma enterprise unificada para governança, observabilidade, inteligência de mudanças e operação orientada por contexto**.

A visão de design não é apenas “fazer telas bonitas”.  
É construir uma experiência onde o usuário sente que:

- entende o estado da plataforma rapidamente
- consegue localizar APIs, serviços, riscos e incidentes com clareza
- navega por contextos complexos sem se perder
- confia na ferramenta para tomar decisão operacional
- percebe maturidade, segurança e alto nível de engenharia

A tela de login já desenhada é a fundação visual e emocional dessa experiência.

---

## 2. Papel da tela de login

A tela de login do NexTraceOne não é só uma porta de entrada.  
Ela estabelece as quatro promessas visuais do produto:

### 2.1 Promessa 1 — Plataforma séria e enterprise
O layout, o contraste, a organização e a mensagem textual posicionam a solução como produto corporativo de alto valor.

### 2.2 Promessa 2 — Tecnologia com clareza
Mesmo com visual sofisticado, a interface permanece legível, objetiva e centrada em leitura rápida.

### 2.3 Promessa 3 — Segurança e controle
O card de autenticação transmite confiança, estabilidade e sensação de ambiente gerenciado.

### 2.4 Promessa 4 — Inteligência operacional
O preview gráfico e os chips funcionais comunicam que o produto é operacional, analítico e vivo.

Tudo no restante da plataforma precisa honrar essas promessas.

---

## 3. North Star da experiência

A experiência ideal do NexTraceOne pode ser resumida assim:

> “Entrar, entender o cenário, localizar o problema ou a oportunidade e agir com confiança.”

Essa é a régua para avaliar qualquer decisão de design.

---

## 4. Pilares da experiência

## 4.1 Contexto sempre visível
O usuário precisa saber o tempo todo:

- em qual workspace/tenant está
- qual ambiente está analisando
- qual módulo está usando
- qual janela temporal está aplicada
- qual entidade está em foco

## 4.2 Hierarquia operacional
A tela deve mostrar primeiro:
- o que importa agora
- o que mudou
- o que está em risco
- o que exige ação

## 4.3 Drill-down natural
O usuário deve conseguir sair do macro para o detalhe sem ruptura visual:

- overview → domínio → serviço → API → versão → evento/política/evidência

## 4.4 Consistência entre módulos
Mesmo quando a natureza do conteúdo muda, o comportamento visual deve se manter coerente.

## 4.5 Densidade com legibilidade
A plataforma pode ser rica em dados, mas deve sempre preservar leitura e priorização.

---

## 5. Tradução da identidade em experiência

## 5.1 O que o produto deve “parecer”
- command center corporativo
- plataforma de inteligência operacional
- cockpit de governança e observabilidade
- ambiente de alta confiabilidade

## 5.2 O que o produto não deve “parecer”
- painel genérico de BI
- admin panel comum
- site de marketing escuro
- dashboard colorido sem rigor semântico

---

## 6. Direção visual macro

## 6.1 Background
Fundo escuro profundo com nuances frias, reforçando seriedade e tecnologia.

## 6.2 Surface language
Painéis escuros com borda delicada, brilho sutil e sensação de material premium.

## 6.3 Accent logic
Poucos acentos, usados para:
- foco
- saúde
- status
- risco
- chamada à ação

## 6.4 Information framing
Informação sempre enquadrada em blocos claros:
- cards
- seções
- tabelas
- timelines
- mapas
- painéis laterais

---

## 7. Arquitetura de navegação do produto

## 7.1 App shell
O shell da aplicação é o principal mecanismo de orientação.

### Sidebar
Responsável por:
- navegação primária por módulos
- leitura da estrutura do produto
- reforço do módulo atual

### Topbar
Responsável por:
- contexto de tenant/workspace
- busca global
- notificações
- atalhos
- ações de perfil e ambiente

## 7.2 Estrutura de página
Cada página do NexTraceOne deve, em regra, conter:

1. contexto da página
2. sinais principais
3. bloco analítico
4. caminhos de ação
5. profundidade opcional

---

## 8. Famílias de telas

## 8.1 Auth
Telas:
- login
- recuperação de senha
- ativação
- seleção de workspace
- SSO/OIDC
- MFA/2FA
- convite

### Regras
- composição limpa
- hero institucional consistente
- card de autenticação estável
- foco absoluto em confiança e clareza

## 8.2 Overview / Command center
Telas que consolidam:
- platform health
- mudanças recentes
- risco
- incidentes ativos
- topologia
- insights
- compliance

### Regras
- visão executiva primeiro
- cards de KPI no topo
- drill-down claro
- pouco ruído visual

## 8.3 Catálogo e inventário
Telas:
- catálogo de serviços
- catálogo de APIs
- assets técnicos
- domínios, owners, consumers, producers, versões

### Regras
- busca e filtro fortes
- tabelas enterprise
- cards de resumo
- detalhes em abas e seções

## 8.4 Governança
Telas:
- policies
- compliance center
- scorecards
- ownership
- documentação
- lifecycle
- changelog

### Regras
- status semântico muito claro
- visualização de aderência
- gaps e violações fáceis de localizar

## 8.5 Observabilidade e operação
Telas:
- telemetria
- traces
- métricas
- logs
- runbooks
- incidentes
- timeline de mudanças

### Regras
- leitura temporal forte
- filtros poderosos
- correlação visual entre eventos

## 8.6 Configuração e administração
Telas:
- organization
- identity & access
- integrações
- políticas
- settings

### Regras
- menos cenografia, mais clareza
- formulários robustos
- padrões de segurança visíveis
- feedback explícito

---

## 9. Blueprint da experiência por áreas

## 9.1 Login
### Objetivo
Entrar no sistema com segurança e confiança.

### Elementos chave
- branding
- headline clara
- prova visual de valor
- workspace
- email
- senha
- manter sessão
- recuperar senha
- CTA principal
- CTA SSO/OIDC

### Sensação desejada
“Estou entrando em uma plataforma corporativa séria, moderna e confiável.”

## 9.2 Dashboard / Overview
### Objetivo
Entender o estado atual da plataforma em poucos segundos.

### Estrutura ideal
- header com contexto
- KPI cards
- incidentes e risco
- mudanças recentes
- mapa/topologia
- insights e compliance
- links de aprofundamento

### Sensação desejada
“Consigo ver o que está saudável, o que mudou e o que exige atenção.”

## 9.3 Catálogo de serviços e APIs
### Objetivo
Descobrir, filtrar e navegar por ativos com clareza.

### Estrutura ideal
- busca poderosa
- filtros persistentes
- tabela principal
- score/status por linha
- detalhes em painel ou página dedicada

### Sensação desejada
“Tudo está catalogado, rastreável e governado.”

## 9.4 Detalhe de entidade
### Pode ser
- serviço
- API
- versão
- domínio
- incidente
- política
- evidência

### Estrutura ideal
- header rico
- status
- owner
- criticidade
- abas
- timeline
- relações/dependências
- ações

### Sensação desejada
“Tenho contexto suficiente para decidir sem caçar informação em várias telas.”

---

## 10. Regras de priorização visual

## 10.1 Em cada tela, definir explicitamente
- o que é mais importante
- o que é monitorado
- o que é ação
- o que é contexto auxiliar

## 10.2 Ordem recomendada de leitura
1. título e escopo
2. status e KPIs
3. alertas e exceções
4. conteúdo analítico
5. ações secundárias

---

## 11. Conteúdo e microcopy

## 11.1 Tom
- profissional
- direto
- sem excesso de marketing
- preciso
- claro

## 11.2 Exemplos corretos
- “Abrir incidents”
- “Ver topologia”
- “Analisar impacto”
- “Executar runbook”
- “Aplicar política”
- “Revisar evidências”

## 11.3 Exemplos ruins
- “Magic insights”
- “Awesome dashboard”
- “Try now”
- “Boost performance”
- “All good”

---

## 12. Motion e resposta

## 12.1 Função da motion
A motion do NexTraceOne existe para:
- orientar
- confirmar
- focar
- conectar contexto

## 12.2 Motion recomendada
- entrada suave de cards
- hover discreto
- drawers com transição firme
- mudança de tabs rápida
- skeletons elegantes

## 12.3 Motion proibida
- elementos pulando
- animação contínua sem propósito
- exagero de escala
- brilho pulsante em excesso

---

## 13. Segurança percebida como parte do design

Em um produto como o NexTraceOne, design não é só estética.  
É também **percepção de segurança**.

### Isso significa:
- campos de autenticação claros e confiáveis
- labels explícitos
- separação clara entre ações
- mensagens de erro seguras
- fluxo de senha e SSO bem desenhado
- nenhuma decisão visual que sugira improviso

Em especial, dados sensíveis como senha nunca podem parecer expostos, seja visualmente, seja pelo comportamento da aplicação.

---

## 14. Estratégia para evolução do produto

## 14.1 Ordem correta de expansão do design
1. autenticação
2. shell principal
3. overview
4. catálogo
5. detalhe de entidade
6. governança/compliance
7. observabilidade e incidentes
8. administração

## 14.2 Como adicionar novas telas
Toda nova tela deve responder:
- qual é seu objetivo operacional?
- qual sinal principal deve aparecer primeiro?
- qual componente existente resolve isso?
- qual padrão de navegação já deve ser reaproveitado?

Se a resposta for “vamos inventar um layout novo”, provavelmente a direção está errada.

---

## 15. Critérios de qualidade

Uma tela do NexTraceOne só está boa quando:

- parece parte de um ecossistema unificado
- é bonita sem sacrificar operação
- tem hierarquia forte
- usa cor com semântica
- tem navegação previsível
- transmite segurança
- ajuda o usuário a agir

---

## 16. Resumo executivo

**Design do NexTraceOne = comando, clareza, contexto e confiança.**

A tela de login estabelece o tom.  
O restante da plataforma deve expandir esse tom para uma experiência completa de:

- governança
- observabilidade
- inteligência de mudança
- operação enterprise
