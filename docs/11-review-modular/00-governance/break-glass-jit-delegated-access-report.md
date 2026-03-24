# Relatório de Auditoria — Break Glass, JIT, Delegação e Access Review

> **Módulo:** `src/modules/identityaccess/`  
> **Data da análise:** 2025-07  
> **Classificação:** IMPLEMENTED_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O NexTraceOne implementa quatro mecanismos de controlo de acesso avançado, todos com entidades de domínio, endpoints de API e persistência em base de dados:

1. **JIT Access** — Acesso just-in-time com aprovação, prazo de 4h e janela de 8h
2. **Break Glass** — Acesso de emergência imediato com janela de 2h, post-mortem obrigatório e limite trimestral
3. **Delegação** — Transferência temporária de papel com justificação obrigatória
4. **Access Review** — Campanhas de revisão periódica com modelo item-a-item

A classificação IMPLEMENTED_APPARENT reflecte que todos os quatro mecanismos possuem implementação funcional ao nível de entidade + endpoint + persistência, com lacunas residuais em automação (post-mortem enforcement, cleanup de expirados).

---

## 2. JIT Access (Just-In-Time)

### 2.1 Entidade JitAccessRequest

| Campo | Tipo | Propósito |
|---|---|---|
| Id | GUID | Identificador único |
| RequestedBy | FK → User | Solicitante |
| TenantId | FK → Tenant | Tenant |
| PermissionCode | string | Permissão solicitada |
| Scope | string | Âmbito da permissão |
| Justification | string | Justificação obrigatória |
| Status | enum | Pending / Approved / Rejected / Revoked |
| GrantedFrom | DateTime? | Início da concessão |
| GrantedUntil | DateTime? | Fim da concessão |

### 2.2 Lifecycle

```
┌──────────┐     Aprovação     ┌──────────┐     Janela     ┌──────────┐
│ Pending  │ ─────────────────→│ Approved │ ────expira────→│ Expired  │
└──────────┘                   └──────────┘                └──────────┘
     │                              │
     │ Rejeição                     │ Revogação manual
     ↓                              ↓
┌──────────┐                  ┌──────────┐
│ Rejected │                  │ Revoked  │
└──────────┘                  └──────────┘
```

### 2.3 Parâmetros Temporais

| Parâmetro | Valor | Justificação |
|---|---|---|
| Prazo de aprovação | 4 horas | Evita pedidos pendentes indefinidamente |
| Janela de concessão | 8 horas | Limita exposição temporal |

### 2.4 Endpoints

| Endpoint | Acção | Módulo |
|---|---|---|
| POST /jit-access | Criar pedido | JitAccessEndpoints |
| GET /jit-access | Listar pedidos | JitAccessEndpoints |
| PUT /jit-access/{id}/approve | Aprovar | JitAccessEndpoints |
| PUT /jit-access/{id}/reject | Rejeitar | JitAccessEndpoints |
| PUT /jit-access/{id}/revoke | Revogar | JitAccessEndpoints |

### 2.5 Análise de Segurança

| Aspecto | Estado | Avaliação |
|---|---|---|
| Justificação obrigatória | ✅ | Rastreabilidade |
| Aprovação necessária | ✅ | Segregação de deveres |
| Prazo de aprovação | ✅ | Anti-stale |
| Janela temporal | ✅ | Minimização de exposição |
| Revogação manual | ✅ | Controlo |
| SecurityEvent tracking | ✅ | Auditoria |
| Cleanup automático de expirados | ❌ | **Lacuna** |
| Self-approval prevention | ⚠️ | Verificar implementação |

---

## 3. Break Glass (Acesso de Emergência)

### 3.1 Entidade BreakGlassRequest

| Campo | Tipo | Propósito |
|---|---|---|
| Id | GUID | Identificador único |
| UserId | FK → User | Utilizador |
| TenantId | FK → Tenant | Tenant |
| Reason | string | Motivo da emergência |
| Status | enum | Active / Expired / Revoked / PostMortemCompleted |
| ActivatedAt | DateTime | Momento de activação |
| ExpiresAt | DateTime | Expiração (2h após activação) |
| RevokedAt | DateTime? | Revogação manual |
| PostMortemAt | DateTime? | Timestamp do post-mortem |
| PostMortemContent | string? | Conteúdo do post-mortem |
| IpAddress | string | IP de activação |
| UserAgent | string | Agente de activação |

### 3.2 Lifecycle

```
┌──────────┐     2 horas      ┌──────────┐    Post-mortem    ┌────────────────────┐
│  Active  │ ────────────────→│ Expired  │ ─────────────────→│ PostMortemCompleted │
└──────────┘                  └──────────┘                   └────────────────────┘
     │
     │ Revogação manual
     ↓
┌──────────┐    Post-mortem    ┌────────────────────┐
│ Revoked  │ ─────────────────→│ PostMortemCompleted │
└──────────┘                   └────────────────────┘
```

### 3.3 Controlos de Segurança

| Controlo | Implementação | Avaliação |
|---|---|---|
| Activação imediata (sem aprovação) | ✅ | Propósito de emergência |
| Janela de 2 horas | ✅ | Limita exposição |
| Post-mortem obrigatório em 24h | ✅ (policy, não enforcement) | ⚠️ |
| Limite trimestral: 3/utilizador | ✅ | Anti-abuso |
| IP tracking | ✅ | Rastreabilidade |
| UserAgent tracking | ✅ | Rastreabilidade |
| SecurityEvent (RiskScore: 60) | ✅ | Auditoria |
| Razão obrigatória | ✅ | Documentação |

### 3.4 Endpoints

| Endpoint | Acção |
|---|---|
| POST /break-glass | Activar |
| GET /break-glass | Listar |
| PUT /break-glass/{id}/revoke | Revogar |
| PUT /break-glass/{id}/post-mortem | Submeter post-mortem |

### 3.5 Lacunas Específicas

| Lacuna | Severidade | Detalhe |
|---|---|---|
| Enforcement automatizado de post-mortem | MÉDIA | Sem notificação/escalação automática após 24h |
| Notificação em tempo real | MÉDIA | Sem alerta a admins no momento da activação |
| Background job para expiração | BAIXA | Sem job que transiciona Active→Expired automaticamente |

---

## 4. Delegação

### 4.1 Entidade Delegation

| Campo | Tipo | Propósito |
|---|---|---|
| Id | GUID | Identificador único |
| DelegatedBy | FK → User | Quem delega |
| DelegatedTo | FK → User | Quem recebe |
| RoleId | FK → Role | Papel delegado |
| TenantId | FK → Tenant | Tenant |
| StartsAt | DateTime | Início da delegação |
| EndsAt | DateTime | Fim da delegação |
| Reason | string | Justificação obrigatória |
| RevokedAt | DateTime? | Revogação manual |

### 4.2 Lifecycle

```
┌──────────┐     StartsAt     ┌──────────┐     EndsAt      ┌──────────┐
│ Pending  │ ────────────────→│  Active  │ ───────────────→│ Expired  │
└──────────┘                  └──────────┘                 └──────────┘
                                   │
                                   │ Revogação
                                   ↓
                              ┌──────────┐
                              │ Revoked  │
                              └──────────┘
```

### 4.3 Controlos de Segurança

| Controlo | Implementação | Avaliação |
|---|---|---|
| Time-bounded | ✅ StartsAt/EndsAt | Sem delegação indefinida |
| Reason obrigatório | ✅ | Rastreabilidade |
| Scope por RoleId + TenantId | ✅ | Limitado ao papel e tenant |
| SecurityEvent tracking | ✅ | DelegationCreated/Revoked/Used |
| Revogação manual | ✅ | Controlo imediato |

### 4.4 Endpoints

| Endpoint | Acção |
|---|---|
| POST /delegations | Criar |
| GET /delegations | Listar |
| PUT /delegations/{id}/revoke | Revogar |

### 4.5 Casos de Uso Típicos

| Cenário | Delegação |
|---|---|
| Férias do TechLead | Delega papel TechLead a Developer sénior |
| Operação de manutenção | Delega papel Admin temporariamente |
| Substituição temporária | Delega papel por período definido |

---

## 5. Access Review (Revisão de Acessos)

### 5.1 Modelo Bidimensional

#### AccessReviewCampaign

| Campo | Tipo | Propósito |
|---|---|---|
| Id | GUID | Identificador único |
| TenantId | FK → Tenant | Tenant |
| Title | string | Título da campanha |
| StartedAt | DateTime | Início |
| DeadlineAt | DateTime | Prazo |
| Status | enum | Active / Completed / Cancelled |

#### AccessReviewItem

| Campo | Tipo | Propósito |
|---|---|---|
| Id | GUID | Identificador único |
| CampaignId | FK → Campaign | Campanha |
| UserId | FK → User | Utilizador em revisão |
| RoleId | FK → Role | Papel em revisão |
| DecidedBy | FK → User? | Quem decidiu |
| Decision | enum | Approved / Revoked / Pending |
| Reason | string? | Justificação |

### 5.2 Lifecycle

```
┌──────────┐     Decisões      ┌───────────┐     Todas decididas    ┌───────────┐
│  Active  │ ─────────────────→│  Active   │ ──────────────────────→│ Completed │
└──────────┘    (item by item) └───────────┘                        └───────────┘
     │
     │ Cancelamento
     ↓
┌───────────┐
│ Cancelled │
└───────────┘
```

### 5.3 Endpoints

| Endpoint | Acção |
|---|---|
| POST /access-reviews | Criar campanha |
| GET /access-reviews | Listar campanhas |
| GET /access-reviews/{id}/items | Listar items |
| PUT /access-reviews/{id}/items/{itemId}/decide | Decidir item |
| PUT /access-reviews/{id}/complete | Completar campanha |
| PUT /access-reviews/{id}/cancel | Cancelar campanha |

### 5.4 Controlos de Segurança

| Controlo | Implementação | Avaliação |
|---|---|---|
| Campanha com prazo | ✅ DeadlineAt | Evita reviews pendentes |
| Decisão item-a-item | ✅ | Granularidade |
| Justificação por item | ✅ Reason | Rastreabilidade |
| Quem decidiu | ✅ DecidedBy | Responsabilização |
| Per-tenant | ✅ TenantId | Isolamento |

---

## 6. Matriz Comparativa dos 4 Mecanismos

| Dimensão | JIT Access | Break Glass | Delegação | Access Review |
|---|---|---|---|---|
| **Propósito** | Acesso temporário controlado | Emergência | Substituição de papel | Revisão periódica |
| **Aprovação prévia** | ✅ Obrigatória | ❌ Imediato | N/A | N/A |
| **Justificação** | ✅ Obrigatória | ✅ Obrigatória | ✅ Obrigatória | ✅ Por item |
| **Time-bounded** | ✅ 8h | ✅ 2h | ✅ StartsAt/EndsAt | ✅ DeadlineAt |
| **Rate limiting** | Anti-abuse | 3/trimestre | N/A | N/A |
| **Post-action** | Auto-expira | Post-mortem 24h | Auto-expira | Decisão permanente |
| **SecurityEvent** | ✅ | ✅ (score 60) | ✅ | ✅ |
| **IP/UA tracking** | Via sessão | ✅ Dedicado | Via sessão | Via sessão |
| **Entidade domínio** | ✅ | ✅ | ✅ | ✅ (2 entidades) |
| **Endpoints** | ✅ | ✅ | ✅ | ✅ |
| **Persistência** | ✅ | ✅ | ✅ | ✅ |

---

## 7. Análise de Segurança Transversal

### 7.1 Pontos Fortes Comuns

| Aspecto | Detalhe |
|---|---|
| Todas as entidades têm domínio rico | Não são meros DTOs — contêm lógica de negócio |
| Todos têm endpoints dedicados | API completa para cada mecanismo |
| Todos têm persistência | Dados auditáveis e recuperáveis |
| Todos geram SecurityEvent | Rastreabilidade unificada |
| Todos são time-bounded | Nenhum acesso concedido permanentemente |
| Todos exigem justificação | Rastreabilidade de intenção |

### 7.2 Lacunas Transversais

| Lacuna | Afecta | Severidade |
|---|---|---|
| Background job para expiração | JIT, Break Glass, Delegação | MÉDIA |
| Notificação em tempo real | Break Glass (activação), JIT (aprovação) | MÉDIA |
| Enforcement automatizado de post-mortem | Break Glass | MÉDIA |
| Self-action prevention | JIT (self-approval), Delegation (self-delegation) | MÉDIA |
| UI completa | Todos (verificar surfacing na UI) | BAIXA |
| Relatórios de utilização | Todos | BAIXA |

---

## 8. Dados de Seed e Evidência

### 8.1 Entidades no IdentityDbContext

```csharp
// IdentityDbContext — 15 DbSets incluindo:
public DbSet<BreakGlassRequest> BreakGlassRequests { get; set; }
public DbSet<JitAccessRequest> JitAccessRequests { get; set; }
public DbSet<Delegation> Delegations { get; set; }
public DbSet<AccessReviewCampaign> AccessReviewCampaigns { get; set; }
public DbSet<AccessReviewItem> AccessReviewItems { get; set; }
```

### 8.2 Endpoints Registados

```csharp
// 11 módulos de endpoint incluindo:
JitAccessEndpoints
BreakGlassEndpoints
DelegationEndpoints
AccessReviewEndpoints
```

---

## 9. Recomendações

### Prioridade ALTA

1. **Implementar background job** para transição automática de estados:
   - JIT: Pending → Expired (após 4h sem aprovação)
   - JIT: Approved → Expired (após 8h)
   - Break Glass: Active → Expired (após 2h)
   - Delegation: Active → Expired (após EndsAt)

2. **Automatizar enforcement de post-mortem Break Glass**:
   - Notificação após 12h
   - Escalação após 20h
   - Bloqueio de novo Break Glass sem post-mortem pendente completado

3. **Implementar self-action prevention**:
   - JIT: Proibir auto-aprovação
   - Delegation: Proibir auto-delegação

### Prioridade MÉDIA

4. **Notificação em tempo real** para:
   - Break Glass activado → alerta a PlatformAdmin e SecurityReview
   - JIT pendente → alerta a aprovadores
   - Delegação criada → alerta ao delegatário

5. **Dashboard de acesso avançado** para Auditor e SecurityReview:
   - JIT activos e histórico
   - Break Glass activos e pendentes de post-mortem
   - Delegações activas
   - Access Reviews em curso

### Prioridade BAIXA

6. **Relatórios de utilização** por mecanismo (mensal/trimestral)
7. **Integração com IA** para detecção de padrões anómalos (ex: Break Glass recorrente para o mesmo scope)
8. **Templates de justificação** pré-definidos por cenário

---

## 10. Conformidade

| Requisito Regulatório | JIT | Break Glass | Delegação | Access Review |
|---|---|---|---|---|
| Menor privilégio | ✅ | N/A (emergência) | ✅ | ✅ |
| Segregação de deveres | ✅ (aprovação) | ⚠️ (sem aprovação) | ✅ | ✅ |
| Rastreabilidade | ✅ | ✅ | ✅ | ✅ |
| Time-bounding | ✅ | ✅ | ✅ | ✅ |
| Justificação | ✅ | ✅ | ✅ | ✅ |
| Revisão periódica | N/A | Post-mortem | N/A | ✅ (core purpose) |
| Persistência | ✅ | ✅ | ✅ | ✅ |

---

> **Classificação final:** IMPLEMENTED_APPARENT — Todos os quatro mecanismos de controlo de acesso avançado possuem entidades de domínio, endpoints de API e persistência, com lacunas residuais em automação de lifecycle e enforcement de post-mortem.
