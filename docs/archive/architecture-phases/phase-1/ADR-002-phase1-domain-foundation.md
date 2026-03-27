# ADR-002 — Fase 1: Fundação de Domínio, Contexto e Contratos-Base

**Status:** Accepted  
**Data:** 2026-03-20  
**Autores:** Arquitetura NexTraceOne  
**Revisores:** Engenharia, Produto, Segurança  
**Relacionado com:** ADR-001 (Tenant-Aware Environment Context)

---

## 1. Contexto

O ADR-001 identificou os problemas estruturais do modelo atual: ambientes como string literal, entidades sem TenantId, IA sem contexto. A Fase 1 é a fundação para corrigir esses problemas de forma incremental e não destrutiva.

Esta ADR documenta as decisões arquiteturais tomadas na Fase 1, que estabelece a base de domínio sobre a qual as Fases 2+ construirão.

---

## 2. Decisões Tomadas

### D1 — Ambiente é entidade com perfil operacional, não enum fixo

**Decisão:** O conceito de `EnvironmentProfile` (enum) é separado do nome do ambiente (string livre do tenant). A entidade `Environment` passa a ter um `Profile` que define seu comportamento operacional base.

**Motivação:** Uma empresa pode chamar seu ambiente de "QA-EUROPA" e outra de "HML-BANCO-X", mas ambos têm perfil `Validation`. O produto não pode hardcodar 3 ambientes fixos nem depender do nome para tomar decisões.

**Perfis disponíveis:** Development, Validation, Staging, Production, Sandbox, DisasterRecovery, Training, UserAcceptanceTesting, PerformanceTesting.

**Implementação:**
- `NexTraceOne.IdentityAccess.Domain/Enums/EnvironmentProfile.cs`
- `NexTraceOne.IdentityAccess.Domain/Enums/EnvironmentCriticality.cs`
- Extensão de `Environment` entity com campos: `Profile`, `Code`, `Description`, `Criticality`, `Region`, `IsProductionLike`

### D2 — TenantId + EnvironmentId é o contexto mínimo obrigatório

**Decisão:** Toda operação com escopo operacional deve ser capaz de ser resolvida com `TenantId + EnvironmentId`. O `TenantEnvironmentContext` é o value object que materializa este contexto.

**Motivação:** Sem o par `{TenantId, EnvironmentId}`, não é possível garantir isolamento, roteamento correto de telemetria, ou autorização de acesso à IA.

**Implementação:**
- `NexTraceOne.IdentityAccess.Domain/ValueObjects/TenantEnvironmentContext.cs`

### D3 — Backend é a fonte de verdade para comportamento contextual do frontend

**Decisão:** O `EnvironmentUiProfile` é gerado e enviado pelo backend para que o frontend materialize a experiência contextual (badge color, avisos de proteção, habilitação de ações destrutivas).

**Motivação:** O frontend não deve decidir se um ambiente é "perigoso" ou não. Esta decisão é do domínio.

**Implementação:**
- `NexTraceOne.IdentityAccess.Domain/ValueObjects/EnvironmentUiProfile.cs`

### D4 — Políticas de ambiente são entidades de domínio, não configurações de infraestrutura

**Decisão:** `EnvironmentPolicy`, `EnvironmentIntegrationBinding` e `EnvironmentTelemetryPolicy` são entidades de domínio que formalizam as regras operacionais por ambiente. A persistência é adiada para Fase 2.

**Motivação:** Centralizar no domínio a modelagem de regras operacionais evita que essas regras fiquem espalhadas em configurações de infraestrutura ou hardcoded no frontend.

**Implementação:**
- `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentPolicy.cs`
- `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentIntegrationBinding.cs`
- `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentTelemetryPolicy.cs`

### D5 — IA é uma capacidade transversal e única, context-aware

**Decisão:** Não existem "IA de DEV", "IA de QA", "IA de PROD". Existe uma IA que opera com `AiExecutionContext` explícito. O contexto determina o escopo de dados, profundidade de análise e modo de atuação.

**Motivação:** Criar IAs separadas por ambiente multiplica a complexidade sem benefício. O que muda é o contexto, não a capacidade.

**Implementação:**
- `NexTraceOne.AIKnowledge.Domain/Orchestration/Context/AiExecutionContext.cs`
- Inclui: `TenantId`, `EnvironmentId`, `EnvironmentProfile`, `IsProductionLike`, `AiUserContext`, `AllowedDataScopes`, `ModuleContext`, `AiTimeWindow`, `AiReleaseContext`

### D6 — IA deve ser capaz de analisar ambientes não produtivos para proteção da produção

**Decisão:** A IA tem papel central na análise de pré-produção: detectar regressões, calcular risco de promoção e gerar avaliações de readiness. Isso é formalizado no domínio como:
- `PromotionRiskAnalysisContext` — contexto de análise de risco antes de uma promoção
- `EnvironmentComparisonContext` — comparação entre dois ambientes do mesmo tenant
- `RiskFinding` — achado de risco rastreável com evidências
- `RegressionSignal` — sinal de regressão mensurável (valor atual vs baseline)
- `ReadinessAssessment` — avaliação de prontidão com score (0-100) e recomendação

**Motivação:** Problemas detectáveis em QA/UAT que chegam à produção representam falha do sistema. A IA deve ser o primeiro nível de detecção desses problemas.

### D7 — Abstrações de contexto vivem nos módulos corretos, não nos BuildingBlocks

**Decisão:** As interfaces de resolução de contexto (`IEnvironmentContextAccessor`, `ITenantEnvironmentContextResolver`, `IEnvironmentProfileResolver`) foram colocadas em `IdentityAccess.Application.Abstractions`. As interfaces de IA (`IAIContextBuilder`, `IPromotionRiskContextBuilder`) foram colocadas em `AIKnowledge.Application.Abstractions`.

**Motivação:** Adicionar referências de módulos ao `BuildingBlocks.Application` criaria dependências circulares (os módulos já dependem dos BuildingBlocks). As abstrações devem viver no módulo que as "possui", não na camada transversal.

---

## 3. O Que Esta Decisão Não Abrange (Fase 2+)

- Migração de banco para os novos campos de `Environment` (pendente `AddEnvironmentProfileFields`)
- Migração de banco para `EnvironmentPolicy`, `EnvironmentIntegrationBinding`, `EnvironmentTelemetryPolicy`
- Refatoração dos módulos operacionais (ChangeGovernance, OperationalIntelligence) para usar `EnvironmentId`
- Implementação dos resolvers e builders (apenas interfaces/contratos criados)
- Integração da IA com as fontes de dados reais (telemetria, incidentes, contratos)
- Frontend contextual orientado por `EnvironmentUiProfile`

---

## 4. Impacto de Compatibilidade

Nenhuma funcionalidade existente foi quebrada. A entidade `Environment` mantém backward compatibility através da sobrecarga do factory method. Os novos campos são ignorados pelo EF Core via `builder.Ignore()` até que a migration da Fase 2 seja adicionada.

---

## 5. Critérios de Revisão

Esta decisão deve ser revisada quando:
- A Fase 2 iniciar a migração de banco
- Mais de 3 módulos forem refatorados para usar `EnvironmentId` tipado
- A implementação da IA de análise de regressão for iniciada
