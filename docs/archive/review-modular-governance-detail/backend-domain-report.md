# Relatório do Modelo de Domínio — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria de domínio DDD  
> **Escopo:** Entidades, value objects, agregados, invariantes, serviços de domínio

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Entidades de domínio totais | 382 |
| Entity configurations | 130 |
| Módulos com domínio | 9 |
| Base entity | Entity\<TId\> |
| Base aggregate | AggregateRoot\<TId\> |
| Value object base | ValueObject |
| Auditable base | AuditableEntity\<TId\> |
| Strongly-typed IDs | TypedIdBase |

---

## 2. Fundações do Domínio (Building Block Core)

### 2.1 Hierarquia de Entidades

```
object
  └── Entity<TId>                    // entidade base com identidade
       ├── AuditableEntity<TId>      // entidade com soft delete e timestamps
       │    └── AggregateRoot<TId>   // raiz de agregado com eventos de domínio
       └── ValueObject               // objecto de valor (comparação por conteúdo)
```

### 2.2 TypedIdBase

Todos os IDs de domínio são strongly-typed via `TypedIdBase`:

```csharp
public record ServiceId(Guid Value) : TypedIdBase(Value);
public record ContractId(Guid Value) : TypedIdBase(Value);
```

**Benefícios:**
- Impossibilidade de misturar IDs de tipos diferentes
- Conversão automática para persistência
- Segurança de tipos em compile-time

### 2.3 AuditableEntity

Todas as entidades persistidas herdam de `AuditableEntity<TId>`, que fornece:

| Campo | Tipo | Finalidade |
|-------|------|-----------|
| CreatedAt | DateTimeOffset | Data de criação |
| CreatedBy | string | Utilizador que criou |
| ModifiedAt | DateTimeOffset? | Data de modificação |
| ModifiedBy | string? | Utilizador que modificou |
| IsDeleted | bool | Flag de soft delete |
| DeletedAt | DateTimeOffset? | Data de eliminação |

---

## 3. Análise por Módulo

### 3.1 catalog — 82 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | ALTA — 82 entidades cobrindo contratos, serviços, versões, schemas |
| Value objects | Presentes para tipos de contrato, versões, estados |
| Agregados | ServiceAggregate, ContractAggregate como raízes de agregado |
| Invariantes | Validações de consistência em contratos e versões |
| Domain services | Serviços de compatibilidade, validação, diff |
| Acoplamento com infra | BAIXO — domínio limpo |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| Service | AggregateRoot | CatalogGraphDb |
| ServiceDependency | Entity | CatalogGraphDb |
| ServiceOwnership | Entity | CatalogGraphDb |
| Contract | AggregateRoot | ContractsDb |
| ContractVersion | Entity | ContractsDb |
| ContractSchema | ValueObject | ContractsDb |
| ApiEndpoint | Entity | ContractsDb |
| EventContract | Entity | ContractsDb |
| SoapService | Entity | ContractsDb |
| DeveloperPortalEntry | Entity | DeveloperPortalDb |

**Observações:**
- Módulo mais rico em entidades — justificado pela centralidade no produto
- Bom uso de agregados com limites claros
- Value objects para schemas e versões é padrão DDD adequado

---

### 3.2 aiknowledge — 72 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | ALTA — 72 entidades cobrindo IA externa, governança, orquestração |
| Value objects | Presentes para configuração de modelos, tokens, quotas |
| Agregados | AiModel, AiSession, AiPolicy como raízes |
| Invariantes | Validações de quota, budget, acesso a modelos |
| Domain services | Orquestração de sessões, avaliação de políticas |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| AiProvider | AggregateRoot | ExternalAiDb |
| AiModel | Entity | ExternalAiDb |
| AiSession | AggregateRoot | AiOrchestrationDb |
| AiMessage | Entity | AiOrchestrationDb |
| AiKnowledgeSource | Entity | AiOrchestrationDb |
| AiPolicy | AggregateRoot | AiGovernanceDb |
| AiTokenBudget | Entity | AiGovernanceDb |
| AiAccessRule | Entity | AiGovernanceDb |
| AiAuditEntry | Entity | AiGovernanceDb |
| AiIdeExtension | Entity | AiGovernanceDb |

**Observações:**
- Três bounded contexts bem separados (External, Governance, Orchestration)
- Modelo de governança de IA é sofisticado e alinhado com a visão do produto

---

### 3.3 governance — 58 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | ALTA — 58 entidades cobrindo governança organizacional completa |
| Value objects | Presentes para políticas, scores, métricas |
| Agregados | Team, Domain, Policy, GovernancePack como raízes |
| Invariantes | Regras de ownership, compliance, waivers |
| Domain services | Avaliação de compliance, cálculo de risco, FinOps |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| Team | AggregateRoot | GovernanceDb |
| Domain | AggregateRoot | GovernanceDb |
| Policy | AggregateRoot | GovernanceDb |
| GovernancePack | Entity | GovernanceDb |
| Waiver | Entity | GovernanceDb |
| ComplianceReport | Entity | GovernanceDb |
| RiskAssessment | Entity | GovernanceDb |
| FinOpsEntry | Entity | GovernanceDb |
| Control | Entity | GovernanceDb |
| Evidence | Entity | GovernanceDb |

**Observações:**
- Único módulo com 18 endpoint modules — alta granularidade no acesso
- Modelo de domínio é coerente apesar da abrangência

---

### 3.4 operationalintelligence — 51 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | MÉDIA-ALTA — 51 entidades em 5 bounded contexts |
| Value objects | Presentes para severidades, estados de incidente |
| Agregados | Incident, Automation, RunbookExecution como raízes |
| Invariantes | Estados de incidente, aprovações de automação |
| Domain services | Correlação de incidentes, execução de runbooks |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| Incident | AggregateRoot | IncidentDb |
| MitigationAction | Entity | IncidentDb |
| Automation | AggregateRoot | AutomationDb |
| AutomationExecution | Entity | AutomationDb |
| Runbook | Entity | AutomationDb |
| CostReport | AggregateRoot | CostIntelDb |
| ReliabilityMetric | Entity | ReliabilityDb |
| RuntimeInsight | Entity | RuntimeIntelDb |

**Observações:**
- 5 DbContexts para 51 entidades — boa separação por bounded context
- Cada subdomain (Incident, Automation, Cost, Reliability, Runtime) é coerente

---

### 3.5 changegovernance — 47 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | MÉDIA-ALTA — 47 entidades em 4 bounded contexts |
| Value objects | Presentes para tipos de mudança, estados de workflow |
| Agregados | Change, PromotionRequest, Ruleset, WorkflowInstance como raízes |
| Invariantes | Regras de promoção, gates, aprovações |
| Domain services | Avaliação de blast radius, correlação change-incident |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| Change | AggregateRoot | ChangeIntelDb |
| BlastRadius | Entity | ChangeIntelDb |
| ChangeCorrelation | Entity | ChangeIntelDb |
| PromotionRequest | AggregateRoot | PromotionDb |
| PromotionGate | Entity | PromotionDb |
| Ruleset | AggregateRoot | RulesetGovernanceDb |
| RulesetExecution | Entity | RulesetGovernanceDb |
| WorkflowTemplate | AggregateRoot | WorkflowDb |
| WorkflowInstance | Entity | WorkflowDb |
| WorkflowApproval | Entity | WorkflowDb |

**Observações:**
- 4 bounded contexts claros e bem separados
- Pilar central do produto (Change Confidence)

---

### 3.6 identityaccess — 37 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | MÉDIA — 37 entidades cobrindo identidade completa |
| Value objects | Presentes para permissões, claims, tokens |
| Agregados | User, Role, Tenant como raízes |
| Invariantes | Regras de sessão, expiração, delegação |
| Domain services | Autenticação, gestão de acessos |
| Acoplamento com infra | BAIXO (mas JWT handling cruza camadas) |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| User | AggregateRoot | IdentityDb |
| Role | AggregateRoot | IdentityDb |
| Permission | Entity | IdentityDb |
| Tenant | AggregateRoot | IdentityDb |
| Session | Entity | IdentityDb |
| BreakGlassAccess | Entity | IdentityDb |
| JitAccess | Entity | IdentityDb |
| Delegation | Entity | IdentityDb |
| AccessReview | Entity | IdentityDb |
| EnvironmentAccess | Entity | IdentityDb |

**Observações:**
- 10 sub-módulos de endpoint para 37 entidades — granularidade elevada
- Falta seed de produção para roles e permissões iniciais

---

### 3.7 notifications — 15 Entidades

**Classificação:** ⚠️ PARCIAL

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | BAIXA — 15 entidades, funcionalidade incompleta |
| Value objects | Presentes para tipos de notificação, canais |
| Agregados | Notification como raiz |
| Invariantes | Regras de preferência e envio |
| Domain services | Limitados |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| Notification | AggregateRoot | NotificationsDb |
| NotificationPreference | Entity | NotificationsDb |
| NotificationChannel | ValueObject | NotificationsDb |
| NotificationTemplate | Entity | NotificationsDb |

**Observações:**
- Estado PARCIAL — sem migrações, funcionalidade incompleta
- Modelo de domínio é simples mas adequado para o escopo actual

---

### 3.8 auditcompliance — 11 Entidades

**Classificação:** ✅ COERENTE

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | BAIXA — 11 entidades focadas em auditoria |
| Value objects | Presentes para tipos de evento, severidades |
| Agregados | AuditEvent como raiz |
| Invariantes | Imutabilidade de registos de auditoria |
| Domain services | Limitados (predominantemente leitura) |
| Acoplamento com infra | BAIXO |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| AuditEvent | AggregateRoot | AuditDb |
| AuditTrail | Entity | AuditDb |
| ComplianceCheck | Entity | AuditDb |
| ComplianceReport | Entity | AuditDb |

---

### 3.9 configuration — 6 Entidades

**Classificação:** ⚠️ PARCIAL

| Aspecto | Avaliação |
|---------|-----------|
| Riqueza do modelo | MÍNIMA — 6 entidades para configurações |
| Value objects | Tipos de configuração, fases |
| Agregados | ConfigurationDefinition como raiz |
| Invariantes | Validação de valores de configuração |
| Domain services | Limitados |
| Acoplamento com infra | ⚠️ EnsureCreated cria dependência com infra |

**Entidades principais (estimativa):**

| Entidade | Tipo | DbContext |
|----------|------|----------|
| ConfigurationDefinition | AggregateRoot | ConfigurationDb |
| ConfigurationValue | Entity | ConfigurationDb |
| ConfigurationPhase | ValueObject | ConfigurationDb |

**Observações:**
- Usa EnsureCreated em vez de migrações — problema operacional
- 600+ definições de configuração via seeder

---

## 4. Classificação Global

| Módulo | Entidades | Classificação | Justificação |
|--------|-----------|---------------|-------------|
| catalog | 82 | ✅ COERENTE | Modelo rico, bem estruturado, pilar do produto |
| aiknowledge | 72 | ✅ COERENTE | 3 bounded contexts bem separados, governança sofisticada |
| governance | 58 | ✅ COERENTE | Abrangente mas coerente, 18 endpoints |
| operationalintelligence | 51 | ✅ COERENTE | 5 bounded contexts bem isolados |
| changegovernance | 47 | ✅ COERENTE | 4 bounded contexts claros |
| identityaccess | 37 | ✅ COERENTE | Modelo seguro, 10 sub-módulos |
| notifications | 15 | ⚠️ PARCIAL | Funcionalidade incompleta, sem migrações |
| auditcompliance | 11 | ✅ COERENTE | Compacto e focado |
| configuration | 6 | ⚠️ PARCIAL | EnsureCreated em vez de migrações |

### Classificações Possíveis

| Classificação | Definição |
|---------------|-----------|
| ✅ COERENTE | Modelo de domínio rico, bem estruturado, aderente a DDD |
| ⚠️ PARCIAL | Modelo funcional mas com lacunas ou problemas |
| ❌ ANÉMICO | Modelo sem comportamento, apenas dados (getters/setters) |
| 🔴 ACOPLADO_A_INFRA | Domínio com dependências de infraestrutura |
| 🟡 INCONSISTENTE | Convenções inconsistentes entre entidades |

---

## 5. Análise de Padrões Transversais

### 5.1 Strongly-Typed IDs

| Aspecto | Estado |
|---------|--------|
| Cobertura | ✅ Todos os módulos usam TypedIdBase |
| Conversão automática | ✅ Via EF Core value converters |
| Type safety | ✅ Impossível misturar IDs de tipos diferentes |

### 5.2 Soft Deletes

| Aspecto | Estado |
|---------|--------|
| Implementação | Via AuditableEntity com IsDeleted/DeletedAt |
| Filtro global | ✅ Aplicado via query filters no EF Core |
| Consistência | ✅ Todas as entidades persistidas herdam AuditableEntity |

### 5.3 Eventos de Domínio

| Aspecto | Estado |
|---------|--------|
| Mecanismo | AggregateRoot com coleção de domain events |
| Publicação | Via Outbox pattern para eventual consistency |
| Consistência | ✅ Eventos publicados apenas após commit bem sucedido |

---

## 6. Recomendações

### Prioridade ALTA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 1 | Criar migrações para configuration | configuration | EnsureCreated impede evolução controlada |
| 2 | Criar migrações para notifications | notifications | Schema não versionado |
| 3 | Completar modelo de domínio do notifications | notifications | Estado PARCIAL |

### Prioridade MÉDIA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 4 | Documentar invariantes de negócio por agregado | todos | Facilita manutenção e review |
| 5 | Verificar cobertura de domain events | todos | Garantir eventual consistency |
| 6 | Auditar value objects vs entidades | todos | Garantir classificação correcta |

### Prioridade BAIXA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 7 | Adicionar XML docs a entidades de domínio | todos | Documentação ausente |
| 8 | Criar diagrama de agregados por módulo | todos | Facilita onboarding |
