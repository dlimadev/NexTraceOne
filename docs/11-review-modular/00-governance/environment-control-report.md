# Relatório de Auditoria — Controlo de Ambientes

> **Módulo:** `src/modules/identityaccess/`  
> **Data da análise:** 2025-07  
> **Classificação:** FIRST_CLASS_CONCERN_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O NexTraceOne trata ambientes como uma dimensão first-class do modelo de segurança. A entidade `Environment` inclui perfil (Development/Validation/Staging/Production/DisasterRecovery), criticidade (Low/Medium/High/Critical), flags `IsProductionLike` e `IsPrimaryProduction` (com unique partial index por tenant activo). O controlo de acesso por ambiente é modelado via `EnvironmentAccess` com níveis granulares (ReadOnly/ReadWrite/Admin/None) e grants temporais. O `EnvironmentResolutionMiddleware` valida que o ambiente pertence ao tenant activo, e o frontend injecta o header `X-Environment-Id` automaticamente.

A classificação FIRST_CLASS_CONCERN_APPARENT reflecte um modelo maduro de ambiente como dimensão de segurança, com lacunas residuais no enforcement completo ao nível dos handlers de comando.

---

## 2. Modelo de Dados — Entidade Environment

### 2.1 Campos

| Campo | Tipo | Propósito | Avaliação |
|---|---|---|---|
| Id | GUID | Identificador único | ✅ |
| TenantId | FK → Tenant | Vinculação ao tenant | ✅ |
| Name | string | Nome legível | ✅ |
| Slug | string | Identificador URL-friendly | ✅ |
| SortOrder | int | Ordenação na UI | ✅ |
| Profile | enum | Development / Validation / Staging / Production / DisasterRecovery | ✅ Abrangente |
| Criticality | enum | Low / Medium / High / Critical | ✅ |
| IsProductionLike | bool | Indica ambientes com comportamento de produção | ✅ |
| IsPrimaryProduction | bool | Produção principal (unique partial index) | ✅ |

### 2.2 Unique Partial Index

```sql
-- Unique partial index: apenas UM IsPrimaryProduction=true por tenant activo
CREATE UNIQUE INDEX IX_Environment_PrimaryProduction
ON Environments (TenantId)
WHERE IsPrimaryProduction = true AND IsActive = true;
```

**Avaliação:** ✅ Garante integridade de dados — cada tenant tem no máximo uma produção principal.

### 2.3 Perfis de Ambiente

| Perfil | Criticidade Esperada | IsProductionLike |
|---|---|---|
| Development | Low | false |
| Validation | Medium | false |
| Staging | High | true |
| Production | Critical | true |
| DisasterRecovery | Critical | true |

---

## 3. Controlo de Acesso por Ambiente

### 3.1 Entidade EnvironmentAccess

| Campo | Tipo | Propósito |
|---|---|---|
| UserId | FK → User | Utilizador |
| TenantId | FK → Tenant | Tenant |
| EnvironmentId | FK → Environment | Ambiente |
| AccessLevel | enum | ReadOnly / ReadWrite / Admin / None |
| GrantedAt | DateTime | Timestamp de concessão |
| ExpiresAt | DateTime? | Expiração opcional (grants temporais) |
| GrantedBy | FK → User | Quem concedeu |
| RevokedAt | DateTime? | Timestamp de revogação |

### 3.2 Níveis de Acesso

| Nível | Leitura | Escrita | Admin | Uso Típico |
|---|---|---|---|---|
| **None** | ❌ | ❌ | ❌ | Acesso explicitamente negado |
| **ReadOnly** | ✅ | ❌ | ❌ | Viewers, auditores |
| **ReadWrite** | ✅ | ✅ | ❌ | Developers, operadores |
| **Admin** | ✅ | ✅ | ✅ | Gestão do ambiente |

### 3.3 Grants Temporais

O campo `ExpiresAt` permite grants com prazo:
- JIT access a ambientes de produção
- Acesso temporário para debugging
- Acesso limitado para operações de manutenção

### 3.4 EnvironmentAccessValidator

| Aspecto | Estado |
|---|---|
| Interface definida | ✅ |
| Validação de pertença ao tenant | ✅ |
| Validação de nível de acesso | ✅ (interface) |
| Integração em handlers de comando | ⚠️ PARCIAL |

**Lacuna:** A interface `EnvironmentAccessValidator` existe e define o contrato, mas a integração nos handlers de comando dos vários módulos não está completa em todos os fluxos.

---

## 4. Middleware de Resolução de Ambiente

### 4.1 EnvironmentResolutionMiddleware

| Aspecto | Implementação |
|---|---|
| Posição no pipeline | Após TenantResolutionMiddleware |
| Fontes de resolução | Header `X-Environment-Id`, query string |
| Validação | Ambiente pertence ao tenant activo |
| Rejeição | Se ambiente não pertence ao tenant |

### 4.2 Pipeline Completo

```
UseAuthentication
  → TenantResolutionMiddleware    ← Resolve tenant
    → EnvironmentResolutionMiddleware  ← Resolve ambiente (valida tenant)
      → UseAuthorization
```

**Avaliação:** ✅ Ordem correcta — ambiente resolvido após tenant, com validação de pertença.

---

## 5. Integração com Frontend

### 5.1 EnvironmentProvider

| Aspecto | Implementação |
|---|---|
| Carregamento | GET `/identity/environments` |
| Auto-selecção | Selecciona ambiente default |
| Persistência | sessionStorage |
| Contexto React | `EnvironmentProvider` → `useEnvironment` |

### 5.2 Injecção no API Client (Axios)

O interceptor Axios injecta automaticamente:
- `X-Environment-Id` em todos os pedidos
- Valor obtido do `EnvironmentProvider`

### 5.3 Experiência do Utilizador

| Cenário | Comportamento |
|---|---|
| Primeiro acesso | Auto-selecção do ambiente default |
| Mudança de ambiente | Selector na UI, actualiza contexto |
| Refresh da página | Recupera de sessionStorage |
| Mudança de tenant | Recarrega ambientes do novo tenant |

**Avaliação:** ✅ Experiência fluida com persistência adequada.

---

## 6. Dados de Seed e Verificação

### 6.1 Ambientes de Seed

| Tenant | Ambiente | Profile | Criticality | IsPrimaryProduction |
|---|---|---|---|---|
| NexTrace Corp | Development | Development | Low | false |
| NexTrace Corp | Staging | Staging | High | false |
| NexTrace Corp | Production | Production | Critical | true |
| Acme Fintech | Development | Development | Low | false |
| Acme Fintech | Production | Production | Critical | true |

### 6.2 EnvironmentAccess de Seed

8 registos de acesso demonstrando:
- Diferentes níveis (ReadOnly, ReadWrite, Admin)
- Diferentes utilizadores
- Acesso a diferentes ambientes

---

## 7. Análise de Segurança

### 7.1 Pontos Fortes

| Aspecto | Detalhe |
|---|---|
| Ambiente como dimensão de segurança | ✅ First-class entity |
| Controlo de acesso granular | ✅ 4 níveis (None/ReadOnly/ReadWrite/Admin) |
| Grants temporais | ✅ ExpiresAt |
| Validação tenant-ambiente | ✅ Middleware |
| Unique partial index | ✅ Integridade IsPrimaryProduction |
| Frontend auto-injecção | ✅ X-Environment-Id |

### 7.2 Lacunas Identificadas

| Lacuna | Severidade | Detalhe |
|---|---|---|
| Enforcement parcial nos handlers | MÉDIA | EnvironmentAccessValidator existe, integração incompleta |
| Limpeza de grants expirados | BAIXA | Sem background job para limpar ExpiresAt vencidos |
| Auditoria de mudança de ambiente | BAIXA | Sem evento de segurança para "user mudou para ambiente X" |
| Protecção contra deploy em produção | MÉDIA | Ambiente classificado mas sem gate de deploy |

---

## 8. Interacção com Outros Subsistemas

### 8.1 Break Glass

| Aspecto | Relação |
|---|---|
| Break Glass em produção | Activação imediata, 2h window |
| Ambiente como scope | Break Glass pode ser scoped por ambiente |

### 8.2 JIT Access

| Aspecto | Relação |
|---|---|
| JIT para produção | Aprovação obrigatória, 8h window |
| Ambiente como scope | JIT pode ser scoped por ambiente |

### 8.3 Change Intelligence

| Aspecto | Relação |
|---|---|
| Mudanças em produção | Devem considerar criticidade do ambiente |
| Blast radius | Ambiente influencia cálculo de impacto |

---

## 9. Recomendações

### Prioridade ALTA

1. **Completar integração de EnvironmentAccessValidator** em todos os handlers de comando que operam sobre recursos tenant-scoped + environment-scoped
2. **Adicionar validação de AccessLevel** nos endpoints de escrita — verificar que user tem ReadWrite ou Admin para o ambiente alvo

### Prioridade MÉDIA

3. **Background job para limpeza de grants expirados** — marcar EnvironmentAccess expirados como revogados
4. **Evento de segurança** para mudança de ambiente activo (tracking de contexto)
5. **Gate de deploy** que utiliza criticidade do ambiente para exigir aprovação adicional

### Prioridade BAIXA

6. **Dashboard de acesso por ambiente** — visualização de quem tem acesso a quê
7. **Relatório de ambiente** — acesso, operações, incidentes por ambiente
8. **Alertas** quando grants temporários estão prestes a expirar

---

## 10. Conformidade

| Requisito | Estado | Evidência |
|---|---|---|
| Ambiente como entidade first-class | ✅ | Entity com Profile/Criticality |
| Controlo de acesso por ambiente | ✅ | EnvironmentAccess |
| Grants temporais | ✅ | ExpiresAt |
| Validação middleware | ✅ | EnvironmentResolutionMiddleware |
| Integridade de produção principal | ✅ | Unique partial index |
| Frontend context injection | ✅ | X-Environment-Id |
| Enforcement completo em handlers | ⚠️ | Parcialmente implementado |
| Auditoria de acesso por ambiente | ⚠️ | SecurityEvent genérico, sem evento específico |

---

> **Classificação final:** FIRST_CLASS_CONCERN_APPARENT — Ambiente como dimensão de segurança first-class com modelo completo, middleware de validação e controlo de acesso granular. Enforcement nos handlers parcialmente implementado.
