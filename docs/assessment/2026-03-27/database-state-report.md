# Relatório de Estado da Base de Dados e Persistência — NexTraceOne

**Data:** 2026-03-27
**Tipo:** Relatório de Avaliação Técnica
**Escopo:** Arquitetura de persistência, DbContexts, migrações, multi-tenancy, auditoria, encriptação e completude face à visão do produto
**Versão do PostgreSQL alvo:** 16

---

## Índice

1. [Visão Geral da Arquitetura de Persistência](#1-visão-geral-da-arquitetura-de-persistência)
2. [Análise Detalhada de Cada DbContext por Módulo](#2-análise-detalhada-de-cada-dbcontext-por-módulo)
3. [Estado das Migrações e Lacunas Identificadas](#3-estado-das-migrações-e-lacunas-identificadas)
4. [Consistência de Prefixos de Tabela](#4-consistência-de-prefixos-de-tabela)
5. [Suporte a Multi-Tenancy](#5-suporte-a-multi-tenancy)
6. [Auditoria e Encriptação](#6-auditoria-e-encriptação)
7. [Completude das Entidades face à Visão do Produto](#7-completude-das-entidades-face-à-visão-do-produto)
8. [Problemas Identificados e Recomendações](#8-problemas-identificados-e-recomendações)
9. [O que Está em Falta](#9-o-que-está-em-falta)
10. [Conclusão e Próximos Passos](#10-conclusão-e-próximos-passos)

---

## 1. Visão Geral da Arquitetura de Persistência

### 1.1 Filosofia Arquitetural

O NexTraceOne adopta uma estratégia de **base de dados física única** com **isolamento lógico por módulo**.
Esta abordagem está alinhada com o princípio de **modular monolith orientado a DDD**, onde cada
bounded context mantém o seu próprio DbContext e prefixo de tabela, mas partilha a mesma instância
PostgreSQL 16.

Esta decisão é deliberada e traz benefícios claros para o contexto enterprise e self-hosted:

- **Simplicidade operacional**: um único servidor PostgreSQL para gerir, fazer backup e monitorizar.
- **Transações cross-module**: quando necessário, transações distribuídas são evitadas.
- **Isolamento lógico**: cada módulo tem o seu namespace de tabelas via prefixo.
- **Evolução preparada**: caso no futuro seja necessário separar fisicamente, a separação de DbContexts já está feita.

### 1.2 Números Actuais

| Métrica                        | Valor   |
|-------------------------------|---------|
| Base de dados física          | 1       |
| DbContexts                   | 23      |
| Módulos com persistência      | 12      |
| Connection strings configuradas| 24     |
| Directórios de migração       | 20      |
| Ficheiros de migração         | 71      |
| Entidades totais estimadas    | ~150+   |

### 1.3 Classe Base: NexTraceDbContextBase

Todos os DbContexts herdam de `NexTraceDbContextBase`, que fornece:

- Configuração padronizada de conexão PostgreSQL.
- Integração com interceptors de auditoria (`AuditInterceptor`).
- Integração com interceptor de Row-Level Security (`TenantRlsInterceptor`).
- Suporte ao padrão Outbox para mensagens transaccionais.
- Converter de encriptação (`EncryptedStringConverter`) para campos sensíveis.
- Convenções de naming consistentes.

### 1.4 Padrões Cross-Cutting Implementados

| Padrão                     | Implementação                          | Estado   |
|---------------------------|----------------------------------------|----------|
| Outbox Pattern            | Tabela `*_outbox_messages` por módulo  | Activo   |
| Row-Level Security (RLS)  | `TenantRlsInterceptor` via `set_config`| Activo   |
| Campos de Auditoria       | `AuditInterceptor`                     | Activo   |
| Encriptação de Campos     | `AesGcmEncryptor` + Converter EF Core  | Activo   |
| SQL Parameterizado        | EF Core apenas (sem SQL raw)           | Activo   |

---

## 2. Análise Detalhada de Cada DbContext por Módulo

### 2.1 IdentityAccess — `IdentityDbContext` (prefixo: `iam_`)

**Contextos:** 1
**Entidades:** 21
**Responsabilidade:** Gestão de identidade, acesso, autenticação, autorização e segurança operacional.

Este é o módulo mais maduro em termos de entidades, cobrindo:

- **Users**: gestão de utilizadores internos e federados.
- **Roles**: papéis com granularidade fina.
- **Permissions**: permissões atómicas associáveis a papéis.
- **Sessions**: sessões activas com rastreabilidade.
- **Tenants**: isolamento multi-tenant como first-class citizen.
- **Environments**: ambientes (Development, Pre-Production, Production) como entidade de domínio.
- **BreakGlass**: protocolo de acesso de emergência com auditoria completa.
- **JIT (Just-In-Time Access)**: acesso privilegiado temporário com expiração automática.
- **Delegations**: delegação de acesso com controlo de expiração e revogação.
- **AccessReviews**: revisões periódicas de acesso para conformidade.
- **SSO**: configuração de Single Sign-On (OIDC/SAML).

**Avaliação:** Este módulo está bem dimensionado para as necessidades enterprise do produto. A presença
de BreakGlass, JIT e AccessReviews demonstra maturidade de segurança que é rara em MVPs.

**Riscos:** Nenhum risco crítico identificado. O módulo pode crescer com entidades adicionais para
machine fingerprinting e license enforcement, mas isso pertence ao domínio de Licensing.

### 2.2 Catalog — 3 DbContexts (prefixo: `cat_`)

**Contextos:** 3 (`CatalogGraphDbContext`, `ContractsDbContext`, `DeveloperPortalDbContext`)
**Entidades:** ~30+ (modelo de grafo complexo)
**Responsabilidade:** Service Catalog, contratos (REST, SOAP, eventos), topologia de dependências, portal de desenvolvimento.

#### CatalogGraphDbContext
Modelo orientado a grafo para representar serviços, dependências e topologia.
Este é o coração do Service Catalog e da Source of Truth operacional.

#### ContractsDbContext
Gestão de contratos como first-class citizens: APIs REST, SOAP/WSDL, Event Contracts,
AsyncAPI, background services. Inclui versionamento, diff semântico, validação e publicação.

#### DeveloperPortalDbContext
Portal de consumo e descoberta de contratos e serviços para equipas de desenvolvimento.

**Avaliação:** A separação em três contextos é justificada pela complexidade do domínio.
O modelo de grafo é essencial para topology e blast radius. A presença de contratos como
entidade central está perfeitamente alinhada com a visão do produto.

**Riscos:** Com ~30+ entidades distribuídas por 3 contextos, a coerência entre os contextos
precisa de validação contínua. Mudanças num contrato devem reflectir-se no grafo e no portal.

### 2.3 ChangeGovernance — 4 DbContexts (prefixo: `chg_`)

**Contextos:** 4 (`ChangeIntelligenceDbContext`, `WorkflowDbContext`, `RulesetGovernanceDbContext`, `PromotionDbContext`)
**Entidades:** ~20+ (distribuídas pelo ciclo de vida da mudança)
**Responsabilidade:** Change Intelligence, validação de mudanças, governance de promoção, confiança em produção.

#### ChangeIntelligenceDbContext
Contexto central para mudanças: identidade, origem, escopo, ambiente, risco, blast radius,
correlação change-to-incident, evidence packs, rollback intelligence.

#### WorkflowDbContext
Workflows de aprovação e validação associados a mudanças.

#### RulesetGovernanceDbContext
Regras e políticas de governance que se aplicam a mudanças e promoções.

#### PromotionDbContext
Gestão de promoção entre ambientes com gates, critérios e validações.

**Avaliação:** A decomposição em 4 contextos reflecte correctamente os subdominios de Change
Intelligence. Cada contexto tem responsabilidade clara e evita o anti-padrão de "DbContext gigante".

**Riscos:** Todos os 4 contextos têm apenas a migração `InitialCreate`. Isto indica que o módulo
pode estar numa fase inicial de implementação, sem evoluções de schema ainda.

### 2.4 AIKnowledge — 3 DbContexts (prefixo: `aik_`)

**Contextos:** 3 (`ExternalAiDbContext`, `AiGovernanceDbContext`, `AiOrchestrationDbContext`)
**Entidades:** ~20+ (subsistema de IA)
**Responsabilidade:** Integração com IA externa, governança de modelos, orquestração de agentes.

#### ExternalAiDbContext
Configuração e gestão de integrações com provedores de IA externos (OpenAI, Azure AI, etc.).

#### AiGovernanceDbContext
Políticas de acesso a modelos, budgets de tokens, auditoria de uso, restrições por tenant/persona.

#### AiOrchestrationDbContext
Orquestração de agentes de IA especializados, contextos de execução, histórico de interacções.

**Avaliação:** A presença de governance de IA como bounded context separado é um diferenciador
significativo. Alinha-se com o princípio de que "IA é capacidade governada, não feature genérica".

**Riscos:** Tal como ChangeGovernance, cada contexto tem apenas `InitialCreate`. A evolução
do schema será necessária à medida que os agentes de IA ganhem complexidade.

### 2.5 OperationalIntelligence — 5 DbContexts (prefixo: `ops_`)

**Contextos:** 5 (`RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext`, `AutomationDbContext`, `IncidentDbContext`, `ReliabilityDbContext`)
**Entidades:** ~25+ (o módulo com mais contextos)
**Responsabilidade:** Inteligência operacional, FinOps, automação, incidentes, confiabilidade.

#### RuntimeIntelligenceDbContext
Telemetria contextualizada, métricas operacionais, análise de comportamento em runtime.
1 migração: `InitialCreate`.

#### CostIntelligenceDbContext
FinOps contextual por serviço, equipa, domínio, ambiente e operação.
2 migrações: `InitialCreate` + `P6_4` — indica evolução activa.

#### AutomationDbContext
Runbooks automatizados, acções de mitigação, automação operacional.
1 migração: `InitialCreate`.

#### IncidentDbContext
Gestão de incidentes com correlação a mudanças, contratos e dependências.
1 migração: `InitialCreate`.

#### ReliabilityDbContext
Service Reliability orientada por ownership e equipa. SLOs, SLIs, error budgets.
2 migrações: `InitialCreate` + `P6_1` — indica evolução activa.

**Avaliação:** Este é o módulo com maior decomposição (5 contextos), justificada pela
diversidade de subdomínios. CostIntelligence e Reliability já mostram evolução para além
do InitialCreate, o que é sinal positivo de maturidade.

**Riscos:** Com 5 contextos e ~25+ entidades, a manutenção e evolução requer disciplina.
A correlação entre estes contextos e ChangeGovernance deve ser bem definida.

### 2.6 AuditCompliance — `AuditDbContext` (prefixo: `aud_`)

**Contextos:** 1
**Entidades:** 6
**Responsabilidade:** Trilha de auditoria, conformidade, rastreabilidade de acções.

3 migrações: `InitialCreate` + `P7_4_AuditCorrelationId` — o módulo mais evoluído em migrações.

**Avaliação:** Módulo essencial para conformidade enterprise. A presença do CorrelationId
nas migrações indica evolução para rastreabilidade cross-module.

### 2.7 Configuration — `ConfigurationDbContext` (prefixo: `cfg_`)

**Contextos:** 1
**Entidades:** 6
**Responsabilidade:** Parametrização dinâmica, feature flags, configuração por tenant/ambiente.

1 migração: `InitialCreate`.

**Avaliação:** Alinha-se com a diretriz de que parâmetros operacionais devem migrar de
`appsettings.json` para persistência no banco.

### 2.8 Governance — `GovernanceDbContext` (prefixo: `gov_`)

**Contextos:** 1
**Entidades:** 9 (inicialmente 8, adicionada rule bindings)
**Responsabilidade:** Políticas, regras, compliance, risk center, gestão de governance.

1 migração: `InitialCreate`.

**Avaliação:** A adição de rule bindings mostra evolução do modelo. O módulo precisa crescer
para suportar o Risk Center e compliance de forma mais completa.

### 2.9 Notifications — `NotificationsDbContext` (prefixo: `ntf_`)

**Contextos:** 1
**Entidades:** 6
**Responsabilidade:** Notificações, canais, templates, preferências.

4 migrações: `InitialCreate`, `P7_1`, `P7_2`, `P7_3` — o **segundo módulo mais evoluído**.

**Avaliação:** A presença de 4 migrações indica desenvolvimento activo e iterativo. Este é um
bom exemplo de evolução incremental de schema.

### 2.10 Integrations — `IntegrationsDbContext` (prefixo: `int_`)

**Contextos:** 1
**Entidades:** 3
**Responsabilidade:** Integrações externas (GitLab, Jenkins, GitHub, Azure DevOps, etc.).

**Migrações:** ⚠️ **NÃO FORAM ENCONTRADAS MIGRAÇÕES EXPLÍCITAS** — lacuna identificada.

**Avaliação:** Com apenas 3 entidades e sem migrações, este módulo está numa fase muito
inicial. Para suportar a visão de integrações governadas, precisará crescer significativamente.

### 2.11 Knowledge — `KnowledgeDbContext` (prefixo: `knw_`)

**Contextos:** 1
**Entidades:** 3
**Responsabilidade:** Documentation Hub, Knowledge Hub, Source of Truth Views, notas operacionais.

**Migrações:** ⚠️ **SEM MIGRAÇÕES EF** — lacuna crítica identificada.

**Avaliação:** Este é um gap significativo. O Knowledge Hub é pilar central do NexTraceOne
(Source of Truth & Operational Knowledge). A ausência de migrações indica que o módulo não
está operacional a nível de persistência.

### 2.12 ProductAnalytics — `ProductAnalyticsDbContext` (prefixo: `pan_`)

**Contextos:** 1
**Entidades:** 1
**Responsabilidade:** Telemetria de uso do produto, métricas de adopção.

**Migrações:** ⚠️ **NÃO FORAM ENCONTRADAS MIGRAÇÕES EXPLÍCITAS** — lacuna identificada.

**Avaliação:** Módulo com entidade mínima. Pode estar em fase de prototipagem ou ser candidato
a usar ClickHouse directamente para workloads analíticos no futuro.

---

## 3. Estado das Migrações e Lacunas Identificadas

### 3.1 Inventário Completo de Migrações

| Módulo                    | Contexto                      | Migrações | Estado          |
|--------------------------|-------------------------------|-----------|-----------------|
| AuditCompliance          | AuditDbContext                | 3         | ✅ Evoluído      |
| Notifications            | NotificationsDbContext         | 4         | ✅ Mais evoluído |
| OperationalIntelligence  | CostIntelligenceDbContext     | 2         | ✅ Evoluído      |
| OperationalIntelligence  | ReliabilityDbContext          | 2         | ✅ Evoluído      |
| OperationalIntelligence  | RuntimeIntelligenceDbContext  | 1         | ⚙️ Inicial       |
| OperationalIntelligence  | AutomationDbContext           | 1         | ⚙️ Inicial       |
| OperationalIntelligence  | IncidentDbContext             | 1         | ⚙️ Inicial       |
| ChangeGovernance         | ChangeIntelligenceDbContext   | 1         | ⚙️ Inicial       |
| ChangeGovernance         | WorkflowDbContext             | 1         | ⚙️ Inicial       |
| ChangeGovernance         | RulesetGovernanceDbContext    | 1         | ⚙️ Inicial       |
| ChangeGovernance         | PromotionDbContext            | 1         | ⚙️ Inicial       |
| IdentityAccess           | IdentityDbContext             | 1         | ⚙️ Inicial       |
| Governance               | GovernanceDbContext           | 1         | ⚙️ Inicial       |
| Catalog                  | CatalogGraphDbContext         | 1         | ⚙️ Inicial       |
| Catalog                  | ContractsDbContext            | 1         | ⚙️ Inicial       |
| Catalog                  | DeveloperPortalDbContext      | 1         | ⚙️ Inicial       |
| Configuration            | ConfigurationDbContext        | 1         | ⚙️ Inicial       |
| AIKnowledge              | ExternalAiDbContext           | 1         | ⚙️ Inicial       |
| AIKnowledge              | AiGovernanceDbContext         | 1         | ⚙️ Inicial       |
| AIKnowledge              | AiOrchestrationDbContext      | 1         | ⚙️ Inicial       |
| Knowledge                | KnowledgeDbContext            | 0         | ❌ Sem migração  |
| Integrations             | IntegrationsDbContext         | 0         | ❌ Sem migração  |
| ProductAnalytics         | ProductAnalyticsDbContext     | 0         | ❌ Sem migração  |

**Total:** 20 directórios de migração, 71 ficheiros, 3 módulos sem migrações.

### 3.2 Análise de Maturidade

- **Nível 3 (Evoluído):** AuditCompliance, Notifications, CostIntelligence, Reliability — têm migrações
  incrementais para além do InitialCreate, indicando evolução activa do schema.
- **Nível 2 (Inicial):** 16 contextos com apenas InitialCreate — modelo definido mas sem evolução.
- **Nível 1 (Sem migração):** Knowledge, Integrations, ProductAnalytics — lacuna crítica.

### 3.3 Lacunas Críticas

#### Knowledge — Sem Migrações EF

O módulo Knowledge é pilar central do NexTraceOne enquanto Source of Truth e Knowledge Hub
operacional. A ausência de migrações significa que:

- As tabelas `knw_*` não são criadas automaticamente pelo pipeline de migrações.
- O módulo não pode ser deployado de forma consistente em ambientes novos.
- Existe risco de inconsistência entre o modelo de código e o schema real.

**Recomendação:** Gerar migração `InitialCreate` para `KnowledgeDbContext` com prioridade alta.

#### Integrations — Sem Migrações Explícitas

Com apenas 3 entidades e sem migrações, o módulo de integrações externas está funcional
apenas a nível de modelo. As integrações governadas (GitLab, Jenkins, GitHub, Azure DevOps)
dependem deste módulo para persistência.

**Recomendação:** Gerar migração `InitialCreate` para `IntegrationsDbContext` com prioridade média.

#### ProductAnalytics — Sem Migrações Explícitas

Com 1 entidade e sem migrações, este módulo é candidato a avaliação: deve persistir em
PostgreSQL ou directamente em ClickHouse para workloads analíticos?

**Recomendação:** Decidir estratégia de persistência antes de gerar migração. Se PostgreSQL
for confirmado, gerar `InitialCreate`. Se ClickHouse for preferido, documentar a decisão.

---

## 4. Consistência de Prefixos de Tabela

### 4.1 Convenção Implementada

Todos os módulos seguem a convenção de prefixo de 3 caracteres seguido de underscore:

| Módulo                   | Prefixo | Exemplo de Tabela            |
|--------------------------|---------|------------------------------|
| IdentityAccess           | `iam_`  | `iam_users`, `iam_roles`     |
| Catalog                  | `cat_`  | `cat_services`, `cat_contracts` |
| ChangeGovernance         | `chg_`  | `chg_changes`, `chg_workflows` |
| AIKnowledge              | `aik_`  | `aik_models`, `aik_policies`  |
| OperationalIntelligence  | `ops_`  | `ops_incidents`, `ops_costs`  |
| AuditCompliance          | `aud_`  | `aud_entries`, `aud_trails`   |
| Configuration            | `cfg_`  | `cfg_parameters`, `cfg_flags` |
| Governance               | `gov_`  | `gov_policies`, `gov_rules`   |
| Notifications            | `ntf_`  | `ntf_channels`, `ntf_templates` |
| Integrations             | `int_`  | `int_providers`, `int_connections` |
| Knowledge                | `knw_`  | `knw_articles`, `knw_notes`   |
| ProductAnalytics         | `pan_`  | `pan_events`                  |

### 4.2 Tabela Outbox

Cada módulo inclui uma tabela `{prefixo}_outbox_messages` para suporte ao padrão Outbox.
Esta consistência é fundamental para:

- Processamento fiável de eventos de integração entre módulos.
- Garantia de at-least-once delivery sem dependência de message broker externo para MVP1.
- Rastreabilidade de mensagens pendentes e falhas.

### 4.3 Avaliação de Consistência

**A convenção de prefixos está 100% consistente.** Todos os 12 módulos seguem o mesmo padrão,
o que é excelente para:

- Identificação rápida de tabelas por módulo em ferramentas de administração.
- Queries de diagnóstico e monitorização.
- Isolamento lógico claro sem ambiguidade.
- Preparação para eventual separação física se necessário.

**Nota:** O prefixo `int_` para Integrations pode gerar confusão com a keyword `int` em
linguagens de programação, mas no contexto PostgreSQL não há conflito.

---

## 5. Suporte a Multi-Tenancy

### 5.1 Estratégia Implementada: Row-Level Security (RLS)

O NexTraceOne utiliza **PostgreSQL Row-Level Security** como mecanismo principal de isolamento
multi-tenant. Esta é uma escolha sofisticada e adequada para o contexto enterprise.

### 5.2 Mecanismo: TenantRlsInterceptor

O `TenantRlsInterceptor` funciona da seguinte forma:

1. Antes de cada query, executa `SET LOCAL app.current_tenant = '{tenant_id}'`.
2. O PostgreSQL aplica políticas RLS configuradas nas tabelas relevantes.
3. Cada query retorna automaticamente apenas dados do tenant correcto.
4. O isolamento é enforced a nível de base de dados, não apenas a nível de aplicação.

### 5.3 Vantagens desta Abordagem

- **Segurança por desenho:** mesmo que o código aplicacional tenha um bug, o PostgreSQL
  impede acesso a dados de outro tenant.
- **Transparência:** o código EF Core não precisa de filtros `WHERE tenant_id = @id`
  explícitos em todas as queries.
- **Performance:** o PostgreSQL optimiza queries com RLS policies.
- **Auditabilidade:** as policies RLS são declarativas e podem ser auditadas.

### 5.4 Considerações e Riscos

- **Migrações:** as policies RLS devem ser criadas como parte das migrações ou scripts
  complementares. Verificar se todas as tabelas têm policies definidas.
- **Superuser bypass:** conexões com role de superuser ignoram RLS. O aplicativo deve
  usar roles restritivas.
- **Performance em escala:** com muitos tenants, as policies RLS devem ser monitorizadas
  para garantir que não degradam performance.
- **Testes:** os testes de integração devem validar isolamento entre tenants.

---

## 6. Auditoria e Encriptação

### 6.1 Auditoria via AuditInterceptor

O `AuditInterceptor` garante que todas as entidades que implementam a interface de auditoria
têm os seguintes campos preenchidos automaticamente:

| Campo        | Preenchimento           | Descrição                        |
|-------------|-------------------------|----------------------------------|
| `CreatedBy`  | Na criação da entidade  | Identificador do utilizador      |
| `CreatedAt`  | Na criação da entidade  | Timestamp UTC da criação         |
| `ModifiedBy` | Em cada actualização    | Identificador do utilizador      |
| `ModifiedAt` | Em cada actualização    | Timestamp UTC da modificação     |

**Avaliação:** A implementação via interceptor EF Core garante consistência sem depender de
disciplina dos desenvolvedores. Qualquer operação de SaveChanges é interceptada.

### 6.2 Encriptação via AesGcmEncryptor + EncryptedStringConverter

Para campos sensíveis (credenciais, tokens, dados pessoais), o NexTraceOne utiliza:

- **AesGcmEncryptor:** Implementação de encriptação AES-GCM (Galois/Counter Mode).
- **EncryptedStringConverter:** Value Converter do EF Core que encripta na escrita e
  desencripta na leitura de forma transparente.

#### Vantagens do AES-GCM

- Encriptação autenticada (confidencialidade + integridade).
- Resistente a ataques de padding oracle (ao contrário de AES-CBC).
- Performance adequada para campos individuais.

#### Considerações

- **Gestão de chaves:** a chave de encriptação deve ser gerida de forma segura
  (Key Vault, HSM ou configuração protegida).
- **Rotação de chaves:** deve existir estratégia para rotação sem perda de dados.
- **Pesquisa:** campos encriptados não podem ser pesquisados directamente no SQL.
  Se pesquisa for necessária, considerar hash indexável separado.

### 6.3 Ausência de SQL Raw

O codebase utiliza exclusivamente EF Core com queries parameterizadas, eliminando
riscos de SQL Injection. Não foi identificado uso de `FromSqlRaw`, `ExecuteSqlRaw`
ou concatenação manual de SQL.

**Avaliação:** Excelente prática de segurança que deve ser mantida como regra do projecto.

---

## 7. Completude das Entidades face à Visão do Produto

### 7.1 Análise por Pilar do Produto

#### Pilar 1: Service Governance ✅

O módulo Catalog com ~30+ entidades e 3 DbContexts cobre adequadamente:
- Service Catalog e grafo de dependências.
- Topologia de serviços.
- Metadata e classificação.

**Lacuna menor:** Verificar se lifecycle states de serviço estão completamente modelados.

#### Pilar 2: Contract Governance ✅

O `ContractsDbContext` e `DeveloperPortalDbContext` suportam:
- Contratos REST, SOAP, eventos, AsyncAPI.
- Versionamento e diff semântico.
- Publicação e workflow de aprovação.

**Lacuna menor:** Verificar se exemplos e schemas são persistidos como entidades ou como
atributos JSON de contratos.

#### Pilar 3: Change Intelligence ✅

O módulo ChangeGovernance com 4 DbContexts cobre o ciclo completo:
- Identidade da mudança.
- Validação e workflow.
- Governance de rulesets.
- Promoção entre ambientes.

**Lacuna:** Apenas migrações InitialCreate. Schema pode não reflectir evolução real.

#### Pilar 4: Operational Reliability ✅

O módulo OperationalIntelligence com 5 DbContexts é o mais abrangente:
- Runtime intelligence.
- Cost intelligence (FinOps).
- Automação (runbooks).
- Incidentes.
- Reliability (SLOs/SLIs).

**Ponto positivo:** CostIntelligence e Reliability já têm migrações evolutivas.

#### Pilar 5: AI Governance ✅

O módulo AIKnowledge com 3 DbContexts diferencia claramente:
- Integração com IA externa.
- Governance de modelos e políticas.
- Orquestração de agentes.

**Lacuna:** Apenas migrações InitialCreate. A evolução do domínio de IA será inevitável.

#### Pilar 6: Knowledge Hub ⚠️

O módulo Knowledge com apenas 3 entidades e **sem migrações** é a lacuna mais
significativa face à visão do produto. O Knowledge Hub é descrito como pilar central
da Source of Truth.

**Prioridade:** Alta. Este módulo necessita de evolução urgente.

#### Pilar 7: FinOps Contextual ✅

Coberto pelo `CostIntelligenceDbContext` dentro de OperationalIntelligence.
Já com migração evolutiva (P6_4).

### 7.2 Contagem de Entidades por Módulo

| Módulo                   | Entidades | Adequação face à Visão |
|--------------------------|-----------|------------------------|
| IdentityAccess           | 21        | ✅ Excelente            |
| Catalog                  | ~30+      | ✅ Excelente            |
| ChangeGovernance         | ~20+      | ✅ Boa                  |
| AIKnowledge              | ~20+      | ✅ Boa                  |
| OperationalIntelligence  | ~25+      | ✅ Excelente            |
| Governance               | 9         | ⚙️ Em evolução          |
| Notifications            | 6         | ✅ Adequada             |
| AuditCompliance          | 6         | ✅ Adequada             |
| Configuration            | 6         | ✅ Adequada             |
| Integrations             | 3         | ⚠️ Insuficiente         |
| Knowledge                | 3         | ⚠️ Insuficiente         |
| ProductAnalytics         | 1         | ⚠️ Mínima              |

---

## 8. Problemas Identificados e Recomendações

### 8.1 Problema Crítico: Knowledge sem Migrações

**Severidade:** Alta
**Impacto:** O Knowledge Hub não pode ser deployado de forma consistente em novos ambientes.
**Recomendação:** Gerar `InitialCreate` para `KnowledgeDbContext` imediatamente. Considerar
expandir as 3 entidades actuais para cobrir:
- Artigos e documentação operacional.
- Notas operacionais com ownership.
- Relações entre knowledge items e serviços/contratos/mudanças.
- Changelog operacional.
- Search index para PostgreSQL FTS.

### 8.2 Problema Significativo: Integrations sem Migrações

**Severidade:** Média
**Impacto:** Integrações externas não podem ser persistidas de forma evolutiva.
**Recomendação:** Gerar `InitialCreate` para `IntegrationsDbContext`. Avaliar se 3 entidades
são suficientes para:
- Provedores de integração (GitLab, Jenkins, GitHub, Azure DevOps).
- Conexões configuradas por tenant/ambiente.
- Credenciais encriptadas.
- Estado de sincronização.
- Auditoria de chamadas.

### 8.3 Problema Menor: ProductAnalytics sem Migrações

**Severidade:** Baixa
**Impacto:** Telemetria de uso do produto não tem persistência formal.
**Recomendação:** Decidir se ProductAnalytics persiste em PostgreSQL ou migra para ClickHouse.
Documentar decisão antes de criar migração.

### 8.4 Problema Estrutural: 24 Connection Strings para 1 Banco

**Severidade:** Informativa
**Impacto:** Complexidade de configuração sem benefício imediato.
**Justificação:** Permite separação física futura sem alteração de código.
**Recomendação:** Manter a abordagem actual. Considerar documentar explicitamente que todas
apontam para o mesmo banco físico e que a separação é intencional para evolução.

### 8.5 Problema Potencial: Consistência de RLS Policies

**Severidade:** Média
**Impacto:** Se alguma tabela não tiver policy RLS definida, dados de outros tenants podem
ser acessíveis.
**Recomendação:** Criar teste de integração que valide que todas as tabelas com dados
multi-tenant têm policies RLS activas. Considerar script de verificação no pipeline de CI.

### 8.6 Problema de Evolução: Maioria dos Contextos em InitialCreate

**Severidade:** Informativa
**Impacto:** 16 de 23 contextos têm apenas `InitialCreate`, indicando fase inicial.
**Recomendação:** Isto é normal para o estágio actual do projecto. À medida que os módulos
evoluem, as migrações incrementais aparecerão naturalmente. Não é necessária acção imediata.

### 8.7 Recomendação de Rotação de Chaves de Encriptação

**Severidade:** Média (futura)
**Impacto:** Sem estratégia de rotação, a chave AES-GCM torna-se ponto único de falha.
**Recomendação:** Implementar mecanismo de rotação de chaves com re-encriptação gradual.
Documentar procedimento operacional para self-hosted customers.

---

## 9. O que Está em Falta

### 9.1 Migrações em Falta (Prioridade Ordenada)

| Prioridade | Módulo           | DbContext                    | Acção Necessária                       |
|-----------|------------------|------------------------------|----------------------------------------|
| 🔴 Alta    | Knowledge        | KnowledgeDbContext           | Gerar `InitialCreate` imediatamente    |
| 🟡 Média   | Integrations     | IntegrationsDbContext        | Gerar `InitialCreate`                  |
| 🟢 Baixa   | ProductAnalytics | ProductAnalyticsDbContext    | Decidir estratégia antes de migrar     |

### 9.2 Entidades Potencialmente em Falta para a Visão Completa

#### Knowledge (actualmente 3 entidades, necessário ~8-12)
- `knw_articles` — artigos de documentação.
- `knw_operational_notes` — notas operacionais com ownership.
- `knw_changelogs` — changelogs operacionais.
- `knw_relations` — relações knowledge ↔ serviço/contrato/mudança.
- `knw_categories` — categorização de conhecimento.
- `knw_search_index` — índice para PostgreSQL FTS.
- `knw_versions` — versionamento de artigos.
- `knw_contributors` — contribuidores por artigo.
- `knw_outbox_messages` — suporte outbox.

#### Integrations (actualmente 3 entidades, necessário ~6-8)
- `int_providers` — provedores de integração.
- `int_connections` — conexões configuradas.
- `int_credentials` — credenciais encriptadas.
- `int_sync_state` — estado de sincronização.
- `int_webhook_configs` — configuração de webhooks.
- `int_audit_logs` — auditoria de chamadas externas.
- `int_outbox_messages` — suporte outbox.

### 9.3 Funcionalidades de Persistência em Falta

| Funcionalidade                        | Estado    | Prioridade |
|--------------------------------------|-----------|------------|
| Migrações Knowledge                  | Ausente   | Alta       |
| Migrações Integrations              | Ausente   | Média      |
| Migrações ProductAnalytics          | Ausente   | Baixa      |
| Teste de validação RLS completo     | Ausente   | Média      |
| Rotação de chaves de encriptação    | Ausente   | Média      |
| Script de verificação de schema     | Ausente   | Baixa      |
| Documentação de modelo de dados     | Parcial   | Média      |
| Seed data para ambientes de demo    | Parcial   | Baixa      |

### 9.4 Direcção Futura: ClickHouse

A direcção arquitectural aponta para **ClickHouse** como motor analítico complementar ao
PostgreSQL. As seguintes áreas são candidatas a migrar ou duplicar dados para ClickHouse:

- Telemetria e observabilidade (alto volume temporal).
- ProductAnalytics (workload puramente analítico).
- Métricas de custo agregadas (FinOps reporting).
- Histórico de auditoria de longo prazo (retenção extendida).
- Dados de traces OpenTelemetry.

A decisão sobre ProductAnalytics depende directamente desta avaliação.

---

## 10. Conclusão e Próximos Passos

### 10.1 Estado Geral

A arquitetura de persistência do NexTraceOne está **bem fundamentada e correctamente
estruturada** para um projecto nesta fase de evolução. Os pontos fortes são:

- ✅ Isolamento lógico por módulo com prefixos consistentes.
- ✅ Multi-tenancy via PostgreSQL RLS (abordagem enterprise-grade).
- ✅ Auditoria automática via interceptor.
- ✅ Encriptação de campos sensíveis via AES-GCM.
- ✅ Padrão Outbox para mensagens transaccionais.
- ✅ Ausência de SQL raw (segurança contra injection).
- ✅ 23 DbContexts bem distribuídos por 12 módulos.
- ✅ Classe base consistente (`NexTraceDbContextBase`).

### 10.2 Lacunas a Endereçar

- ⚠️ **Knowledge sem migrações** — prioridade máxima.
- ⚠️ **Integrations sem migrações** — prioridade média.
- ⚠️ **ProductAnalytics sem migrações** — requer decisão arquitectural.
- ⚠️ **Validação de RLS policies** — necessita teste automatizado.
- ⚠️ **Estratégia de rotação de chaves** — necessária para produção.

### 10.3 Próximos Passos Recomendados (por ordem de prioridade)

1. **Gerar migração `InitialCreate` para `KnowledgeDbContext`** — desbloqueio do Knowledge Hub.
2. **Gerar migração `InitialCreate` para `IntegrationsDbContext`** — desbloqueio de integrações.
3. **Decidir estratégia de persistência para ProductAnalytics** — PostgreSQL vs ClickHouse.
4. **Criar teste de validação de RLS policies** — segurança multi-tenant.
5. **Documentar modelo de dados completo** — diagramas ER por módulo.
6. **Planear estratégia de rotação de chaves** — readiness para produção.
7. **Avaliar entidades em falta em Knowledge e Integrations** — expandir modelos.
8. **Definir estratégia de seed data** — ambientes de demo e testes.

### 10.4 Nota Final

A fundação de persistência é sólida e preparada para evolução. As lacunas identificadas são
normais para a fase actual do projecto e não representam problemas estruturais. A prioridade
deve ser fechar os gaps de migrações em Knowledge e Integrations para que todos os módulos
tenham persistência operacional completa.

O NexTraceOne demonstra maturidade arquitectural na camada de dados que é rara em projectos
desta fase, especialmente nos aspectos de segurança (RLS, encriptação, auditoria) e
modularidade (23 contextos com fronteiras claras). Esta fundação permitirá evolução
sustentável à medida que o produto amadurece.

---

*Relatório gerado em 2026-03-27 como parte da avaliação de estado da base de dados e persistência do NexTraceOne.*
