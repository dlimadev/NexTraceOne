# Relatório de Auditoria — Acções Sensíveis e Segurança Administrativa

> **Módulo:** `src/modules/identityaccess/` + `NexTraceOne.BuildingBlocks.Security`  
> **Data da análise:** 2025-07  
> **Classificação:** WELL_PROTECTED_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

Este relatório analisa a protecção de acções sensíveis e operações administrativas no NexTraceOne. O sistema implementa múltiplas camadas de protecção: rate limiting por tiers (auth-sensitive: 10/min), modelo de permissões granular com PlatformAdmin limitado a 57+ de 73 permissões, logging de auditoria em todas as negações de autorização, tracking de SecurityEvent para todas as acções sensíveis, mecanismos de Break Glass com post-mortem obrigatório, e JIT access com aprovação e justificação obrigatórias.

As lacunas residuais incluem a ausência de MFA step-up para operações privilegiadas (política modelada mas enforcement adiado) e a falta de confirmação de acção ao nível do backend.

---

## 2. Classificação de Acções Sensíveis

### 2.1 Mapa de Acções por Severidade

| Severidade | Acção | Protecção Actual |
|---|---|---|
| **CRÍTICA** | Activar Break Glass | Registada como SecurityEvent, IP/UA tracking, limite 3/trimestre |
| **CRÍTICA** | Alterar permissões de PlatformAdmin | Permissão `identity:roles:manage` |
| **CRÍTICA** | Desactivar tenant | Soft-delete, permissão específica |
| **ALTA** | Criar/revogar delegação | SecurityEvent, reason obrigatória |
| **ALTA** | Aprovar/rejeitar JIT access | SecurityEvent, justificação obrigatória |
| **ALTA** | Alterar papéis de utilizador | Permissão `identity:users:write` |
| **ALTA** | Gerir OIDC providers | Permissão de gestão |
| **ALTA** | Gerir ambientes de produção | EnvironmentAccess Admin |
| **MÉDIA** | Alterar configurações de MFA | Permissão de gestão |
| **MÉDIA** | Revogar sessões | Permissão específica |
| **MÉDIA** | Gerir API keys | Permissão de gestão |
| **BAIXA** | Consultar eventos de segurança | Permissão de leitura (Auditor) |

---

## 3. Rate Limiting

### 3.1 Políticas por Tier

| Política | Limite | Aplicação | Avaliação |
|---|---|---|---|
| **Global** | 100/min | Todos os endpoints | ✅ |
| **auth** | 20/min | Endpoints de autenticação | ✅ |
| **auth-sensitive** | 10/min | Login, password reset | ✅ Mais restrito |
| **ai** | 30/min | Endpoints de IA | ✅ |
| **data-intensive** | 50/min | Queries pesadas | ✅ |
| **operations** | 40/min | Operações | ✅ |

### 3.2 Tratamento de IP Não Resolvido

| Cenário | Limite Global |
|---|---|
| IP resolvido normalmente | 100/min |
| IP não resolvido | 20/min |

**Avaliação:** ✅ IPs desconhecidos recebem limites significativamente mais restritos.

### 3.3 Queue Handling

- FIFO (First In, First Out)
- Pedidos que excedem o limite são enfileirados até ao limite da fila

---

## 4. PlatformAdmin — Análise de Privilégios

### 4.1 Perfil de Acesso

| Aspecto | Detalhe | Avaliação |
|---|---|---|
| Nº permissões | 57+ de 73 | ✅ Não tem "tudo" |
| Permissões ausentes | Específicas de operação (ex: execute em contextos específicos) | ✅ Separação de deveres |
| Acesso cross-tenant | Implícito (role de plataforma) | ⚠️ Necessita audit trail |

### 4.2 Acções Exclusivas do PlatformAdmin

| Acção | Permissão |
|---|---|
| Gerir tenants | `identity:tenants:manage` |
| Gerir configurações globais | `platform:config:manage` |
| Gerir model registry (IA) | `ai:models:manage` |
| Gerir integrações de plataforma | `foundation:integrations:manage` |

### 4.3 Recomendações para PlatformAdmin

1. **Audit trail dedicado** para todas as acções de PlatformAdmin
2. **Exigir MFA** para qualquer operação de PlatformAdmin (quando implementado)
3. **Notificação** a outros admins quando acções críticas são executadas
4. **Revisão periódica** de quem tem papel PlatformAdmin

---

## 5. Logging de Auditoria em Acções Sensíveis

### 5.1 PermissionAuthorizationHandler — Logging de Negação

| Aspecto | Implementação |
|---|---|
| Nível | WARNING |
| Informação registada | Utilizador, permissão requerida, resultado |
| Trigger | Toda negação de autorização |

### 5.2 SecurityEvent — Tracking Dedicado

| Tipo de Evento | RiskScore | Cenário |
|---|---|---|
| LoginFailed | 20-80 | Tentativa de login falhada (score varia com padrão) |
| AccessDenied | 30-60 | Acesso negado a recurso |
| UnauthorizedAccess | 50-80 | Tentativa de acesso não autorizado |
| BreakGlassActivated | 60 | Activação de Break Glass |
| BreakGlassRevoked | 30 | Revogação de Break Glass |
| DelegationCreated | 25 | Nova delegação |
| DelegationUsed | 20 | Delegação utilizada |
| AccountLocked | 40 | Conta bloqueada por tentativas falhadas |
| AccountDeactivated | 35 | Conta desactivada |
| PasswordChanged | 15 | Alteração de palavra-passe |
| SessionCreated | 10 | Nova sessão |
| SessionRevoked | 15 | Sessão revogada |

### 5.3 Metadados Rastreados

| Campo | Propósito |
|---|---|
| TenantId | Contexto de tenant |
| UserId | Autor da acção |
| SessionId | Sessão activa |
| IpAddress | Origem |
| UserAgent | Cliente |
| MetadataJson | Dados adicionais da acção |
| OccurredAt | Timestamp |

---

## 6. Break Glass — Protecção de Acções Extremas

### 6.1 Controlos

| Controlo | Implementação | Avaliação |
|---|---|---|
| Activação imediata | Sem aprovação prévia | ✅ (propósito do mecanismo) |
| Janela temporal | 2 horas | ✅ Limitada |
| Post-mortem | Obrigatório em 24h | ✅ (enforcement manual) |
| Limite trimestral | 3 por utilizador | ✅ Anti-abuso |
| Tracking | IP + UserAgent | ✅ Rastreabilidade |
| SecurityEvent | BreakGlassActivated (score 60) | ✅ |
| Status lifecycle | Active → Expired/Revoked → PostMortemCompleted | ✅ |

### 6.2 Lacuna

O enforcement do post-mortem não é automatizado — não existe background job que notifique ou bloqueie em caso de post-mortem atrasado.

---

## 7. JIT Access — Acesso Controlado a Privilégios

### 7.1 Controlos

| Controlo | Implementação | Avaliação |
|---|---|---|
| Aprovação obrigatória | Workflow Pending → Approved | ✅ |
| Prazo de aprovação | 4 horas | ✅ |
| Janela de concessão | 8 horas | ✅ |
| Justificação | Campo obrigatório | ✅ |
| Scope | Definido por PermissionCode | ✅ |
| SecurityEvent | Tracking de criação e uso | ✅ |

---

## 8. Delegação — Transferência Controlada de Acesso

### 8.1 Controlos

| Controlo | Implementação | Avaliação |
|---|---|---|
| Time-bounded | StartsAt / EndsAt | ✅ |
| Reason obrigatório | Campo na entidade | ✅ |
| SecurityEvent | DelegationCreated/Revoked/Used | ✅ |
| Scope | RoleId + TenantId | ✅ |

---

## 9. Análise de Lacunas — Acções Sensíveis

### 9.1 Lacunas Identificadas

| # | Lacuna | Severidade | Impacto |
|---|---|---|---|
| 1 | MFA step-up para operações privilegiadas não implementado | ALTA | Acções críticas não exigem segundo factor |
| 2 | Confirmação de acção no backend não implementada | MÉDIA | Sem "are you sure?" server-side |
| 3 | Enforcement automatizado de post-mortem Break Glass | MÉDIA | Dependência de processo manual |
| 4 | Notificação a admins em acções críticas | MÉDIA | Sem alerta em tempo real |
| 5 | Limpeza automática de JIT/delegações expiradas | BAIXA | Dados acumulam sem cleanup |

### 9.2 Cobertura de Protecção

| Acção Sensível | Auth | Authz | Rate Limit | SecurityEvent | MFA Step-up |
|---|---|---|---|---|---|
| Login | ✅ | N/A | ✅ (10/min) | ✅ | ❌ |
| Break Glass | ✅ | ✅ | ✅ | ✅ | ❌ |
| JIT Request | ✅ | ✅ | ✅ | ✅ | ❌ |
| Delegação | ✅ | ✅ | ✅ | ✅ | ❌ |
| Gerir tenant | ✅ | ✅ | ✅ | ✅ | ❌ |
| Gerir roles | ✅ | ✅ | ✅ | ✅ | ❌ |
| Gerir users | ✅ | ✅ | ✅ | ✅ | ❌ |

A coluna MFA Step-up está ❌ em todas porque o enforcement foi adiado.

---

## 10. Headers de Segurança

### 10.1 Middleware UseSecurityHeaders

| Header | Propósito | Avaliação |
|---|---|---|
| X-Content-Type-Options | Previne MIME sniffing | ✅ |
| X-Frame-Options | Previne clickjacking | ✅ |
| X-XSS-Protection | Protecção XSS legacy | ✅ |
| Content-Security-Policy | Controlo de fontes | ✅ (verificar configuração) |
| Strict-Transport-Security | Força HTTPS | ✅ |

### 10.2 HTTPS Redirection

`UseHttpsRedirection` na posição 2 do pipeline — antes de qualquer processamento de negócio.

---

## 11. Recomendações

### Prioridade CRÍTICA

1. **Implementar MFA step-up** para operações privilegiadas (Break Glass, gestão de tenants, gestão de roles, gestão de OIDC providers)

### Prioridade ALTA

2. **Automatizar enforcement de post-mortem Break Glass** com notificações e escalação
3. **Implementar notificação em tempo real** a PlatformAdmins quando acções críticas são executadas
4. **Audit trail dedicado para PlatformAdmin** com relatório periódico

### Prioridade MÉDIA

5. **Background job de limpeza** para JIT, delegações e grants de ambiente expirados
6. **Confirmação server-side** para acções destrutivas (idempotency key + confirmation token)
7. **Dashboard de acções sensíveis** para SecurityReview e Auditor

### Prioridade BAIXA

8. **Alertas automáticos** baseados em padrões de acções sensíveis (ex: múltiplas activações de Break Glass)
9. **Relatório periódico** de acções privilegiadas executadas

---

> **Classificação final:** WELL_PROTECTED_APPARENT — Acções sensíveis bem protegidas com múltiplas camadas (auth, authz, rate limiting, SecurityEvent tracking), com lacuna principal no MFA step-up.
