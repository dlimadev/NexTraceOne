# NexTraceOne — Avaliação Atual do Projeto e Plano de Teste Funcional

> **Status deste documento:** preparado para avaliação do estado atual do projeto e execução de testes funcionais.
>
> **Importante:** neste ambiente não há um repositório do projeto montado para leitura direta. Por isso, este ficheiro foi gerado como **relatório-base operacional**, com inventário de módulos, critérios de avaliação e plano de testes funcional completo, para ser usado no repositório real do NexTraceOne.
>
> **Uso recomendado:** colocar este ficheiro no repositório e preencher/validar cada secção após leitura do código, execução local da aplicação e navegação pelos fluxos.

---

## 1. Objetivo

Este documento tem 3 objetivos:

1. consolidar uma visão única do **estado atual do projeto NexTraceOne**;
2. registrar uma **descrição resumida de cada módulo**;
3. fornecer um **plano de teste funcional** apenas para o que estiver realmente pronto ou testável.

---

## 2. Escala de estado usada neste documento

Usar os estados abaixo para cada módulo, submódulo e funcionalidade:

- **PRONTO** — funcionalidade implementada, navegável e testável fim a fim.
- **PARCIAL** — funcionalidade existe, mas faltam partes relevantes.
- **ESTRUTURA PRONTA** — rotas, páginas, componentes ou tipos existem, mas o fluxo ainda não está completo.
- **MOCKADO** — experiência presente, mas dependente de dados simulados.
- **PENDENTE** — capacidade prevista, porém não implementada.
- **AUSENTE** — não identificada no projeto.
- **NÃO VALIDADO** — ainda não auditado no código/execução real.

---

## 3. Como avaliar o estado atual do projeto

Antes de preencher o estado dos módulos, executar este roteiro:

### 3.1 Leitura estrutural do repositório
- identificar solução/projetos/pacotes principais;
- mapear frontend, backend, shared libs, design system e integrações;
- localizar rotas, layouts, páginas e features;
- identificar mocks, fixtures, fake APIs e adaptadores de dados.

### 3.2 Execução da aplicação
- iniciar frontend e backend locais;
- validar build e dependências;
- confirmar rotas acessíveis;
- registrar erros de runtime, warnings e páginas quebradas.

### 3.3 Navegação manual
- login;
- dashboard;
- shell/navegação;
- catálogo;
- studio/workspace;
- portal;
- governança;
- fluxos auxiliares.

### 3.4 Validação técnica mínima
- observar chamadas para backend;
- identificar o que está mockado;
- confirmar persistência real vs estado apenas local;
- verificar validação, tratamento de erros, empty states e loading states.

---

## 4. Visão executiva do projeto

### 4.1 Resumo geral
- **Nome do produto:** NexTraceOne
- **Tipo de produto:** plataforma enterprise de governança de engenharia / serviços / contratos / APIs / eventos / compliance
- **Direção esperada:** dark enterprise premium, governança forte, observabilidade, versionamento, aprovação e rastreabilidade
- **Estado global atual:** **NÃO VALIDADO NESTE AMBIENTE**

### 4.2 Perguntas que esta avaliação deve responder
- O frontend está coeso e navegável?
- O módulo de contratos/Service Studio já está funcional ou apenas estruturado?
- O projeto suporta REST, SOAP, Event APIs/Kafka e Workservices?
- O catálogo, studio, portal e governança estão realmente separados?
- Spectral, validação realtime e Canonical Entities existem de forma real ou apenas conceitual?
- O que já pode ser testado manualmente com segurança?

---

## 5. Inventário resumido de módulos

> **Nota:** as descrições abaixo refletem a visão de produto esperada. O campo **Estado atual** deve ser preenchido após auditoria real do repositório.

| Módulo | Descrição resumida | Estado atual | Observações |
|---|---|---:|---|
| Shell / Layout Global | Estrutura base da aplicação: sidebar, topbar, breadcrumbs, routing, contexto global | NÃO VALIDADO | |
| Login / Autenticação | Acesso à plataforma por credenciais e/ou SSO, sessão, logout e mensagens de erro | NÃO VALIDADO | |
| Dashboard / Painel | Visão consolidada de KPIs, alertas, widgets e ações rápidas | NÃO VALIDADO | |
| Design System / Shared UI | Componentes reutilizáveis, badges, cards, estados, formulários, tabelas e tokens visuais | NÃO VALIDADO | |
| Catálogo de Serviços/Contratos | Lista governada de APIs, eventos, serviços SOAP e workservices | NÃO VALIDADO | |
| Criação de Serviço | Wizard inicial para criar REST, SOAP, Event API ou Workservice | NÃO VALIDADO | |
| Studio / Workspace | Área central de edição, revisão e governança do serviço/contrato | NÃO VALIDADO | |
| Visual Builder REST | Criação visual de APIs REST sem depender de YAML | NÃO VALIDADO | |
| Visual Builder SOAP | Criação visual de serviços SOAP/WSDL | NÃO VALIDADO | |
| Visual Builder Event API | Criação visual de contratos de eventos/Kafka | NÃO VALIDADO | |
| Visual Builder Workservice | Modelagem visual de jobs, schedulers e workers | NÃO VALIDADO | |
| Source Editor | Edição técnica de OpenAPI/AsyncAPI/WSDL/XSD com validação | NÃO VALIDADO | |
| Endpoints / Operations | Gestão de operações, métodos, rotas, inputs/outputs, faults e exemplos | NÃO VALIDADO | |
| Schemas / Models | Gestão de schemas, referências, shared schemas e impacto de mudança | NÃO VALIDADO | |
| Shared Schemas | Catálogo de modelos reutilizáveis entre contratos | NÃO VALIDADO | |
| Security | Definição de auth, authorization model, scopes, claims, mTLS, JWT, etc. | NÃO VALIDADO | |
| Glossary | Vocabulário governado de termos funcionais e técnicos | NÃO VALIDADO | |
| Use Cases | Casos de uso, cenários, regras de negócio e exceções | NÃO VALIDADO | |
| Interactions | Exemplos, fluxos, sequências, happy path e error path | NÃO VALIDADO | |
| Versioning | Gestão de versões, comparação, breaking change e semver | NÃO VALIDADO | |
| Changelog | Histórico de alterações e notas por versão/publicação | NÃO VALIDADO | |
| Portal de Consumo | Visão de leitura/consumo do contrato separada do editor | NÃO VALIDADO | |
| Governança | Aprovações, compliance, lifecycle, policy checks, publish readiness | NÃO VALIDADO | |
| Approvals | Estados de revisão/aprovação por perfis (arquitetura, segurança, etc.) | NÃO VALIDADO | |
| Compliance / Policy Checks | Regras de naming, versioning, documentação, segurança e readiness | NÃO VALIDADO | |
| Spectral Rulesets | Gestão de rulesets Spectral, bindings e configuração de uso | NÃO VALIDADO | |
| Realtime Validation | Validação em tempo real no source editor e nos builders | NÃO VALIDADO | |
| Canonical Entities | Catálogo e governança de entidades canônicas reutilizáveis | NÃO VALIDADO | |
| Consumers / Producers | Relações de consumo e produção entre contratos/eventos/sistemas | NÃO VALIDADO | |
| Dependencies | Mapa de dependências e impacto | NÃO VALIDADO | |
| Audit Trail | Linha do tempo de alterações, publicações, aprovações e eventos | NÃO VALIDADO | |
| Integrações | Ligações com repositórios, rulesets externos, catálogo, identity providers, etc. | NÃO VALIDADO | |
| IA / Assistência | Recursos de apoio por IA para contrato, exemplos, descrição e revisão | NÃO VALIDADO | |
| Mudanças / Change Management | Controlo de mudanças e impacto operacional | NÃO VALIDADO | |
| Auditoria / Compliance Corporativo | Capacidade transversal de rastreabilidade e evidências | NÃO VALIDADO | |

---

## 6. Avaliação resumida por macroárea

### 6.1 Plataforma base
**Inclui:** login, shell, dashboard, design system, rotas, estados globais.

**Objetivo:** garantir que a plataforma oferece experiência consistente, segura e navegável.

**Estado atual:** NÃO VALIDADO

**O que validar:**
- login e logout;
- item ativo no menu;
- topbar;
- empty/loading/error states;
- responsividade mínima;
- consistência visual.

### 6.2 Módulo de contratos / Service Studio
**Inclui:** catálogo, criação, studio, builders, source editor, portal, governança.

**Objetivo:** ser o centro de modelagem, documentação, governança e publicação de contratos de serviço.

**Estado atual:** NÃO VALIDADO

**O que validar:**
- separação clara entre catálogo, studio, portal e governança;
- fluxo de criação de serviço;
- builders visuais;
- edição source;
- schemas/shared schemas;
- versioning/changelog;
- approvals/compliance;
- Spectral/canonical entities.

### 6.3 Governança técnica
**Inclui:** lifecycle, policy checks, Spectral, canonical, publish readiness, auditoria.

**Objetivo:** impedir publicação sem qualidade, padronização e aderência às políticas da organização.

**Estado atual:** NÃO VALIDADO

**O que validar:**
- regras advisory vs blocking;
- bindings de rulesets;
- visibilidade de violations;
- aderência a entidades canônicas;
- histórico/auditoria.

---

## 7. Matriz de prontidão do projeto

| Área | Status geral | Confiança | Pronto para teste funcional? | Pronto para demo? | Pronto para integração? | Pronto para produção? | Observações |
|---|---:|---:|---:|---:|---:|---:|---|
| Login | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Shell / Navegação | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Dashboard | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Catálogo | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Criação de Serviço | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Studio | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Visual Builders | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Source Editor | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Portal | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Governança | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Spectral / Validation | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Canonical Entities | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Design System / Shared UI | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Empty / Loading / Error states | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Integrações backend | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |
| Mocks / Dados de teste | NÃO VALIDADO | Baixa | Não validado | Não validado | Não validado | Não validado | |

---

## 8. Plano de teste funcional

> **Regra principal:** executar testes apenas nas áreas realmente prontas ou minimamente navegáveis. O que estiver apenas estrutural ou mockado deve ser classificado como teste exploratório ou parcial.

### 8.1 Estratégia de execução

#### Ordem recomendada
1. **Smoke tests** da plataforma base
2. **Fluxos P0** de login, shell e navegação
3. **Catálogo e acesso ao módulo de contratos**
4. **Fluxos principais do Studio**
5. **Builders visuais e source editor**
6. **Versioning / compliance / approvals**
7. **Portal de consumo**
8. **Spectral / Canonical / governança avançada**
9. **Regressão mínima**

#### Critérios de entrada
- aplicação sobe sem erros críticos de build;
- rotas principais carregam;
- existe pelo menos 1 utilizador de teste;
- existe massa mínima de dados, mockada ou real;
- ambiente local ou de teste acessível.

#### Critérios de saída
- happy path executado nas áreas marcadas como prontas;
- erros críticos classificados;
- falhas bloqueantes registadas;
- evidências recolhidas;
- regressão mínima executada.

### 8.2 Prioridades
- **P0** — login, shell, rotas críticas, catálogo, acesso ao studio
- **P1** — criação/edição principal, builders, source editor, portal
- **P2** — governança avançada, Spectral, canonical, auditoria
- **P3** — refinamentos, empty states menos críticos, UX complementar

---

## 9. Cenários de teste por módulo

### 9.1 Login e autenticação
**Objetivo:** validar acesso seguro à plataforma.

Cenários:
- carregar página de login;
- validar campos obrigatórios;
- autenticar com credenciais válidas;
- autenticar com credenciais inválidas;
- validar mensagens de erro;
- validar loading/submissão;
- logout;
- sessão expirada;
- SSO, se existir.

### 9.2 Shell / Navegação
**Objetivo:** validar que a plataforma é navegável e consistente.

Cenários:
- carregar layout principal;
- validar sidebar e topbar;
- item ativo no menu;
- navegação entre rotas;
- fallback para rota inválida;
- breadcrumbs/contexto;
- responsividade principal.

### 9.3 Dashboard
**Objetivo:** validar visão inicial da plataforma.

Cenários:
- carregamento do painel;
- KPIs/cards;
- widgets/ações rápidas;
- empty states;
- comportamento com dados mockados;
- falha de carregamento.

### 9.4 Catálogo de serviços/contratos
**Objetivo:** validar descoberta e acesso ao acervo.

Cenários:
- abrir listagem;
- pesquisa;
- filtros;
- ordenação;
- badges/estados;
- action menu;
- navegação para resumo/studio/portal;
- empty state;
- no results.

### 9.5 Criação de serviço
**Objetivo:** validar criação inicial por tipo de serviço.

Cenários:
- escolher REST/SOAP/Event API/Workservice;
- escolher modo visual ou source;
- cancelar criação;
- guardar rascunho, se existir;
- validação de campos iniciais.

### 9.6 Studio / Workspace
**Objetivo:** validar a área principal de edição e revisão.

Cenários:
- abrir resumo;
- navegar entre secções;
- validar right rail;
- editar definição;
- quick actions;
- loading/error/empty;
- persistência ou simulação de persistência.

### 9.7 Visual Builders
**Objetivo:** validar criação visual sem YAML/WSDL/AsyncAPI manual.

Cenários:
- builder REST;
- builder SOAP;
- builder Event API;
- builder Workservice;
- criação de inputs/outputs;
- validação por campo;
- geração de estrutura mínima.

### 9.8 Source Editor
**Objetivo:** validar edição técnica avançada.

Cenários:
- abrir editor;
- editar conteúdo;
- syntax highlight;
- validação em tempo real;
- mensagens de erro;
- diff/preview, se existir.

### 9.9 Schemas / Models / Shared Schemas
**Objetivo:** validar modelos de dados e reutilização.

Cenários:
- listar schemas;
- abrir detalhe;
- referências/usages;
- shared schema;
- promoção/reutilização;
- validações de estrutura.

### 9.10 Security
**Objetivo:** validar configuração de segurança do contrato.

Cenários:
- auth type;
- scopes/roles/claims;
- requisitos de segurança;
- campos obrigatórios;
- exibição no portal/studio.

### 9.11 Glossary / Use Cases / Interactions
**Objetivo:** validar documentação funcional e operacional.

Cenários:
- criar/editar/listar termos;
- criar/editar casos de uso;
- criar/editar interações;
- persistência;
- visualização no studio/portal.

### 9.12 Versioning / Changelog
**Objetivo:** validar evolução controlada do contrato.

Cenários:
- criar nova versão;
- comparar versões;
- classificar breaking change;
- changelog por versão;
- navegação entre versões.

### 9.13 Governança / Approvals / Compliance
**Objetivo:** validar readiness para review e publish.

Cenários:
- lifecycle;
- approvals;
- policy checks;
- warnings/violations;
- publish readiness;
- bloqueios.

### 9.14 Spectral / Realtime Validation
**Objetivo:** validar linting e feedback técnico.

Cenários:
- Spectral ativo;
- Spectral desativado;
- ausência de ruleset;
- ruleset blocking;
- ruleset advisory;
- validação realtime;
- navegação até ao erro.

### 9.15 Canonical Entities
**Objetivo:** validar governança de entidades canônicas.

Cenários:
- listar/cadastrar/editar canonical entity;
- associar a schema/payload;
- visualizar usages;
- warning de não aderência;
- promoção de schema para canonical, se existir.

### 9.16 Portal de Consumo
**Objetivo:** validar leitura/consumo do contrato sem mistura com o editor.

Cenários:
- overview;
- endpoints/operations;
- schemas;
- versões/changelog;
- segurança;
- examples;
- owners/support;
- comportamento com dados ausentes.

### 9.17 Audit / Dependencies / Consumers / Producers
**Objetivo:** validar rastreabilidade e impacto.

Cenários:
- linha do tempo de alterações;
- dependências;
- relações consumidor/produtor;
- visualização de impacto;
- evidências de governança.

---

## 10. Casos de teste funcionais (modelo operacional)

> Usar esta tabela como base de execução manual.

| ID | Módulo | Cenário | Prioridade | Pré-condições | Passos resumidos | Resultado esperado | Status | Evidência |
|---|---|---|---:|---|---|---|---|---|
| FT-001 | Login | Acesso à página de login | P0 | app iniciada | abrir rota de login | página carrega sem erro | A executar | |
| FT-002 | Login | Login com credenciais válidas | P0 | utilizador válido | preencher e submeter | acesso concedido | A executar | |
| FT-003 | Login | Login com credenciais inválidas | P0 | app iniciada | submeter credenciais inválidas | erro amigável sem crash | A executar | |
| FT-004 | Shell | Navegação pelo menu lateral | P0 | utilizador autenticado | clicar nos módulos principais | rotas navegam corretamente | A executar | |
| FT-005 | Dashboard | Carregamento do painel | P1 | utilizador autenticado | abrir dashboard | cards/widgets carregam | A executar | |
| FT-006 | Catálogo | Pesquisa por contrato/serviço | P0 | catálogo acessível | pesquisar termo válido | lista filtrada corretamente | A executar | |
| FT-007 | Catálogo | Filtros por tipo de serviço | P1 | catálogo acessível | aplicar filtros | itens corretos exibidos | A executar | |
| FT-008 | Studio | Abertura do resumo do serviço | P0 | item existente | abrir studio | resumo carrega | A executar | |
| FT-009 | Studio | Navegação entre secções | P1 | studio aberto | alternar secções | secções abrem sem erro | A executar | |
| FT-010 | Definição | Edição de metadados | P1 | studio aberto | editar campos e guardar | dados persistem ou feedback é exibido | A executar | |
| FT-011 | Builder REST | Criar endpoint visualmente | P1 | builder disponível | adicionar endpoint/método | estrutura mínima criada | A executar | |
| FT-012 | Source Editor | Editar source e validar | P1 | editor disponível | alterar conteúdo | validação atualiza | A executar | |
| FT-013 | Schemas | Abrir schema e ver referências | P1 | schema existente | abrir schema | detalhe e usages visíveis | A executar | |
| FT-014 | Versioning | Criar nova versão | P1 | contrato existente | acionar new version | nova versão criada/rascunho aberto | A executar | |
| FT-015 | Compliance | Ver policy checks | P1 | studio/governança acessível | abrir compliance | checks exibidos | A executar | |
| FT-016 | Spectral | Rodar linting | P2 | Spectral configurado | editar contrato | issues exibidas | A executar | |
| FT-017 | Canonical | Associar canonical entity | P2 | entidade canônica existente | associar no schema/builder | vínculo salvo e exibido | A executar | |
| FT-018 | Portal | Abrir portal de consumo | P1 | contrato publicado ou acessível | abrir portal | visão de leitura carrega | A executar | |
| FT-019 | Governança | Ver approvals/lifecycle | P1 | contrato com estado definido | abrir governança | estados exibidos corretamente | A executar | |
| FT-020 | Audit | Ver histórico do contrato | P2 | histórico disponível | abrir audit trail | timeline visível | A executar | |

---

## 11. Testes negativos obrigatórios

| ID | Área | Cenário negativo | Resultado esperado |
|---|---|---|---|
| NEG-001 | Login | submeter formulário vazio | validação de campos |
| NEG-002 | Login | credenciais inválidas | erro amigável, sem exposição técnica |
| NEG-003 | Catálogo | filtro sem resultados | estado “sem resultados” |
| NEG-004 | Studio | abrir item inexistente | erro controlado / not found |
| NEG-005 | Builder | salvar estrutura inválida | bloqueio com mensagens claras |
| NEG-006 | Source Editor | inserir source malformado | erro de parsing/validação |
| NEG-007 | Spectral | contrato sem ruleset atribuído | estado claro e não crashar |
| NEG-008 | Spectral | violation bloqueante | impedir publish/review quando aplicável |
| NEG-009 | Canonical | ausência de mapping obrigatório | warning/violation conforme regra |
| NEG-010 | Portal | contrato sem dados suficientes | empty state útil |

---

## 12. Regressão mínima recomendada

Executar sempre após alterações no módulo:

1. login;
2. navegação principal;
3. abertura do catálogo;
4. abertura de um item no studio;
5. navegação entre secções;
6. edição simples de definição;
7. abrir source editor;
8. abrir portal;
9. abrir governança/compliance;
10. validar absence de crashes em empty/loading/error states.

---

## 13. Massa de teste sugerida

### 13.1 Serviços/contratos sugeridos
- **REST API simples** — `Customer Query API v1`
- **REST API complexa** — `Payments API v2` com múltiplos endpoints
- **SOAP Service** — `Billing Integration SOAP Service`
- **Event API Producer** — `OrderCreated Producer`
- **Event API Consumer** — `OrderCreated Consumer`
- **Workservice** — `Invoice Reconciliation Worker`

### 13.2 Estados sugeridos
- 1 contrato `Draft`
- 1 contrato `In Review`
- 1 contrato `Approved`
- 1 contrato `Published`
- 1 contrato `Deprecated`

### 13.3 Qualidade/validação
- 1 contrato com `Spectral warnings`
- 1 contrato com `Spectral blocking issue`
- 1 contrato com `canonical adherence ok`
- 1 contrato com `canonical missing`
- 1 contrato com `documentation incomplete`

---

## 14. Próximos passos recomendados

### Curto prazo
- auditar o repositório real e preencher este documento;
- classificar cada módulo usando a escala definida;
- executar smoke tests da plataforma;
- validar catálogo + studio como prioridade P0/P1.

### Médio prazo
- consolidar builders e source editor;
- fechar lacunas de portal e governança;
- estabilizar compliance, Spectral e Canonical;
- reduzir dependência de mocks.

### Pré-homologação
- atualizar a matriz de prontidão;
- congelar massa de teste padrão;
- executar regressão mínima;
- abrir relatório de gaps bloqueantes.

---

## 15. Resumo executivo final

**Estado atual geral do projeto:** não verificável diretamente neste ambiente, pois o repositório não está montado aqui.

**O que este ficheiro já entrega:**
- inventário completo de módulos;
- descrição resumida de cada macrocapacidade;
- matriz de prontidão pronta para preenchimento;
- plano de teste funcional completo;
- casos de teste iniciais;
- testes negativos;
- regressão mínima;
- massa de teste sugerida.

**Próximo passo recomendado:** colocar este ficheiro no repositório do NexTraceOne e preenchê-lo a partir da leitura real do código e da execução da aplicação. Depois disso, transformar a secção de testes numa checklist de QA sprintável.

