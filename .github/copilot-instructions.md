# Copilot Instructions v2 — NexTraceOne

## Identidade oficial do produto

O NexTraceOne é uma plataforma unificada para:

* observabilidade com change intelligence
* governança de APIs e serviços
* AIOps operacional
* Production Change Confidence
* Service Contract Governance
* Operational Consistency
* Team-owned Service Reliability
* AI-assisted Analysis, Definition and Mitigation
* Source of Truth para contratos, serviços e conhecimento operacional
* FinOps contextual por serviço, equipa, domínio e operação

### Definição curta

**NexTraceOne é a fonte de verdade para serviços, contratos, mudanças e conhecimento operacional.**

### Definição expandida

**NexTraceOne é uma plataforma unificada de governança de serviços e contratos, confiança em mudanças de produção, consistência operacional, confiabilidade dos serviços sob responsabilidade das equipas, inteligência assistida por IA e otimização operacional.**

---

## O que o NexTraceOne não é

* Não é apenas um dashboard genérico de observabilidade.
* Não é apenas um catálogo de APIs.
* Não é apenas uma ferramenta de incidentes.
* Não é apenas uma UI de contratos.
* Não é uma réplica de Dynatrace, Datadog, Grafana ou Swagger Editor.
* Não deve derivar para uma experiência genérica centrada em métricas sem contexto operacional.

Toda decisão de implementação deve reforçar o papel do produto como:

* fonte da verdade
* plataforma de governança de contratos
* plataforma de confiança em mudanças de produção
* plataforma de consistência operacional
* plataforma de assistência de IA governada e auditável

---

## Pilares oficiais do produto

1. **Service Governance**
2. **Contract Governance**
3. **Change Confidence**
4. **Operational Reliability**
5. **AI-assisted Operations**
6. **Source of Truth & Operational Knowledge**
7. **AI Governance & Developer Acceleration**
8. **Operational Intelligence & Optimization**

---

## Regra mestra de evolução

Toda alteração no repositório deve responder positivamente às perguntas abaixo:

1. Isso aproxima o sistema da visão oficial do NexTraceOne?
2. Isso fortalece o NexTraceOne como Source of Truth?
3. Isso melhora governança de contratos e definição de serviços?
4. Isso melhora confiança em mudanças de produção?
5. Isso melhora confiabilidade dos serviços sob responsabilidade das equipas?
6. Isso respeita segmentação por persona, i18n, segurança e arquitetura modular?
7. Isso evita transformar o produto em uma ferramenta genérica de observabilidade?

Se qualquer resposta for negativa, a implementação precisa ser revista.

---

## Personas oficiais do produto

As experiências do produto devem ser persona-aware.

### Personas oficiais

* Engineer
* Tech Lead
* Architect
* Product
* Executive
* Platform Admin
* Auditor

### Segmentação obrigatória por persona em todo o produto

A segmentação deve existir em:

* Home
* ordem do menu
* landing page do módulo
* escopo padrão dos dados
* linguagem da interface
* nível de detalhe
* ações disponíveis
* relatórios
* alertas
* busca
* IA
* quick actions
* exportações

### Regra importante

Não segmentar apenas por cargo literal. Considerar sempre:

* persona funcional
* escopo organizacional
* permissões
* objetivos do utilizador
* responsabilidade sobre serviços, contratos e operação

---

## Contratos e definições de serviço são pilares centrais

O NexTraceOne deve tratar contratos e definições de serviço como first-class citizens.

### Tipos de contratos cobertos pelo produto

* REST APIs
* SOAP services
* Kafka / event contracts
* Background services
* Shared schemas / DTOs / canonical objects
* Integrações internas e externas

### O produto deve ser a fonte oficial para consulta de:

* request/response de APIs
* contratos SOAP
* tópicos Kafka
* payloads/eventos
* producers e consumers
* ownership
* versionamento
* changelog
* documentação operacional e técnica

### Capacidades obrigatórias para contratos

* criação assistida por IA
* edição manual
* versionamento
* diff
* validação
* compatibilidade
* publicação
* approval workflow
* examples
* ownership
* policies
* documentação viva

### Regra obrigatória

Toda funcionalidade relacionada a APIs, eventos, integrações e serviços deve reforçar o papel do NexTraceOne como **Source of Truth**.

---

## IA interna, IA externa e governança de IA

### Princípio obrigatório

A IA interna/local é o padrão arquitetural do produto.

A IA externa deve ser opcional, governada, auditada e controlada por política.

### O produto deve suportar

* IA interna local
* integração com IAs externas
* controle por utilizador, grupo, perfil e persona
* quotas e budgets de tokens
* model registry
* permitir adicionar ou remover modelos
* atribuir modelo a utilizador, grupo, papel ou contexto
* bloquear modelos por política
* auditar uso completo
* usar IA externa de forma controlada para enriquecer a IA interna

### Casos de uso obrigatórios da IA

#### IA operacional

* investigar problemas em produção
* correlacionar incidentes
* sugerir causa provável
* recomendar mitigação
* consultar telemetry, incidents, changes, contracts, topology e runbooks

#### IA de engenharia

* gerar contratos
* sugerir schemas
* validar compatibilidade
* explicar APIs, tópicos e serviços
* acelerar desenvolvimento com contexto seguro

#### IA governada

* limitar acesso a modelos
* controlar saída de dados
* controlar tokens
* auditar prompts, respostas e contexto utilizado

### Integrações com IDE

Faz parte do escopo do produto:

* frontend estilo Copilot para uso web
* componente/extensão para Visual Studio
* componente/extensão para VS Code
* acesso governado à IA do NexTraceOne
* auditoria completa de uso
* respeito a permissões, persona e escopo

### Regra obrigatória

Nenhuma feature de IA deve ser implementada como chat genérico sem:

* contexto do produto
* segurança
* auditoria
* governança de acesso
* política de modelo
* i18n na UI

---

## Módulos oficiais do produto

### Foundation

* Identity
* Organization
* Teams
* Ownership
* Environments
* Integrations

### Services

* Service Catalog
* Team Services
* Dependencies / Topology
* Service Reliability
* Service Lifecycle

### Contracts

* API Contracts
* SOAP Contracts
* Event Contracts
* Background Service Contracts
* Contract Studio
* Versioning & Compatibility
* Contract Policies
* Publication Center

### Changes

* Change Intelligence
* Change Validation
* Production Change Confidence
* Blast Radius
* Change-to-Incident Correlation

### Operations

* Incidents & Mitigation
* Runbooks
* Operational Consistency
* AIOps Insights
* Monitoring contextualizado

### Knowledge

* Documentation & Knowledge Hub
* Source of Truth Views
* Changelog
* Operational Notes
* Search / Command Palette

### AI

* AI Assistant
* Model Registry
* AI Access Policies
* External AI Integrations
* AI Token & Budget Governance
* AI Knowledge Sources
* AI Audit & Usage
* IDE Extensions Management

### Governance

* Reports
* Risk Center
* Compliance
* FinOps
* Executive Views

---

## Estratégia de implementação

### Regra obrigatória

Não recomeçar do zero sem justificativa técnica forte.

A estratégia padrão é:

**refatoração incremental orientada por produto**

### Antes de reescrever qualquer módulo

Sempre:

1. analisar aderência do módulo à visão oficial do produto
2. identificar o que já serve
3. identificar o que precisa ser reposicionado
4. identificar o que precisa ser refatorado
5. identificar o que não existe
6. propor a menor mudança capaz de aproximar o produto da visão oficial

### Proibição

* não reescrever módulos inteiros sem necessidade
* não criar novo bounded context sem justificativa forte
* não criar microserviços prematuramente
* não criar features desconectadas da narrativa central do produto

---

## Estratégia para backend e endpoints

### Regra obrigatória

Quando uma funcionalidade de frontend ou fluxo de produto precisar de dados:

1. procurar primeiro endpoint/contrato já existente
2. reutilizar se estiver alinhado ao domínio
3. se não existir, criar no bounded context correto
4. se não for possível implementar completo, criar contrato e stub adequados
5. documentar claramente o gap e o que falta completar

### Não fazer

* não criar endpoints genéricos demais
* não concentrar responsabilidades incoesas em um único endpoint ou service
* não retornar mensagens de UX hardcoded do backend

### Fazer

* contratos estáveis
* DTOs claros
* code/messageKey/params/correlationId quando aplicável
* mapeamento coerente entre domínio, aplicação e API

---

## UI/UX obrigatória

### Identidade visual

* Toda nova tela deve respeitar a identidade visual oficial do NexTraceOne.
* O fundo visual deve ser consistente entre login, home e módulos.
* Não criar estilos conflitantes entre telas.
* Não usar aparência gamer, cyberpunk ou sci-fi exagerada.
* O visual deve ser enterprise, sóbrio, premium e coerente.

### i18n obrigatório

No frontend, absolutamente todo texto visível ao usuário deve vir de i18n.

Aplicar em:

* títulos
* labels
* menus
* subtítulos
* placeholders
* botões
* tooltips
* banners
* modais
* estados vazios
* loading
* mensagens de erro
* mensagens de sucesso
* fluxos de IA
* páginas administrativas

### Regra de UX por persona

A UI deve variar por persona em:

* conteúdo da Home
* ordem do menu
* widgets
* linguagem
* escopo de dados
* quick actions
* relatórios
* detalhes das páginas

---

## Critérios específicos por domínio

### Change Intelligence

Toda implementação deve focar em:

* consistência da alteração
* validação pós-change
* impacto em produção
* correlação com incidente
* blast radius
* confiança na mudança
* mitigação

### Service Catalog

Não deve ser apenas lista de serviços.
Deve ser a visão oficial de:

* ownership
* contratos
* dependências
* criticidade
* operação
* documentação
* histórico relevante

### Contract Studio / Contract Governance

Toda evolução deve considerar:

* criação manual e assistida por IA
* geração de artefatos reais
* diff e versionamento
* compatibilidade
* publicação
* approval workflow
* políticas e padrões

### Observabilidade / Monitoring

Nunca implementar observabilidade como fim em si mesma.
Ela deve sempre estar contextualizada por:

* equipa
* serviço
* mudança
* contrato
* incidente
* mitigação

### FinOps

No NexTraceOne, FinOps deve ser contextualizado por:

* serviço
* equipa
* domínio
* operação
* alteração
* ineficiência e desperdício operacional

Não implementar FinOps como dashboard genérico de custos cloud.

---

## Regras obrigatórias de código e arquitetura

### Idioma

* código, logs, nomes, exceptions: inglês
* documentação inline e técnica: português
* UI: i18n obrigatório

### Qualidade

* classes finais como sealed quando aplicável
* CancellationToken em toda async
* Result<T> para falhas controladas
* guard clauses no início
* strongly typed IDs
* nunca DateTime.Now
* nunca quebrar isolamento entre módulos
* preservar contratos públicos sempre que possível

### Separação de responsabilidades

* Domain sem infraestrutura
* Application sem detalhes técnicos de persistência
* API fina, apenas delegando
* Infrastructure com adapters e detalhes externos
* módulos comunicam por eventos e contratos

### Refatoração segura

* preferir pequenas refatorações incrementais
* proteger comportamento com testes
* documentar breaking changes inevitáveis

---

## Testes e documentação

Toda tarefa relevante só está completa se:

* funcionalidade estiver implementada
* testes estiverem adequados
* documentação inline estiver madura
* logs e exceptions estiverem corretos
* i18n estiver aplicado no frontend
* UX respeitar a persona e a visão oficial do produto
* a alteração aproximar o sistema da definição oficial do NexTraceOne

---

## Ordem recomendada de evolução do produto

### Núcleo obrigatório primeiro

1. Service Catalog & Ownership
2. Contract Governance
3. Source of Truth
4. Change Intelligence / Change Confidence

### Confiabilidade operacional depois

5. Team-owned Service Reliability
6. Incident Correlation & Mitigation
7. AI-assisted Analysis and Recommendations
8. Operational Consistency

### Governança e otimização depois

9. Reports by Persona
10. Compliance
11. Risk
12. FinOps contextual
13. AI Governance & IDE integrations

---

## Instrução final para qualquer agente de IA

Ao alterar qualquer parte do repositório:

* pense primeiro no produto, depois no código
* preserve a narrativa central do NexTraceOne
* trate contratos e serviços como entidades centrais
* trate IA como capacidade governada, não como feature genérica
* trate o produto como Source of Truth
* respeite segmentação por persona
* respeite i18n, segurança, auditoria e arquitetura modular
* prefira refatoração incremental com intenção clara
