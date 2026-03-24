# Relatório de Auditoria — Análise de Lacunas Cross-Layer

> **Escopo:** Frontend ↔ Backend ↔ Base de Dados  
> **Data da análise:** 2025-07  
> **Classificação:** WELL_INTEGRATED_WITH_KNOWN_GAPS  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

Este relatório analisa a coerência de segurança entre as três camadas do NexTraceOne: Frontend (React), Backend (ASP.NET Core) e Base de Dados (PostgreSQL). A análise revela um alinhamento sólido entre camadas (84+ permissões frontend alinhadas com 73 backend, RLS em todas as operações DB, explicit no-enforcement policy no frontend), com lacunas conhecidas em áreas específicas: MFA (UI parcial, enforcement zero), SAML (ausente em todas as camadas), API Key (config only, sem tabela DB), session anomaly (dados recolhidos mas sem enforcement), Break Glass post-mortem (entidade suporta, automação ausente), e Access Review (entidade + endpoints existem, UI pode não surfacar completamente).

---

## 2. Análise Cross-Layer: Frontend ↔ Backend

### 2.1 Permissões

| Aspecto | Frontend | Backend | Alinhamento |
|---|---|---|---|
| Nº de permissões | 84+ chaves | 73 códigos | ✅ Frontend superset |
| Formato | `módulo:recurso:acção` | `módulo:recurso:acção` | ✅ Idêntico |
| Enforcement | UX only (comentário explícito) | Deny-by-default | ✅ Correcto |
| Carregamento | Endpoint `/me` | Server-driven | ✅ |
| Lookup performance | Set O(1) | Claims O(n) no token | ✅/⚠️ |

**Lacuna:** A diferença de contagem (84 vs 73) pode indicar chaves frontend sem correspondência backend. Necessita sincronização automatizada.

### 2.2 Autenticação

| Aspecto | Frontend | Backend | Alinhamento |
|---|---|---|---|
| JWT | Armazena em sessionStorage | Emite e valida | ✅ |
| Refresh | In-memory closure | SHA-256 hash em DB | ✅ |
| CSRF | Envia X-Csrf-Token | Valida middleware | ✅ |
| 401 handling | Silent refresh | Retorna 401 | ✅ |
| Cookie session | Suporta | Emite + valida | ✅ |

**Lacuna:** Nenhuma identificada nesta camada.

### 2.3 Tenant Context

| Aspecto | Frontend | Backend | Alinhamento |
|---|---|---|---|
| Injecção | X-Tenant-Id header | TenantResolutionMiddleware | ✅ |
| Fonte primária | Contexto do utilizador | JWT claim tenant_id | ✅ |
| Mudança de tenant | UI selector | Sessão re-autenticada | ✅ |

### 2.4 Environment Context

| Aspecto | Frontend | Backend | Alinhamento |
|---|---|---|---|
| Injecção | X-Environment-Id header | EnvironmentResolutionMiddleware | ✅ |
| Persistência | sessionStorage | Per-request resolution | ✅ |
| Validação | Nenhuma (confia no backend) | Valida pertença ao tenant | ✅ |

### 2.5 MFA — LACUNA CROSS-LAYER

| Aspecto | Frontend | Backend | Lacuna |
|---|---|---|---|
| UI de configuração | ⚠️ Parcial | N/A | UI incompleta |
| Enrollment TOTP | ❌ | ❌ | Não implementado |
| Step-up challenge | ❌ | ❌ | Não implementado |
| Política | N/A | ✅ Modelada | Enforcement ausente |

**Severidade:** ALTA — A política de MFA está modelada no backend mas nenhuma camada implementa o enforcement.

---

## 3. Análise Cross-Layer: Backend ↔ Base de Dados

### 3.1 Isolamento de Tenant

| Aspecto | Backend | Base de Dados | Alinhamento |
|---|---|---|---|
| Resolução | Middleware + JWT | N/A (confia no backend) | ✅ |
| Enforcement | TenantRlsInterceptor | RLS policies | ✅ |
| SQL | Parametrizado | set_config('app.current_tenant_id') | ✅ |
| Cobertura | ALL EF Core commands | ALL queries | ✅ |

### 3.2 Schema e Entidades

| DbSet | Entidade Backend | Tabela DB | Alinhamento |
|---|---|---|---|
| Users | ✅ | ✅ | ✅ |
| Roles | ✅ | ✅ | ✅ |
| Permissions | ✅ | ✅ | ✅ |
| Tenants | ✅ | ✅ | ✅ |
| Sessions | ✅ | ✅ | ✅ |
| TenantMemberships | ✅ | ✅ | ✅ |
| ExternalIdentities | ✅ | ✅ | ✅ |
| SsoGroupMappings | ✅ | ✅ | ✅ |
| BreakGlassRequests | ✅ | ✅ | ✅ |
| JitAccessRequests | ✅ | ✅ | ✅ |
| Delegations | ✅ | ✅ | ✅ |
| AccessReviewCampaigns | ✅ | ✅ | ✅ |
| AccessReviewItems | ✅ | ✅ | ✅ |
| SecurityEvents | ✅ | ✅ | ✅ |
| Environments | ✅ | ✅ | ✅ |
| EnvironmentAccesses | ✅ | ✅ | ✅ |

**Total:** 15 DbSets com correspondência 1:1 entre backend e DB.

### 3.3 Migrations

| Migration | Conteúdo | Estado |
|---|---|---|
| InitialCreate | Schema completo | ✅ Aplicada |
| AddIsPrimaryProductionToEnvironment | Partial unique index | ✅ Aplicada |

### 3.4 Seed Data

| Dados | Nº Registos | Estado |
|---|---|---|
| Tenants | 2 | ✅ |
| Utilizadores | Múltiplos | ✅ |
| Papéis | 7 | ✅ |
| Permissões | 73 | ✅ |
| Ambientes | 5 | ✅ |
| EnvironmentAccess | 8 | ✅ |
| SecurityEvents | 8 | ✅ |

### 3.5 API Key — LACUNA CROSS-LAYER

| Aspecto | Backend | Base de Dados | Lacuna |
|---|---|---|---|
| Armazenamento | In-memory (appsettings) | ❌ Sem tabela | TABELA AUSENTE |
| Validação | FixedTimeEquals | N/A | ✅ (timing-safe) |
| Rotação | ❌ | ❌ | Não implementada |
| Scoping | ❌ | ❌ | Não implementado |

**Severidade:** MÉDIA — API Keys existem apenas em configuração, sem persistência em BD. Impede rotação, scoping e auditoria.

---

## 4. Análise Cross-Layer: Frontend ↔ Backend ↔ DB

### 4.1 Fluxos End-to-End Verificados

| Fluxo | Frontend | Backend | DB | Estado |
|---|---|---|---|---|
| Login local | ✅ Form + API | ✅ Auth + JWT | ✅ User + Session | ✅ COMPLETO |
| Login OIDC | ✅ Redirect flow | ✅ OIDC callback | ✅ ExternalIdentity | ✅ COMPLETO |
| Selecção de tenant | ✅ UI selector | ✅ Middleware | ✅ RLS | ✅ COMPLETO |
| Selecção de ambiente | ✅ UI selector | ✅ Middleware | ✅ Validation | ✅ COMPLETO |
| Verificação de permissão | ✅ UX gating | ✅ Deny-by-default | N/A | ✅ COMPLETO |
| Refresh de token | ✅ Silent 401 | ✅ Rotação | ✅ Hash update | ✅ COMPLETO |
| Break Glass | ⚠️ UI parcial? | ✅ Endpoints | ✅ Entidade | ⚠️ VERIFICAR UI |
| JIT Access | ⚠️ UI parcial? | ✅ Endpoints | ✅ Entidade | ⚠️ VERIFICAR UI |
| Access Review | ⚠️ UI parcial? | ✅ Endpoints | ✅ Entidade | ⚠️ VERIFICAR UI |
| Delegação | ⚠️ UI parcial? | ✅ Endpoints | ✅ Entidade | ⚠️ VERIFICAR UI |

### 4.2 Fluxos com Lacunas

| Fluxo | Frontend | Backend | DB | Lacuna |
|---|---|---|---|---|
| **MFA** | ❌ | ⚠️ Modelado | ✅ Policy | Enforcement ausente |
| **SAML** | ❌ | ❌ | ❌ | Ausente em todas as camadas |
| **Session anomaly** | N/A | ⚠️ Dados recolhidos | ✅ IP/UA armazenados | Enforcement ausente |
| **API Key management** | ❌ | ⚠️ In-memory | ❌ Sem tabela | Sem gestão |

---

## 5. Mapa de Lacunas por Severidade

### 5.1 Lacunas CRÍTICAS (enforcement ausente em todas as camadas)

| # | Lacuna | Frontend | Backend | DB | Acção |
|---|---|---|---|---|---|
| 1 | **MFA enforcement** | ❌ UI enrollment | ❌ Step-up | ✅ Policy | Implementar end-to-end |

### 5.2 Lacunas ALTAS (parcialmente implementadas)

| # | Lacuna | Frontend | Backend | DB | Acção |
|---|---|---|---|---|---|
| 2 | **SAML** | ❌ | ❌ | ❌ | Implementar ISamlProvider |
| 3 | **Session anomaly** | N/A | ⚠️ Dados existem | ✅ Armazenados | Implementar validação |
| 4 | **API Key storage** | ❌ Sem UI | ⚠️ In-memory | ❌ Sem tabela | Migrar para BD |

### 5.3 Lacunas MÉDIAS (entidade existe, automação ausente)

| # | Lacuna | Frontend | Backend | DB | Acção |
|---|---|---|---|---|---|
| 5 | **Break Glass post-mortem** | ⚠️ | ✅ Endpoint | ✅ Campos | Automatizar enforcement |
| 6 | **Access Review UI** | ⚠️ Verificar | ✅ Endpoints | ✅ Entidades | Completar UI |
| 7 | **JIT cleanup** | N/A | ❌ Sem job | ✅ Dados | Background job |
| 8 | **Anomaly response** | N/A | ❌ Sem engine | ✅ Scores | Engine de regras |

### 5.4 Lacunas BAIXAS (melhorias futuras)

| # | Lacuna | Frontend | Backend | DB | Acção |
|---|---|---|---|---|---|
| 9 | **Permissão sync test** | 84 chaves | 73 códigos | N/A | Teste automatizado |
| 10 | **Environment enforcement** | ✅ Header | ⚠️ Validator parcial | ✅ Access entity | Completar validators |
| 11 | **GDPR export** | ❌ | ❌ | ✅ Dados existem | Endpoint dedicado |
| 12 | **SIEM export** | N/A | ❌ | ✅ Events | Integração |

---

## 6. Análise de Consistência por Mecanismo

### 6.1 Autenticação

```
Frontend:  [✅ JWT storage] [✅ Refresh] [✅ CSRF] [⚠️ MFA UI] [❌ SAML]
Backend:   [✅ JWT emit]    [✅ Rotate]  [✅ CSRF] [⚠️ MFA policy] [✅ OIDC] [❌ SAML]
Database:  [✅ Session]     [✅ Hash]    [N/A]    [✅ MFA config] [✅ External ID] [❌ SAML]
```

### 6.2 Autorização

```
Frontend:  [✅ 84 perms] [✅ UX gating] [✅ No enforcement]
Backend:   [✅ 73 perms] [✅ Deny-default] [✅ Dynamic policy] [✅ Audit log]
Database:  [✅ Role-perm mapping] [✅ 73 seeds] [✅ RLS]
```

### 6.3 Tenant Isolation

```
Frontend:  [✅ X-Tenant-Id] [✅ Tenant selector]
Backend:   [✅ Middleware] [✅ JWT claim] [✅ RLS interceptor]
Database:  [✅ TenantId FK] [✅ set_config] [✅ RLS policies]
```

### 6.4 Environment Control

```
Frontend:  [✅ X-Environment-Id] [✅ Environment selector] [✅ sessionStorage]
Backend:   [✅ Middleware] [✅ Validation] [⚠️ Partial enforcement in handlers]
Database:  [✅ Environment entity] [✅ EnvironmentAccess] [✅ Partial index]
```

### 6.5 Advanced Access

```
Frontend:  [⚠️ UI verification needed for all 4 mechanisms]
Backend:   [✅ JIT endpoints] [✅ BG endpoints] [✅ Delegation endpoints] [✅ AR endpoints]
Database:  [✅ JIT entity] [✅ BG entity] [✅ Delegation entity] [✅ AR entities]
```

---

## 7. Dependências Implícitas Entre Camadas

### 7.1 Dependências Correctas

| Dependência | Direcção | Avaliação |
|---|---|---|
| Frontend depende de backend para permissões | FE → BE | ✅ Correcto |
| Frontend depende de backend para tokens | FE → BE | ✅ Correcto |
| Backend depende de DB para RLS | BE → DB | ✅ Correcto |
| Backend depende de DB para tenant data | BE → DB | ✅ Correcto |
| Frontend NÃO faz enforcement | FE ✗ | ✅ Correcto |

### 7.2 Dependências Problemáticas

| Dependência | Direcção | Problema |
|---|---|---|
| Backend depende de appsettings para API Keys | BE → Config | ⚠️ Deveria depender de DB |
| MFA policy em DB sem enforcement em BE | DB → BE (quebrada) | ⚠️ Dados sem uso |

---

## 8. Recomendações Priorizadas por Camada

### 8.1 Cross-Layer (Prioritárias)

| # | Recomendação | Camadas Afectadas | Prioridade |
|---|---|---|---|
| 1 | Implementar MFA end-to-end | FE + BE + DB | CRÍTICA |
| 2 | Migrar API Keys para BD | BE + DB | ALTA |
| 3 | Implementar SAML | FE + BE + DB | ALTA |
| 4 | Implementar session anomaly detection | BE (+ FE alert) | ALTA |

### 8.2 Backend-Specific

| # | Recomendação | Prioridade |
|---|---|---|
| 5 | Background job para expiração JIT/BG/Delegation | MÉDIA |
| 6 | Engine de resposta a anomalias | MÉDIA |
| 7 | Completar EnvironmentAccessValidator em handlers | MÉDIA |

### 8.3 Frontend-Specific

| # | Recomendação | Prioridade |
|---|---|---|
| 8 | Verificar e completar UI de access control avançado | MÉDIA |
| 9 | Teste automatizado de alinhamento de permissões | MÉDIA |
| 10 | MFA enrollment UI (TOTP QR code) | ALTA (quando BE pronto) |

### 8.4 Database-Specific

| # | Recomendação | Prioridade |
|---|---|---|
| 11 | Criar tabela ApiKeys | ALTA |
| 12 | Política de retenção para SecurityEvents | BAIXA |

---

## 9. Matriz de Risco Residual

| Risco | Probabilidade | Impacto | Mitigação Existente | Risco Residual |
|---|---|---|---|---|
| Acesso sem MFA a operação privilegiada | ALTA | ALTO | Rate limiting + audit | MÉDIO |
| Empresa com SAML não consegue federar | MÉDIA | ALTO | OIDC disponível | MÉDIO |
| API Key comprometida sem rotação | BAIXA | ALTO | FixedTimeEquals + rate limit | MÉDIO |
| Session hijacking | BAIXA | MÉDIO | 60-min expiry + rotation | BAIXO |
| Cross-tenant via bug aplicacional | MUITO BAIXA | CRÍTICO | RLS PostgreSQL | MÍNIMO |
| XSS roubo de access token | BAIXA | MÉDIO | 60-min expiry + CSP | BAIXO |

---

## 10. Conclusão

O NexTraceOne demonstra uma integração cross-layer sólida, com alinhamento coerente entre frontend, backend e base de dados na maioria das dimensões de segurança. As lacunas identificadas são **conhecidas, documentadas e priorizáveis**:

1. **MFA** é a lacuna mais significativa — policy modelada sem enforcement em nenhuma camada
2. **SAML** é ausente em todas as camadas — impacta enterprises com ADFS
3. **API Key management** necessita migração de config para BD
4. **Automação** (cleanup, post-mortem, anomaly response) necessita background jobs

A arquitectura é **sólida o suficiente para suportar** a implementação de todas as lacunas identificadas sem redesign significativo.

---

> **Classificação final:** WELL_INTEGRATED_WITH_KNOWN_GAPS — Integração cross-layer coerente com lacunas documentadas e priorizadas, sem redesign necessário para resolução.
