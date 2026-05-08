# Copilot Instructions v3 — NexTraceOne

## Finalidade deste documento

Este documento existe para orientar qualquer agente de IA de desenvolvimento, revisão, refatoração, implementação, documentação ou suporte técnico a atuar no repositório do **NexTraceOne** com conhecimento suficiente sobre:

- visão oficial do produto
- fronteiras do domínio
- prioridades de negócio
- arquitetura alvo
- restrições técnicas
- governança
- segurança
- experiência por persona
- regras de implementação
- padrões de qualidade
- critérios de aceite
- ordem de decisão

O objetivo é impedir que o agente implemente funcionalidades “corretas tecnicamente” mas desalinhadas com a visão do produto.

---

# 1. Identidade oficial do produto

## 1.1 Definição curta

**NexTraceOne é a fonte de verdade para serviços, contratos, mudanças, operação e conhecimento operacional.**

## 1.2 Definição expandida

**NexTraceOne é uma plataforma enterprise unificada para governança de serviços e contratos, change intelligence, confiança em mudanças de produção, confiabilidade operacional orientada por equipas, inteligência operacional assistida por IA, conhecimento operacional governado e otimização contextual de operação e custo.**

## 1.3 Posicionamento oficial

O NexTraceOne deve ser entendido como uma plataforma que combina, num único sistema coerente:

- **observabilidade contextualizada**
- **change intelligence**
- **governança de APIs, eventos e serviços**
- **source of truth operacional**
- **AIOps governado**
- **knowledge hub operacional e técnico**
- **service reliability orientada por ownership**
- **FinOps contextual por serviço, domínio, equipa, ambiente e operação**

## 1.4 O que o produto resolve

O NexTraceOne existe para resolver lacunas reais e comuns em ambientes enterprise:

- equipas não sabem qual serviço é dono de quê
- contratos REST, SOAP e eventos ficam dispersos em múltiplos locais
- mudanças entram em produção sem contexto suficiente
- incidentes são analisados sem ligação clara com deploys, contratos e dependências
- observabilidade existe, mas sem semântica de negócio e sem contexto operacional real
- runbooks, notas operacionais e conhecimento técnico ficam fragmentados
- IA é usada sem governança, sem política e sem auditoria
- custo operacional e desperdício não são correlacionados com serviços e mudanças
- existe baixa confiança na promoção de mudanças entre ambientes

---

# 2. O que o NexTraceOne não é

O agente **não deve** desviar o produto para nenhum destes formatos:

- não é apenas um dashboard genérico de observabilidade
- não é apenas um catálogo de APIs
- não é apenas um repositório documental
- não é apenas uma ferramenta de incidentes
- não é apenas um editor de OpenAPI
- não é apenas um chat com LLM
- não é um clone de Dynatrace, Datadog, Grafana, SwaggerHub, Backstage ou ServiceNow
- não deve virar uma coleção de widgets sem narrativa operacional
- não deve virar um “monitoring center” genérico orientado apenas a métricas

## Regra central

Toda decisão deve reforçar o NexTraceOne como:

- **fonte da verdade**
- **plataforma de governança de serviços e contratos**
- **plataforma de confiança em mudanças de produção**
- **plataforma de consistência operacional**
- **plataforma de IA governada, auditável e útil ao contexto real**

---

# 3. Pilares oficiais do produto

Toda evolução deve reforçar um ou mais pilares abaixo.

1. **Service Governance**
2. **Contract Governance**
3. **Change Intelligence & Production Change Confidence**
4. **Operational Reliability**
5. **Operational Consistency**
6. **AI-assisted Operations & Engineering**
7. **Source of Truth & Operational Knowledge**
8. **AI Governance & Developer Acceleration**
9. **Operational Intelligence & Optimization**
10. **FinOps contextual**

---

# 4. Regra mestra de evolução

Antes de alterar qualquer ficheiro, responder internamente:

1. Isto aproxima o sistema da visão oficial do NexTraceOne?
2. Isto reforça o NexTraceOne como Source of Truth?
3. Isto melhora governança de contratos, serviços ou mudanças?
4. Isto aumenta confiança para mudanças em produção?
5. Isto melhora confiabilidade orientada por equipa e ownership?
6. Isto respeita arquitetura modular, segurança, auditoria, i18n e personas?
7. Isto evita transformar o produto numa ferramenta genérica de observabilidade?
8. Isto mantém o produto útil para ambiente enterprise self-hosted e on-prem?

Se a resposta a qualquer ponto crítico for negativa, rever a solução.

---

# 5. Princípios estruturais do produto

## 5.1 Source of Truth por desenho

O NexTraceOne deve centralizar a consulta confiável sobre:

- serviços
- ownership
- equipas
- ambientes
- contratos
- dependências
- mudanças
- incidentes
- evidências
- documentação operacional
- políticas
- conhecimento governado

## 5.2 Contratos e definições de serviço são first-class citizens

Qualquer visão de serviço sem contratos, ownership, dependências, ambiente e contexto operacional está incompleta.

## 5.3 Mudança é entidade central

Mudança não é “metadado de deploy”.
Mudança precisa ser tratada como entidade de negócio com:

- identidade própria
- origem
- escopo
- ambiente
- risco
- impacto potencial
- evidências
- correlações
- validação
- histórico

## 5.4 Observabilidade é meio, não fim

Telemetria, logs, traces e eventos existem para alimentar contexto, análise, correlação e decisão. Nunca como fim isolado.

## 5.5 IA é capacidade governada

IA não pode existir como camada paralela sem política, contexto, autorização e auditoria.

## 5.6 Persona awareness é obrigatório

O mesmo produto deve falar de forma diferente com Engineer, Tech Lead, Architect, Product, Executive, Platform Admin e Auditor.

---

# 6. Personas oficiais

## 6.1 Personas principais

- **Engineer**
- **Tech Lead**
- **Architect**
- **Product**
- **Executive**
- **Platform Admin**
- **Auditor**

## 6.2 Segmentação obrigatória por persona

A segmentação deve refletir-se em:

- home/dashboard
- ordem do menu
- landing pages de módulos
- quick actions
- nível de detalhe
- relatórios
- pesquisas
- alerts e insights
- conteúdo de IA
- escopo padrão de dados
- métricas priorizadas
- linguagem usada na UI

## 6.3 Regra importante

Nunca segmentar apenas por nome do cargo.
Considerar sempre:

- papel funcional
- escopo organizacional
- responsabilidade operacional
- permissões reais
- ownership
- contexto do ambiente
- objetivos daquela persona

---

# 7. Módulos oficiais do produto

O agente deve preservar coerência entre estes módulos e não inventar módulos redundantes sem necessidade forte.

## 7.1 Foundation

- Identity
- Organization
- Teams
- Ownership
- Environments
- Integrations
- Licensing & Entitlements
- Audit & Traceability

## 7.2 Services

- Service Catalog
- Team Services
- Dependencies / Topology
- Service Reliability
- Service Lifecycle
- Service Metadata & Classification

## 7.3 Contracts

- API Contracts
- SOAP Contracts
- Event Contracts
- Background Service Contracts
- Contract Studio
- Versioning & Compatibility
- Publication Center
- Contract Policies
- Examples & Schemas

## 7.4 Changes

- Change Intelligence
- Change Validation
- Promotion Governance
- Production Change Confidence
- Blast Radius
- Change-to-Incident Correlation
- Release Identity
- Evidence Pack
- Rollback Intelligence
- Release Calendar

## 7.5 Operations

- Incidents & Mitigation
- Runbooks
- Operational Consistency
- AIOps Insights
- Monitoring contextualizado
- Post-change Verification

## 7.6 Knowledge

- Documentation & Knowledge Hub
- Source of Truth Views
- Changelog
- Operational Notes
- Search / Command Palette
- Knowledge Relations

## 7.7 AI

- AI Assistant
- AI Agents
- Model Registry
- AI Access Policies
- External AI Integrations
- AI Token & Budget Governance
- AI Knowledge Sources
- AI Audit & Usage
- IDE Extensions Management

## 7.8 Governance

- Reports
- Risk Center
- Compliance
- FinOps
- Executive Views
- Policy Management

---

# 8. Contratos e definições de serviço

## 8.1 Tipos de contratos cobertos

O NexTraceOne deve tratar como contratos de primeira classe:

- REST APIs
- SOAP services / WSDL
- Kafka / event contracts
- AsyncAPI
- background services e jobs relevantes
- shared schemas / canonical DTOs
- integrações internas e externas
- webhooks
- consumidores e produtores de eventos

## 8.2 O que deve ser possível consultar

O sistema deve ser capaz de responder, com contexto:

- quem é o owner do contrato
- qual serviço publica
- quem consome
- qual versão está em uso
- quais breaking changes existem
- quais exemplos e payloads são válidos
- quais políticas se aplicam
- quais ambientes usam aquela versão
- quais mudanças recentes tocaram no contrato
- quais incidentes ou anomalias podem estar correlacionados

## 8.3 Capacidades obrigatórias

- criação manual
- criação assistida por IA
- importação e exportação
- versionamento
- diff semântico
- validação
- análise de compatibilidade
- examples
- ownership
- publication workflow
- approval workflow
- políticas e linting
- documentação viva

## 8.4 Regra obrigatória

Sempre que uma feature tocar API, evento, serviço, schema ou integração, ela deve fortalecer o NexTraceOne como **source of truth dos contratos e do comportamento esperado**.

---

# 9. Mudanças, releases e ambientes

## 9.1 Mudança é um domínio central

Toda mudança relevante precisa ter, quando aplicável:

- identificador próprio
- serviço afetado
- ambiente alvo
- versão/release identity
- janela de mudança
- tipo de alteração
- risco
- blast radius potencial
- evidências
- aprovações
- estado de promoção
- vínculo com deploy ou ingestão externa

## 9.2 Ambientes são first-class citizens

O produto deve distinguir explicitamente:

- Development
- Pre-Production / Non-Production
- Production

E também suportar múltiplos ambientes por tenant/organização.

## 9.3 Regra crítica do produto

A análise de comportamento em **ambientes não produtivos** é mandatória para ajudar a impedir que problemas cheguem à produção.

Isto deve impactar:

- análise de risco
- change confidence
- relatórios comparativos
- validação pós-change
- critérios de promoção
- insights de IA
- recomendação de mitigação

## 9.4 Capacidades obrigatórias em mudanças

- ingestão de eventos de deploy/change
- associação a serviço e ambiente
- correlação com incidentes
- análise de impacto
- blast radius básico e evolutivo
- scoring de confiança
- validação pós-change
- evidence pack
- approval workflow
- promotion gates por ambiente
- rollback intelligence
- release calendar com janelas e restrições
- detecção de mudança sem contrato correspondente quando aplicável

---

# 10. Observabilidade e telemetria

## 10.1 Regra de produto

Observabilidade no NexTraceOne **nunca** deve ser implementada como dashboards genéricos soltos.
Ela deve estar contextualizada por:

- serviço
- equipa
- ownership
- contrato
- versão
- ambiente
- mudança
- incidente
- runbook
- mitigação

## 10.2 Fontes de dados relevantes

O produto deve considerar como fontes válidas:

- traces OpenTelemetry
- logs estruturados
- métricas
- eventos de deploy/change
- eventos de pipeline
- eventos Kafka
- logs IIS
- logs de aplicações .NET
- telemetry proveniente de collector
- dados recolhidos via CLR profiler quando aplicável

## 10.3 Armazenamento e direção arquitetural

Nas conversas do projeto, a direção evoluiu para privilegiar **Elasticsearch** como base principal para workloads analíticos e de observabilidade, em vez de dependência da stack Loki/Tempo/Prometheus como centro do produto. ClickHouse permanece como opção alternativa para cenários específicos.

Isto significa:

- a observabilidade deve ser desenhada com forte capacidade analítica
- queries históricas e correlações devem ser eficientes
- custos e performance de retenção importam
- a modelação dos dados deve servir análise operacional e change intelligence

## 10.4 Regras de implementação

- não acoplar o produto a uma única stack visual externa
- abstrair ingestão e storage quando fizer sentido
- preservar semântica do domínio sobre a telemetria
- permitir variações de implantação conforme contexto do cliente
- suportar IIS, Docker e evolução futura para Kubernetes

---

# 11. IA interna, IA externa e governança de IA

## 11.1 Princípio principal

A **IA interna/local** é o padrão arquitetural preferencial do produto.

A **IA externa** é opcional, governada, auditável, controlada por política e usada quando permitido.

## 11.2 O que o produto deve suportar

- modelos internos
- modelos externos
- escolha de modelo por utilizador, grupo, papel, contexto ou agente
- model registry
- quotas e budgets de tokens
- políticas de acesso por ambiente e por persona
- bloqueio de modelos por política
- auditoria completa de prompts, contexto, resposta e custo
- armazenamento de conhecimento produzido para reduzir dependência futura de vendors

## 11.3 Casos de uso obrigatórios

### IA operacional

- investigar problema em produção
- correlacionar incidente com mudança
- sugerir causa provável
- recomendar mitigação
- consultar telemetry, changes, topology, contracts, runbooks e notas operacionais

### IA de engenharia

- gerar contratos REST
- gerar contratos SOAP
- gerar contratos de eventos e AsyncAPI
- sugerir schemas e exemplos
- validar compatibilidade
- explicar impacto de alteração
- apoiar cenários de teste
- acelerar desenvolvimento com contexto governado

### IA governada

- controlar quem pode usar qual modelo
- controlar quais dados podem sair para modelos externos
- aplicar budget de tokens
- auditar uso completo
- registrar contexto e justificativa quando necessário

## 11.4 Agentes especializados

O NexTraceOne deve evoluir para suportar agentes especializados, por exemplo:

- agente de criação de contrato REST
- agente de criação de contrato SOAP
- agente de contrato de eventos/Kafka/AsyncAPI
- agente de análise de change impact
- agente de investigação operacional
- agente de geração de cenários de teste
- agente de sumarização de incidentes e evidências

Também deve permitir que utilizadores criem agentes próprios sob governança.

## 11.5 Regra obrigatória

Nenhuma feature de IA deve ser construída como chat genérico sem:

- contexto do produto
- política de acesso
- auditoria
- observabilidade de uso
- governança de modelo
- i18n na UI
- respeito por tenant, ambiente, persona e permissões

---

# 12. Extensões IDE e developer acceleration

Faz parte do escopo do produto:

- experiência estilo copiloto via web
- integração/extensão para Visual Studio
- integração/extensão para VS Code
- possibilidade futura de integração com outros ambientes de desenvolvimento
- uso governado da IA do NexTraceOne a partir do IDE
- auditoria de uso no IDE
- respeito a permissões, persona, escopo, ambiente e política de modelo

O agente nunca deve tratar estas integrações como “extra cosmético”; elas fazem parte da proposta de valor de aceleração segura.

---

# 13. FinOps contextual

## 13.1 Regra de produto

FinOps no NexTraceOne não é dashboard genérico de custo cloud.

Deve ser contextualizado por:

- serviço
- equipa
- domínio
- ambiente
- operação
- versão
- mudança
- ineficiência
- desperdício operacional

## 13.2 Objetivo

Permitir que custo seja interpretado em conjunto com:

- comportamento operacional
- confiabilidade
- anomalias
- uso indevido
- regressões após mudança
- impacto por domínio ou equipa

---

# 14. Arquitetura alvo e princípios técnicos

## 14.1 Estilo arquitetural

A direção principal é:

**modular monolith, orientado a DDD, Clean Architecture, SOLID, crescimento evolutivo e separação clara por bounded context**.

## 14.2 Regra importante

Não criar microserviços prematuramente.
O produto deve nascer consistente como monólito modular bem desenhado, preparado para evoluir sem ruptura desnecessária.

## 14.3 Regras estruturais

- cada módulo deve ter fronteira clara
- evitar acoplamento indevido entre módulos
- domínio não depende de infraestrutura
- application não conhece detalhe de persistência
- API deve ser fina
- infrastructure contém adapters e integrações
- comunicação entre módulos deve preferir contratos claros e eventos quando apropriado
- preservar coesão alta e baixo acoplamento

## 14.4 Stack alvo consolidada

### Backend

- .NET 10 / ASP.NET Core 10
- EF Core 10
- Npgsql
- PostgreSQL 16
- MediatR
- FluentValidation
- Quartz.NET
- OpenTelemetry
- Serilog

### Frontend

- React 18
- TypeScript
- Vite
- TanStack Router
- TanStack Query
- Zustand
- Tailwind CSS
- Radix UI
- Apache ECharts
- Playwright

### Infra e operação

- PostgreSQL 16 como base central no MVP1/primeiras fases
- Docker Compose para POC/avaliação
- IIS com suporte explícito
- Windows e Linux
- SMTP
- evolução posterior para Kubernetes
- stack analítica/observabilidade com Elasticsearch como provider padrão (ClickHouse como alternativa)

## 14.5 Restrições conhecidas

- evitar dependências proprietárias que prejudiquem uso enterprise/self-hosted
- privilegiar bibliotecas open-source com licenciamento comercial viável
- não depender de Redis no MVP1 sem necessidade real
- não depender de OpenSearch no MVP1 se PostgreSQL FTS for suficiente
- não depender de Temporal no MVP1 se Quartz + PostgreSQL resolverem

---

# 15. Persistência e dados

## 15.1 Princípios

- PostgreSQL é peça central do produto
- dados de domínio devem ser modelados com clareza e auditabilidade
- dados analíticos/telemetria devem respeitar a natureza temporal e de volume
- retenção, histórico, pesquisa e trilha de auditoria são essenciais

## 15.2 O agente deve evitar

- espalhar estado crítico sem necessidade
- duplicar fonte de verdade sem governança
- criar modelos genéricos demais que percam semântica de domínio
- misturar tabelas de domínio com tabelas puramente técnicas sem critério

## 15.3 Pesquisa e leitura

Quando viável:

- usar PostgreSQL full-text search para MVP1 e primeiras etapas
- modelar eventos de auditoria e timeline de forma consultável
- preservar rastreabilidade entre mudanças, contratos, serviços, incidentes e evidências

---

# 16. Segurança, identidade e acesso

## 16.1 Princípios obrigatórios

- segurança por desenho
- least privilege
- tenant-aware
- environment-aware authorization
- auditoria de ações sensíveis
- LGPD/GDPR como referência padrão

## 16.2 Autenticação e identidade

O produto deve suportar:

- OIDC
- SAML
- login federado como caminho preferencial
- deep-link preservation no login
- password local apenas quando SSO não estiver configurado

## 16.3 Autorização

A autorização deve considerar dimensões como:

- módulo
- página
- ação
- ambiente
- tipo de API/contrato
- capacidade de IA
- tenant
- papel/política

## 16.4 Capacidades importantes

- Break Glass Access Protocol
- Just-In-Time privileged access
- delegated access com expiração e revogação
- access reviews
- eventos de segurança auditados

## 16.5 Regra obrigatória

Frontend nunca é fonte de verdade para autorização.
Frontend apenas reflete UX; backend é a autoridade final.

---

# 17. Licensing, proteção de código e operação self-hosted

## 17.1 Requisito estratégico

Licensing e proteção de código fazem parte do escopo central do produto, não são detalhe secundário.

## 17.2 O produto deve suportar

- self-hosted
- on-premises
- license online
- license offline
- entitlements por capacidade
- trial/freemium quando aplicável
- enforcement no backend
- ativação
- validação recorrente
- heartbeat
- revogação remota quando aplicável
- machine fingerprinting
- assembly integrity verification
- anti-tampering e anti-debugging conforme desenho do projeto

## 17.3 Regra de implementação

Separar claramente:

- building blocks de segurança genérica
- domínio de licensing
- runtime de enforcement
- storage/licensing adapters

---

# 18. UI, frontend e experiência do produto

## 18.1 Identidade visual esperada

O frontend deve seguir uma linha:

- enterprise
- sóbria
- premium
- clara
- consistente
- inspirada em experiências de alto nível corporativo, com referência estética próxima da linguagem Dynatrace, sem clonagem literal

## 18.2 Não fazer

- não usar aparência gamer
- não usar sci-fi exagerado
- não usar cyberpunk chamativo
- não misturar estilos visuais conflitantes entre módulos
- não criar páginas sem relação com o design system

## 18.3 Regras de UX

Toda tela nova deve considerar:

- persona
- contexto do módulo
- contexto do ambiente
- clareza de ownership
- ações principais claras
- densidade adequada de informação
- responsividade real
- acessibilidade mínima coerente

## 18.4 Regra crítica de frontend

Não pedir ao utilizador para introduzir GUIDs/IDs técnicos manualmente em fluxos de negócio, exceto em cenários administrativos muito específicos e justificados.

## 18.5 Segurança frontend

- não expor segredos indevidamente
- não deixar credenciais de forma insegura em fluxos do browser
- não usar `dangerouslySetInnerHTML` com conteúdo não sanitizado
- validar URLs e redirects
- usar i18n em todo o texto visível
- nunca confiar em checks de permissão apenas no frontend

---

# 19. Idioma, logs, exceções e i18n

## 19.1 Convenção de idioma

- código: inglês
- logs: inglês
- exceptions: inglês
- nomes técnicos: inglês
- documentação narrativa e comentários XML: português
- UI: i18n obrigatório

## 19.2 Backend para frontend

Mensagens retornadas pelo backend para UX devem seguir contrato estável com chaves de i18n, por exemplo:

- `code`
- `messageKey`
- `params`
- `correlationId`
- `details` quando aplicável

## 19.3 Regra obrigatória de frontend

Todo texto visível deve vir de i18n, incluindo:

- títulos
- labels
- placeholders
- menus
- tabs
- botões
- tooltips
- mensagens de erro
- mensagens de sucesso
- banners
- estados vazios
- loading states
- páginas de IA
- páginas administrativas

---

# 20. Regras obrigatórias de código

## 20.1 Qualidade técnica

Aplicar sempre que fizer sentido:

- `sealed` para classes finais
- `CancellationToken` em toda operação async
- `Result<T>` para falhas controladas
- guard clauses no início dos métodos
- strongly typed IDs
- nunca `DateTime.Now`; usar abstração correta ou UTC
- contratos públicos estáveis
- evitar breaking changes desnecessários
- logging útil e estruturado

## 20.2 Regras de módulo

- um módulo não deve aceder diretamente ao `DbContext` de outro módulo
- não quebrar fronteira de bounded context por conveniência
- não criar service classes gigantes com múltiplas responsabilidades
- não colapsar domínio e infraestrutura num mesmo tipo por pressa
- não fazer API controller com lógica de negócio pesada

## 20.3 Refatoração segura

Preferir:

- pequenas refatorações incrementais
- testes antes de mudanças destrutivas
- documentação de gaps quando não der para concluir tudo
- relatórios claros sobre o que está pronto, parcial ou em falta

---

# 21. Estratégia de implementação para qualquer tarefa

Quando receber uma tarefa, o agente deve seguir este fluxo mental:

## 21.1 Passo 1 — Entender o objetivo de produto

Responder:

- qual dor do produto esta tarefa resolve?
- qual pilar do produto reforça?
- qual persona principal beneficia?
- qual módulo é o dono natural desta responsabilidade?

## 21.2 Passo 2 — Inspecionar o que já existe

Antes de criar, verificar:

- módulos existentes
- contratos existentes
- endpoints existentes
- DTOs existentes
- componentes existentes
- states e stores existentes
- migrações e entidades existentes
- testes existentes
- i18n keys existentes
- policies/permissões existentes

## 21.3 Passo 3 — Reutilizar antes de inventar

Prioridade:

1. reutilizar o que está alinhado
2. refatorar o que está quase alinhado
3. completar o que está parcial
4. só criar do zero quando realmente necessário

## 21.4 Passo 4 — Implementar no bounded context correto

Nunca resolver uma falta de modelagem do domínio apenas no frontend.
Nunca resolver uma falta de contrato apenas com mock permanente.

## 21.5 Passo 5 — Fechar a tarefa por completo

Uma tarefa relevante só deve ser considerada concluída quando incluir, conforme aplicável:

- implementação
- testes
- validação de arquitetura
- validação de segurança
- i18n
- documentação inline
- logs adequados
- estados de erro adequados
- UX coerente com persona

---

# 22. Regras específicas para backend

## 22.1 Endpoints e contratos

Quando uma feature precisar de dados:

1. procurar endpoint existente
2. reutilizar se aderente ao domínio
3. refatorar se necessário
4. criar novo endpoint apenas no módulo correto
5. documentar gaps se algo ficar stubado

## 22.2 Não fazer

- endpoints genéricos demais
- endpoints com múltiplas responsabilidades não relacionadas
- payloads inconsistentes
- mensagens hardcoded de UX no backend
- contratos instáveis quebrados sem necessidade

## 22.3 Fazer

- DTOs claros
- validações explícitas
- erros coerentes
- contratos previsíveis
- filtros por ambiente, tenant, ownership e persona quando aplicável
- paginação e search onde fizer sentido

---

# 23. Regras específicas para frontend

## 23.1 Regra de produto

Frontend não é vitrine estática; é superfície operacional e de governança.

## 23.2 Cada tela deve responder

- qual persona usa?
- qual decisão esta tela ajuda a tomar?
- qual ação principal deve estar em evidência?
- qual contexto de ambiente, serviço, contrato ou mudança deve estar visível?
- o utilizador entende ownership, estado e risco?

## 23.3 Não esconder funcionalidade relevante

Se um módulo existe mas está incompleto, a decisão padrão não é remover silenciosamente do menu. A preferência é:

- corrigir
- completar
- sinalizar claramente preview/pendência quando necessário
- preservar coerência do produto

## 23.4 Design system e consistência

- usar componentes consistentes
- evitar estilos duplicados
- respeitar navegação previsível
- manter responsividade real
- evitar overload visual

---

# 24. Testes, validação e evidência de conclusão

## 24.1 Uma tarefa não está pronta apenas porque compila

O agente deve considerar:

- testes unitários
- testes de integração quando necessários
- testes end-to-end em fluxos críticos
- validação manual do fluxo
- coerência com permissões
- coerência com i18n
- coerência com persona
- coerência com ambiente

## 24.2 Sempre que possível, gerar evidência

Por exemplo:

- lista de ficheiros alterados
- gaps encontrados
- decisões tomadas
- impacto esperado
- riscos remanescentes
- testes executados

---

# 25. Documentação obrigatória

A alteração deve manter coerência com a estrutura documental do produto, incluindo áreas como:

- visão do produto
- capacidades
- arquitetura
- plataforma engineering
- governance
- AI system
- data architecture
- security architecture
- operations

Se a mudança introduzir comportamento importante, o agente deve atualizar documentação relevante.

---

# 26. Ordem recomendada de priorização do produto

## Núcleo estratégico

1. Service Catalog & Ownership
2. Contract Governance
3. Source of Truth
4. Change Intelligence / Change Confidence

## Operação e confiabilidade

5. Team-owned Service Reliability
6. Incident Correlation & Mitigation
7. Operational Consistency
8. AIOps contextual

## Conhecimento e aceleração

9. Knowledge Hub
10. AI-assisted Analysis
11. AI Agents
12. IDE integrations

## Governança e otimização

13. Reports by Persona
14. Risk
15. Compliance
16. FinOps contextual
17. Advanced AI Governance

---

# 27. Anti-padrões explícitos

O agente deve evitar estes erros recorrentes:

- transformar o produto em observabilidade genérica
- criar chats de IA sem governança
- criar telas bonitas mas vazias de valor operacional
- esconder módulos porque estão incompletos
- pedir GUID ao utilizador final
- misturar regras de domínio com detalhes de infra
- quebrar isolamento entre módulos por conveniência
- criar endpoints “faz tudo”
- duplicar fonte de verdade
- criar abstrações antes da necessidade real
- reescrever módulos inteiros sem justificativa técnica forte
- criar microserviços antes da hora
- introduzir dependências pesadas sem benefício claro
- deixar textos hardcoded fora de i18n
- ignorar tenant, ambiente e persona

---

# 28. Critério de qualidade esperado para qualquer agente

O agente deve trabalhar com padrão **enterprise-grade**.

Isto significa:

- pensar primeiro no produto, depois no código
- preservar narrativa central do NexTraceOne
- compreender impactos cross-module
- respeitar bounded contexts
- respeitar segurança, auditoria e governança
- produzir alterações legíveis, sustentáveis e evolutivas
- preferir clareza e robustez em vez de hacks rápidos
- documentar incertezas em vez de esconder problemas

---

# 29. Modelo de resposta esperado do agente ao executar tarefas complexas

Quando executar uma tarefa maior, a resposta ideal do agente deve incluir:

1. **Objetivo da tarefa no contexto do produto**
2. **Estado atual encontrado**
3. **Gaps identificados**
4. **Decisão arquitetural adotada**
5. **Alterações implementadas**
6. **Ficheiros principais impactados**
7. **Riscos ou pendências remanescentes**
8. **Testes executados ou recomendados**
9. **Próximos passos sugeridos**

---

# 30. Instrução final para qualquer agente de IA

Ao alterar qualquer parte do repositório do NexTraceOne:

- pense primeiro no produto, depois no código
- trate serviços, contratos, mudanças e conhecimento operacional como entidades centrais
- trate IA como capacidade governada, não como feature genérica
- trate observabilidade como insumo contextual, não como fim isolado
- trate ambientes não produtivos como fonte crítica para prevenir falhas em produção
- trate o produto como Source of Truth
- respeite segmentação por persona
- respeite segurança, auditoria, licensing e self-hosted constraints
- respeite i18n, arquitetura modular e contratos estáveis
- prefira refatoração incremental com intenção explícita
- quando algo não estiver concluído, seja transparente, documente o gap e proponha o próximo passo correto


---

# 31. Modos de operação do agente

O agente deve saber em que modo está a trabalhar. Nem toda tarefa é de implementação direta.

## 31.1 Modo Analysis

Usar quando a tarefa pede diagnóstico, mapeamento, revisão ou investigação.

Entregáveis esperados:

- visão do problema no contexto do produto
- inventário do que já existe
- gaps funcionais, técnicos e arquiteturais
- riscos
- proposta de abordagem mínima viável e correta

## 31.2 Modo Implementation

Usar quando a tarefa pede construir ou completar funcionalidade.

Entregáveis esperados:

- implementação funcional
- contratos coerentes
- testes mínimos adequados
- i18n aplicado
- documentação inline atualizada

## 31.3 Modo Refactor

Usar quando a tarefa pede reorganização, limpeza, simplificação ou correção estrutural.

Entregáveis esperados:

- melhoria estrutural sem perda de comportamento esperado
- redução de acoplamento e melhoria de coesão
- preservação dos contratos públicos sempre que possível
- relatório claro de alterações sensíveis

## 31.4 Modo Stabilization

Usar quando a tarefa pede fechar pendências, retirar stubs permanentes, reduzir resíduos, endurecer fluxos.

Entregáveis esperados:

- remoção de comportamentos frágeis
- eliminação de placeholders indevidos
- consolidação de fluxos incompletos
- redução de inconsistências de UX e domínio

## 31.5 Modo Hardening

Usar quando a tarefa pede segurança, confiabilidade, auditabilidade ou readiness para produção.

Entregáveis esperados:

- reforço de autorização e auditoria
- correção de exposição indevida de dados
- melhor tratamento de erros
- reforço de tracing/logging/validation

## 31.6 Modo Documentation

Usar quando a tarefa pede atualizar visão, arquitetura, operação, instalação, configuração ou decisão técnica.

Entregáveis esperados:

- documentação clara
- alinhamento com produto real
- exemplos quando úteis
- explicitação de limites e próximos passos

---

# 32. Como lidar com mocks, stubs, placeholders e resíduos

O NexTraceOne não deve esconder dívida técnica com aparência de “feature pronta”.

## 32.1 Quando encontrar mock

O agente deve classificar:

- mock aceitável temporário
- mock inadequado para fluxo crítico
- mock que mascara ausência de backend real

## 32.2 Quando encontrar stub

O agente deve verificar:

- existe contrato real definido?
- o stub está claramente sinalizado?
- o fluxo depende dele em produção?
- qual o plano mínimo para substituir por implementação real?

## 32.3 Quando encontrar placeholder cosmético

O agente deve preferir:

- completar com valor real
- ou tornar explícito que está em preview
- ou remover apenas se for comprovadamente fora de escopo

## 32.4 Regra importante

Não remover funcionalidade relevante apenas porque está incompleta, sem antes avaliar:

- valor para o produto
- impacto na navegação
- expectativa da persona
- possibilidade de concluir incrementalmente

## 32.5 Resíduos fora de escopo

Quando encontrar código, telas, serviços ou referências fora da visão oficial:

- documentar o motivo da remoção
- verificar impacto em navegação e dependências
- evitar remover sem confirmar que não compõe uma funcionalidade em evolução

---

# 33. Configuração e parametrização

O NexTraceOne deve evoluir para reduzir dependência excessiva de `appsettings.*.json` para regras de negócio e parâmetros operacionais variáveis.

## 33.1 Devem permanecer em configuração técnica quando apropriado

- connection strings
- segredos e credenciais externas
- endpoints técnicos de infraestrutura
- flags estritamente técnicas de bootstrap

## 33.2 Devem ser avaliados para parametrização no banco quando fizer sentido

- políticas por ambiente
- janelas de deploy
- categorias de mudança
- critérios de aprovação
- configuração de modelos de IA
- budgets e quotas
- integrações e respectivos metadados funcionais
- severidades e classificações
- regras de catalogação e ownership
- políticas de retenção lógicas
- parâmetros de scoring e thresholds do domínio

## 33.3 Regra de decisão

Se a configuração precisa ser alterada pelo utilizador/admin funcional sem redeploy, ela deve ser fortemente candidata a parametrização persistida.

---

# 34. Estratégia para mudanças de base de dados e migrações

## 34.1 Princípios

- mudanças de schema devem respeitar bounded context
- evitar migrações caóticas e desorganizadas
- preservar clareza do modelo evolutivo
- preferir migrations coerentes e auditáveis

## 34.2 O agente deve verificar

- se a mudança pertence mesmo àquele módulo
- se o nome da migration descreve a intenção
- se há impacto em dados existentes
- se a mudança precisa de backfill ou script complementar
- se existe quebra de compatibilidade

## 34.3 Regra importante

Não alterar schema por conveniência de UI. O modelo deve refletir domínio e uso real.

---

# 35. Integrações e ingestão externa

O produto deve tratar integrações como capacidades governadas, não como acoplamentos ad-hoc.

## 35.1 Exemplos de integrações relevantes

- GitLab
- Jenkins
- GitHub
- Azure DevOps
- sistemas de identity provider
- fontes de telemetria
- fontes de documentação
- provedores de IA externos

## 35.2 Regras

- integrar via contratos claros
- auditar chamadas relevantes
- permitir configuração por tenant/ambiente quando aplicável
- evitar lógica de vendor hardcoded espalhada
- preservar modelo canônico interno do NexTraceOne

---

# 36. Requisitos não funcionais e metas operacionais

As implementações devem respeitar a direção já estabelecida para o produto.

## 36.1 Metas de valor

- Time to First Value baixo
- Time to Core Value baixo
- onboarding guiado
- auditabilidade ampla
- operação self-hosted viável

## 36.2 Exemplos de metas funcionais já discutidas

- import de contrato rápido
- diff semântico rápido
- blast radius em poucos segundos
- pesquisa de catálogo rápida
- pesquisa de auditoria em janela operacional aceitável

## 36.3 Regra

Quando houver escolha entre elegância abstrata e entrega confiável com performance adequada, preferir a solução mais simples que cumpra o nível enterprise esperado.

---

# 37. Checklist de revisão de frontend

Ao revisar ou construir telas, o agente deve verificar:

- a tela representa claramente o valor do módulo?
- existe aderência ao design system?
- a navegação está consistente?
- há responsividade real?
- há estados de loading, erro e vazio?
- há i18n completo?
- a persona está refletida na informação mostrada?
- há contexto de ambiente, serviço, contrato ou mudança quando necessário?
- a ação principal está clara?
- existem campos técnicos desnecessários para o utilizador final?
- há risco de segurança no browser?

---

# 38. Checklist de revisão de backend

Ao revisar backend, o agente deve verificar:

- o endpoint pertence ao módulo correto?
- o contrato está claro?
- a validação está adequada?
- há tratamento de erro coerente?
- há logs úteis e não excessivos?
- há auditoria quando necessário?
- o fluxo respeita tenant e ambiente?
- há separação adequada entre API, Application, Domain e Infrastructure?
- existe acoplamento indevido com outro módulo?
- a operação async usa CancellationToken?

---

# 39. Checklist de revisão de IA

Ao implementar ou rever recursos de IA, o agente deve verificar:

- existe contexto suficiente do produto?
- o modelo foi explicitamente escolhido ou resolvido por política?
- há controle de acesso?
- há trilha de auditoria?
- há proteção contra saída indevida de dados?
- a resposta da IA pode ser explicada ou contextualizada?
- a feature é útil para operação/engenharia ou é apenas chat genérico?
- existe política por tenant, ambiente, grupo ou persona?
- o uso pode ser medido por custo/token/budget?

---

# 40. Checklist de revisão de change intelligence

Ao implementar fluxos de change intelligence, o agente deve verificar:

- existe identidade da mudança?
- a mudança está ligada ao serviço correto?
- o ambiente está explícito?
- há correlação possível com deploy, contrato e incidente?
- existem evidências e timeline?
- há noção de blast radius?
- há validação pós-change?
- há leitura comparativa entre não produção e produção quando aplicável?
- a saída ajuda uma decisão real de promover, bloquear, investigar ou mitigar?

---

# 41. Como o agente deve propor solução

Ao apresentar solução, o agente deve preferir a estrutura:

## 41.1 Contexto

Explicar qual problema do produto está a ser resolvido.

## 41.2 Estado atual

Explicar o que existe hoje e onde estão os gaps.

## 41.3 Solução mínima correta

Propor a menor solução capaz de aproximar o produto da visão oficial sem over-engineering.

## 41.4 Impacto

Dizer quais módulos, contratos, telas, entidades ou fluxos são afetados.

## 41.5 Riscos remanescentes

Ser transparente sobre o que não ficou resolvido.

---

# 42. Regra final de maestria

O agente deve agir como alguém que entende simultaneamente:

- produto
- arquitetura
- operação
- governança
- segurança
- experiência enterprise
- evolução incremental

Não basta “fazer funcionar”.
É necessário fazer com **intenção arquitetural, coerência de produto e capacidade real de evolução**.

---

# 43. Estado de implementação atual (maio 2026)

Este capítulo descreve o que está efetivamente construído no código, distinguindo claramente entre o que está operacional, o que está parcialmente implementado e o que está planeado mas ainda não construído.

## 43.1 Stack técnica atual (operacional)

- **.NET 10 / ASP.NET Core 10** — versão alvo em todos os projetos
- **EF Core 10 + Npgsql** — persistência primária em PostgreSQL 16
- **MediatR** — CQRS via Commands/Queries + pipeline behaviors
- **FluentValidation** — validação integrada no pipeline MediatR
- **Outbox pattern** — `OutboxMessage` persistido no `SaveChanges` de cada módulo; processado por `ModuleOutboxProcessorJob<TContext>` (um por DbContext)
- **`Result<T>/Error`** — padrão uniforme de retorno em handlers (sem exceções de controle de fluxo)
- **Strongly typed IDs** — todos os agregados usam `record TypedIdBase(Guid Value)` (ex: `AuditEventId`, `ApiAssetId`)
- **`ICurrentTenant`** — tenant ativo injetado via `TenantResolutionMiddleware` (JWT claims, header, subdomínio)
- **`TenantRlsInterceptor`** — configura `set_config('app.current_tenant_id', @id)` antes de cada query PostgreSQL
- **`AuditInterceptor`** — preenche `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` automaticamente
- **`TenantIsolationBehavior`** — MediatR pipeline behavior que rejeita requests sem tenant ativo
- **`IDateTimeProvider`** — abstração de clock (nunca usar `DateTime.Now`)
- **Serilog** — logging estruturado
- **OpenTelemetry** — traces e métricas; Elasticsearch como destino analítico principal
- **ClickHouse** — desativado por padrão; ativado via `NexTrace:Analytics:Provider=ClickHouse`
- **Kafka** — desativado por padrão; `NullKafkaEventProducer` como fallback; `ConfluentKafkaEventProducer` ativado via configuração
- **Redis** — **não implementado** (sem `IDistributedCache`); cache atual é in-memory `IMemoryCache`
- **Polly** — **não implementado**; sem circuit breakers ou retry policies nos HttpClients externos
- **pg_advisory_lock** — implementado em `ModuleOutboxProcessorJob` para evitar processamento duplicado em deploys horizontais

## 43.2 Módulos com código real (bounded contexts)

Cada módulo segue estrutura `Domain / Application / Contracts / Infrastructure / API`:

| Pasta em `src/modules/` | DbContext(s) principal(is) | Database (connection string key) |
|---|---|---|
| `identityaccess` | `IdentityDbContext` | `IdentityDatabase` |
| `catalog` | `CatalogGraphDbContext`, `ContractsDbContext`, `DeveloperPortalDbContext`, `LegacyAssetsDbContext`, `TemplatesDbContext`, `DependencyGovernanceDbContext` | `CatalogDatabase`, `ContractsDatabase` |
| `changegovernance` | `ChangeIntelligenceDbContext`, `RulesetGovernanceDbContext`, `WorkflowDbContext`, `PromotionDbContext` | `ChangeIntelligenceDatabase` |
| `aiknowledge` | `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext` | `AiGovernanceDatabase` |
| `auditcompliance` | `AuditDbContext` | `AuditDatabase` |
| `governance` | `GovernanceDbContext` | `GovernanceDatabase` |
| `operationalintelligence` | `RuntimeIntelligenceDbContext`, `ReliabilityDbContext`, `CostIntelligenceDbContext`, `IncidentDbContext`, `AutomationDbContext` | `RuntimeIntelligenceDatabase`, `CostIntelligenceDatabase`, etc. |
| `integrations` | `IntegrationsDbContext` | `IntegrationsDatabase` |
| `knowledge` | `KnowledgeDbContext` | `KnowledgeDatabase` |
| `productanalytics` | `ProductAnalyticsDbContext` | `ProductAnalyticsDatabase` |
| `notifications` | `NotificationsDbContext` | `NotificationsDatabase` |
| `configuration` | `ConfigurationDbContext` | `ConfigurationDatabase` |

Total: 24 bases de dados PostgreSQL isoladas (uma por módulo/contexto).

## 43.3 Plataforma

- `src/platform/NexTraceOne.ApiHost` — host HTTP principal; monta todos os módulos; registra middleware de tenant, RLS, auditoria
- `src/platform/NexTraceOne.BackgroundWorkers` — worker service; registra `ModuleOutboxProcessorJob<TContext>` para todos os 21+ DbContexts; jobs de manutenção
- `src/platform/NexTraceOne.Ingestion.Api` — API dedicada para ingestão de telemetria/eventos externos

## 43.4 Building Blocks

- `NexTraceOne.BuildingBlocks.Core` — primitivos: `AggregateRoot<T>`, `Entity<T>`, `TypedIdBase`, `Result<T>`, `Error`, `DomainEvent`
- `NexTraceOne.BuildingBlocks.Application` — abstrações: `ICurrentTenant`, `ICurrentUser`, `IDateTimeProvider`, `IUnitOfWork`, `IEventBus`, behaviors MediatR
- `NexTraceOne.BuildingBlocks.Infrastructure` — EF Core base: `NexTraceDbContextBase`, interceptors (`TenantRlsInterceptor`, `AuditInterceptor`, `EncryptionInterceptor`), outbox, configurações
- `NexTraceOne.BuildingBlocks.Security` — multi-tenancy: `CurrentTenantAccessor`, `TenantResolutionMiddleware`, JWT, Break Glass
- `NexTraceOne.BuildingBlocks.Observability` — métricas, tracing, health checks, ingestion collectors

## 43.5 Padrões críticos do código

### Honest-Null pattern

Interfaces de leitura analítica (`IXxxReader`) são registadas com implementações Null que retornam listas vazias — são placeholders intencionais para pipelines cross-módulo futuros. Não remover; não confundir com bugs.

**Repositórios** (`IXxxRepository`) que retornam dados persistidos DEVEM ter implementações EF Core reais.

### Outbox e eventos

1. `AggregateRoot<T>` acumula `DomainEvent` em `DomainEvents`
2. `NexTraceDbContextBase.SaveChangesAsync` converte domain events → `OutboxMessage` no mesmo `SaveChanges`
3. `ModuleOutboxProcessorJob<TContext>` processa e publica via `IEventBus` a cada 5s
4. Cada ciclo usa `pg_try_advisory_lock` para coordenação em multi-instância

### SaaS / Licensing

- `TenantLicense` → `TenantPlan` (Starter, Professional, Enterprise, Trial)
- `TenantCapabilities.ForPlan(plan)` → conjunto de capabilities habilitadas
- `ICurrentTenant.HasCapability(string)` — verificação em handlers e middleware
- Capabilities embutidas no JWT; lidas pelo `TenantResolutionMiddleware`
- Fallback: sem licença → `Enterprise` (all capabilities enabled)

### Encriptação at-rest

Campos marcados com `[EncryptedField]` são encriptados automaticamente via `EncryptedStringConverter` (AES-256-GCM) na convenção do `NexTraceDbContextBase`.

---

# 44. O que ainda não está implementado

O agente deve conhecer estas lacunas e não assumir que estão prontas:

## 44.1 Infraestrutura horizontal (P0 — crítico para produção SaaS)

- **Redis** — sem cache distribuído; adição futura via `IDistributedCache` + `StackExchange.Redis`
- **Polly** — sem retry policies ou circuit breakers nos HttpClients externos (Elasticsearch, ClickHouse, Ollama, AI providers, webhooks)

## 44.2 Funcional (P1 — importante)

- `LicenseRecalculationJob` — atualização real de `TenantLicense.CurrentHostUnits` a partir de uso real; atualmente não existe
- Provisionamento automático de tenant — criação de roles default, equipa inicial, ambiente default (parcialmente manual)
- `TenantSchemaManager` — criado mas não utilizado; suporte futuro a schema-per-tenant
- Kafka — desativado; `ConfluentKafkaEventProducer` existe mas não registado por default
- Payment provider integration — não implementado

## 44.3 Qualidade (P2)

- Dead Letter Queue — `IDeadLetterRepository` referenciado em `ModuleOutboxProcessorJob` mas implementação real pode não estar registada em todos os ambientes
- GDPR/exportação unificada de dados por tenant — não implementado
- Trial plan capabilities — atualmente recebe Enterprise (all); deve receber subset limitado

---

# 45. Convenções de código obrigatórias

## 45.1 Nomenclatura e organização

- **Classes finais**: sempre `sealed` (exceto entidades base)
- **Records imutáveis**: preferir para DTOs, Value Objects, IDs
- **Namespaces**: alinhar exatamente com pasta (sem nesting extra)
- **Ficheiros de entidade**: uma entidade pública por ficheiro; tipos auxiliares (IDs, enums) podem acompanhar
- **Testes**: pasta `tests/modules/<nome>` espelhando estrutura de `src/modules/`

## 45.2 Async e cancelação

- Todo método async deve receber e propagar `CancellationToken`
- Nunca usar `Task.Run` sem justificativa explícita
- Nunca usar `.Result` ou `.Wait()` em código async

## 45.3 Result pattern

```csharp
// Handler retorna:
Result<MyDto>   // sucesso com valor
Result.Success  // sucesso sem valor
Result.Failure(Error.Validation(“Code”, “Message”))  // falha controlada
// Nunca lançar exceção para controle de fluxo de negócio
```

## 45.4 IDs fortemente tipados

```csharp
// ID correto:
public sealed record MyEntityId(Guid Value) : TypedIdBase(Value)
{
    public static MyEntityId New() => new(Guid.NewGuid());
    public static MyEntityId From(Guid id) => new(id);
}
// Ao comparar com Guid, usar .Value:
assets.Where(a => guids.Contains(a.Id.Value))
```

## 45.5 Tenant awareness obrigatório

Qualquer repositório que retorne dados de utilizador deve filtrar por `currentTenant.Id`. A exceção são jobs de plataforma (sem contexto de tenant, como retention jobs).

## 45.6 Sem comentários triviais

Comentários no código devem explicar **porquê**, nunca **o quê**. Se o nome do método/variável já diz o quê, não adicionar comentário.

---

# 46. Decisões arquiteturais registadas (ADRs implícitos)

| Decisão | Escolha | Razão |
|---|---|---|
| Monolito modular vs microserviços | Monolito modular | Coerência, menor complexidade operacional no MVP |
| ORM | EF Core 10 + Npgsql | Ecossistema .NET, migrations, interceptors |
| Search | PostgreSQL FTS no MVP1 | Sem dependência adicional; Elasticsearch para analítica |
| Cache | In-memory no MVP1 | Redis adicionado quando escala horizontal necessitar |
| Mensageria | In-process EventBus (Outbox) | Kafka opcional/desativado por default |
| Auth | JWT + OIDC | Claims-based, multi-tenant aware |
| RLS | PostgreSQL set_config + interceptor | Defense-in-depth com aplicação-level filters |
| IA interna | Ollama (self-hosted) por default | Privacidade, self-hosted compliance |
| IA externa | OpenAI/Anthropic/Azure via policy | Governado, auditado, opt-in por tenant |
| Outbox lock | pg_try_advisory_lock | Coordenação leve sem Redis, usando conexão existente |
