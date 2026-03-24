# Relatório de Auditoria — Rastreabilidade e Eventos de Segurança

> **Módulo:** `src/modules/identityaccess/` + Bridge para AuditCompliance  
> **Data da análise:** 2025-07  
> **Classificação:** TRACEABLE  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O NexTraceOne implementa um sistema abrangente de rastreabilidade de segurança centrado na entidade `SecurityEvent`, com 15+ tipos de evento, risk scoring de 0-100, tracking de IP/UserAgent/metadata JSON, e bridge MediatR para o módulo central de AuditCompliance. Os dados de seed incluem 8 eventos de segurança diversificados demonstrando diferentes perfis de risco. Todas as entidades possuem campos de auditoria (`CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`) via `AuditInterceptor`. As sessões são rastreadas com hashes de refresh token.

A classificação TRACEABLE reflecte um sistema com capacidade demonstrada de rastrear acções de segurança de forma granular e integrada.

---

## 2. Entidade SecurityEvent

### 2.1 Campos

| Campo | Tipo | Propósito | Avaliação |
|---|---|---|---|
| Id | GUID | Identificador único | ✅ |
| TenantId | FK → Tenant | Contexto de tenant | ✅ |
| UserId | FK → User | Autor da acção | ✅ |
| SessionId | FK → Session | Sessão activa | ✅ |
| EventType | enum/string | Tipo de evento | ✅ |
| Description | string | Descrição legível | ✅ |
| RiskScore | int (0-100) | Score de risco | ✅ |
| IpAddress | string | IP de origem | ✅ |
| UserAgent | string | Agente/browser | ✅ |
| MetadataJson | string (JSON) | Dados adicionais extensíveis | ✅ |
| OccurredAt | DateTime | Timestamp do evento | ✅ |
| IsReviewed | bool | Se foi revisado | ✅ |
| ReviewedAt | DateTime? | Quando foi revisado | ✅ |
| ReviewedBy | FK → User? | Quem revisou | ✅ |

### 2.2 Extensibilidade

O campo `MetadataJson` permite armazenar dados adicionais específicos por tipo de evento sem alterar o schema:

```json
{
  "failedAttempts": 5,
  "lastAttemptIp": "192.168.1.100",
  "lockDuration": "15m",
  "previousRole": "Developer",
  "newRole": "TechLead"
}
```

**Avaliação:** ✅ Design extensível sem migrações para novos tipos de metadados.

---

## 3. Tipos de Evento

### 3.1 Catálogo Completo

| Tipo de Evento | Categoria | RiskScore Típico | Cenário |
|---|---|---|---|
| **LoginFailed** | Autenticação | 20-80 | Tentativa de login falhada |
| **LoginSuccess** | Autenticação | 0-10 | Login bem-sucedido |
| **PasswordChanged** | Autenticação | 15 | Alteração de palavra-passe |
| **AccessDenied** | Autorização | 30-60 | Acesso negado a recurso |
| **UnauthorizedAccess** | Autorização | 50-80 | Tentativa não autorizada |
| **BreakGlassActivated** | Acesso Avançado | 60 | Activação de Break Glass |
| **BreakGlassRevoked** | Acesso Avançado | 30 | Revogação de Break Glass |
| **BreakGlassPostMortem** | Acesso Avançado | 10 | Post-mortem submetido |
| **DelegationCreated** | Acesso Avançado | 25 | Nova delegação |
| **DelegationRevoked** | Acesso Avançado | 15 | Delegação revogada |
| **DelegationUsed** | Acesso Avançado | 20 | Delegação utilizada |
| **SessionCreated** | Sessão | 10 | Nova sessão |
| **SessionRevoked** | Sessão | 15 | Sessão revogada |
| **SessionRotated** | Sessão | 5 | Token rotacionado |
| **AccountLocked** | Conta | 40 | Conta bloqueada |
| **AccountDeactivated** | Conta | 35 | Conta desactivada |
| **AccountReactivated** | Conta | 20 | Conta reactivada |

### 3.2 Distribuição por Categoria

| Categoria | Nº Eventos | Cobertura |
|---|---|---|
| Autenticação | 3 | ✅ Login, password |
| Autorização | 2 | ✅ Deny, unauthorized |
| Acesso Avançado | 6 | ✅ Break Glass, Delegation |
| Sessão | 3 | ✅ Create, revoke, rotate |
| Conta | 3 | ✅ Lock, deactivate, reactivate |
| **Total** | **17** | ✅ |

---

## 4. Risk Scoring

### 4.1 Modelo de Scoring

| Cenário de Anomalia | Score | Justificação |
|---|---|---|
| Localização incomum | 60 | Login de IP/geo diferente do habitual |
| Força bruta (padrão) | 80 | Múltiplas tentativas falhadas em sequência |
| Aprovação rápida | 45 | JIT aprovado segundos após pedido |
| Fora de horário | 25 | Acção em horário atípico |
| Login normal | 0-10 | Sem anomalia |
| Rotação de token | 5 | Operação normal |

### 4.2 Escalas de Risco

| Range | Nível | Acção Esperada |
|---|---|---|
| 0-20 | BAIXO | Log, sem acção |
| 21-40 | MÉDIO | Log + possível alerta |
| 41-60 | ALTO | Alerta + investigação |
| 61-80 | CRÍTICO | Alerta imediato + possível bloqueio |
| 81-100 | EMERGÊNCIA | Acção automática recomendada |

### 4.3 Lacuna

As regras de detecção de anomalia existem nos dados de seed, mas a **resposta automatizada** não está implementada. O sistema regista os scores mas não actua automaticamente sobre eles.

**Recomendação:** Implementar engine de regras que, para scores acima de threshold configurável, dispare notificações e/ou acções automáticas.

---

## 5. Bridge de Auditoria (MediatR)

### 5.1 Arquitectura

```
┌────────────────────────┐
│ ISecurityEventTracker  │ ← Interface pública
└────────────┬───────────┘
             │
┌────────────▼───────────┐
│ SecurityEventTracker   │ ← Scoped accumulator (per-request)
└────────────┬───────────┘
             │
┌────────────▼─────────────────┐
│ SecurityEventAuditBehavior   │ ← MediatR pipeline behavior
└────────────┬─────────────────┘
             │
┌────────────▼───────────┐
│ ISecurityAuditBridge   │ ← Interface de bridge
└────────────┬───────────┘
             │
┌────────────▼───────────┐
│ Módulo AuditCompliance │ ← Destino central de auditoria
└────────────────────────┘
```

### 5.2 Funcionamento

1. **Acumulação:** `SecurityEventTracker` (scoped) acumula eventos durante o request
2. **Publicação:** `SecurityEventAuditBehavior` (MediatR pipeline) publica eventos acumulados após o handler
3. **Bridge:** `ISecurityAuditBridge` transfere para o módulo centralizado de AuditCompliance
4. **Persistência:** Eventos persistidos no IdentityDbContext E no módulo AuditCompliance

### 5.3 Vantagens da Arquitectura

| Aspecto | Benefício |
|---|---|
| Scoped accumulator | Agrupa eventos por request |
| MediatR behavior | Transparente para handlers de comando |
| Bridge interface | Desacoplamento entre módulos |
| Dual persistence | Eventos disponíveis localmente e centralizadamente |

**Avaliação:** ✅ Arquitectura limpa com separação de responsabilidades.

---

## 6. AuditInterceptor — Campos de Auditoria em Entidades

### 6.1 Campos Automáticos

Todas as entidades possuem:

| Campo | Tipo | Preenchimento |
|---|---|---|
| CreatedAt | DateTime | Automático na criação |
| CreatedBy | string | Id do utilizador actual |
| UpdatedAt | DateTime? | Automático na actualização |
| UpdatedBy | string? | Id do utilizador actual |

### 6.2 Mecanismo

O `AuditInterceptor` (EF Core `SaveChangesInterceptor`) preenche automaticamente estes campos em TODAS as operações de escrita.

**Avaliação:** ✅ Rastreabilidade universal de quem criou/alterou cada registo.

---

## 7. Rastreamento de Sessões

### 7.1 Campos de Sessão

| Campo | Propósito | Segurança |
|---|---|---|
| RefreshTokenHash | SHA-256 do refresh token | ✅ Token nunca em claro |
| ExpiresAt | Expiração | ✅ Temporal |
| CreatedByIp | IP de criação | ✅ Rastreabilidade |
| UserAgent | Browser/cliente | ✅ Rastreabilidade |
| RevokedAt | Revogação | ✅ Controlo |

### 7.2 Eventos de Sessão

| Evento | Quando |
|---|---|
| SessionCreated | Nova sessão criada |
| SessionRevoked | Sessão revogada manualmente |
| SessionRotated | Token rotacionado no refresh |

### 7.3 Lacuna

Os dados de IP e UserAgent são **recolhidos** mas **não validados** durante a sessão. Não há detecção de session hijacking baseada em mudança de IP/UserAgent.

---

## 8. Dados de Seed — Eventos de Exemplo

### 8.1 Eventos Demonstrativos

| # | EventType | RiskScore | Cenário Demonstrado |
|---|---|---|---|
| 1 | LoginFailed | 20 | Tentativa falhada simples |
| 2 | LoginFailed | 80 | Padrão de força bruta |
| 3 | LoginSuccess | 0 | Login normal |
| 4 | LoginSuccess | 60 | Login de localização incomum |
| 5 | BreakGlassActivated | 60 | Activação de emergência |
| 6 | AccessDenied | 45 | Acesso negado a recurso sensível |
| 7 | SessionCreated | 10 | Nova sessão normal |
| 8 | AccountLocked | 40 | Conta bloqueada |

### 8.2 Diversidade dos Exemplos

Os 8 eventos de seed demonstram:
- Diferentes tipos de evento (6 tipos distintos)
- Diferentes perfis de risco (0 a 80)
- Diferentes categorias (autenticação, autorização, sessão, conta, acesso avançado)

---

## 9. Workflow de Revisão

### 9.1 Campos de Revisão no SecurityEvent

| Campo | Propósito |
|---|---|
| IsReviewed | Flag de revisão |
| ReviewedAt | Timestamp da revisão |
| ReviewedBy | Quem revisou |

### 9.2 Estado Actual

| Aspecto | Estado |
|---|---|
| Campos de revisão | ✅ Implementados na entidade |
| Workflow de revisão na UI | ⚠️ Não completamente surfaced |
| Alertas para eventos pendentes de revisão | ❌ Não implementado |
| Dashboard de revisão | ⚠️ Parcial |

**Lacuna:** O modelo suporta workflow de revisão mas a UI pode não surfacar completamente a funcionalidade.

---

## 10. Análise de Lacunas

| # | Lacuna | Severidade | Detalhe |
|---|---|---|---|
| 1 | Resposta automatizada a risk scores | MÉDIA | Scores registados mas sem acção automática |
| 2 | Workflow de revisão não totalmente na UI | MÉDIA | Campos existem, UI parcial |
| 3 | Validação de IP/UserAgent em sessão | MÉDIA | Dados recolhidos, não validados |
| 4 | Alertas em tempo real | MÉDIA | Sem notificação para eventos críticos |
| 5 | Retenção e rotação de eventos | BAIXA | Sem política de retenção definida |
| 6 | Exportação para SIEM | BAIXA | Sem integração com ferramentas externas |

---

## 11. Recomendações

### Prioridade ALTA

1. **Implementar engine de regras** para resposta automatizada a risk scores elevados
2. **Surfacar workflow de revisão** completamente na UI para Auditor e SecurityReview

### Prioridade MÉDIA

3. **Implementar validação de sessão** usando IP/UserAgent já recolhidos
4. **Alertas em tempo real** para eventos com RiskScore > 60 (SignalR ou push)
5. **Dashboard de eventos de segurança** com filtros por tipo, risco, tenant, período

### Prioridade BAIXA

6. **Política de retenção** para eventos de segurança (ex: 2 anos)
7. **Exportação para SIEM** (Splunk, Sentinel, etc.)
8. **Relatórios periódicos** de segurança por tenant

---

## 12. Conformidade

| Requisito | Estado | Evidência |
|---|---|---|
| Tracking de eventos de segurança | ✅ | SecurityEvent entity |
| 15+ tipos de evento | ✅ | Catálogo documentado |
| Risk scoring | ✅ | 0-100 com regras |
| IP/UserAgent tracking | ✅ | Campos dedicados |
| Metadata extensível | ✅ | MetadataJson |
| Bridge para auditoria central | ✅ | MediatR behavior → AuditCompliance |
| Campos de auditoria universais | ✅ | AuditInterceptor |
| Sessão com hash de token | ✅ | SHA-256 |
| Workflow de revisão | ⚠️ | Entidade sim, UI parcial |
| Resposta automática | ❌ | Não implementada |

---

> **Classificação final:** TRACEABLE — Sistema abrangente de rastreabilidade com 15+ tipos de evento, risk scoring, bridge MediatR, e auditoria universal de entidades. Lacunas em automação de resposta e surfacing completo da UI de revisão.
